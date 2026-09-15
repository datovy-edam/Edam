using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Edam.WebApi;

/// <summary>
/// BL-6.7: correlation ID propagation across the Wave-1 mesh.
/// Accepts an inbound <c>X-Correlation-ID</c> (or generates one), echoes it on the
/// response, tags the ambient OTel <see cref="Activity"/> with it, and logs a structured
/// request line (so the MEL/OTel logs carry the same correlation id a caller sees).
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger _log;

    public CorrelationIdMiddleware(RequestDelegate next, ILoggerFactory logFactory)
    {
        _next = next;
        _log = logFactory.CreateLogger(nameof(CorrelationIdMiddleware));
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var inbound = ctx.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(inbound)
            ? Guid.NewGuid().ToString("N")
            : inbound;

        ctx.Response.Headers[HeaderName] = correlationId;
        ctx.Items[HeaderName] = correlationId;

        if (Activity.Current is { } activity)
        {
            activity.SetTag("correlation_id", correlationId);
        }

        _log.LogInformation(
            "Request {Method} {Path} correlationId={CorrelationId} traceId={TraceId}",
            ctx.Request.Method, ctx.Request.Path, correlationId,
            Activity.Current?.TraceId.ToString() ?? "-");

        await _next(ctx);
    }
}
