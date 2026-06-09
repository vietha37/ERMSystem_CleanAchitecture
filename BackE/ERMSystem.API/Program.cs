using ERMSystem.Application.Authorization;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Services;
using ERMSystem.API.HealthChecks;
using ERMSystem.API.Services;
using ERMSystem.Infrastructure.Repositories;
using ERMSystem.Infrastructure.Services;
using ERMSystem.Infrastructure.Messaging;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (OperatingSystem.IsWindows() && !builder.Environment.IsDevelopment())
{
    builder.Logging.AddEventLog();
}

var dataProtectionKeyPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeyPath);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));

builder.Services.Configure<RequestObservabilityOptions>(builder.Configuration.GetSection("Observability"));
builder.Services.Configure<OpenTelemetryTracingOptions>(builder.Configuration.GetSection("OpenTelemetry"));
builder.Services.Configure<OperationalAlertOptions>(builder.Configuration.GetSection("OperationalAlerts"));
builder.Services.Configure<HospitalPaymentGatewayOptions>(builder.Configuration.GetSection("PaymentGateway"));
builder.Services.Configure<TwoFactorAuthOptions>(builder.Configuration.GetSection("Security:TwoFactor"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<OutboxPublisherOptions>(builder.Configuration.GetSection("OutboxPublisher"));
builder.Services.Configure<NotificationConsumerOptions>(builder.Configuration.GetSection("NotificationConsumer"));
builder.Services.Configure<NotificationDispatchOptions>(builder.Configuration.GetSection("NotificationDispatch"));
builder.Services.Configure<RetentionCleanupOptions>(builder.Configuration.GetSection("RetentionCleanup"));
builder.Services.Configure<DashboardCacheOptions>(builder.Configuration.GetSection("DashboardCache"));
builder.Services.Configure<RevisitReminderOptions>(builder.Configuration.GetSection("RevisitReminder"));
builder.Services.Configure<SatisfactionSurveyOptions>(builder.Configuration.GetSection("SatisfactionSurvey"));
builder.Services.Configure<CustomerCareFollowUpOptions>(builder.Configuration.GetSection("CustomerCareFollowUp"));
builder.Services.Configure<DistributedCacheRuntimeOptions>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<HospitalDocumentStorageOptions>(builder.Configuration.GetSection("DocumentStorage"));
builder.Services.Configure<ExternalDrugKnowledgeOptions>(builder.Configuration.GetSection("ExternalDrugKnowledge"));
builder.Services.Configure<AiSymptomChatOptions>(builder.Configuration.GetSection("AiSymptomChat"));
builder.Services.AddSingleton<ApiMetricsCollector>();
builder.Services.AddSingleton<IBusinessMetricsRecorder, BusinessMetricsRecorder>();
builder.Services.AddSingleton<BackgroundWorkerHealthRegistry>();
builder.Services.AddSingleton<DashboardCacheMetricsRegistry>();
builder.Services.AddSingleton<NotificationPipelineMetricsReader>();
builder.Services.AddSingleton<OperationalAlertEvaluator>();
builder.Services.AddSingleton<OperationalAlertWebhookNotifier>();

var paymentGatewayOptions =
    builder.Configuration.GetSection("PaymentGateway").Get<HospitalPaymentGatewayOptions>()
    ?? new HospitalPaymentGatewayOptions();
ValidatePaymentGatewayOptions(paymentGatewayOptions);

var documentStorageOptions =
    builder.Configuration.GetSection("DocumentStorage").Get<HospitalDocumentStorageOptions>()
    ?? new HospitalDocumentStorageOptions();
ValidateDocumentStorageOptions(documentStorageOptions);

var externalDrugKnowledgeOptions =
    builder.Configuration.GetSection("ExternalDrugKnowledge").Get<ExternalDrugKnowledgeOptions>()
    ?? new ExternalDrugKnowledgeOptions();
ValidateExternalDrugKnowledgeOptions(externalDrugKnowledgeOptions);

var operationalAlertOptions =
    builder.Configuration.GetSection("OperationalAlerts").Get<OperationalAlertOptions>()
    ?? new OperationalAlertOptions();
ValidateOperationalAlertOptions(operationalAlertOptions);

builder.Services.AddHttpClient("operational-alert-webhook", (serviceProvider, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, operationalAlertOptions.Webhook.TimeoutSeconds));
});

