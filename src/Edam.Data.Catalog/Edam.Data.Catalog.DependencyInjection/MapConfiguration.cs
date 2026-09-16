using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Edam.Data.Catalog.DependencyInjection;

/// <summary>
/// Minimal flat-dictionary <see cref="IConfiguration"/> for desktop/UI hosts (notably the WinUI
/// shell) that don't load appsettings.json. Backs
/// <see cref="CatalogServices.AddCatalogServices(Microsoft.Extensions.DependencyInjection.IServiceCollection, IReadOnlyDictionary{string,string})"/>.
/// </summary>
internal sealed class MapConfiguration : IConfiguration
{
   private readonly IReadOnlyDictionary<string, string> _map;

   public MapConfiguration(IReadOnlyDictionary<string, string> map)
   {
      _map = map;
      Key = "";
      Path = "";
   }

   public string? this[string key]
   {
      get => _map.TryGetValue(key, out var v) ? v : null;
      set => throw new NotSupportedException();
   }

   public IConfigurationSection GetSection(string key) => new MapSection(this, key);
   public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
   public IChangeToken GetReloadToken() => NullChangeToken.Instance;

   public string Key { get; }
   public string Path { get; }

   private sealed class MapSection : IConfigurationSection
   {
      private readonly MapConfiguration _parent;

      public MapSection(MapConfiguration parent, string key) { _parent = parent; Key = key; }

      public string? this[string key]
      {
         get => _parent[Combine(Key, key)];
         set => throw new NotSupportedException();
      }

      public IConfigurationSection GetSection(string key) => new MapSection(_parent, Combine(Key, key));
      public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
      public IChangeToken GetReloadToken() => NullChangeToken.Instance;

      public string? Value { get => _parent[Key]; set => throw new NotSupportedException(); }
      public string Key { get; }
      public string Path => Key;

      private static string Combine(string a, string b) => a.Length == 0 ? b : a + ":" + b;
   }
}

/// <summary>A never-changing <see cref="IChangeToken"/> (MapConfiguration has no reload support).</summary>
internal sealed class NullChangeToken : IChangeToken
{
   public static readonly IChangeToken Instance = new NullChangeToken();

   public bool ActiveChangeCallbacks => false;
   public bool HasChanged => false;
   public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new Noop();

   private sealed class Noop : IDisposable { public void Dispose() { } }
}
