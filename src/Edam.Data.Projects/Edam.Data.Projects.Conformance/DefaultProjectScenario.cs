using Edam.Data.Projects.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Edam.Data.Projects.Conformance;

/// <summary>
/// <b>DP-3 (ADR-0012 + ADR-0013 decision 12)</b> checks the <b>gated default project</b>: the gates (a
/// local binding, the switch, never-twice, an empty collection), the name (the answer, else the documented
/// default, always validated) and the rule that matters — <b>the marker is the proof of the ACTION</b>: it
/// is written only once the project really exists, so a failure leaves the question <b>pending</b> and asks
/// again, while success ends the asking.
/// </summary>
public static class DefaultProjectScenario
{
   /// <summary>A host we control: remembers what it was asked, and fails on demand.</summary>
   private sealed class StubHost : IDefaultProjectHost
   {
      public bool Empty { get; set; } = true;
      public bool Succeeds { get; set; } = true;
      public List<string> Created { get; } = new();

      public Task<bool> IsContainerEmptyAsync(CancellationToken ct) => Task.FromResult(Empty);

      public Task<bool> CreateProjectAsync(string name, CancellationToken ct)
      {
         if (!Succeeds)
         {
            return Task.FromResult(false);
         }

         Created.Add(name);
         return Task.FromResult(true);
      }
   }

   private static UserConfiguration Build(
      string root, string name, params (string Key, string Value)[] values)
   {
      var config = new ConfigurationBuilder()
         .AddInMemoryCollection(values.Select(v =>
            new KeyValuePair<string, string?>(v.Key, v.Value)))
         .Build();

      return new UserConfiguration(
         config,
         new FileStateStore(Path.Combine(root, name)),
         new InMemoryStateStore("(packaged)"));
   }

