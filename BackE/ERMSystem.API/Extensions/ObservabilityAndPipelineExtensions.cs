using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
using ERMSystem.API.HealthChecks;
using ERMSystem.API.Services;
using ERMSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ERMSystem.API.Extensions;

/// <summary>
/// Các phương thức mở rộng cấu hình Giám sát (Observability), Rate Limiting và các Endpoint chẩn đoán sức khỏe hệ thống.
/// </summary>
public static class ObservabilityAndPipelineExtensions
{
    public static IServiceCollection AddApiObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        OpenTelemetryTracingOptions openTelemetryTracingOptions)
    {
        if (openTelemetryTracingOptions.Enabled)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(
                    serviceName: string.IsNullOrWhiteSpace(openTelemetryTracingOptions.ServiceName)
                        ? "ERMSystem.API"
                        : openTelemetryTracingOptions.ServiceName.Trim(),
                    serviceVersion: string.IsNullOrWhiteSpace(openTelemetryTracingOptions.ServiceVersion)
                        ? "1.0.0"
                        : openTelemetryTracingOptions.ServiceVersion.Trim()))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(ErmTelemetry.ActivitySourceName)
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.Filter = httpContext =>
                                !httpContext.Request.Path.StartsWithSegments("/metrics", StringComparison.OrdinalIgnoreCase);
                        })
                        .AddHttpClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation();

                    if (openTelemetryTracingOptions.UseConsoleExporter)
                    {
                        tracing.AddConsoleExporter();
                    }

                    if (!string.IsNullOrWhiteSpace(openTelemetryTracingOptions.OtlpEndpoint))
                    {
                        tracing.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(openTelemetryTracingOptions.OtlpEndpoint.Trim());
                        });
                    }
                });
        }

        return services;
    }

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authPermitLimit = configuration.GetValue<int?>("Security:AuthRateLimit:PermitLimit") ?? 12;
        var authWindowSeconds = configuration.GetValue<int?>("Security:AuthRateLimit:WindowSeconds") ?? 60;
        var aiChatPermitLimit = configuration.GetValue<int?>("AiSymptomChat:RateLimit:PermitLimit") ?? 8;
        var aiChatWindowSeconds = configuration.GetValue<int?>("AiSymptomChat:RateLimit:WindowSeconds") ?? 60;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(
                    ApiErrorResponseFactory.Create(
                        context.HttpContext,
                        "rate_limit_exceeded",
                        "Too many requests. Please retry later."));

                await context.HttpContext.Response.WriteAsync(payload, token);
            };

            options.AddPolicy("auth-fixed-window", httpContext =>
            {
                var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: remoteIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authPermitLimit,
                        Window = TimeSpan.FromSeconds(authWindowSeconds),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });

            options.AddPolicy("ai-chat-fixed-window", httpContext =>
            {
                var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: remoteIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = aiChatPermitLimit,
                        Window = TimeSpan.FromSeconds(aiChatWindowSeconds),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    public static IEndpointRouteBuilder MapObservabilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () => Results.Ok(new
        {
            status = "ok",
            service = "ERMSystem.API",
            utcNow = DateTime.UtcNow
        })).AllowAnonymous();

        endpoints.MapGet("/health/alerts", async (OperationalAlertEvaluator evaluator, CancellationToken ct) =>
        {
            var snapshot = await evaluator.EvaluateAsync(ct);
            return Results.Ok(snapshot);
        }).AllowAnonymous();

        endpoints.MapGet("/metrics", async (
            ApiMetricsCollector collector,
            DashboardCacheMetricsRegistry dashboardCacheMetricsRegistry,
            NotificationPipelineMetricsReader notificationPipelineMetricsReader,
            BackgroundWorkerHealthRegistry backgroundWorkerHealthRegistry,
            CancellationToken ct) =>
        {
            var payload = collector.RenderPrometheus();
            var builder = new StringBuilder(payload, payload.Length + 2048);

            builder.AppendLine("# HELP ermsystem_dashboard_cache_hits_total Dashboard cache hits by segment.");
            builder.AppendLine("# TYPE ermsystem_dashboard_cache_hits_total counter");
            builder.AppendLine("# HELP ermsystem_dashboard_cache_misses_total Dashboard cache misses by segment.");
            builder.AppendLine("# TYPE ermsystem_dashboard_cache_misses_total counter");
            builder.AppendLine("# HELP ermsystem_dashboard_cache_writes_total Dashboard cache writes by segment.");
            builder.AppendLine("# TYPE ermsystem_dashboard_cache_writes_total counter");
            builder.AppendLine("# HELP ermsystem_dashboard_cache_invalidations_total Dashboard cache invalidations.");
            builder.AppendLine("# TYPE ermsystem_dashboard_cache_invalidations_total counter");

            foreach (var snapshot in dashboardCacheMetricsRegistry.GetSnapshots())
            {
                var labels = $"{{segment=\"{snapshot.CacheSegment}\"}}";
                builder.Append("ermsystem_dashboard_cache_hits_total").Append(labels).Append(' ').Append(snapshot.Hits).AppendLine();
                builder.Append("ermsystem_dashboard_cache_misses_total").Append(labels).Append(' ').Append(snapshot.Misses).AppendLine();
                builder.Append("ermsystem_dashboard_cache_writes_total").Append(labels).Append(' ').Append(snapshot.Writes).AppendLine();
                builder.Append("ermsystem_dashboard_cache_invalidations_total").Append(labels).Append(' ').Append(snapshot.Invalidations).AppendLine();
            }

            var pipelineMetrics = await notificationPipelineMetricsReader.GetSnapshotAsync(ct);
            builder.AppendLine("# HELP ermsystem_notification_outbox_pending_total Pending outbox messages awaiting publish.");
            builder.AppendLine("# TYPE ermsystem_notification_outbox_pending_total gauge");
            builder.Append("ermsystem_notification_outbox_pending_total ").Append(pipelineMetrics.PendingOutboxCount).AppendLine();
            builder.AppendLine("# HELP ermsystem_notification_outbox_oldest_age_seconds Age in seconds of the oldest pending outbox message.");
            builder.AppendLine("# TYPE ermsystem_notification_outbox_oldest_age_seconds gauge");
            builder.Append("ermsystem_notification_outbox_oldest_age_seconds ").Append(FormatAgeSeconds(pipelineMetrics.GeneratedAtUtc, pipelineMetrics.OldestPendingOutboxAtUtc)).AppendLine();
            builder.AppendLine("# HELP ermsystem_notification_deliveries_queued_total Queued notification deliveries awaiting dispatch.");
            builder.AppendLine("# TYPE ermsystem_notification_deliveries_queued_total gauge");
            builder.Append("ermsystem_notification_deliveries_queued_total ").Append(pipelineMetrics.QueuedDeliveryCount).AppendLine();
            builder.AppendLine("# HELP ermsystem_notification_deliveries_stale_total Queued notification deliveries older than 15 minutes.");
            builder.AppendLine("# TYPE ermsystem_notification_deliveries_stale_total gauge");
            builder.Append("ermsystem_notification_deliveries_stale_total ").Append(pipelineMetrics.StaleQueuedDeliveryCount).AppendLine();
            builder.AppendLine("# HELP ermsystem_notification_deliveries_oldest_age_seconds Age in seconds of the oldest queued notification delivery.");
            builder.AppendLine("# TYPE ermsystem_notification_deliveries_oldest_age_seconds gauge");
            builder.Append("ermsystem_notification_deliveries_oldest_age_seconds ").Append(FormatAgeSeconds(pipelineMetrics.GeneratedAtUtc, pipelineMetrics.OldestQueuedDeliveryAtUtc)).AppendLine();

            builder.AppendLine("# HELP ermsystem_background_worker_heartbeat_age_seconds Seconds since the last worker heartbeat.");
            builder.AppendLine("# TYPE ermsystem_background_worker_heartbeat_age_seconds gauge");
            builder.AppendLine("# HELP ermsystem_background_worker_status Worker health status encoded as 1 for the current state label.");
            builder.AppendLine("# TYPE ermsystem_background_worker_status gauge");

            var knownStatuses = new[] { "Starting", "Healthy", "Degraded", "Unhealthy", "Unknown" };
            foreach (var snapshot in backgroundWorkerHealthRegistry.GetAll())
            {
                builder
                    .Append("ermsystem_background_worker_heartbeat_age_seconds{worker=\"")
                    .Append(snapshot.WorkerName)
                    .Append("\"} ")
                    .Append(FormatAgeSeconds(DateTime.UtcNow, snapshot.LastHeartbeatUtc))
                    .AppendLine();

                foreach (var status in knownStatuses)
                {
                    builder
                        .Append("ermsystem_background_worker_status{worker=\"")
                        .Append(snapshot.WorkerName)
                        .Append("\",status=\"")
                        .Append(status)
                        .Append("\"} ")
                        .Append(string.Equals(snapshot.Status, status, StringComparison.OrdinalIgnoreCase) ? "1" : "0")
                        .AppendLine();
                }
            }

            return Results.Text(
                builder.ToString(),
                "text/plain; version=0.0.4; charset=utf-8");
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var payload = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.ToDictionary(
                        entry => entry.Key,
                        entry => new
                        {
                            status = entry.Value.Status.ToString(),
                            description = entry.Value.Description,
                            data = entry.Value.Data
                        })
                });

                await context.Response.WriteAsync(payload);
            }
        }).AllowAnonymous();

        return endpoints;
    }

    public static string FormatAgeSeconds(DateTime nowUtc, DateTime? value)
    {
        if (!value.HasValue)
        {
            return "0";
        }

        return Math.Max(0, Math.Round((nowUtc - value.Value).TotalSeconds, 0))
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
