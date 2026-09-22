namespace Edam.Data.Projects.Contracts;

/// <summary>
/// A <b>project-relative</b>, provider-agnostic path (always leading-slash, forward-slash
/// normalized, no trailing slash except root). This is the type that lets a Project stop being a
/// file-system location: <c>./Archive/datovy.hc.cd.schema.xlsx</c> from a <c>*.Args.json</c>
/// resolves to <c>/Archive/datovy.hc.cd.schema.xlsx</c> whether the bytes live on disk or in a
/// catalog <c>IContentStore</c> — and no process current-directory is involved.
/// </summary>
public readonly record struct ProjectPath(string Value)
{
   /// <summary>The project root (<c>/</c>).</summary>
   public static ProjectPath Root => new("/");

   /// <summary>True when this is the project root.</summary>
   public bool IsRoot => string.IsNullOrEmpty(Value) || Value == "/";

   /// <summary>The last path segment (empty for the root).</summary>
   public string Name
   {
      get
      {
         var value = Trimmed();
         if (value.Length == 0) return string.Empty;
         var index = value.LastIndexOf('/');
         return index < 0 ? value : value[(index + 1)..];
      }
   }

   /// <summary>The file extension including the dot (empty when there is none).</summary>
   public string Extension
   {
      get
      {
         var name = Name;
         var index = name.LastIndexOf('.');
         return index <= 0 ? string.Empty : name[index..];
      }
   }

   /// <summary>The parent path, or the root when already at the root.</summary>
   public ProjectPath Parent
   {
      get
      {
         var value = Trimmed();
         var index = value.LastIndexOf('/');
         return index <= 0 ? Root : new ProjectPath("/" + value[..index]);
      }
   }

   /// <summary>Append a relative segment (e.g. <c>ProjectFolders.Archive</c>, <c>"x.xlsx"</c>).</summary>
   public ProjectPath Combine(string? relative)
      => Parse(string.IsNullOrWhiteSpace(relative) ? Value : Trimmed() + "/" + relative);

   /// <summary>Parse and normalize an arbitrary path (tolerates <c>./</c>, <c>..</c>, <c>\</c>).</summary>
   public static ProjectPath Parse(string? value) => new(Normalize(value));

   public override string ToString() => Normalize(Value);

   private string Trimmed() => (Value ?? string.Empty).Trim('/');

   /// <summary>Leading slash, forward slashes, collapsed separators, "." dropped, ".." trimmed.</summary>
   private static string Normalize(string? value)
   {
      if (string.IsNullOrWhiteSpace(value)) return "/";

      var segments = value.Replace('\\', '/')
         .Split('/', StringSplitOptions.RemoveEmptyEntries);

      var stack = new List<string>(segments.Length);
      foreach (var segment in segments)
      {
         if (segment == ".") continue;
         if (segment == "..")
         {
            if (stack.Count > 0) stack.RemoveAt(stack.Count - 1);
            continue;
         }
         stack.Add(segment);
      }

      return stack.Count == 0 ? "/" : "/" + string.Join('/', stack);
   }
}

/// <summary>A resource (folder or file) inside a Project, addressed project-relative.</summary>
/// <param name="Path">Its project-relative path.</param>
/// <param name="Name">The last path segment.</param>
/// <param name="IsFolder">True for a folder (a catalog branch), false for a file (a leaf/content).</param>
/// <param name="Length">Content length in bytes (0 for folders / unknown).</param>
/// <param name="UpdatedAt">Last updated timestamp, when known.</param>
public sealed record ProjectResourceInfo(
   ProjectPath Path,
   string Name,
   bool IsFolder,
   long Length = 0,
   DateTimeOffset UpdatedAt = default);