   public static async Task<List<ProjectScenario.Check>> Run(string root)
   {
      var checks = new List<ProjectScenario.Check>();
      void Check(string name, bool passed, string detail)
         => checks.Add(new ProjectScenario.Check(name, passed, detail));

      Directory.CreateDirectory(root);

      var createSwitch = DefaultProjectService.CREATE_SWITCH_KEY;
      var markerKey = ConfigurableItems.DEFAULT_PROJECT_MARKER_KEY;
      var nameKey = ConfigurableItems.DEFAULT_PROJECT_NAME_KEY;

      // ---- the gates that need no I/O ---------------------------------------------------------------
      Check("A localhost/'.'-style catalog counts as a LOCAL target (so the starter project is offered there too)",
         LocalTarget.IsLocalServer("Host=localhost;Port=5432;Database=edam") &&
         LocalTarget.IsLocalServer("Server=.;Database=edam;Integrated Security=True") &&
         LocalTarget.IsLocalServer(@"Server=.\SQLEXPRESS;Database=edam") &&
         LocalTarget.IsLocalServer("Server=(local);Database=edam") &&
         LocalTarget.IsLocalServer("Server=localhost,1433;Database=edam"),
         "localhost | . | (local) | .\\INSTANCE | host,port all count as local");

      Check("...while a REMOTE server does not, so a shared collection never gets a starter project",
         !LocalTarget.IsLocalServer("Server=edam-db.corp.example.com;Database=edam") &&
         !LocalTarget.IsLocalServer("Host=10.20.30.40;Port=5432;Database=edam"),
         "a remote host is refused");

      var remote = Build(root, "remote", (nameKey, "Edam.Sample"));
      var remoteOutcome = new DefaultProjectService(remote, new StubHost(), isLocalTarget: false);
      Check("A NON-LOCAL binding never offers or creates the starter project",
         !remoteOutcome.IsApplicable &&
         !(await remoteOutcome.TryCreateAsync()).Created,
         $"applicable={remoteOutcome.IsApplicable}");

      var switchedOff = new DefaultProjectService(
         Build(root, "off", (nameKey, "Edam.Sample"), (createSwitch, "false")),
         new StubHost(), isLocalTarget: true);
      Check("The switch being off stops it, so an installation can opt out",
         !switchedOff.IsApplicable,
         $"applicable={switchedOff.IsApplicable}");

      var handled = new DefaultProjectService(
         Build(root, "handled", (nameKey, "Edam.Sample"), (markerKey, "true")),
         new StubHost(), isLocalTarget: true);
      Check("Once HANDLED (the marker is set), it is NEVER offered or created again — a deleted project stays deleted",
         !handled.IsApplicable && handled.WasAlreadyHandled,
         $"applicable={handled.IsApplicable} handled={handled.WasAlreadyHandled}");

      // ---- the name --------------------------------------------------------------------------------
      var own = Build(root, "own", (nameKey, "My.Own.Name"));
      var ownHost = new StubHost();
      var ownOutcome = await new DefaultProjectService(own, ownHost, true).TryCreateAsync();
      Check("The name the person ANSWERED is the one created (not the default)",
         ownOutcome.Created && ownHost.Created.Count == 1 && ownHost.Created[0] == "My.Own.Name",
         string.Join(", ", ownHost.Created));

      var dflt = Build(root, "default", (nameKey, ""));
      var dfltHost = new StubHost();
      var dfltOutcome = await new DefaultProjectService(dflt, dfltHost, true).TryCreateAsync();
      Check("With nothing usable stated (an empty value), the documented default Edam.Sample is used",
         dfltOutcome.Created && dfltHost.Created.Count == 1 &&
         dfltHost.Created[0] == ConfigurableItems.DEFAULT_PROJECT_NAME,
         dfltOutcome.Problem ?? "created " + string.Join(", ", dfltHost.Created));

      var bad = Build(root, "bad", (nameKey, "../Templates"));
      var badHost = new StubHost();
      var badOutcome = await new DefaultProjectService(bad, badHost, true).TryCreateAsync();
      Check("An unusable name is REFUSED before anything is created (the CF-5 rules, reused)",
         !badOutcome.Created && badHost.Created.Count == 0 &&
         !string.IsNullOrWhiteSpace(badOutcome.Problem),
         $"{badOutcome.Problem} created={badHost.Created.Count}");

      var full = Build(root, "full", (nameKey, "Edam.Sample"));
      var fullHost = new StubHost { Empty = false };
      var fullOutcome = await new DefaultProjectService(full, fullHost, true).TryCreateAsync();
      Check("A collection that already holds projects is left alone",
         !fullOutcome.Created && fullHost.Created.Count == 0,
         $"{fullOutcome.Problem} created={fullHost.Created.Count}");

      // ---- the marker is the proof of the ACTION ---------------------------------------------------
      var good = Build(root, "good", (nameKey, "Edam.Sample"));
      var goodHost = new StubHost();
      var goodService = new DefaultProjectService(good, goodHost, true);

      Check("Before the action, the question is open (the batch asks for the name)",
         good.ToAsk().Count == 1 &&
         good.ToAsk()[0].Item.Id == ConfigurableItems.DEFAULT_PROJECT_NAME_KEY,
         string.Join(", ", good.ToAsk().Select(a => a.Item.Id)));

      var goodOutcome = await goodService.TryCreateAsync();

      Check("SUCCESS creates the project and writes the marker (the action really happened)",
         goodOutcome.Created &&
         good.Installation.Read(markerKey) == "true" &&
         good.Overlay.Read(markerKey) is null,
         $"created={goodOutcome.Created} marker='{good.Installation.Read(markerKey)}'");

      Check("...and only then does the question STOP being asked (the answer is complete)",
         good.ToAsk().Count == 0,
         string.Join(", ", good.ToAsk().Select(a => a.Item.Id)));

      var failing = Build(root, "failing", (nameKey, "Edam.Sample"));
      var failingOutcome = await new DefaultProjectService(
         failing, new StubHost { Succeeds = false }, true).TryCreateAsync();

      Check("FAILURE creates nothing and writes NO marker — so the answer is NOT treated as complete",
         !failingOutcome.Created &&
         !string.IsNullOrWhiteSpace(failingOutcome.Problem) &&
         failing.Installation.Read(markerKey) is null,
         $"{failingOutcome.Problem} marker='{failing.Installation.Read(markerKey)}'");

      Check("...and the question is asked AGAIN (it keeps asking until the action is completed)",
         failing.ToAsk().Count == 1 &&
         failing.ToAsk()[0].Current == "Edam.Sample",
         failing.ToAsk().Count == 0
            ? "asked nothing (WRONG: nothing was created)"
            : "asked again, pre-filled '" + failing.ToAsk()[0].Current + "'");

      return checks;
   }
}