var openTelemetryTracingOptions =
    builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryTracingOptions>()
    ?? new OpenTelemetryTracingOptions();
ValidateOpenTelemetryTracingOptions(openTelemetryTracingOptions);

if (openTelemetryTracingOptions.Enabled)
{
    builder.Services.AddOpenTelemetry()
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

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ERMSystem.Infrastructure.HospitalData.HospitalDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("HospitalConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        }));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var payload = ApiErrorResponseFactory.Create(
                    context.HttpContext,
                    "unauthorized",
                    "Authentication is required to access this resource.");

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";

                var payload = ApiErrorResponseFactory.Create(
                    context.HttpContext,
                    "forbidden",
                    "You do not have permission to access this resource.");

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim(AppPermissions.ClaimType, permission));
    }
});

var distributedCacheRuntimeOptions =
    builder.Configuration.GetSection("Redis").Get<DistributedCacheRuntimeOptions>()
    ?? new DistributedCacheRuntimeOptions();

if (distributedCacheRuntimeOptions.Enabled)
{
    if (string.IsNullOrWhiteSpace(distributedCacheRuntimeOptions.ConnectionString))
    {
        if (!distributedCacheRuntimeOptions.AllowInMemoryFallback)
        {
            throw new InvalidOperationException(
                "Redis cache is enabled but Redis:ConnectionString is missing and fallback is disabled.");
        }

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSingleton(
            new DistributedCacheRuntimeInfo(
                provider: "memory",
                isFallback: true,
                reason: "Redis is enabled in config but ConnectionString is missing. Falling back to in-memory distributed cache."));
    }
    else
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = distributedCacheRuntimeOptions.ConnectionString;
            options.InstanceName = distributedCacheRuntimeOptions.InstanceName;
        });

        builder.Services.AddSingleton(
            new DistributedCacheRuntimeInfo(
                provider: "redis",
                isFallback: false,
                reason: "Redis distributed cache is active."));
    }
}
else
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton(
        new DistributedCacheRuntimeInfo(
            provider: "memory",
            isFallback: false,
            reason: "Redis is disabled in configuration. Using in-memory distributed cache."));
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();
builder.Services.AddHealthChecks()
    .AddCheck<DependencyReadinessHealthCheck>("dependency-readiness", tags: ["ready"]);

var authPermitLimit = builder.Configuration.GetValue<int?>("Security:AuthRateLimit:PermitLimit") ?? 12;
var authWindowSeconds = builder.Configuration.GetValue<int?>("Security:AuthRateLimit:WindowSeconds") ?? 60;
var aiChatPermitLimit = builder.Configuration.GetValue<int?>("AiSymptomChat:RateLimit:PermitLimit") ?? 8;
var aiChatWindowSeconds = builder.Configuration.GetValue<int?>("AiSymptomChat:RateLimit:WindowSeconds") ?? 60;

