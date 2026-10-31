using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Edam.Data.Projects.DependencyInjection;
using Edam.Services.Contracts;
using Edam.Services.Core;
using Edam.Settings.Contracts;
using Edam.Settings.Core;

// BL-6.3: thin CLI shell driving the Wave-1 core from a terminal (UI-agnostic).
// One avenue for Python scripting (BL-6.5) builds on this boundary.
var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole());
services.AddWave1Services();
using var sp = services.BuildServiceProvider();

string[] argsArgs = Environment.GetCommandLineArgs().Length > 1
    ? Environment.GetCommandLineArgs().Skip(1).ToArray() : Array.Empty<string>();
string cmd = argsArgs.Length > 0 ? argsArgs[0].ToLowerInvariant() : "wave1";

switch (cmd)
{
    case "wave1":
    case "list":
        foreach (var s in new IWave1Service[]
        {
            sp.GetRequiredService<ICatalogService>(),
            sp.GetRequiredService<IBookletMappingService>(),
            sp.GetRequiredService<IVocabularyService>()
        })
        {
            var d = s.Describe();
            Console.WriteLine($"{d.Kind,-16} {d.Name,-26} {d.Version,-8} {d.Status}/{d.Health}");
        }
        break;

    case "health":
        Console.WriteLine("edam-cli: healthy (Wave-1 shell up)");
        break;

    // ADR-0016: the settings surface, from a host with NO UI — proof that the feature is UI-agnostic rather
    // than WinUI-shaped. It reads and writes through the same catalog, editor and service as the desktop page.
    case "settings":
        return await SettingsAsync(argsArgs.Skip(1).ToArray());

    default:
        Console.WriteLine("Usage: edam <wave1|health|settings>");
        break;
}

return 0;

static async Task<Int32> SettingsAsync(String[] a)
{
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    // a caller may state the state root (Edam__Settings__StateRoot / EDAM…); the desktop-style fallback only
    // applies when nothing did, so a container or a test can point the CLI at its own folder
    var stateRoot = config[ProjectSettingsComposition.STATE_ROOT_KEY];
    var settings = ProjectSettingsComposition.Create(
        config, String.IsNullOrWhiteSpace(stateRoot) ? DefaultStateRoot() : stateRoot);
    var action = a.Length > 0 ? a[0].ToLowerInvariant() : "list";

    switch (action)
    {
        case "list":
            foreach (var group in settings.Catalog.Groups)
            {
                Console.WriteLine();
                Console.WriteLine(group);

                foreach (var item in settings.Catalog.InGroup(group))
                {
                    var value = item.Display ?? "(not set)";
                    var note = item.IsEditable
                        ? string.Empty
                        : "  [read-only: " + (item.Reason ?? "no reason given") + "]";

                    Console.WriteLine(
                        $"  {item.Title,-34} {value,-32} ({SettingsText.Source(item.Source)}){note}");
                }
            }
            return 0;

        case "get":
            if (a.Length < 2)
            {
                Console.WriteLine("Usage: edam settings get <id>");
                return 2;
            }

            var found = settings.Catalog.Find(a[1]);
            if (found is null)
            {
                Console.WriteLine($"'{a[1]}' is not a setting this host can see.");
                return 1;
            }

            Console.WriteLine(SettingsText.Describe(found));
            return 0;

        case "set":
            if (a.Length < 3)
            {
                Console.WriteLine("Usage: edam settings set <id> <value>");
                return 2;
            }

            return Report(await settings.SetAsync(a[1], String.Join(' ', a.Skip(2))));

        case "reset":
            if (a.Length < 2)
            {
                Console.WriteLine("Usage: edam settings reset <id|--all>");
                return 2;
            }

            if (String.Equals(a[1], "--all", StringComparison.OrdinalIgnoreCase))
            {
                var forgotten = await settings.ResetAllAsync();
                Console.WriteLine(
                    $"Forgot {forgotten} answer(s); recorded installation state was kept.");
                return 0;
            }

            return Report(await settings.ResetAsync(a[1]));

        default:
            Console.WriteLine(
                "Usage: edam settings <list|get <id>|set <id> <value>|reset <id|--all>>");
            return 2;
    }

    static Int32 Report(SettingsChangeResult result)
    {
        if (!result.Changed)
        {
            Console.WriteLine("Not changed: " + result.Problem);
            return 1;
        }

        // a stored value that could not be APPLIED is reported as such — never printed as success
        Console.WriteLine(result.Problem is null ? "Saved." : result.Problem);
        return 0;
    }
}

static String DefaultStateRoot() => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Edam", "Cli");
