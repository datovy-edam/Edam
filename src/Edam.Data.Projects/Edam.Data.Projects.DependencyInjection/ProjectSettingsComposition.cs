using Edam.Settings.Contracts;
using Edam.Settings.Core;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// Composes the settings surface for a host that has <b>no per-user application data of its own</b> (ADR-0016) — an
/// API service, a CLI, an MCP tool. The <b>mechanism is identical</b> to the desktop's; only the <b>state root</b>
/// differs, which is exactly the seam that makes the feature UI-agnostic instead of WinUI-shaped.
/// <para>
/// The desktop composes its own (its overlay sits at the app-data root, and its marker in packaged settings,
/// ADR-0010) because those locations are only meaningful to a packaged application.
/// </para>
/// </summary>
public static class ProjectSettingsComposition
{
   /// <summary>The per-user answers file (the overlay).</summary>
   public const String OVERLAY_FILE = "Edam.UserState.json";

   /// <summary>The once-per-installation state file (markers) for a host with no packaged settings.</summary>
   public const String INSTALLATION_FILE = "Edam.InstallationState.json";

   /// <summary>Where a host states its settings state root.</summary>
   public const String STATE_ROOT_KEY = "Edam:Settings:StateRoot";

   /// <summary>
   /// Build the one service a surface drives: catalog (read) + editor (write) + the host's applier (make it real).
   /// </summary>
   /// <param name="config">The host's configuration — the layer <b>below</b> the answers.</param>
   /// <param name="stateRoot">Where answers are kept; defaults to the stated root, else the host's own folder.</param>
   /// <param name="applier">What makes a stored value real; null for a host with nothing to recycle.</param>
   /// <param name="environment">The override source (null = the process environment) — explicit for tests.</param>
   public static SettingsService Create(
      IConfiguration config,
      String? stateRoot = null,
      ISettingsApplier? applier = null,
      IReadOnlyDictionary<String, String?>? environment = null)
   {
      if (config is null) throw new ArgumentNullException(nameof(config));

      // a read surface must never fail because it could not work out where to write: writes report their own
      // failure, and reads still answer from the deployment's configuration
      var root = !String.IsNullOrWhiteSpace(stateRoot)
         ? stateRoot!
         : !String.IsNullOrWhiteSpace(config[STATE_ROOT_KEY])
            ? config[STATE_ROOT_KEY]!
            : Path.Combine(AppContext.BaseDirectory, "settings-state");

      var user = new UserConfiguration(
         config,
         new FileStateStore(root, OVERLAY_FILE),
         new FileStateStore(root, INSTALLATION_FILE));

      return new SettingsService(
         new ProjectSettingsCatalog(user, environment),
         new ProjectSettingsEditor(user),
         applier);
   }
}
