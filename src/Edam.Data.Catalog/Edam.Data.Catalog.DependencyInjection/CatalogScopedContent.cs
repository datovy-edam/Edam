using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.FileSystem;
using Edam.Data.Catalog.PostgreSql;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;

namespace Edam.Data.Catalog.DependencyInjection;

/// <summary>
/// Builds <b>container-scoped</b> content stores for the configured catalog storage (LM-2b-ii /
/// ADR-0011). The content seam keeps its pure shape — <see cref="IContentStore"/> addresses a
/// <b>path</b> only — so the <b>container is the instance's scope</b>: whoever knows the storage
/// configuration (the service, the project composition root) hands out a store created <i>for</i> a
/// container, and one policy serves every caller.
/// <para>
/// <b>Compatibility rule:</b> the sentinel <c>default</c> (and an empty container id) means the
/// <b>legacy, unscoped</b> namespace, so content written before container scoping existed stays
/// reachable by clients that send no container.
/// </para>
/// </summary>
public static class CatalogScopedContent
{
   /// <summary>The sentinel container id that means "the legacy, unscoped namespace".</summary>
   public const string UnscopedSentinel = "default";

   /// <summary>A cached factory: container id → a content store scoped to that container.</summary>
   public static Func<string, IContentStore?> Factory(IConfiguration config)
   {
      if (config is null) throw new ArgumentNullException(nameof(config));

      var target = (config["Edam:Catalog:Target"] ?? "postgres").Trim().ToLowerInvariant();
      var root = config["Edam:Catalog:FileSystemRoot"];
      var connection = config["ConnectionStrings:catalog"] ?? config["Edam:Catalog:ConnectionString"];
      var cache = new ConcurrentDictionary<string, IContentStore>(StringComparer.OrdinalIgnoreCase);

      return containerId =>
      {
         if (string.IsNullOrWhiteSpace(containerId) ||
             string.Equals(containerId, UnscopedSentinel, StringComparison.OrdinalIgnoreCase))
         {
            return null;   // the legacy namespace: the caller's unscoped store
         }

         switch (target)
         {
            case "filesystem" or "fs" or "file-system" or "folder":
               if (string.IsNullOrWhiteSpace(root)) return null;
               return cache.GetOrAdd(containerId, id => new FileSystemContentStore(root!, id));

            case "postgres" or "postgresql" or "pg":
               if (string.IsNullOrWhiteSpace(connection)) return null;
               return cache.GetOrAdd(containerId, id => new PostgreSqlContentStore(connection!, id));

            default:
               return null;
         }
      };
   }
}
