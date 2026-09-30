using Microsoft.Data.SqlClient;

namespace Edam.Data.Catalog.MsSql;

/// <summary>
/// The <b>MS-SQL schema</b> for the catalog (LM-8): the T-SQL peer of the PostgreSQL DDL, kept in one
/// place so the store and the conformance share a single definition.
/// <para>
/// Every batch is <b>idempotent</b> (guarded by <c>IF OBJECT_ID … IS NULL</c> and <c>sys.indexes</c>), so
/// running it repeatedly is safe, and it mirrors the PostgreSQL schema faithfully — including LM-2b's
/// <b>composite key <c>(container_id, resource_path)</c></b> on the content table.
/// </para>
/// <para>
/// <b>No migration is needed here.</b> LM-2b's <c>ALTER TABLE … ADD PRIMARY KEY</c> was an in-place
/// upgrade of an <i>existing PostgreSQL</i> database; this storage is new, so the composite key is simply
/// part of its create script. (Worth stating because the PostgreSQL peer's code reads as if the migration
/// were universal.)
/// </para>
/// <para>
/// Type mapping (PostgreSQL → T-SQL): <c>uuid</c> → <c>uniqueidentifier</c>; <c>text</c> →
/// <c>nvarchar(max)</c> — except <b>key and index columns</b>, which use <c>nvarchar(450)</c> because SQL
/// Server cannot index <c>nvarchar(max)</c>; <c>integer</c> → <c>int</c>; <c>timestamptz</c> →
/// <c>datetimeoffset</c>; <c>bytea</c> → <c>varbinary(max)</c>.
/// </para>
/// </summary>
public static class MsSqlSchema
{
   /// <summary>The DDL, as ordered idempotent batches.</summary>
   public static readonly string[] Batches =
   {
      @"IF OBJECT_ID(N'dbo.edam_container', N'U') IS NULL
        CREATE TABLE dbo.edam_container (
           id uniqueidentifier NOT NULL CONSTRAINT pk_edam_container PRIMARY KEY,
           container_id nvarchar(450) NOT NULL,
           description nvarchar(max) NOT NULL,
           container_type int NOT NULL,
           container_uri nvarchar(max) NOT NULL
              CONSTRAINT df_edam_container_uri DEFAULT N'',
           content_type nvarchar(400) NOT NULL
              CONSTRAINT df_edam_container_content_type DEFAULT N'application/json'
        );",

      @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ux_edam_container_container_id')
        CREATE UNIQUE INDEX ux_edam_container_container_id ON dbo.edam_container(container_id);",

      @"IF OBJECT_ID(N'dbo.edam_item', N'U') IS NULL
        CREATE TABLE dbo.edam_item (
           id uniqueidentifier NOT NULL CONSTRAINT pk_edam_item PRIMARY KEY,
           container_id uniqueidentifier NOT NULL
              CONSTRAINT fk_edam_item_container REFERENCES dbo.edam_container(id),
           full_path nvarchar(450) NOT NULL,
           name nvarchar(450) NOT NULL,
           description nvarchar(max) NULL,
           item_type int NOT NULL,
           created_date datetimeoffset NOT NULL,
           updated_date datetimeoffset NOT NULL
        );",

      @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_edam_item_container')
        CREATE INDEX ix_edam_item_container ON dbo.edam_item(container_id);",

      @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_edam_item_path')
        CREATE INDEX ix_edam_item_path ON dbo.edam_item(full_path);",

      @"IF OBJECT_ID(N'dbo.edam_item_data', N'U') IS NULL
        CREATE TABLE dbo.edam_item_data (
           id uniqueidentifier NOT NULL CONSTRAINT pk_edam_item_data PRIMARY KEY,
           item_id uniqueidentifier NOT NULL
              CONSTRAINT fk_edam_item_data_item REFERENCES dbo.edam_item(id),
           partition_id nvarchar(450) NOT NULL
              CONSTRAINT df_edam_item_data_partition DEFAULT N'default',
           name nvarchar(450) NOT NULL,
           content_type_id nvarchar(450) NULL,
           value nvarchar(max) NULL
        );",

      @"IF OBJECT_ID(N'dbo.edam_content_type', N'U') IS NULL
        CREATE TABLE dbo.edam_content_type (
           type_id nvarchar(450) NOT NULL CONSTRAINT pk_edam_content_type PRIMARY KEY,
           description nvarchar(max) NULL
        );",

      // the content table is keyed by (container_id, resource_path) — LM-2b
      @"IF OBJECT_ID(N'dbo.edam_content', N'U') IS NULL
        CREATE TABLE dbo.edam_content (
           container_id nvarchar(450) NOT NULL,
           resource_path nvarchar(450) NOT NULL,
           body varbinary(max) NULL,
           CONSTRAINT pk_edam_content PRIMARY KEY (container_id, resource_path)
        );"
   };

   /// <summary>Create whatever is missing; safe to call repeatedly (the store calls it once, lazily).</summary>
   public static async Task EnsureAsync(String connectionString, CancellationToken ct = default)
   {
      await using var connection = new SqlConnection(connectionString);
      await connection.OpenAsync(ct).ConfigureAwait(false);

      foreach (var batch in Batches)
      {
         await using var command = new SqlCommand(batch, connection);
         await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
      }
   }
}
