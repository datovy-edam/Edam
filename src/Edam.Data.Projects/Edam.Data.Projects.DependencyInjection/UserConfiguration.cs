using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// The <b>answers</b> (CF-3 / ADR-0013): what the user has stated, kept where each registry item says it
/// should be kept, validated <b>where it is entered</b>, and layered onto configuration so that
/// "ask once, remember, never ask again" is a property of the code rather than a promise.
/// <para>
/// Three behaviours are deliberately asymmetric: an answer that the item's validator rejects is
/// <b>refused</b> and nothing is written; <see cref="Reset()"/> forgets the user's answers but
/// <b>not</b> the once-per-installation marker (a project the user deleted is never resurrected); and
/// the <b>overlay</b> can always be deleted by hand, so the file is plain, readable JSON.
/// </para>
/// </summary>
public sealed class UserConfiguration
{
   private readonly IConfiguration _configuration;
   private readonly IStateStore _overlay;
   private readonly IStateStore _installation;

   /// <param name="configuration">The base configuration (files, environment) <b>below</b> the answers.</param>
   /// <param name="overlay">Where the user's answers live (the app-data overlay file).</param>
   /// <param name="installation">Where the once-per-installation marker lives (packaged settings).</param>
   public UserConfiguration(
      IConfiguration configuration, IStateStore overlay, IStateStore? installation = null)
   {
      _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
      _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
      _installation = installation ?? new InMemoryStateStore("(packaged state not provided)");
   }

   /// <summary>The user's answers store.</summary>
   public IStateStore Overlay => _overlay;

   /// <summary>The packaged (marker) store.</summary>
   public IStateStore Installation => _installation;

   /// <summary>
   /// The configuration as the user has answered it: base configuration <b>plus</b> the overlay, so an
   /// answer is an ordinary configuration value from then on.
   /// </summary>
   public IConfiguration Effective => new ConfigurationBuilder()
      .AddConfiguration(_configuration)
      .AddInMemoryCollection(_overlay.ReadAll())
      .Build();

   /// <summary>The batch that still needs the user's input — judged <b>after</b> the answers.</summary>
   public IReadOnlyList<ConfigurableAsk> ToAsk() => ConfigurableItems.ToAsk(Effective);

   /// <summary>One item's state, after the answers.</summary>
   public ConfigurationItemInfo StateOf(ConfigurableItemInfo item) =>
      ConfigurableItems.StateOf(Effective, item);

   /// <summary>
   /// Store an answer for an item, in the store its <see cref="ConfigurableItemInfo.Storage"/> names.
   /// </summary>
   /// <returns>
   /// Why the answer was refused (nothing is written in that case), or null when it was stored.
   /// </returns>
   public string? Answer(ConfigurableItemInfo item, string? value)
   {
      if (item is null) throw new ArgumentNullException(nameof(item));

      if (value is not null)
      {
         // validated where it is entered (ADR-0013 decision 7): an unusable answer never lands on disk
         var problem = item.Validate?.Invoke(value);
         if (problem is not null)
         {
            return problem;
         }
      }

      Store(item).Write(item.Key, value);
      return null;
   }

   /// <summary>
   /// Forget the user's <b>answers</b>, so unset items are asked about again. The once-per-installation
   /// marker is deliberately <b>not</b> cleared (ADR-0012 decision 4).
   /// </summary>
   public void Reset()
   {
      foreach (var key in _overlay.ReadAll().Keys.ToList())
      {
         _overlay.Write(key, null);
      }
   }

   /// <summary>Forget one answer, so exactly that item is asked about again.</summary>
   public void Reset(string key) => _overlay.Write(key, null);

   /// <summary>
   /// Record that the starter project has been offered (the marker) — written to the <b>packaged</b>
   /// store, so a later reset of the user's answers cannot re-offer it.
   /// </summary>
   public void MarkStarterProjectOffered(string value = "true")
   {
      var item = ConfigurableItems.Find(ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY);
      if (item is not null)
      {
         Store(item).Write(item.Key, value);
      }
   }

   private IStateStore Store(ConfigurableItemInfo item) =>
      item.Storage == ConfigurationStorage.PackagedSettings ? _installation : _overlay;
}
