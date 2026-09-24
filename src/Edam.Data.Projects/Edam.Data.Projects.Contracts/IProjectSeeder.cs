namespace Edam.Data.Projects.Contracts;

/// <summary>
/// Seeds a project's content from a template that lives at an <b>address</b> (LM-5 / ADR-0011) —
/// "read an address, write an address".
/// <para>
/// Scaffolding stays structure-only (<see cref="IProjectStore.CreateAsync"/> creates the project and
/// its folders); this is the <b>seeding</b> half, which restores the behaviour the legacy
/// <c>Edam.Data.AssetProject.Project.CreateProject</c> had — a starter <c>*.Args.json</c> in
/// <c>Arguments/</c> — but without an app-settings path and without touching the process current
/// directory: the template is simply another artifact.
/// </para>
/// </summary>
public interface IProjectSeeder
{
   /// <summary>
   /// Copy the template at <paramref name="template"/> into the project's <c>Arguments</c> folder as
   /// <c>&lt;project name&gt;.&lt;template file name&gt;</c> (the legacy naming), or as
   /// <paramref name="fileName"/> when given.
   /// </summary>
   /// <returns>The project-relative path written, or <c>null</c> when the template is not there.</returns>
   Task<ProjectPath?> SeedArgumentsAsync(
      ProjectInfo project,
      CatalogAddress template,
      string? fileName = null,
      CancellationToken ct = default);
}

/// <summary>Naming shared by the seeders — the legacy <c>&lt;project&gt;.&lt;template&gt;</c> convention.</summary>
public static class ProjectSeeding
{
   /// <summary>The project-relative <c>Arguments</c> path a template seeds into.</summary>
   public static ProjectPath TargetPath(
      ProjectInfo project, CatalogAddress template, string? fileName = null)
      => ProjectFolders.Path(ProjectFolders.Arguments)
         .Combine(fileName ?? DefaultFileName(project, template));

   /// <summary><c>&lt;project name&gt;.&lt;template file name&gt;</c> — what the legacy scaffolder wrote.</summary>
   public static string DefaultFileName(ProjectInfo project, CatalogAddress template)
      => project.Name + "." + template.Path.Name;
}
