using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Edam.Services.Contracts;
using Edam.Services.Core;

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

    default:
        Console.WriteLine("Usage: edam <wave1|health>");
        break;
}
