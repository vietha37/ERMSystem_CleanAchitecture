using System.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace ERMSystem.API.Services;

public class RequestObservabilityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestObservabilityMiddleware> _logger;
    private readonly RequestObservabilityOptions _options;
    private readonly ApiMetricsCollector _metricsCollector;

    public RequestObservabilityMiddleware(
        RequestDelegate next,
        ILogger<RequestObservabilityMiddleware> logger,
        IOptions<RequestObservabilityOptions> options,
        ApiMetricsCollector metricsCollector)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _metricsCollector = metricsCollector;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationHeaderName = string.IsNullOrWhiteSpace(_options.CorrelationHeaderName)
            ? "X-Correlation-Id"
            : _options.CorrelationHeaderName.Trim();

        var correlationId = ResolveCorrelationId(context, correlationHeaderName);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[correlationHeaderName] = correlationId;
        Activity.Current?.SetTag("ermsystem.correlation_id", correlationId);

        var stopwatch = Stopwatch.StartNew();
        var shouldCollectMetrics = ShouldCollectMetrics(context.Request.Path);

        if (shouldCollectMetrics)
        {
            _metricsCollector.OnRequestStarted();
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.Value,
            ["RequestMethod"] = context.Request.Method
        });

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;
            var logLevel = ResolveLogLevel(statusCode, elapsedMs);
            var routeLabel = ResolveRouteLabel(context);
            var isSlowRequest = elapsedMs >= _options.SlowRequestThresholdMs;
            var activity = Activity.Current;

            activity?.SetTag("ermsystem.route", routeLabel);
            activity?.SetTag("ermsystem.slow_request", isSlowRequest);
            activity?.SetTag("ermsystem.elapsed_ms", elapsedMs);

            if (shouldCollectMetrics)
            {
                _metricsCollector.OnRequestCompleted(
                    context.Request.Method,
                    routeLabel,
                    statusCode,
                    elapsedMs,
                    isSlowRequest);
            }

            _logger.Log(
                logLevel,
                "HTTP {HttpMethod} {RequestPath} responded {ResponseStatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                elapsedMs);
        }
    }

    private static string ResolveCorrelationId(HttpContext context, string headerName)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var headerValue))
        {
            var candidate = headerValue.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }

    private static bool ShouldCollectMetrics(PathString path)
    {
        return !path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveRouteLabel(HttpContext context)
    {
        if (context.GetEndpoint() is RouteEndpoint routeEndpoint)
        {
            return routeEndpoint.RoutePattern.RawText ?? context.Request.Path.Value ?? "/";
        }

        return context.Request.Path.Value ?? "/";
    }

    private LogLevel ResolveLogLevel(int statusCode, long elapsedMs)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogLevel.Error;
        }

        if (elapsedMs >= _options.SlowRequestThresholdMs)
        {
            return LogLevel.Warning;
        }

        return LogLevel.Information;
    }
}
