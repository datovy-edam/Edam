using Edam.Data.Catalog.Contracts;
using Edam.Data.Catalog.Folder;

namespace Edam.Data.Projects.Catalog;

/// <summary>
/// Seeds a <b>container location</b> from a folder (LM-5): the development story is "point a
/// development container at the repo's <c>app-data/</c>", which makes that folder a <b>seed source</b>
/// — never a configured location (ADR-0011).
/// <para>
/// It is a thin, discoverable wrapper over <see cref="FolderCatalogIndexer"/> with a target path
/// prefix, so a host does not have to know the indexer's details: items <b>and</b> content are
/// written, and the container is <b>not</b> re-enlisted (a seed can never rewrite a collection's
/// binding).
/// </para>
/// </summary>
public static class CatalogFolderSeeder
{
   /// <summary>
   /// Index <paramref name="sourceFolder"/> into <paramref name="targetPath"/> of the container
   /// (for example the repo's <c>Edam.App.Data/Templates</c> folder into <c>/Templates</c>).
   /// </summary>
   /// <returns>The number of files seeded.</returns>
   public static async Task<int> SeedAsync(
      Guid containerId, ICatalogItem items, IContentStore content,
      string sourceFolder, string targetPath, CancellationToken ct = default)
   {
      if (items is null) throw new ArgumentNullException(nameof(items));
      if (content is null) throw new ArgumentNullException(nameof(content));

      var result = await FolderCatalogIndexer.IndexDetailedAsync(
         containerId, items, content, sourceFolder,
         indexContent: true, ct: ct, pathPrefix: targetPath).ConfigureAwait(false);

      return result.Files;
   }

   /// <summary>Convenience overload: resolve the container by id, then seed.</summary>
   public static async Task<int> SeedAsync(
      ICatalogContainer containers, ICatalogItem items, IContentStore content,
      string containerId, string sourceFolder, string targetPath, CancellationToken ct = default)
   {
      if (containers is null) throw new ArgumentNullException(nameof(containers));

      var container = containers.GetContainer(containerId)
         ?? throw new InvalidOperationException($"Container '{containerId}' is not registered.");

      return await SeedAsync(container.Id, items, content, sourceFolder, targetPath, ct)
         .ConfigureAwait(false);
   }
}
