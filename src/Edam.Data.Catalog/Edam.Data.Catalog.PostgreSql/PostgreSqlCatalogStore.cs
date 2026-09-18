using Edam.Data.Catalog.Contracts;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Edam.Data.Catalog.PostgreSql;

/// <summary>
/// PostgreSQL (Npgsql) relational provider for the catalog <see cref="ICatalogStore"/>
/// metadata seam (BL-7.2). Provider-agnostic: implements only the contract, no EF, no target
/// types leak (ADR-0006/0007). Schema is created idempotently on first use. Resource addressing
/// is path/URI-based via <see cref="ItemInfo.FullPath"/>.
/// </summary>
public sealed class PostgreSqlCatalogStore : ICatalogStore
{
    private static readonly string SchemaSql = """
        CREATE TABLE IF NOT EXISTS edam_container (
          id uuid PRIMARY KEY,
          container_id text NOT NULL,
          description text NOT NULL,
          container_type integer NOT NULL,
          container_uri text NOT NULL DEFAULT '',
          content_type text NOT NULL DEFAULT 'application/json'
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ux_edam_container_container_id ON edam_container(container_id);
        CREATE TABLE IF NOT EXISTS edam_item (
          id uuid PRIMARY KEY,
          container_id uuid NOT NULL REFERENCES edam_container(id),
          full_path text NOT NULL,
          name text NOT NULL,
          description text NULL,
          item_type integer NOT NULL,
          created_date timestamptz NOT NULL,
          updated_date timestamptz NOT NULL
        );
        CREATE INDEX IF NOT EXISTS ix_edam_item_container ON edam_item(container_id);
        CREATE INDEX IF NOT EXISTS ix_edam_item_path ON edam_item(full_path);
        CREATE TABLE IF NOT EXISTS edam_item_data (
          id uuid PRIMARY KEY,
          item_id uuid NOT NULL REFERENCES edam_item(id),
          partition_id text NOT NULL DEFAULT 'default',
          name text NOT NULL,
          content_type_id text NULL,
          value text NULL
        );
        CREATE TABLE IF NOT EXISTS edam_content_type (
          type_id text PRIMARY KEY,
          description text NULL
        );
        CREATE TABLE IF NOT EXISTS edam_content (
          resource_path text PRIMARY KEY,
          body bytea NULL
        );
        """;

    private readonly string _connection;
    private int _initialized;

    public PostgreSqlCatalogStore(string connectionString) => _connection = connectionString;

    public PostgreSqlCatalogStore(IConfiguration config)
        : this(config["ConnectionStrings:catalog"] ?? config["Edam:Catalog:ConnectionString"]
               ?? throw new InvalidOperationException("Catalog PostgreSQL connection string is not configured."))
    { }

    public string DescribeStore() => "postgres";

