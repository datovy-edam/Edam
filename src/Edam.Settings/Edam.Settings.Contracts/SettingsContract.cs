namespace Edam.Settings.Contracts;

/// <summary>
/// The section names a settings surface shows (ADR-0016). They are <b>contract</b> vocabulary, not a surface's
/// taste: a WinUI page, a web page and a CLI must group the same way, and moving an item between sections must be
/// one edit rather than three.
/// </summary>
public static class SettingsGroups
{
   /// <summary>Projects — the starter project and its shape.</summary>
   public const String Projects = "Projects";

   /// <summary>Arguments — the documents a project runs, and the domain they name.</summary>
   public const String Arguments = "Arguments";

   /// <summary>Catalog — where the containers, projects and content actually live.</summary>
   public const String Catalog = "Catalog";

   /// <summary>Where the deployment's own facts are shown, read-only.</summary>
   public const String Installation = "Set for this installation";

   /// <summary>Recorded state — evidence that an action happened; not a preference.</summary>
   public const String Recorded = "Recorded state";

   /// <summary>The order the sections appear in, for every host.</summary>
   public static IReadOnlyList<String> Order { get; } =
      new[] { Projects, Arguments, Catalog, Installation, Recorded };
}

/// <summary>
/// Where a value actually came from — the question the diagnostics panel was built to answer
/// ("where did this value come from?"), asked now of configuration itself.
/// <para>
/// It matters because the layers are not equal: an <b>environment</b> variable <b>wins</b> over the
/// deployment's file, which wins over a documented default. A surface that showed "your value" while an
/// environment variable was in force would be the silent-fallback defect ADR-0013 exists to prevent,
/// wearing a friendlier face.
/// </para>
/// </summary>
public enum SettingsSource
{
   /// <summary>Nothing has stated it, and no default applies.</summary>
   Unset,

   /// <summary>No one stated it, so the item's documented default is in force.</summary>
   Default,

   /// <summary>The installation stated it (its configuration file).</summary>
   Deployment,

   /// <summary>An environment variable overrides everything else.</summary>
   Environment,

   /// <summary>The person stated it (the per-user overlay).</summary>
   User,

   /// <summary>Recorded once for this installation (packaged state, e.g. a completion marker).</summary>
   Installation
}

/// <summary>
/// The three tiers a setting belongs to (ADR-0016). They are shown <b>separately</b> because mixing them
/// is what makes a settings surface untrustworthy: a preference, a deployment fact and a piece of recorded
/// evidence are not the same kind of thing, and only one of them is the person's to change.
/// </summary>
public enum SettingsTier
{
   /// <summary><b>Your values</b> — the person's to state, kept in the per-user overlay.</summary>
   UserValue,

   /// <summary><b>Set for this installation</b> — a deployment/environment fact; read-only here.</summary>
   Installation,

   /// <summary><b>Recorded state</b> — evidence that an action happened, not a preference; never editable.</summary>
   RecordedState
}

/// <summary>How a setting is presented and edited — so a surface is generated, never hardcoded per item.</summary>
public enum SettingsKind
{
   /// <summary>Free text (a name, a path, an address).</summary>
   Text,

   /// <summary>One of a stated list (<see cref="SettingsItemInfo.Choices"/>).</summary>
   Choice,

   /// <summary>On or off.</summary>
   Switch,

   /// <summary>Displayed only — there is nothing to edit here.</summary>
   ReadOnly
}

/// <summary>
/// <b>When</b> a change takes effect. Stated per item because the alternative is a surface that says
/// "saved" and means "perhaps": the value is stored either way, but only <see cref="Immediate"/> is
/// applied to the running application by the host.
/// </summary>
public enum SettingsApplyScope
{
   /// <summary>Applied at once — the host recycles whatever depended on it.</summary>
   Immediate,

   /// <summary>Takes effect the next time the dependent feature is used.</summary>
   NextUse,

   /// <summary>Needs a restart; the surface must say so instead of implying it is live.</summary>
   Restart
}

/// <summary>The three states a setting's value can be in (CF-1 / ADR-0013 decision 1).</summary>
public enum SettingsValueState
{
   /// <summary>Nothing has stated it.</summary>
   Unset,

   /// <summary>Stated — the value may legitimately be empty.</summary>
   Set,

   /// <summary>Stated, but unusable; see <see cref="SettingsItemInfo.Problem"/>.</summary>
   Invalid
}

