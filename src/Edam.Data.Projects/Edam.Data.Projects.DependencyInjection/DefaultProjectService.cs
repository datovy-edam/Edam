using System.Threading;
using System.Threading.Tasks;

namespace Edam.Data.Projects.DependencyInjection;

/// <summary>
/// The <b>host-side half</b> of the gated default project (DP-3 / ADR-0012): the two things only the host
/// knows — whether the bound collection is empty, and how a project is actually created there. The
/// <b>decision</b> (which gates apply, which name, when the marker may be written) stays in
/// <see cref="DefaultProjectService"/>, outside the UI, so it is provable headlessly (ADR-0013 decision 10).
/// </summary>
public interface IDefaultProjectHost
{
   /// <summary>True when the bound collection currently holds no projects.</summary>
   Task<bool> IsContainerEmptyAsync(CancellationToken ct);

   /// <summary>Create the project in the bound collection; false when it could not be created.</summary>
   Task<bool> CreateProjectAsync(string name, CancellationToken ct);
}

/// <summary>What an attempt at the default project did — and why not, when it did not.</summary>
/// <param name="Created">True only when the project really exists (which is when the marker is written).</param>
/// <param name="Applicable">False when the gates say this host must never offer or create it.</param>
/// <param name="Name">The name that was (or would have been) used.</param>
/// <param name="Problem">Why it was not created, or null.</param>
public sealed record DefaultProjectOutcome(
   bool Created,
   bool Applicable,
   string? Name = null,
   string? Problem = null);

/// <summary>
/// The <b>gated default project</b> (DP-3 / ADR-0012): on a new installation, with a local collection, the
/// application offers a starter project — and <b>creates it the moment the person accepts the name</b>.
/// <para>
/// The gates: the person must be offered it only once (the <b>marker</b>), the binding must be
/// <b>local/file-system</b>, the switch <see cref="CREATE_SWITCH_KEY"/> must not be off, and the collection
/// must be <b>empty</b>. The <b>marker is the proof of the action</b>: it is written only after the project
/// really exists, so a failure leaves the question <b>pending</b> and it is asked again — an answer is not
/// completion (ADR-0013 decision 12).
/// </para>
/// </summary>
public sealed class DefaultProjectService
{
   /// <summary>Configurable switch: offer/create the starter project. On for local, off otherwise.</summary>
   public const string CREATE_SWITCH_KEY = "Edam:Projects:CreateDefaultProject";

   private readonly UserConfiguration _user;
   private readonly IDefaultProjectHost _host;
   private readonly Boolean _isLocalTarget;

   /// <param name="isLocalTarget">
   /// True for a <b>local</b> installation — a file-system binding, or a catalog whose server is this
   /// machine (see <see cref="LocalTarget"/>) — which is the only place ADR-0012 offers the starter project.
   /// </param>
   public DefaultProjectService(
      UserConfiguration user, IDefaultProjectHost host, Boolean isLocalTarget)
   {
      _user = user ?? throw new ArgumentNullException(nameof(user));
      _host = host ?? throw new ArgumentNullException(nameof(host));
      _isLocalTarget = isLocalTarget;
   }

   /// <summary>The name to use: what the person stated, else the documented default (`Edam.Sample`).</summary>
   public String Name
   {
      get
      {
         var state = ProjectSettings.ReadItem(
            _user.Effective, ConfigurableItems.DEFAULT_PROJECT_NAME_KEY);

         return String.IsNullOrWhiteSpace(state.Value)
            ? ConfigurableItems.DEFAULT_PROJECT_NAME
            : state.Value!;
      }
   }

   /// <summary>
   /// The gates that need no I/O: the binding is local, the switch is not off, and the marker has never
   /// been written. When this is false the host must not offer the default project either — there is no
   /// point asking for a name that would never be used.
   /// </summary>
   public Boolean IsApplicable
   {
      get
      {
         if (!_isLocalTarget)
         {
            return false;   // ADR-0012: a shared/remote collection is never given a starter project
         }

         if (ReadSwitch() == false)
         {
            return false;
         }

         return ProjectSettings.ReadItem(
            _user.Effective, ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY)
               .State == ConfigurationState.Unset;
      }
   }

   /// <summary>True when the marker records that the starter project has been handled already.</summary>
   public Boolean WasAlreadyHandled =>
      ProjectSettings.ReadItem(_user.Effective, ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY)
         .State == ConfigurationState.Set;

   /// <summary>
   /// Attempt the creation. The marker is written <b>only</b> after the project really exists, so a
   /// failure keeps the question pending and it is asked again (ADR-0013 decision 12).
   /// </summary>
   public async Task<DefaultProjectOutcome> TryCreateAsync(CancellationToken ct = default)
   {
      var name = Name;

      if (!IsApplicable)
      {
         return new DefaultProjectOutcome(false, false, name);
      }

      var problem = ConfigurableItems.ValidateProjectName(name);
      if (problem is not null)
      {
         return new DefaultProjectOutcome(false, true, name, problem);
      }

      if (!await _host.IsContainerEmptyAsync(ct).ConfigureAwait(false))
      {
         return new DefaultProjectOutcome(
            false, true, name, "the collection already holds projects");
      }

      var created = await _host.CreateProjectAsync(name, ct).ConfigureAwait(false);
      if (!created)
      {
         return new DefaultProjectOutcome(
            false, true, name, "the project could not be created in the collection");
      }

      // the action happened: only now is the question COMPLETE, which is what stops the asking
      _user.MarkStarterProjectOffered();

      return new DefaultProjectOutcome(true, true, name);
   }

   private Boolean? ReadSwitch()
   {
      var state = ProjectSettings.ReadItem(_user.Effective, CREATE_SWITCH_KEY);
      if (state.State != ConfigurationState.Set)
      {
         return null;   // stated nowhere: allowed (the binding already has to be local)
      }

      return Boolean.TryParse(state.Value, out var value) ? value : null;
   }
}
