using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Edam.Services.Contracts;
using Edam.Data.Catalog.DependencyInjection;
using PlatformCatalogStore = Edam.Data.Catalog.Contracts.ICatalogStore;

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
    /// <summary>
    /// Registers the Wave-1 surfaces with no host configuration — the catalog/asset boundary
    /// resolves to the in-memory fallback. Used by shells that only read the service descriptors
    /// (e.g. the CLI's <c>edam wave1</c>).
    /// </summary>
    public static IServiceCollection AddWave1Services(this IServiceCollection services)
        => services.AddWave1Services(null);

    /// <summary>
    /// BL-7.5 seam swap: with configuration, the catalog/asset persistence boundary resolves from the
    /// <b>real catalog platform</b> (<c>Edam.Data.Catalog</c> — PostgreSQL/FileSystem behind DI,
    /// BL-7.2/BL-7.4) instead of a self-contained stand-in. The in-memory store remains only the
    /// registered <b>fallback</b> (active when no catalog target/connection/root is configured).
    /// </summary>
    public static IServiceCollection AddWave1Services(this IServiceCollection services, IConfiguration? config)
    {
        var platform = PlatformCatalogConfiguration(config);
        if (platform is not null)
            services.AddCatalogServices(platform);

        return services
            .AddSingleton<ICatalogService, InMemoryCatalogService>()
            .AddSingleton<IBookletMappingService, InMemoryBookletMappingService>()
            .AddSingleton<IVocabularyService, InMemoryVocabularyService>()
            // BL-6.6/BL-7.5: catalog/asset persistence boundary. Real platform store when a target
            // resolves; otherwise the in-memory Wave-1 baseline keeps the mesh up.
            .AddSingleton<ICatalogStore>(sp =>
            {
                var store = sp.GetService<PlatformCatalogStore>();
                if (store is not null)
                    return new CatalogPlatformStore(store, sp.GetService<ILogger<CatalogPlatformStore>>());

                sp.GetService<ILogger<CatalogPlatformStore>>()?.LogWarning(
                    "No catalog platform target resolved (Edam:Catalog:Target / ConnectionStrings:catalog / "
                    + "Edam:CatalogRoot); using the in-memory catalog fallback");
                return ActivatorUtilities.CreateInstance<InMemoryCatalogStore>(sp);
            })
            // BL-4.3: governance runtime (engine + approval gate + immutable audit log + conformance registry).
            .AddSingleton<IGovernanceEngine, GovernanceEngine>()
            .AddSingleton<IAuditLog, InMemoryAuditLog>()
            .AddSingleton<IApprovalGate, InMemoryApprovalGate>()
            .AddSingleton<IConformanceRegistry, InMemoryConformanceRegistry>();
    }

    /// <summary>
    /// Maps the Wave-1 catalog configuration keys onto the platform composition-root keys
    /// (<c>Edam:Catalog:Target</c>, <c>ConnectionStrings:catalog</c>, <c>Edam:Catalog:FileSystemRoot</c>)
    /// so hosts keep their existing configuration while the store comes from the platform.
    /// Returns <c>null</c> when nothing selects a target (→ in-memory fallback).
    /// </summary>
    private static Dictionary<string, string>? PlatformCatalogConfiguration(IConfiguration? config)
    {
        if (config is null) return null;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var connection = config["ConnectionStrings:catalog"];
        var root = config["Edam:CatalogRoot"] ?? config["DefaultRootFileFolder"];
        var mode = config["Edam:CatalogStore"]?.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(connection)) map["ConnectionStrings:catalog"] = connection;
        if (!string.IsNullOrWhiteSpace(root)) map["Edam:Catalog:FileSystemRoot"] = root;

        var target = config["Edam:Catalog:Target"];
        if (string.IsNullOrWhiteSpace(target))
        {
            if (mode == "postgres" || !string.IsNullOrWhiteSpace(connection)) target = "postgres";
            else if (mode == "filesystem" || !string.IsNullOrWhiteSpace(root)) target = "filesystem";
        }
        if (!string.IsNullOrWhiteSpace(target)) map["Edam:Catalog:Target"] = target;

        return map.Count == 0 ? null : map;
    }
}
