using Edam.Data.Catalog.Contracts;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Catalog;

/// <summary>
/// Shared mapping between the project model and the catalog model (PE-3 / ADR-0009):
/// a Collection is a <b>container</b>, a Project is the <b>branch</b> <c>/Projects/&lt;name&gt;</c>
/// inside it, and every resource's <b>full item path is also its content key</b>.
/// </summary>
internal static class CatalogProjectSupport
{
   /// <summary>The folder that holds a collection's projects.</summary>
   internal const string ProjectsFolder = "Projects";

   /// <summary>
   /// The branch that holds a collection's projects — <b>scoped by the collection id</b>. Catalog
   /// item paths are global (not container-scoped: <c>GetItemByPath</c> ignores the container), so
   /// two collections would otherwise collide on <c>/Projects/&lt;name&gt;</c>.
   /// </summary>
   internal static string ProjectsRoot(string collectionId)
      => "/" + (collectionId ?? string.Empty).Trim('/') + "/" + ProjectsFolder;

   /// <summary>The branch of a project within its collection.</summary>
   internal static ProjectPath ProjectBranch(string collectionId, string projectName)
      => ProjectPath.Parse(ProjectsRoot(collectionId)).Combine(projectName);

   /// <summary>The catalog (full) path of a project-relative resource.</summary>
   internal static string Full(ProjectInfo project, ProjectPath path)
      => path.IsRoot ? project.Path.Value : project.Path.Combine(path.Value).Value;

   /// <summary>The project-relative path of a catalog (full) path.</summary>
   internal static ProjectPath RelativeTo(ProjectInfo project, string fullPath)
   {
      var root = project.Path.Value;
      if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
         return ProjectPath.Parse(fullPath[root.Length..]);
      return ProjectPath.Parse(fullPath);
   }

   /// <summary>Resolve the collection's container (by containerId, else by scanning).</summary>
   internal static async Task<ContainerInfo> RequireContainerAsync(
      ICatalogContainer containers, string collectionId, CancellationToken ct)
   {
      var container = containers.GetContainer(collectionId);
      if (container is not null) return container;

      var all = await containers.GetContainersAsync(ct).ConfigureAwait(false);
      return all.FirstOrDefault(c =>
            string.Equals(c.ContainerId, collectionId, StringComparison.OrdinalIgnoreCase))
         ?? throw new InvalidOperationException(
            $"Collection '{collectionId}' is not registered in the catalog.");
   }

   /// <summary>Normalize an extension filter to a leading-dot form (null when unfiltered).</summary>
   internal static string? NormalizeExtension(string? extension)
   {
      if (string.IsNullOrWhiteSpace(extension)) return null;
      var value = extension.Trim();
      return value.StartsWith('.') ? value : "." + value;
   }

   /// <summary>Map a catalog item to a project resource description.</summary>
   internal static ProjectResourceInfo ToResource(ProjectInfo project, ItemInfo item)
      => new(RelativeTo(project, item.FullPath), item.Name,
         item.Type == ItemType.Branch, 0, item.UpdatedDate);
}
