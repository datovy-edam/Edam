using System.Text.Json;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// Somewhere to keep <b>stated values</b> (CF-3 / ADR-0013 decision 6). Two are in play, and the
/// difference matters: the per-user <b>app-data overlay</b> holds the user's answers and <b>can be
/// reset</b> (so an item is asked about again), while the <b>packaged</b> per-user state holds the
/// once-per-installation marker, which a user-state reset must <b>not</b> clear — otherwise the starter
/// project could be offered again after the user deleted it.
/// </summary>
public interface IStateStore
{
   /// <summary>Where this state lives — reported so "where did this value come from?" is answerable.</summary>
   string Location { get; }

   /// <summary>The stored value, or null when nothing is stored.</summary>
   string? Read(string key);

   /// <summary>Store a value; <c>null</c> forgets it (that is how an item is asked about again).</summary>
   void Write(string key, string? value);

   /// <summary>Every stored entry.</summary>
   IReadOnlyDictionary<string, string?> ReadAll();
}

/// <summary>
/// The app-data <b>overlay</b>: a small JSON file of stated values, kept <b>separate from the packaged
/// seed</b> (ADR-0010) so a seed refresh can never fight a user's answer.
/// <para>
/// Robustness rules learned the hard way in this codebase: the folder is <b>created when missing</b> (a
/// write must not fail because the app-data folder does not exist yet), the file is replaced atomically,
/// and a missing, empty or <b>corrupt</b> file is treated as "nothing stated" rather than fatal.
/// </para>
/// </summary>
public sealed class FileStateStore : IStateStore
{
   private static readonly JsonSerializerOptions Pretty =
      new() { WriteIndented = true };

   private readonly string _path;

   /// <param name="rootPath">The per-user state root (the app-data folder the host resolved).</param>
   /// <param name="fileName">The overlay's file name — never the packaged seed's.</param>
   public FileStateStore(string rootPath, string fileName = "Edam.UserState.json")
   {
      if (string.IsNullOrWhiteSpace(rootPath))
      {
         throw new ArgumentException("A state root is required.", nameof(rootPath));
      }

      _path = Path.Combine(Path.GetFullPath(rootPath), fileName);
   }

   public string Location => _path;

   public string? Read(string key)
      => ReadAll().TryGetValue(key, out var value) ? value : null;

   public void Write(string key, string? value)
   {
      var values = new Dictionary<string, string?>(ReadAll(), StringComparer.OrdinalIgnoreCase);
      if (value is null)
      {
         values.Remove(key);
      }
      else
      {
         values[key] = value;
      }

      Save(values);
   }

   /// <summary>Every stored entry; a missing or unreadable file means "nothing stated".</summary>
   public IReadOnlyDictionary<string, string?> ReadAll()
   {
      var empty = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

      try
      {
         if (!File.Exists(_path))
         {
            return empty;
         }

         var values = JsonSerializer.Deserialize<Dictionary<string, string?>>(
            File.ReadAllText(_path));

         return values is null
            ? empty
            : new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase);
      }
      catch (Exception)
      {
         // a corrupt state file must never be fatal
         return empty;
      }
   }

   private void Save(Dictionary<string, string?> values)
   {
      var folder = Path.GetDirectoryName(_path);
      if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
      {
         Directory.CreateDirectory(folder);
      }

      var temp = _path + ".tmp";
      File.WriteAllText(temp, JsonSerializer.Serialize(values, Pretty));
      File.Move(temp, _path, overwrite: true);
   }
}

/// <summary>
/// In-memory <see cref="IStateStore"/> (CF-3): used by tests, and as the packaged state until a host
/// supplies its own (the Studio's will be the packaged per-user settings — CF-4).
/// </summary>
public sealed class InMemoryStateStore : IStateStore
{
   private readonly Dictionary<string, string?> _values =
      new(StringComparer.OrdinalIgnoreCase);

   public InMemoryStateStore(string location = "(in memory)") => Location = location;

   public string Location { get; }

   public string? Read(string key) => _values.TryGetValue(key, out var value) ? value : null;

   public void Write(string key, string? value)
   {
      if (value is null)
      {
         _values.Remove(key);
      }
      else
      {
         _values[key] = value;
      }
   }

   public IReadOnlyDictionary<string, string?> ReadAll() =>
      new Dictionary<string, string?>(_values, StringComparer.OrdinalIgnoreCase);
}
