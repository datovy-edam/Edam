using Edam.Settings.Contracts;

namespace Edam.Settings.Core;

/// <summary>The text a surface shows for a setting, written once for every host.</summary>
public static class SettingsText
{
   /// <summary>A human phrase for where a value came from.</summary>
   public static String Source(SettingsSource source) => source switch
   {
      SettingsSource.User => "your value",
      SettingsSource.Installation => "recorded for this installation",
      SettingsSource.Deployment => "the installation's configuration",
      SettingsSource.Environment => "an environment variable",
      SettingsSource.Default => "the documented default",
      _ => "not set"
   };

   /// <summary>A human phrase for the state of a value.</summary>
   public static String State(SettingsItemInfo item) => item.State switch
   {
      SettingsValueState.Invalid => "needs fixing",
      SettingsValueState.Set => "set",
      _ => item.Default is null ? "not set" : "default"
   };

   /// <summary>A human phrase for when a change takes effect.</summary>
   public static String Apply(SettingsApplyScope scope) => scope switch
   {
      SettingsApplyScope.Immediate => "takes effect immediately",
      SettingsApplyScope.NextUse => "takes effect the next time it is used",
      _ => "takes effect after a restart"
   };

   /// <summary>
   /// One line for the diagnostics panel — the audit trail for "where did this value come from?".
   /// The value is included because a provenance line that cannot say <i>what</i> was set is not evidence.
   /// </summary>
   public static String Describe(SettingsItemInfo item) =>
      $"{item.Title} ({item.Id}) = " + (item.Display is null ? "(not set)" : $"'{item.Display}'") +
      $" — stated by {Source(item.Source)}; {State(item)}; {Apply(item.ApplyScope)}" +
      (item.Problem is null ? String.Empty : $"; problem: {item.Problem}");
}

/// <summary>
/// Changing settings, in <b>one</b> place (ADR-0016), whoever is asking: a settings page, an API endpoint, a CLI
/// or an MCP tool. It guards what the surface used to have to remember — that a setting exists, that it is
/// editable <i>here</i>, and that a stored value is actually applied (so "saved" never quietly means "inert").
/// </summary>
public sealed class SettingsService
{
   private readonly ISettingsCatalog _catalog;
   private readonly ISettingsEditor _editor;
   private readonly ISettingsApplier? _applier;

   /// <param name="catalog">The settings this host can show.</param>
   /// <param name="editor">The one way a value is stored.</param>
   /// <param name="applier">
   /// The host's hook that makes a stored value real. Optional — a host with nothing to recycle (a CLI run, say)
   /// may omit it, and then a change is simply recorded.
   /// </param>
   public SettingsService(
      ISettingsCatalog catalog, ISettingsEditor editor, ISettingsApplier? applier = null)
   {
      _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
      _editor = editor ?? throw new ArgumentNullException(nameof(editor));
      _applier = applier;
   }

   /// <summary>The settings this host can show.</summary>
   public ISettingsCatalog Catalog => _catalog;

   /// <summary>Store a value (null clears it) and apply it when the item says it applies immediately.</summary>
   /// <returns>The outcome: refused (nothing written), saved, or saved-but-not-applied (reported).</returns>
   public async Task<SettingsChangeResult> SetAsync(
      String id, String? value, CancellationToken ct = default)
   {
      var item = _catalog.Find(id);
      if (item is null)
      {
         return SettingsChangeResult.Refused($"'{id}' is not a setting this host can change");
      }

      if (!item.IsEditable)
      {
         return SettingsChangeResult.Refused(
            item.Reason ?? $"'{item.Title}' cannot be changed here");
      }

      var result = await _editor.SetAsync(id, value, ct).ConfigureAwait(false);
      return await ApplyAsync(item, value, result, ct).ConfigureAwait(false);
   }

   /// <summary>Forget one answer, so the deployment's value or the documented default applies again.</summary>
   public async Task<SettingsChangeResult> ResetAsync(String id, CancellationToken ct = default)
   {
      var item = _catalog.Find(id);
      if (item is null)
      {
         return SettingsChangeResult.Refused($"'{id}' is not a setting this host can change");
      }

      if (item.Source != SettingsSource.User)
      {
         // "nothing to reset" and "not yours to reset" are different answers, and saying the wrong one
         // would send a person looking for a value they never set
         return SettingsChangeResult.Refused(item.Source == SettingsSource.Unset
            ? $"'{item.Title}' is not stated, so there is nothing to reset"
            : $"'{item.Title}' is not your value to reset — it came from {SettingsText.Source(item.Source)}");
      }

      var result = await _editor.ResetAsync(id, ct).ConfigureAwait(false);
      return await ApplyAsync(item, null, result, ct).ConfigureAwait(false);
   }

   /// <summary>
   /// Forget every <b>answer</b>. Recorded installation state is deliberately spared (ADR-0012 decision 4: a
   /// project the user deleted is never resurrected), so this is "give me back the deployment's values",
   /// not "factory reset".
   /// </summary>
   public Task<Int32> ResetAllAsync(CancellationToken ct = default) => _editor.ResetAllAsync(ct);

   private async Task<SettingsChangeResult> ApplyAsync(
      SettingsItemInfo item, String? value, SettingsChangeResult result, CancellationToken ct)
   {
      if (!result.Changed)
      {
         return result;
      }

      // only an IMMEDIATE item is the host's to apply; anything else would be a lie about being live
      if (_applier is null || result.ApplyScope != SettingsApplyScope.Immediate)
      {
         return result;
      }

      try
      {
         var problem = await _applier.ApplyAsync(item, value, ct).ConfigureAwait(false);
         return problem is null
            ? result
            : SettingsChangeResult.SavedButNotApplied(result.ApplyScope, problem);
      }
      catch (Exception ex)
      {
         // applying is the host's work and may fail for reasons the person cannot fix; it is REPORTED, never
         // thrown, because the value itself is already safely stored
         return SettingsChangeResult.SavedButNotApplied(
            result.ApplyScope, ex.Message);
      }
   }
}