builder.Services.AddRateLimiter(options =>
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

// ── OpenAPI / Swagger ─────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── DI – Auth ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IAuthSecurityMonitor, AuthSecurityMonitor>();
builder.Services.AddScoped<IHospitalIdentityBridgeService, HospitalIdentityBridgeService>();
builder.Services.AddScoped<IComplianceAuditRecorder, ComplianceAuditRecorder>();

// ── DI – Patient ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IPatientService, PatientService>();

// ── DI – Doctor ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IDoctorService, DoctorService>();

// ── DI – Medicine ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IMedicineRepository, MedicineRepository>();
builder.Services.AddScoped<IMedicineService, MedicineService>();

// ── DI – Appointment ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// ── DI – MedicalRecord ────────────────────────────────────────────────────────
builder.Services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();

// ── DI – Prescription ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IPrescriptionItemRepository, PrescriptionItemRepository>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IPrescriptionItemService, PrescriptionItemService>();
builder.Services.AddScoped<IDashboardQueryCache, DashboardQueryCache>();

// ── DI – Dashboard ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHttpClient<IAiSymptomChatService, OllamaSymptomChatService>();
builder.Services.AddScoped<IHospitalCatalogRepository, HospitalCatalogRepository>();
builder.Services.AddScoped<IHospitalCatalogService, HospitalCatalogService>();
builder.Services.AddScoped<IHospitalDoctorRepository, HospitalDoctorRepository>();
builder.Services.AddScoped<IHospitalDoctorService, HospitalDoctorService>();
builder.Services.AddScoped<IHospitalDoctorWorklistRepository, HospitalDoctorWorklistRepository>();
builder.Services.AddScoped<IHospitalDoctorWorklistService, HospitalDoctorWorklistService>();
builder.Services.AddScoped<IHospitalAppointmentRepository, HospitalAppointmentRepository>();
builder.Services.AddScoped<IHospitalAppointmentService, HospitalAppointmentService>();
builder.Services.AddScoped<IHospitalEncounterRepository, HospitalEncounterRepository>();
builder.Services.AddScoped<IHospitalEncounterService, HospitalEncounterService>();
builder.Services.AddSingleton<IHospitalDocumentStorageService, ConfigurableHospitalDocumentStorageService>();
builder.Services.AddScoped<IHospitalPrescriptionRepository, HospitalPrescriptionRepository>();
builder.Services.AddHttpClient<IExternalDrugKnowledgeProvider, ConfigurableExternalDrugKnowledgeProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, externalDrugKnowledgeOptions.TimeoutSeconds));
});
builder.Services.AddScoped<IHospitalPrescriptionService, HospitalPrescriptionService>();
builder.Services.AddScoped<IHospitalClinicalOrderRepository, HospitalClinicalOrderRepository>();
builder.Services.AddScoped<IHospitalClinicalOrderService, HospitalClinicalOrderService>();
builder.Services.AddScoped<IHospitalBillingRepository, HospitalBillingRepository>();
builder.Services.AddScoped<IHospitalBillingService, HospitalBillingService>();
builder.Services.AddSingleton<IHospitalPaymentGatewayService, ConfigurableHospitalPaymentGatewayService>();
builder.Services.AddScoped<IHospitalPatientPortalRepository, HospitalPatientPortalRepository>();
builder.Services.AddScoped<IHospitalPatientPortalService, HospitalPatientPortalService>();
builder.Services.AddScoped<IHospitalNotificationDeliveryRepository, HospitalNotificationDeliveryRepository>();
builder.Services.AddScoped<IHospitalNotificationDeliveryService, HospitalNotificationDeliveryService>();
builder.Services.AddSingleton<INotificationChannelSender, MockEmailNotificationSender>();
builder.Services.AddSingleton<INotificationChannelSender, MockSmsNotificationSender>();
builder.Services.AddHostedService<HospitalOutboxPublisherService>();
builder.Services.AddHostedService<HospitalNotificationConsumerService>();
builder.Services.AddHostedService<HospitalNotificationDispatchService>();
builder.Services.AddHostedService<RetentionCleanupService>();
builder.Services.AddHostedService<RevisitReminderCampaignService>();
builder.Services.AddHostedService<SatisfactionSurveyCampaignService>();
builder.Services.AddHostedService<CustomerCareFollowUpCampaignService>();
builder.Services.AddHostedService<OperationalAlertDispatchService>();
builder.Services.AddHostedService<DocumentStorageCleanupService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                   ?? new[] { "http://localhost:3000", "http://localhost:3001" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .SetIsOriginAllowed(origin =>
            {
                if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!builder.Environment.IsDevelopment())
                {
                    return false;
                }

                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                var host = uri.Host;
                if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1")
                {
                    return true;
                }

                if (host.StartsWith("192.168.", StringComparison.Ordinal))
                {
                    return true;
                }

                if (host.StartsWith("10.", StringComparison.Ordinal))
                {
                    return true;
                }

                if (host.StartsWith("172.", StringComparison.Ordinal))
                {
                    var parts = host.Split('.');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var secondOctet))
                    {
                        return secondOctet >= 16 && secondOctet <= 31;
                    }
                }

                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var payload = ApiErrorResponseFactory.Create(
                context.HttpContext,
                "validation_failed",
                "The request payload is invalid.",
                ApiErrorResponseFactory.BuildValidationDetails(context.ModelState));

            return new BadRequestObjectResult(payload);
        };
    });

var app = builder.Build();

// ── HTTP Pipeline ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";

    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }

    await next();
});

app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseCors("AllowFrontend");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "ok",
    service = "ERMSystem.API",
    utcNow = DateTime.UtcNow
})).AllowAnonymous();

app.MapGet("/health/alerts", async (OperationalAlertEvaluator evaluator, CancellationToken ct) =>
{
    var snapshot = await evaluator.EvaluateAsync(ct);
    return Results.Ok(snapshot);
}).AllowAnonymous();

