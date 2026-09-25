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
   /// The branch that holds a container's projects: <c>/Projects</c>.
   /// <para>
   /// <b>LM-2c:</b> the container is <b>no longer embedded in the path</b>. It used to be
   /// <c>/&lt;collectionId&gt;/Projects</c> because catalog item paths were global
   /// (<c>GetItemByPath</c> ignored the container) and content keys were path-only, so two collections
   /// would have collided on the same path. Both are now <b>container-scoped</b> (LM-2a items, LM-2b
   /// content, LM-2b-ii providers and wire), so a project's address is identical in <b>every</b>
   /// provider — <c>/Projects/&lt;name&gt;</c>, exactly as the file-system provider already had it.
   /// </para>
   /// </summary>
   internal static string ProjectsRoot => "/" + ProjectsFolder;

   /// <summary>The branch of a project inside its container.</summary>
   internal static ProjectPath ProjectBranch(string projectName)
      => ProjectPath.Parse(ProjectsRoot).Combine(projectName);

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
