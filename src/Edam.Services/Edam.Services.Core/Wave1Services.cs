using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Edam.Services.Contracts;

namespace Edam.Services.Core;

/// <summary>
/// BL-6.2: in-memory Wave-1 service implementations (onboarded surfaces behind interfaces).
/// BL-6.7: every surface logs through MEL (injected <see cref="ILogger{T}"/>), so it flows to
/// the OTel pipeline the host configures (ServiceDefaults). Deep data operations build on these
/// once persistence lands (BL-6.6) — the same DI composition is used either way.
/// </summary>
public sealed class InMemoryCatalogService : ICatalogService
{
    private readonly ILogger<InMemoryCatalogService> _log;
    public InMemoryCatalogService(ILogger<InMemoryCatalogService> log) => _log = log;

    public Wave1ServiceInfo Describe()
    {
        _log.LogInformation("Describe {Kind} '{Name}' v{Version} trace={Trace}",
            ServiceKind.HostedService, "edam-data-catalog", "1.0.0",
            Activity.Current?.TraceId.ToString() ?? "-");
        return new("edam-data-catalog", "1.0.0", ServiceKind.HostedService,
            "running", "healthy", "/catalog");
    }
}

public sealed class InMemoryBookletMappingService : IBookletMappingService
{
    private readonly ILogger<InMemoryBookletMappingService> _log;
    public InMemoryBookletMappingService(ILogger<InMemoryBookletMappingService> log) => _log = log;

    public Wave1ServiceInfo Describe()
    {
        _log.LogInformation("Describe {Kind} '{Name}' v{Version} trace={Trace}",
            ServiceKind.InProcessComponent, "edam-booklet-mapping", "1.0.0",
            Activity.Current?.TraceId.ToString() ?? "-");
        return new("edam-booklet-mapping", "1.0.0", ServiceKind.InProcessComponent,
            "running", "healthy");
    }
}

public sealed class InMemoryVocabularyService : IVocabularyService
{
    private readonly ILogger<InMemoryVocabularyService> _log;
    public InMemoryVocabularyService(ILogger<InMemoryVocabularyService> log) => _log = log;

    public Wave1ServiceInfo Describe()
    {
        _log.LogInformation("Describe {Kind} '{Name}' v{Version} trace={Trace}",
            ServiceKind.InProcessComponent, "edam-vocabulary-lexicon", "1.0.0",
            Activity.Current?.TraceId.ToString() ?? "-");
        return new("edam-vocabulary-lexicon", "1.0.0", ServiceKind.InProcessComponent,
            "running", "healthy");
    }
}

/// <summary>Registers the Wave-1 onboarded surfaces behind their DI-composed interfaces.</summary>
public static class Wave1Services
{
    public static IServiceCollection AddWave1Services(this IServiceCollection services)
    {
        return services
            .AddSingleton<ICatalogService, InMemoryCatalogService>()
            .AddSingleton<IBookletMappingService, InMemoryBookletMappingService>()
            .AddSingleton<IVocabularyService, InMemoryVocabularyService>()
            // BL-6.6/BL-7.5: catalog store is DI-selected by config.
            //   - postgres  (or a "catalog" connection string) -> PostgresCatalogStore
            //   - filesystem (or an Edam:CatalogRoot / DefaultRootFileFolder) -> FileSystemCatalogStore (FileSystem target)
            //   - otherwise -> in-memory (Wave-1 baseline / dev stand-in)
            .AddSingleton<ICatalogStore>(sp =>
            {
                var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                var mode = config["Edam:CatalogStore"]?.ToLowerInvariant();
                var hasRoot = !string.IsNullOrWhiteSpace(config["Edam:CatalogRoot"])
                           || !string.IsNullOrWhiteSpace(config["DefaultRootFileFolder"]);
                if (mode == "postgres" || !string.IsNullOrEmpty(config["ConnectionStrings:catalog"]))
                    return ActivatorUtilities.CreateInstance<PostgresCatalogStore>(sp);
                return hasRoot || mode == "filesystem"
                    ? ActivatorUtilities.CreateInstance<FileSystemCatalogStore>(sp)
                    : ActivatorUtilities.CreateInstance<InMemoryCatalogStore>(sp);
            })
        // BL-4.3: governance runtime (engine + approval gate + immutable audit log + conformance registry).
        .AddSingleton<IGovernanceEngine, GovernanceEngine>()
        .AddSingleton<IAuditLog, InMemoryAuditLog>()
        .AddSingleton<IApprovalGate, InMemoryApprovalGate>()
        .AddSingleton<IConformanceRegistry, InMemoryConformanceRegistry>();
    }
}
