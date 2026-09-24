using Edam.Data.Catalog.Contracts;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.Catalog;

/// <summary>
/// Catalog <see cref="IProjectSeeder"/> (LM-5): the template's <b>address</b> is its catalog path
/// inside the container (<c>catalog://collection/Templates/ToAssets.Args.json</c> →
/// <c>/Templates/ToAssets.Args.json</c>), read through <see cref="IContentStore"/> and written through
/// <see cref="IProjectResources"/> — a copy from one address to another, provider-independent.
/// </summary>
public sealed class CatalogProjectSeeder : IProjectSeeder
{
   private readonly IContentStore _content;
   private readonly IProjectResources _resources;

   public CatalogProjectSeeder(IContentStore content, IProjectResources resources)
   {
      _content = content ?? throw new ArgumentNullException(nameof(content));
      _resources = resources ?? throw new ArgumentNullException(nameof(resources));
   }

   public async Task<ProjectPath?> SeedArgumentsAsync(
      ProjectInfo project, CatalogAddress template, string? fileName = null,
      CancellationToken ct = default)
   {
      ArgumentNullException.ThrowIfNull(project);

      using var stream = await _content.OpenReadAsync(template.Path.Value, ct).ConfigureAwait(false);
      if (stream is null) return null;

      var target = ProjectSeeding.TargetPath(project, template, fileName);
      await _resources.WriteAsync(project, target, stream, ct).ConfigureAwait(false);
      return target;
   }
}
