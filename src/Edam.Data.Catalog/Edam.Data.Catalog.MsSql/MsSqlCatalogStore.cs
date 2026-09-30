using Edam.Data.Catalog.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Catalog.MsSql;

/// <summary>
/// MS-SQL catalog store (LM-8) — the T-SQL peer of <c>PostgreSqlCatalogStore</c>, implementing the same
/// composite contract <see cref="ICatalogStore"/> (<c>ICatalogContainer</c> + <c>ICatalogItem</c> +
/// <c>ICatalogItemData</c>) with the same semantics, so a container is the instance's scope and items are
/// path/URI-based via <see cref="ItemInfo.FullPath"/>.
/// <para>
/// The port is deliberately <b>faithful rather than inventive</b>: every member keeps the peer's shape —
/// an async method plus its blocking wrapper — and only the dialect changes. The three idiom translations
/// are called out where they occur: <c>LIMIT n</c> becomes <c>SELECT TOP n</c> (which must sit immediately
/// after <c>SELECT</c>, hence the extra constants), <c>ON CONFLICT … DO UPDATE</c> becomes
/// <c>MERGE</c>, and <c>RETURNING</c> becomes <c>OUTPUT INSERTED.*</c> (insert/upsert) or
/// <c>OUTPUT DELETED.*</c> (delete).
/// </para>
/// </summary>
public sealed class MsSqlCatalogStore : ICatalogStore
{
   private readonly string _connection;
   private int _initialized;

   public MsSqlCatalogStore(string connectionString) => _connection = connectionString;

   public MsSqlCatalogStore(IConfiguration config)
      : this(config["ConnectionStrings:catalog"] ?? config["Edam:Catalog:ConnectionString"]
             ?? throw new InvalidOperationException(
                "Catalog MS-SQL connection string is not configured."))
   { }

   public string DescribeStore() => "mssql";

   public async Task EnsureSchemaAsync(CancellationToken ct = default)
   {
      if (Interlocked.Exchange(ref _initialized, 1) == 1)
      {
         return;
      }

      await MsSqlSchema.EnsureAsync(_connection, ct).ConfigureAwait(false);
   }

   private async Task<SqlConnection> OpenAsync(CancellationToken ct = default)
   {
      var connection = new SqlConnection(_connection);
      await connection.OpenAsync(ct).ConfigureAwait(false);
      return connection;
   }

   // ---- containers --------------------------------------------------------