app.MapGet("/metrics", async (
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

app.MapHealthChecks("/health/ready", new HealthCheckOptions
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

app.MapControllers();

app.Run();

static string FormatAgeSeconds(DateTime nowUtc, DateTime? value)
{
    if (!value.HasValue)
    {
        return "0";
    }

    return Math.Max(0, Math.Round((nowUtc - value.Value).TotalSeconds, 0))
        .ToString(System.Globalization.CultureInfo.InvariantCulture);
}

static void ValidatePaymentGatewayOptions(HospitalPaymentGatewayOptions options)
{
    if (options.Providers.Count == 0)
    {
        throw new InvalidOperationException("PaymentGateway:Providers must contain at least one provider.");
    }

    var defaultProvider = string.IsNullOrWhiteSpace(options.DefaultProvider)
        ? options.Providers.Keys.FirstOrDefault()
        : options.DefaultProvider.Trim();

    if (string.IsNullOrWhiteSpace(defaultProvider) ||
        !options.Providers.ContainsKey(defaultProvider))
    {
        throw new InvalidOperationException("PaymentGateway:DefaultProvider must match a configured provider.");
    }

    foreach (var pair in options.Providers)
    {
        var providerName = pair.Key?.Trim();
        var provider = pair.Value;
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("PaymentGateway provider name must not be empty.");
        }

        if (provider is null)
        {
            throw new InvalidOperationException($"PaymentGateway:Providers:{providerName} must not be null.");
        }

        if (!provider.Enabled)
        {
            continue;
        }

        if (string.IsNullOrWhiteSpace(provider.ProviderType))
        {
            throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:ProviderType must not be empty.");
        }

        if (string.Equals(provider.ProviderType.Trim(), "VNPay", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(provider.MerchantCode))
            {
                throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:MerchantCode must be configured for VNPay providers.");
            }

            if (string.IsNullOrWhiteSpace(provider.CheckoutBaseUrl))
            {
                throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:CheckoutBaseUrl must be configured for VNPay providers.");
            }

            if (string.IsNullOrWhiteSpace(provider.ReturnUrl))
            {
                throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:ReturnUrl must be configured for VNPay providers.");
            }

            if (string.IsNullOrWhiteSpace(provider.IpnUrl))
            {
                throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:IpnUrl must be configured for VNPay providers.");
            }
        }

        if (!string.IsNullOrWhiteSpace(provider.CheckoutBaseUrl) &&
            !IsAbsoluteHttpUrl(provider.CheckoutBaseUrl))
        {
            throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:CheckoutBaseUrl must be an absolute http/https URL.");
        }

        ValidateOptionalPaymentGatewayUrl(providerName, "ReturnUrl", provider.ReturnUrl);
        ValidateOptionalPaymentGatewayUrl(providerName, "IpnUrl", provider.IpnUrl);

        if (provider.TimestampToleranceMinutes <= 0)
        {
            throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:TimestampToleranceMinutes must be greater than 0.");
        }

        if (provider.RequireSignature)
        {
            if (string.IsNullOrWhiteSpace(provider.SignatureHeaderName))
            {
                throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:SignatureHeaderName must not be empty when RequireSignature=true.");
            }

            if (string.IsNullOrWhiteSpace(provider.WebhookSecret))
            {
                throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:WebhookSecret must be configured when RequireSignature=true.");
            }
        }

        if (provider.SignCheckoutParameters)
        {
            if (string.IsNullOrWhiteSpace(provider.WebhookSecret))
            {
                throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:WebhookSecret must be configured when SignCheckoutParameters=true.");
            }

            if (string.IsNullOrWhiteSpace(provider.CheckoutSignatureParameterName))
            {
                throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:CheckoutSignatureParameterName must not be empty when SignCheckoutParameters=true.");
            }
        }

        foreach (var mappedStatus in provider.StatusMappings.Values)
        {
            if (!string.Equals(mappedStatus, "Captured", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(mappedStatus, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"PaymentGateway:Providers:{providerName}:StatusMappings values must map to Captured or Failed.");
            }
        }

        if (provider.SupportedPaymentMethods.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:SupportedPaymentMethods must not contain empty values.");
        }
    }
}

static void ValidateOptionalPaymentGatewayUrl(string providerName, string optionName, string? value)
{
    if (!string.IsNullOrWhiteSpace(value) && !IsAbsoluteHttpUrl(value))
    {
        throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:{optionName} must be an absolute http/https URL.");
    }
}

static bool IsAbsoluteHttpUrl(string value)
    => Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
       uri.Scheme is "http" or "https";

static void ValidateDocumentStorageOptions(HospitalDocumentStorageOptions options)
{
    if (options.Providers.Count == 0)
    {
        throw new InvalidOperationException("DocumentStorage:Providers must contain at least one provider.");
    }

    var defaultProvider = string.IsNullOrWhiteSpace(options.DefaultProvider)
        ? options.Providers.Keys.FirstOrDefault()
        : options.DefaultProvider.Trim();

    if (string.IsNullOrWhiteSpace(defaultProvider) ||
        !options.Providers.ContainsKey(defaultProvider))
    {
        throw new InvalidOperationException("DocumentStorage:DefaultProvider must match a configured provider.");
    }

    foreach (var pair in options.Providers)
    {
        var providerName = pair.Key?.Trim();
        var provider = pair.Value;
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new InvalidOperationException("DocumentStorage provider name must not be empty.");
        }

        if (provider is null)
        {
            throw new InvalidOperationException($"DocumentStorage:Providers:{providerName} must not be null.");
        }

        if (!provider.Enabled)
        {
            continue;
        }

        if (string.IsNullOrWhiteSpace(provider.Type))
        {
            throw new InvalidOperationException($"DocumentStorage:Providers:{providerName}:Type must not be empty.");
        }

        var accessMode = string.IsNullOrWhiteSpace(provider.AccessMode)
            ? "ProxyTicket"
            : provider.AccessMode.Trim();

        if (accessMode is not ("ProxyTicket" or "DirectUrl"))
        {
            throw new InvalidOperationException($"DocumentStorage:Providers:{providerName}:AccessMode must be ProxyTicket or DirectUrl.");
        }

        if (string.Equals(accessMode, "DirectUrl", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(provider.PublicBaseUrl) ||
                !IsAbsoluteHttpUrl(provider.PublicBaseUrl))
            {
                throw new InvalidOperationException($"DocumentStorage:Providers:{providerName}:PublicBaseUrl must be an absolute http/https URL when AccessMode=DirectUrl.");
            }

            if (provider.RequireSignedDirectUrls &&
                string.IsNullOrWhiteSpace(provider.DirectUrlSigningSecret))
            {
                throw new InvalidOperationException($"DocumentStorage:Providers:{providerName}:DirectUrlSigningSecret must be configured when RequireSignedDirectUrls=true.");
            }
        }

        if (string.IsNullOrWhiteSpace(provider.RootPath))
        {
            throw new InvalidOperationException($"DocumentStorage:Providers:{providerName}:RootPath must not be empty.");
        }

        if (provider.DownloadTicketExpiryMinutes <= 0 ||
            provider.CleanupIntervalHours <= 0 ||
            provider.OrphanFileRetentionDays <= 0 ||
            provider.MaxFileSizeBytes <= 0)
        {
            throw new InvalidOperationException($"DocumentStorage:Providers:{providerName} numeric limits must be greater than 0.");
        }

        if (provider.AllowedExtensions.Length == 0 ||
            provider.AllowedExtensions.Any(extension =>
                string.IsNullOrWhiteSpace(extension) || !extension.Trim().StartsWith(".", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"DocumentStorage:Providers:{providerName}:AllowedExtensions must contain dot-prefixed extensions.");
        }
    }
}

static void ValidateExternalDrugKnowledgeOptions(ExternalDrugKnowledgeOptions options)
{
    if (options.TimeoutSeconds <= 0)
    {
        throw new InvalidOperationException("ExternalDrugKnowledge:TimeoutSeconds must be greater than 0.");
    }

    if (!options.Enabled)
    {
        return;
    }

    if (string.IsNullOrWhiteSpace(options.ProviderName))
    {
        throw new InvalidOperationException("ExternalDrugKnowledge:ProviderName must not be empty when enabled.");
    }

    if (string.IsNullOrWhiteSpace(options.BaseUrl) || !IsAbsoluteHttpUrl(options.BaseUrl))
    {
        throw new InvalidOperationException("ExternalDrugKnowledge:BaseUrl must be an absolute http/https URL when enabled.");
    }

    if (string.IsNullOrWhiteSpace(options.EvaluationPath))
    {
        throw new InvalidOperationException("ExternalDrugKnowledge:EvaluationPath must not be empty when enabled.");
    }

    if (!string.IsNullOrWhiteSpace(options.ApiKey) &&
        string.IsNullOrWhiteSpace(options.ApiKeyHeaderName))
    {
        throw new InvalidOperationException("ExternalDrugKnowledge:ApiKeyHeaderName must not be empty when ApiKey is configured.");
    }
}

static void ValidateOpenTelemetryTracingOptions(OpenTelemetryTracingOptions options)
{
    if (!options.Enabled)
    {
        return;
    }

    if (!options.UseConsoleExporter && string.IsNullOrWhiteSpace(options.OtlpEndpoint))
    {
        throw new InvalidOperationException(
            "OpenTelemetry is enabled but no exporter is configured. Set OpenTelemetry:UseConsoleExporter=true or OpenTelemetry:OtlpEndpoint.");
    }

    if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint) &&
        (!Uri.TryCreate(options.OtlpEndpoint.Trim(), UriKind.Absolute, out var otlpEndpoint) ||
         otlpEndpoint.Scheme is not ("http" or "https")))
    {
        throw new InvalidOperationException("OpenTelemetry:OtlpEndpoint must be an absolute http/https URL.");
    }
}

static void ValidateOperationalAlertOptions(OperationalAlertOptions options)
{
    if (options.DispatchIntervalSeconds <= 0)
    {
        throw new InvalidOperationException("OperationalAlerts:DispatchIntervalSeconds must be greater than 0.");
    }

    if (options.RepeatIntervalMinutes <= 0)
    {
        throw new InvalidOperationException("OperationalAlerts:RepeatIntervalMinutes must be greater than 0.");
    }

    if (options.Webhook.TimeoutSeconds <= 0)
    {
        throw new InvalidOperationException("OperationalAlerts:Webhook:TimeoutSeconds must be greater than 0.");
    }

    ValidateThresholdPair(
        options.PendingOutboxWarningThreshold,
        options.PendingOutboxCriticalThreshold,
        "OperationalAlerts pending outbox thresholds");
    ValidateThresholdPair(
        options.QueuedDeliveryWarningThreshold,
        options.QueuedDeliveryCriticalThreshold,
        "OperationalAlerts queued delivery thresholds");
    ValidateThresholdPair(
        options.StaleQueuedWarningThreshold,
        options.StaleQueuedCriticalThreshold,
        "OperationalAlerts stale queued thresholds");
    ValidateThresholdPair(
        options.OldestQueuedWarningMinutes,
        options.OldestQueuedCriticalMinutes,
        "OperationalAlerts oldest queued thresholds");
    ValidateThresholdPair(
        options.WorkerStaleWarningMinutes,
        options.WorkerStaleCriticalMinutes,
        "OperationalAlerts worker stale thresholds");

    if (options.CacheMinimumSamples < 0)
    {
        throw new InvalidOperationException("OperationalAlerts:CacheMinimumSamples must not be negative.");
    }

    if (options.CacheHitRatioCriticalPercent is < 0 or > 100 ||
        options.CacheHitRatioWarningPercent is < 0 or > 100 ||
        options.CacheHitRatioCriticalPercent > options.CacheHitRatioWarningPercent)
    {
        throw new InvalidOperationException(
            "OperationalAlerts cache hit ratio thresholds must be between 0 and 100, with critical <= warning.");
    }

    if (!options.DispatchEnabled || !options.Webhook.Enabled)
    {
        return;
    }

    if (!Uri.TryCreate(options.Webhook.Url.Trim(), UriKind.Absolute, out var webhookUri) ||
        webhookUri.Scheme is not ("http" or "https"))
    {
        throw new InvalidOperationException(
            "Operational alert webhook dispatch is enabled but OperationalAlerts:Webhook:Url is not a valid absolute http/https URL.");
    }

    if (!string.IsNullOrWhiteSpace(options.Webhook.SigningSecret) &&
        (string.IsNullOrWhiteSpace(options.Webhook.SignatureHeaderName) ||
         string.IsNullOrWhiteSpace(options.Webhook.TimestampHeaderName)))
    {
        throw new InvalidOperationException(
            "Operational alert webhook signing requires SignatureHeaderName and TimestampHeaderName.");
    }
}

static void ValidateThresholdPair(int warning, int critical, string label)
{
    if (warning < 0 || critical < 0 || critical < warning)
    {
        throw new InvalidOperationException($"{label} must be non-negative and critical >= warning.");
    }
}