/// <summary>
/// <b>One setting, as a surface needs to show it</b> (ADR-0016) — the read model that a settings page, an
/// API endpoint, a CLI command and an MCP tool all render from.
/// </summary>
/// <param name="Id">Stable id, also the configuration key for the first entries.</param>
/// <param name="Key">The configuration key the value is read from.</param>
/// <param name="Title">Short label.</param>
/// <param name="Group">The section it belongs to (so sections come from the catalog, not the XAML).</param>
/// <param name="WhatItIs">One sentence: what this is.</param>
/// <param name="WhatItAffects">What changes once it is answered.</param>
/// <param name="Kind">How to present the editor.</param>
/// <param name="Tier">Which of the three tiers it belongs to.</param>
/// <param name="State">Unset · Set · Invalid.</param>
/// <param name="Value">The value <b>in force</b> (what was stated, even when unusable) or null.</param>
/// <param name="Source">Where <paramref name="Value"/> came from.</param>
/// <param name="Problem">Why a stated value is unusable (null unless <see cref="SettingsValueState.Invalid"/>).</param>
/// <param name="IsEditable">Whether this surface may change it.</param>
/// <param name="ApplyScope">When a change takes effect.</param>
/// <param name="Default">The value offered when nothing is stated.</param>
/// <param name="Choices">The permitted values, for a <see cref="SettingsKind.Choice"/>.</param>
/// <param name="IsAdvanced">True to hide it behind an "Advanced" disclosure (recorded state, markers).</param>
/// <param name="Reason">Why it is not editable, or why it is still being asked about.</param>
public sealed record SettingsItemInfo(
   string Id,
   string Key,
   string Title,
   string Group,
   string WhatItIs,
   string WhatItAffects,
   SettingsKind Kind,
   SettingsTier Tier,
   SettingsValueState State,
   string? Value,
   SettingsSource Source,
   string? Problem,
   bool IsEditable,
   SettingsApplyScope ApplyScope,
   string? Default = null,
   IReadOnlyList<string>? Choices = null,
   bool IsAdvanced = false,
   string? Reason = null)
{
   /// <summary>What a person should see in the value field: the stated value, else the default.</summary>
   public string? Display => Value ?? Default;

   /// <summary>True when the value in force came from somewhere other than the person's own answer.</summary>
   public bool IsInherited =>
      Source is SettingsSource.Deployment or SettingsSource.Environment
         or SettingsSource.Default or SettingsSource.Installation;

   /// <summary>True when the value can be used.</summary>
   public bool IsUsable => State != SettingsValueState.Invalid;
}

/// <summary>
/// The <b>read model</b> of every setting a host can show (ADR-0016). It is deliberately <b>not</b> a
/// questionnaire: <c>ToAsk()</c> answers "what must I ask now?", while a catalog answers "what can I see and
/// change?" — and those are different surfaces (a first-run prompt vs a settings page).
/// </summary>
public interface ISettingsCatalog
{
   /// <summary>Every setting, in presentation order.</summary>
   IReadOnlyList<SettingsItemInfo> Items { get; }

   /// <summary>The section names, in the order they should appear.</summary>
   IReadOnlyList<string> Groups { get; }

   /// <summary>The settings that belong to a section.</summary>
   IReadOnlyList<SettingsItemInfo> InGroup(string group);

   /// <summary>One setting by id, or null when it is not known.</summary>
   SettingsItemInfo? Find(string id);
}

/// <summary>The outcome of changing one setting.</summary>
/// <param name="Changed">
/// True when the value was <b>stored</b>. Note that <paramref name="Problem"/> may also be set: a value can be
/// saved and still fail to <b>apply</b>, which the caller must report rather than call a success.
/// </param>
/// <param name="Problem">Why it was refused (nothing stored), or why a stored value did not apply.</param>
/// <param name="ApplyScope">When the stored value takes effect.</param>
public sealed record SettingsChangeResult(
   bool Changed, string? Problem, SettingsApplyScope ApplyScope = SettingsApplyScope.Immediate)
{
   /// <summary>Refused: nothing was written.</summary>
   public static SettingsChangeResult Refused(string problem) =>
      new(false, problem);

   /// <summary>Stored, and taking effect at <paramref name="scope"/>.</summary>
   public static SettingsChangeResult Saved(SettingsApplyScope scope) =>
      new(true, null, scope);

   /// <summary>Stored, but the host could not apply it — reported, never hidden.</summary>
   public static SettingsChangeResult SavedButNotApplied(SettingsApplyScope scope, string problem) =>
      new(true, "saved, but it did not take effect — " + problem, scope);
}

/// <summary>
/// Writing settings (ADR-0016): the <b>one</b> way a value is stored, whoever asks — a settings page, an API
/// endpoint, a CLI, an MCP tool. Validation happens <b>before</b> anything is written, so an unusable value
/// never reaches a store (ADR-0013 decision 7).
/// </summary>
public interface ISettingsEditor
{
   /// <summary>Store a value for a setting; null clears it (the default applies again).</summary>
   Task<SettingsChangeResult> SetAsync(string id, string? value, CancellationToken ct = default);

   /// <summary>Forget one answer, so the default (or the deployment's value) applies again.</summary>
   Task<SettingsChangeResult> ResetAsync(string id, CancellationToken ct = default);

   /// <summary>Forget every <b>answer</b>. Recorded installation state is deliberately spared.</summary>
   Task<Int32> ResetAllAsync(CancellationToken ct = default);
}

/// <summary>
/// A host's chance to make a stored value <b>real</b> (ADR-0016 decision: a saved value that nothing reads is
/// the "configured but inert" defect). Implemented at the composition root, where the affected component can
/// actually be rebuilt.
/// </summary>
public interface ISettingsApplier
{
   /// <param name="item">The setting that changed.</param>
   /// <param name="value">The value now in force (null when cleared).</param>
   /// <returns>Why applying it failed, or null when it took effect.</returns>
   Task<String?> ApplyAsync(SettingsItemInfo item, String? value, CancellationToken ct = default);
}
