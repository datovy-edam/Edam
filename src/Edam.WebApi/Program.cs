using Edam.Services.Contracts;
using Edam.Services.Core;
using Edam.WebApi;

var builder = WebApplication.CreateBuilder(args);

// BL-6.1/6.7: ServiceDefaults — OpenTelemetry (traces/metrics/logs), health checks,
// Polly resilience, service discovery.
builder.AddServiceDefaults();

// BL-6.2: onboarded Wave-1 surfaces, DI-composed behind interfaces.
// BL-7.5: with configuration, the catalog/asset boundary resolves the real catalog platform
// store (PostgreSQL/FileSystem behind DI) instead of the in-memory stand-in.
builder.Services.AddWave1Services(builder.Configuration);
builder.Services.AddHttpContextAccessor();

// BL-6.4: per-service health checks (aggregated into the /health ready probe + Aspire dashboard).
builder.Services.AddHealthChecks()
    .AddCheck<WaveOneServiceHealthCheck<ICatalogService>>("edam-data-catalog", tags: new[] { "wave1" })
    .AddCheck<WaveOneServiceHealthCheck<IBookletMappingService>>("edam-booklet-mapping", tags: new[] { "wave1" })
    .AddCheck<WaveOneServiceHealthCheck<IVocabularyService>>("edam-vocabulary-lexicon", tags: new[] { "wave1" });

var app = builder.Build();

// BL-6.6: expose the catalog persistence boundary — the "basic catalog/asset op callable"
// from BL-6.3, served from the active store (in-memory baseline; Postgres when configured/up).
app.MapGet("/catalog/items", async (
    HttpContext http,
    ICatalogStore store) =>
        new
        {
            Store = store.DescribeStore(),
            Tracked = await store.GetAssetsAsync(),
        });
// BL-6.7: correlation-ID propagation over the mesh, before routing.
app.UseMiddleware<CorrelationIdMiddleware>();

// Map /health (ready) + /alive (liveness) from ServiceDefaults.
app.MapDefaultEndpoints();
app.MapGet("/", () => "EDAM Web API - Wave-1 observability baseline");

// BL-6.4: environment overview/operational heartbeat — structured report of every Wave-1
// service (name/version/kind/status/health + last-seen) plus the overall verdict.
// Always available (unlike /health which the framework gates to Development); this is the aggregate
// "is the ecosystem healthy?" answer for the dashboard.
app.MapGet("/health/report", (
    ICatalogService catalog,
    IBookletMappingService booklet,
    IVocabularyService vocab) =>
{
    var services = new[] { catalog.Describe(), booklet.Describe(), vocab.Describe() };
    var overall = services.All(s => s.Health == "healthy")
        ? "healthy" : services.Any(s => s.Health == "degraded")
            ? "degraded" : "unhealthy";
    return new
    {
        Overall = overall,
        Timestamp = DateTimeOffset.UtcNow,
        Services = services
    };
});

// BL-6.2/6.4/6.7: expose the onboarded surfaces + their health descriptors (mesh-observable),
// along with the caller-visible correlation id and the OTel trace id for cross-checking.
app.MapGet("/wave1", (
    HttpContext http,
    ICatalogService catalog,
    IBookletMappingService booklet,
    IVocabularyService vocab) =>
        new
        {
            CorrelationId = http.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString(),
            TraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(),
            Services = new[]
            {
                catalog.Describe(), booklet.Describe(), vocab.Describe()
            }
        });

// BL-4.3: governance runtime — typed decision per readiness/risk, immutably audited.
app.MapGet("/governance/decision", (
    HttpContext http,
    IGovernanceEngine engine,
    IAuditLog audit) =>
{
    var q = http.Request.Query;
    Enum.TryParse<RiskTier>(q["risk"], ignoreCase: true, out var risk);
    Enum.TryParse<ReadinessTier>(q["readiness"], ignoreCase: true, out var readiness);
    var resource = q["resource"].ToString() ?? "unknown";
    var requestedBy = q["requestedBy"].ToString();

    var result = engine.GetDecision(new GovernanceRequest(resource, readiness, risk, requestedBy));
    var entry = audit.Append(
        string.IsNullOrEmpty(requestedBy) ? "anonymous" : requestedBy, "caller",
        "GetDecision", result.Decision.ToString(), result.Reason, resource,
        "pending", result.Decision.ToString());

    return new
    {
        result.Decision, result.Reason, result.Readiness, result.Risk,
        AuditId = entry.Id
    };
});

// BL-4.3: immutable audit log + integrity verification (append-only, tamper-evident).
app.MapGet("/audit", (IAuditLog audit) => new
{
    Verified = audit.VerifyIntegrity(),
    Count = audit.Read().Count,
    Entries = audit.Read()
});

app.Run();