    public async Task EnsureSchemaAsync(CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1) return;
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = SchemaSql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = new NpgsqlConnection(_connection);
        await conn.OpenAsync(ct);
        return conn;
    }

    // ---- containers --------------------------------------------------------

    public async Task<ContainerInfo?> GetContainerAsync(string? containerId, bool checkId = true, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, container_id, description, container_type, container_uri, content_type FROM edam_container WHERE container_id = @cid";
        cmd.Parameters.AddWithValue("cid", containerId ?? string.Empty);
        using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadContainer(r) : null;
    }

    public ContainerInfo? GetContainer(string? containerId, bool checkId = true) => GetContainerAsync(containerId, checkId).GetAwaiter().GetResult();

    public async Task<ContainerInfo?> GetContainerAsync(Guid containerId, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, container_id, description, container_type, container_uri, content_type FROM edam_container WHERE id = @id";
        cmd.Parameters.AddWithValue("id", containerId);
        using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadContainer(r) : null;
    }

    public ContainerInfo? GetContainer(Guid containerId) => GetContainerAsync(containerId).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ContainerInfo>> GetContainersAsync(CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, container_id, description, container_type, container_uri, content_type FROM edam_container ORDER BY description";
        using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<ContainerInfo>();
        while (await r.ReadAsync(ct)) list.Add(ReadContainer(r));
        return list;
    }

    public IReadOnlyList<ContainerInfo> GetContainers() => GetContainersAsync().GetAwaiter().GetResult();

    public async Task<ContainerInfo> EnlistContainerAsync(string containerId, string description, string? baseUri = null, ContainerType type = ContainerType.DataContext, CancellationToken ct = default)
    {
        await EnsureSchemaAsync(ct);
        var id = Guid.NewGuid();
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        // Upsert by container_id; RETURNING yields the ACTUAL row so the returned Id always matches
        // the persisted row (works both on first insert and on idempotent re-enlist of an existing
        // container — previously the method returned a new random id that diverged from the row,
        // so GetContainer(returned.Id) came back null once the container already existed).
        cmd.CommandText = """
            INSERT INTO edam_container (id, container_id, description, container_type, container_uri, content_type)
            VALUES (@id, @cid, @desc, @cty, @uri, @ctype)
            ON CONFLICT (container_id) DO UPDATE SET description = @desc, container_type = @cty, container_uri = @uri
            RETURNING id, container_id, description, container_type, container_uri, content_type;
            """;
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("cid", containerId);
        cmd.Parameters.AddWithValue("desc", description);
        cmd.Parameters.AddWithValue("cty", (int)type);
        cmd.Parameters.AddWithValue("uri", baseUri ?? string.Empty);
        cmd.Parameters.AddWithValue("ctype", "application/json");
        using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadContainer(r)
               : throw new InvalidOperationException($"Enlist container '{containerId}' failed.");
    }

    public ContainerInfo EnlistContainer(string containerId, string description, string? baseUri = null, ContainerType type = ContainerType.DataContext)
        => EnlistContainerAsync(containerId, description, baseUri, type).GetAwaiter().GetResult();

    public async Task<ContainerInfo> SetContainerAsync(string sessionId, string containerId, CancellationToken ct = default)
        => await GetContainerAsync(containerId, ct: ct) ?? await EnlistContainerAsync(containerId, "Default", null, ContainerType.DataContext, ct);

    public ContainerInfo SetContainer(string sessionId, string containerId) => SetContainerAsync(sessionId, containerId).GetAwaiter().GetResult();

    public async Task<ContainerInfo> DelistContainerAsync(string containerId, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM edam_container WHERE container_id = @cid RETURNING id, container_id, description, container_type, container_uri, content_type";
        cmd.Parameters.AddWithValue("cid", containerId);
        using var r = await cmd.ExecuteReaderAsync(ct);
        return (await r.ReadAsync(ct)) ? ReadContainer(r) : throw new InvalidOperationException($"Container '{containerId}' not found.");
    }

    public ContainerInfo DelistContainer(string containerId) => DelistContainerAsync(containerId).GetAwaiter().GetResult();

    // ---- items -------------------------------------------------------------

    private static readonly string ItemSelect = "SELECT id, container_id, full_path, name, description, item_type, created_date, updated_date FROM edam_item";

    private static ItemInfo ReadItem(NpgsqlDataReader r) => new(
        r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetString(3),
        r.IsDBNull(4) ? null : r.GetString(4),
        (ItemType)r.GetInt32(5),
        r.GetFieldValue<DateTimeOffset>(6), r.GetFieldValue<DateTimeOffset>(7));

    public async Task<ItemInfo?> GetItemAsync(Guid itemId, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = ItemSelect + " WHERE id = @id";
        cmd.Parameters.AddWithValue("id", itemId);
        using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadItem(r) : null;
    }

    public ItemInfo? GetItem(Guid itemId) => GetItemAsync(itemId).GetAwaiter().GetResult();

    public async Task<ItemInfo?> GetItemByPathAsync(string path, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = ItemSelect + " WHERE full_path = @p LIMIT 1";
        cmd.Parameters.AddWithValue("p", path);
        using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadItem(r) : null;
    }

    public ItemInfo? GetItemByPath(string name) => GetItemByPathAsync(name).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ItemInfo>> GetContainerItemsAsync(Guid containerId, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = ItemSelect + " WHERE container_id = @id ORDER BY full_path";
        cmd.Parameters.AddWithValue("id", containerId);
        using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<ItemInfo>();
        while (await r.ReadAsync(ct)) list.Add(ReadItem(r));
        return list;
    }

    public IReadOnlyList<ItemInfo> GetContainerItems(Guid containerId) => GetContainerItemsAsync(containerId).GetAwaiter().GetResult();

    public async Task<ItemInfo?> GetContainerRootItemAsync(Guid id, CancellationToken ct = default)
    {
        var items = await GetContainerItemsAsync(id, ct);
        return items.FirstOrDefault(i => i.Type == ItemType.Unknown || i.FullPath == "/") ?? items.FirstOrDefault();
    }

    public ItemInfo? GetContainerRootItem(Guid containerId) => GetContainerRootItemAsync(containerId).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ItemInfo>> GetBranchAsync(string? path = null, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = ItemSelect + " ORDER BY full_path";
        using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<ItemInfo>();
        while (await r.ReadAsync(ct)) list.Add(ReadItem(r));
        if (string.IsNullOrWhiteSpace(path)) return list;
        var prefix = path.EndsWith("/") ? path : path + "/";
        return list.Where(i => i.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public IReadOnlyList<ItemInfo> GetBranch(string? path = null) => GetBranchAsync(path).GetAwaiter().GetResult();

    public async Task<ItemInfo> AddItemAsync(ItemInfo item, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO edam_item (id, container_id, full_path, name, description, item_type, created_date, updated_date)
            VALUES (@id, @cid, @p, @name, @desc, @ty, @cd, @ud)
            ON CONFLICT (id) DO UPDATE SET full_path = @p, name = @name, description = @desc, item_type = @ty, updated_date = @ud;
            """;
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.Parameters.AddWithValue("cid", item.ContainerId);
        cmd.Parameters.AddWithValue("p", item.FullPath);
        cmd.Parameters.AddWithValue("name", item.Name);
        cmd.Parameters.AddWithValue("desc", (object?)item.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("ty", (int)item.Type);
        cmd.Parameters.AddWithValue("cd", item.CreatedDate.ToUniversalTime());
        cmd.Parameters.AddWithValue("ud", item.UpdatedDate.ToUniversalTime());
        await cmd.ExecuteNonQueryAsync(ct);
        return item;
    }

    public ItemInfo AddItem(ItemInfo item) => AddItemAsync(item).GetAwaiter().GetResult();

    public async Task<ItemInfo> CreateBranchAsync(string path, string? description = null, Guid? containerId = null, CancellationToken ct = default)
    {
        var cid = containerId ?? Guid.Empty;
        var existing = await GetItemByPathAsync(path, ct);
        if (existing is not null) return existing;
        var name = System.IO.Path.GetFileName(path.TrimEnd('/'));
        var item = new ItemInfo(Guid.NewGuid(), cid, path, string.IsNullOrEmpty(name) ? path : name, description, ItemType.Branch, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        return await AddItemAsync(item, ct);
    }

    public ItemInfo CreateBranch(string path, string? description = null, Guid? containerId = null) => CreateBranchAsync(path, description, containerId).GetAwaiter().GetResult();

    public ItemInfo? CreateRootItem(Guid? containerId = null) => CreateBranchAsync("/", "root", containerId).GetAwaiter().GetResult();

    public async Task<bool> DeleteItemAsync(Guid itemId, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM edam_item_data WHERE item_id = @id; DELETE FROM edam_item WHERE id = @id;";
        cmd.Parameters.AddWithValue("id", itemId);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public bool DeleteItem(Guid itemId) => DeleteItemAsync(itemId).GetAwaiter().GetResult();

    // ---- item data ---------------------------------------------------------

    private static readonly string DataSelect = "SELECT id, item_id, name, content_type_id, partition_id, value FROM edam_item_data";

    private static ItemDataInfo ReadData(NpgsqlDataReader r) => new(
        r.GetGuid(0), r.GetGuid(1), r.GetString(2),
        r.IsDBNull(3) ? string.Empty : r.GetString(3),
        r.IsDBNull(4) ? "default" : r.GetString(4),
        r.IsDBNull(5) ? null : r.GetString(5));

    public async Task<IReadOnlyList<ItemDataInfo>> GetItemDataAsync(Guid itemId, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = DataSelect + " WHERE item_id = @id ORDER BY name";
        cmd.Parameters.AddWithValue("id", itemId);
        using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<ItemDataInfo>();
        while (await r.ReadAsync(ct)) list.Add(ReadData(r));
        return list;
    }

    public IReadOnlyList<ItemDataInfo> GetItemData(Guid itemId) => GetItemDataAsync(itemId).GetAwaiter().GetResult();

    public async Task<ItemDataInfo> AddItemAsync(ItemDataInfo item, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO edam_item_data (id, item_id, name, content_type_id, partition_id, value)
            VALUES (@id, @iid, @name, @cty, @part, @val)
            ON CONFLICT (id) DO UPDATE SET name = @name, content_type_id = @cty, partition_id = @part, value = @val;
            """;
        cmd.Parameters.AddWithValue("id", item.Id);
        cmd.Parameters.AddWithValue("iid", item.ItemId);
        cmd.Parameters.AddWithValue("name", item.Name);
        cmd.Parameters.AddWithValue("cty", (object?)item.ContentTypeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("part", item.PartitionId);
        cmd.Parameters.AddWithValue("val", (object?)item.Value ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
        return item;
    }

    public ItemDataInfo AddItem(ItemDataInfo item) => AddItemAsync(item).GetAwaiter().GetResult();

    public async Task<ItemDataInfo?> GetDataAsync(Guid dataId, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = DataSelect + " WHERE id = @id";
        cmd.Parameters.AddWithValue("id", dataId);
        using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadData(r) : null;
    }

    public ItemDataInfo? GetData(Guid dataId) => GetDataAsync(dataId).GetAwaiter().GetResult();

    public async Task<ItemDataInfo?> GetDataByNameAsync(Guid itemId, string name, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = DataSelect + " WHERE item_id = @iid AND name = @name LIMIT 1";
        cmd.Parameters.AddWithValue("iid", itemId);
        cmd.Parameters.AddWithValue("name", name);
        using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadData(r) : null;
    }

    public ItemDataInfo? GetDataByName(Guid itemId, string name) => GetDataByNameAsync(itemId, name).GetAwaiter().GetResult();

    public async Task<ContentTypeInfo?> GetContentTypeAsync(string contentTypeId, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT type_id, description FROM edam_content_type WHERE type_id = @tid";
        cmd.Parameters.AddWithValue("tid", contentTypeId);
        using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new ContentTypeInfo(r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1)) : new ContentTypeInfo(contentTypeId);
    }

    public ContentTypeInfo? GetContentType(string contentTypeId) => GetContentTypeAsync(contentTypeId).GetAwaiter().GetResult();

    public ItemDataInfo? CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, byte[]? dataValue = null)
        => CreateDataLeaf(item, name, dataId, dataValue is null ? null : Convert.ToBase64String(dataValue));

    public ItemDataInfo? CreateDataLeaf(ItemInfo item, string name, Guid? dataId = null, string? dataValue = null)
    {
        var leaf = new ItemDataInfo(dataId ?? Guid.NewGuid(), item.Id, name, string.Empty, "default", dataValue);
        return AddItem(leaf);
    }

    public async Task<bool> DeleteDataAsync(Guid dataId, CancellationToken ct = default)
    {
        using var conn = await OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM edam_item_data WHERE id = @id";
        cmd.Parameters.AddWithValue("id", dataId);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public bool DeleteData(Guid dataId) => DeleteDataAsync(dataId).GetAwaiter().GetResult();

    public Task<bool> DeleteItemDataAsync(Guid itemId, CancellationToken ct = default)
    {
        using var conn = OpenAsync(ct).GetAwaiter().GetResult();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM edam_item_data WHERE item_id = @id";
        cmd.Parameters.AddWithValue("id", itemId);
        return Task.FromResult(cmd.ExecuteNonQuery() > 0);
    }

    public bool DeleteItemData(Guid itemId) => DeleteItemDataAsync(itemId).GetAwaiter().GetResult();

        private static ContainerInfo ReadContainer(NpgsqlDataReader r) => new(
            r.GetGuid(0), r.GetString(1), r.GetString(2),
            (ContainerType)r.GetInt32(3),
            r.IsDBNull(4) ? string.Empty : r.GetString(4),
            r.IsDBNull(5) ? "application/json" : r.GetString(5));
    }