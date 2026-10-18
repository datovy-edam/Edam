using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Edam.Data.Projects.Contracts;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// <b>The default text-map catalog</b> (S3-2): it answers by <b>kind</b>, never by file name, searching
/// <b>the project's own <c>TextMaps/</c> folder first</b> (a map inside the project is the most concrete answer)
/// and then the <b>application's seed folders</b> (the installed <c>app:textmaps</c> location). A component
/// presented at project initialization replaces it through DI, and the UI keeps offering only what
/// <see cref="SupportedKinds"/> says it can honour.
/// </summary>
public sealed class DefaultTextMapCatalog : ITextMapCatalog
{
   private readonly IProjectResources m_Resources;
   private readonly IReadOnlyList<String> m_SeedFolders;
   private readonly Action<String>? m_Report;

   /// <param name="resources">The project's resource seam (the same one the rest of the platform uses).</param>
   /// <param name="seedFolders">Local folders holding the application's shipped maps (may be empty).</param>
   /// <param name="report">Where a rejected file is explained — never silent.</param>
   public DefaultTextMapCatalog(
      IProjectResources resources,
      IEnumerable<String>? seedFolders = null,
      Action<String>? report = null)
   {
      m_Resources = resources ?? throw new ArgumentNullException(nameof(resources));
      m_SeedFolders = seedFolders?.Where(f => !String.IsNullOrWhiteSpace(f)).ToList()
         ?? new List<String>();
      m_Report = report;
   }

   /// <inheritdoc />
   public IReadOnlyCollection<TextMapKind> SupportedKinds { get; } =
      TextMapConvention.KnownTypeIds
         .Select(id => TextMapConvention.ToKind(id))
         .Where(kind => kind.HasValue)
         .Select(kind => kind!.Value)
         .Distinct()
         .ToList();

   /// <inheritdoc />
   public async Task<IReadOnlyList<TextMapOption>> ListAsync(
      ProjectInfo project, CancellationToken ct = default)
   {
      var found = new List<TextMapOption>();

      // 1. the project's own TextMaps folder
      try
      {
         var entries = await m_Resources.ListAsync(
            project, ProjectPath.Parse("/" + ProjectLocations.TextMaps), "json", ct)
            .ConfigureAwait(false);

         foreach (var entry in entries)
         {
            var address = entry.Path.ToString();
            Accept(found, address, address, "the project");
         }
      }
      catch (Exception ex)
      {
         m_Report?.Invoke("The project's TextMaps folder could not be read (" + ex.Message + ").");
      }

      // 2. the application's shipped maps
      foreach (var folder in m_SeedFolders)
      {
         try
         {
            if (!Directory.Exists(folder)) continue;

            foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
            {
               Accept(found, file, file, "the application");
            }
         }
         catch (Exception ex)
         {
            m_Report?.Invoke("The application's TextMaps folder '" + folder +
               "' could not be read (" + ex.Message + ").");
         }
      }

      return found;
   }

   /// <inheritdoc />
   public async Task<TextMapOption?> ResolveAsync(
      ProjectInfo project, TextMapKind kind, CancellationToken ct = default)
   {
      var options = await ListAsync(project, ct).ConfigureAwait(false);

      // the project's own files are found first, so the first match of the kind is the most concrete one
      return options.FirstOrDefault(option => option.Kind == kind);
   }

   /// <summary>Read one candidate; accept it or report why it was ignored.</summary>
   private void Accept(
      List<TextMapOption> found, String name, String address, String where)
   {
      var option = TextMapConvention.TryParse(name, address, out var problem);
      if (option is null)
      {
         m_Report?.Invoke("Ignored the text map '" + name + "' in " + where + ": " + problem);
         return;
      }

      found.Add(option);
   }
}