   public async Task<ContainerInfo?> GetContainerAsync(
      string? containerId, bool checkId = true, CancellationToken ct = default)
   {
      await EnsureSchemaAsync(ct).ConfigureAwait(false);
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         "SELECT id, container_id, description, container_type, container_uri, content_type " +
         "FROM dbo.edam_container WHERE container_id = @cid", conn);
      cmd.Parameters.AddWithValue("cid", containerId ?? string.Empty);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadContainer(r) : null;
   }

   public ContainerInfo? GetContainer(string? containerId, bool checkId = true)
      => GetContainerAsync(containerId, checkId).GetAwaiter().GetResult();

   public async Task<ContainerInfo?> GetContainerAsync(
      Guid containerId, CancellationToken ct = default)
   {
      await EnsureSchemaAsync(ct).ConfigureAwait(false);
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         "SELECT id, container_id, description, container_type, container_uri, content_type " +
         "FROM dbo.edam_container WHERE id = @id", conn);
      cmd.Parameters.AddWithValue("id", containerId);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadContainer(r) : null;
   }

   public ContainerInfo? GetContainer(Guid containerId)
      => GetContainerAsync(containerId).GetAwaiter().GetResult();

   public async Task<IReadOnlyList<ContainerInfo>> GetContainersAsync(
      CancellationToken ct = default)
   {
      await EnsureSchemaAsync(ct).ConfigureAwait(false);
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         "SELECT id, container_id, description, container_type, container_uri, content_type " +
         "FROM dbo.edam_container ORDER BY description", conn);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

      var list = new List<ContainerInfo>();
      while (await r.ReadAsync(ct).ConfigureAwait(false))
      {
         list.Add(ReadContainer(r));
      }

      return list;
   }

   public IReadOnlyList<ContainerInfo> GetContainers()
      => GetContainersAsync().GetAwaiter().GetResult();

   public async Task<ContainerInfo> EnlistContainerAsync(
      string containerId, string description, string? baseUri = null,
      ContainerType type = ContainerType.DataContext, CancellationToken ct = default)
   {
      await EnsureSchemaAsync(ct).ConfigureAwait(false);

      var id = Guid.NewGuid();
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);

      // Upsert by container_id; OUTPUT yields the ACTUAL row so the returned Id always matches the
      // persisted row — on a first insert AND on an idempotent re-enlist (MERGE updates the matched row,
      // so the existing id survives and GetContainer(returned.Id) keeps resolving).
      await using var cmd = new SqlCommand("""
         MERGE dbo.edam_container AS target
         USING (SELECT @cid AS container_id) AS source
            ON target.container_id = source.container_id
         WHEN MATCHED THEN
            UPDATE SET description = @desc, container_type = @cty, container_uri = @uri
         WHEN NOT MATCHED THEN
            INSERT (id, container_id, description, container_type, container_uri, content_type)
            VALUES (@id, @cid, @desc, @cty, @uri, @ctype)
         OUTPUT INSERTED.id, INSERTED.container_id, INSERTED.description, INSERTED.container_type,
                INSERTED.container_uri, INSERTED.content_type;
         """, conn);

      cmd.Parameters.AddWithValue("id", id);
      cmd.Parameters.AddWithValue("cid", containerId);
      cmd.Parameters.AddWithValue("desc", description);
      cmd.Parameters.AddWithValue("cty", (int)type);
      cmd.Parameters.AddWithValue("uri", baseUri ?? string.Empty);
      cmd.Parameters.AddWithValue("ctype", "application/json");

      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      return await r.ReadAsync(ct).ConfigureAwait(false)
         ? ReadContainer(r)
         : throw new InvalidOperationException($"Enlist container '{containerId}' failed.");
   }

   public ContainerInfo EnlistContainer(
      string containerId, string description, string? baseUri = null,
      ContainerType type = ContainerType.DataContext)
      => EnlistContainerAsync(containerId, description, baseUri, type).GetAwaiter().GetResult();

   public async Task<ContainerInfo> SetContainerAsync(
      string sessionId, string containerId, CancellationToken ct = default)
      => await GetContainerAsync(containerId, ct: ct).ConfigureAwait(false)
         ?? await EnlistContainerAsync(
               containerId, "Default", null, ContainerType.DataContext, ct).ConfigureAwait(false);

   public ContainerInfo SetContainer(string sessionId, string containerId)
      => SetContainerAsync(sessionId, containerId).GetAwaiter().GetResult();

   public async Task<ContainerInfo> DelistContainerAsync(
      string containerId, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);

      // RETURNING becomes OUTPUT DELETED.* — the row being removed is what a delete yields
      await using var cmd = new SqlCommand(
         "DELETE FROM dbo.edam_container WHERE container_id = @cid " +
         "OUTPUT DELETED.id, DELETED.container_id, DELETED.description, DELETED.container_type, " +
         "DELETED.container_uri, DELETED.content_type", conn);
      cmd.Parameters.AddWithValue("cid", containerId);

      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      return await r.ReadAsync(ct).ConfigureAwait(false)
         ? ReadContainer(r)
         : throw new InvalidOperationException($"Container '{containerId}' not found.");
   }

   public ContainerInfo DelistContainer(string containerId)
      => DelistContainerAsync(containerId).GetAwaiter().GetResult();

   // ---- items -------------------------------------------------------------

   private const string ItemColumns =
      "id, container_id, full_path, name, description, item_type, created_date, updated_date";

   private static readonly string ItemSelect = "SELECT " + ItemColumns + " FROM dbo.edam_item";

   /// <summary><c>LIMIT 1</c> becomes <c>TOP 1</c>, which must follow <c>SELECT</c> immediately.</summary>
   private static readonly string ItemSelectTop =
      "SELECT TOP 1 " + ItemColumns + " FROM dbo.edam_item";

   private static ItemInfo ReadItem(SqlDataReader r) => new(
      r.GetGuid(0), r.GetGuid(1), r.GetString(2), r.GetString(3),
      r.IsDBNull(4) ? null : r.GetString(4),
      (ItemType)r.GetInt32(5),
      r.GetFieldValue<DateTimeOffset>(6), r.GetFieldValue<DateTimeOffset>(7));

   public async Task<ItemInfo?> GetItemAsync(Guid itemId, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(ItemSelect + " WHERE id = @id", conn);
      cmd.Parameters.AddWithValue("id", itemId);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadItem(r) : null;
   }

   public ItemInfo? GetItem(Guid itemId) => GetItemAsync(itemId).GetAwaiter().GetResult();

   public async Task<ItemInfo?> GetItemByPathAsync(string path, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(ItemSelectTop + " WHERE full_path = @p", conn);
      cmd.Parameters.AddWithValue("p", path);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadItem(r) : null;
   }

   public ItemInfo? GetItemByPath(string name) => GetItemByPathAsync(name).GetAwaiter().GetResult();

   /// <summary>
   /// LM-2a: the <b>container-scoped</b> lookup — the correct way to ask "does <i>this</i> container have
   /// that path?", so one container is never satisfied by another container's item (the PE-3
   /// cross-container defect).
   /// </summary>
   private async Task<ItemInfo?> FindItemAsync(
      Guid containerId, string path, CancellationToken ct)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         ItemSelectTop + " WHERE container_id = @c AND full_path = @p", conn);
      cmd.Parameters.AddWithValue("c", containerId);
      cmd.Parameters.AddWithValue("p", path);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadItem(r) : null;
   }

   public async Task<IReadOnlyList<ItemInfo>> GetContainerItemsAsync(
      Guid containerId, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         ItemSelect + " WHERE container_id = @id ORDER BY full_path", conn);
      cmd.Parameters.AddWithValue("id", containerId);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

      var list = new List<ItemInfo>();
      while (await r.ReadAsync(ct).ConfigureAwait(false))
      {
         list.Add(ReadItem(r));
      }

      return list;
   }

   public IReadOnlyList<ItemInfo> GetContainerItems(Guid containerId)
      => GetContainerItemsAsync(containerId).GetAwaiter().GetResult();

   public async Task<ItemInfo?> GetContainerRootItemAsync(
      Guid id, CancellationToken ct = default)
   {
      var items = await GetContainerItemsAsync(id, ct).ConfigureAwait(false);
      return items.FirstOrDefault(i => i.Type == ItemType.Unknown || i.FullPath == "/")
         ?? items.FirstOrDefault();
   }

   public ItemInfo? GetContainerRootItem(Guid containerId)
      => GetContainerRootItemAsync(containerId).GetAwaiter().GetResult();

   public async Task<IReadOnlyList<ItemInfo>> GetBranchAsync(
      string? path = null, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(ItemSelect + " ORDER BY full_path", conn);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

      var list = new List<ItemInfo>();
      while (await r.ReadAsync(ct).ConfigureAwait(false))
      {
         list.Add(ReadItem(r));
      }

      if (string.IsNullOrWhiteSpace(path))
      {
         return list;
      }

      var prefix = path.EndsWith("/") ? path : path + "/";
      return list
         .Where(i => i.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
         .ToList();
   }

   public IReadOnlyList<ItemInfo> GetBranch(string? path = null)
      => GetBranchAsync(path).GetAwaiter().GetResult();

   public async Task<ItemInfo> AddItemAsync(ItemInfo item, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);

      // upsert by id (ON CONFLICT (id) DO UPDATE → MERGE)
      await using var cmd = new SqlCommand("""
         MERGE dbo.edam_item AS target
         USING (SELECT @id AS id) AS source
            ON target.id = source.id
         WHEN MATCHED THEN
            UPDATE SET full_path = @p, name = @name, description = @desc,
                       item_type = @ty, updated_date = @ud
         WHEN NOT MATCHED THEN
            INSERT (id, container_id, full_path, name, description, item_type,
                    created_date, updated_date)
            VALUES (@id, @cid, @p, @name, @desc, @ty, @cd, @ud);
         """, conn);

      cmd.Parameters.AddWithValue("id", item.Id);
      cmd.Parameters.AddWithValue("cid", item.ContainerId);
      cmd.Parameters.AddWithValue("p", item.FullPath);
      cmd.Parameters.AddWithValue("name", item.Name);
      cmd.Parameters.AddWithValue("desc", (object?)item.Description ?? DBNull.Value);
      cmd.Parameters.AddWithValue("ty", (int)item.Type);
      cmd.Parameters.AddWithValue("cd", item.CreatedDate.ToUniversalTime());
      cmd.Parameters.AddWithValue("ud", item.UpdatedDate.ToUniversalTime());

      await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
      return item;
   }

   public ItemInfo AddItem(ItemInfo item) => AddItemAsync(item).GetAwaiter().GetResult();

   public async Task<ItemInfo> CreateBranchAsync(
      string path, string? description = null, Guid? containerId = null,
      CancellationToken ct = default)
   {
      var cid = containerId ?? Guid.Empty;

      // LM-2a: match INSIDE the container — a path that exists in another container must not satisfy
      // this one (the PE-3 cross-container defect).
      var existing = containerId is null
         ? await GetItemByPathAsync(path, ct).ConfigureAwait(false)
         : await FindItemAsync(cid, path, ct).ConfigureAwait(false);
      if (existing is not null)
      {
         return existing;
      }

      var name = System.IO.Path.GetFileName(path.TrimEnd('/'));
      var item = new ItemInfo(Guid.NewGuid(), cid, path,
         string.IsNullOrEmpty(name) ? path : name, description, ItemType.Branch,
         DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

      return await AddItemAsync(item, ct).ConfigureAwait(false);
   }

   public ItemInfo CreateBranch(string path, string? description = null, Guid? containerId = null)
      => CreateBranchAsync(path, description, containerId).GetAwaiter().GetResult();

   public ItemInfo? CreateRootItem(Guid? containerId = null)
      => CreateBranchAsync("/", "root", containerId).GetAwaiter().GetResult();

   public async Task<bool> DeleteItemAsync(Guid itemId, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);

      // children first (there is no cascade on the references), then the item itself
      await using var cmd = new SqlCommand(
         "DELETE FROM dbo.edam_item_data WHERE item_id = @id; " +
         "DELETE FROM dbo.edam_item WHERE id = @id;", conn);
      cmd.Parameters.AddWithValue("id", itemId);
      return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) > 0;
   }

   public bool DeleteItem(Guid itemId) => DeleteItemAsync(itemId).GetAwaiter().GetResult();

   // ---- item data ---------------------------------------------------------

   private const string DataColumns = "id, item_id, name, content_type_id, partition_id, value";

   private static readonly string DataSelect =
      "SELECT " + DataColumns + " FROM dbo.edam_item_data";

   private static readonly string DataSelectTop =
      "SELECT TOP 1 " + DataColumns + " FROM dbo.edam_item_data";

   private static ItemDataInfo ReadData(SqlDataReader r) => new(
      r.GetGuid(0), r.GetGuid(1), r.GetString(2),
      r.IsDBNull(3) ? string.Empty : r.GetString(3),
      r.IsDBNull(4) ? "default" : r.GetString(4),
      r.IsDBNull(5) ? null : r.GetString(5));

   public async Task<IReadOnlyList<ItemDataInfo>> GetItemDataAsync(
      Guid itemId, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         DataSelect + " WHERE item_id = @id ORDER BY name", conn);
      cmd.Parameters.AddWithValue("id", itemId);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

      var list = new List<ItemDataInfo>();
      while (await r.ReadAsync(ct).ConfigureAwait(false))
      {
         list.Add(ReadData(r));
      }

      return list;
   }

   public IReadOnlyList<ItemDataInfo> GetItemData(Guid itemId)
      => GetItemDataAsync(itemId).GetAwaiter().GetResult();

   public async Task<ItemDataInfo> AddItemAsync(
      ItemDataInfo item, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);

      await using var cmd = new SqlCommand("""
         MERGE dbo.edam_item_data AS target
         USING (SELECT @id AS id) AS source
            ON target.id = source.id
         WHEN MATCHED THEN
            UPDATE SET name = @name, content_type_id = @cty, partition_id = @part, value = @val
         WHEN NOT MATCHED THEN
            INSERT (id, item_id, name, content_type_id, partition_id, value)
            VALUES (@id, @iid, @name, @cty, @part, @val);
         """, conn);

      cmd.Parameters.AddWithValue("id", item.Id);
      cmd.Parameters.AddWithValue("iid", item.ItemId);
      cmd.Parameters.AddWithValue("name", item.Name);
      cmd.Parameters.AddWithValue("cty", (object?)item.ContentTypeId ?? DBNull.Value);
      cmd.Parameters.AddWithValue("part", item.PartitionId);
      cmd.Parameters.AddWithValue("val", (object?)item.Value ?? DBNull.Value);

      await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
      return item;
   }

   public ItemDataInfo AddItem(ItemDataInfo item) => AddItemAsync(item).GetAwaiter().GetResult();

   public async Task<ItemDataInfo?> GetDataAsync(Guid dataId, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(DataSelect + " WHERE id = @id", conn);
      cmd.Parameters.AddWithValue("id", dataId);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadData(r) : null;
   }

   public ItemDataInfo? GetData(Guid dataId) => GetDataAsync(dataId).GetAwaiter().GetResult();

   public async Task<ItemDataInfo?> GetDataByNameAsync(
      Guid itemId, string name, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         DataSelectTop + " WHERE item_id = @iid AND name = @name", conn);
      cmd.Parameters.AddWithValue("iid", itemId);
      cmd.Parameters.AddWithValue("name", name);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
      return await r.ReadAsync(ct).ConfigureAwait(false) ? ReadData(r) : null;
   }

   public ItemDataInfo? GetDataByName(Guid itemId, string name)
      => GetDataByNameAsync(itemId, name).GetAwaiter().GetResult();

   public async Task<ContentTypeInfo?> GetContentTypeAsync(
      string contentTypeId, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         "SELECT type_id, description FROM dbo.edam_content_type WHERE type_id = @tid", conn);
      cmd.Parameters.AddWithValue("tid", contentTypeId);
      await using var r = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

      return await r.ReadAsync(ct).ConfigureAwait(false)
         ? new ContentTypeInfo(r.GetString(0), r.IsDBNull(1) ? null : r.GetString(1))
         : new ContentTypeInfo(contentTypeId);
   }

   public ContentTypeInfo? GetContentType(string contentTypeId)
      => GetContentTypeAsync(contentTypeId).GetAwaiter().GetResult();

   public ItemDataInfo? CreateDataLeaf(
      ItemInfo item, string name, Guid? dataId = null, byte[]? dataValue = null)
      => CreateDataLeaf(item, name, dataId,
         dataValue is null ? null : Convert.ToBase64String(dataValue));

   public ItemDataInfo? CreateDataLeaf(
      ItemInfo item, string name, Guid? dataId = null, string? dataValue = null)
   {
      var leaf = new ItemDataInfo(
         dataId ?? Guid.NewGuid(), item.Id, name, string.Empty, "default", dataValue);
      return AddItem(leaf);
   }

   public async Task<bool> DeleteDataAsync(Guid dataId, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         "DELETE FROM dbo.edam_item_data WHERE id = @id", conn);
      cmd.Parameters.AddWithValue("id", dataId);
      return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) > 0;
   }

   public bool DeleteData(Guid dataId) => DeleteDataAsync(dataId).GetAwaiter().GetResult();

   public async Task<bool> DeleteItemDataAsync(Guid itemId, CancellationToken ct = default)
   {
      await using var conn = await OpenAsync(ct).ConfigureAwait(false);
      await using var cmd = new SqlCommand(
         "DELETE FROM dbo.edam_item_data WHERE item_id = @id", conn);
      cmd.Parameters.AddWithValue("id", itemId);
      return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false) > 0;
   }

   public bool DeleteItemData(Guid itemId)
      => DeleteItemDataAsync(itemId).GetAwaiter().GetResult();

   private static ContainerInfo ReadContainer(SqlDataReader r) => new(
      r.GetGuid(0), r.GetString(1), r.GetString(2),
      (ContainerType)r.GetInt32(3),
      r.IsDBNull(4) ? string.Empty : r.GetString(4),
      r.IsDBNull(5) ? "application/json" : r.GetString(5));
}
