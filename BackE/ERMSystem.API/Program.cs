using ERMSystem.Application;
using ERMSystem.Application.Authorization;
using ERMSystem.Application.Interfaces;
using ERMSystem.API.Extensions;
using ERMSystem.API.HealthChecks;
using ERMSystem.API.Services;
using ERMSystem.Infrastructure;
using ERMSystem.Infrastructure.Messaging;
using ERMSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.IO;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ── Logging Configuration ─────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (OperatingSystem.IsWindows() && !builder.Environment.IsDevelopment())
{
    builder.Logging.AddEventLog();
}

// ── Data Protection ───────────────────────────────────────────────────────────
var dataProtectionKeyPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeyPath);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));

// ── Strongly-typed Options Registration ───────────────────────────────────────
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

// ── API Telemetry & Alert Singletons ──────────────────────────────────────────
builder.Services.AddSingleton<ApiMetricsCollector>();
builder.Services.AddSingleton<IBusinessMetricsRecorder, BusinessMetricsRecorder>();
builder.Services.AddSingleton<OperationalAlertEvaluator>();
builder.Services.AddSingleton<OperationalAlertWebhookNotifier>();

// ── Startup Configuration Validation (Fail-Fast) ─────────────────────────────
var paymentGatewayOptions = builder.Configuration.GetSection("PaymentGateway").Get<HospitalPaymentGatewayOptions>() ?? new HospitalPaymentGatewayOptions();
ConfigurationValidationExtensions.ValidatePaymentGatewayOptions(paymentGatewayOptions);

var documentStorageOptions = builder.Configuration.GetSection("DocumentStorage").Get<HospitalDocumentStorageOptions>() ?? new HospitalDocumentStorageOptions();
ConfigurationValidationExtensions.ValidateDocumentStorageOptions(documentStorageOptions);

var externalDrugKnowledgeOptions = builder.Configuration.GetSection("ExternalDrugKnowledge").Get<ExternalDrugKnowledgeOptions>() ?? new ExternalDrugKnowledgeOptions();
ConfigurationValidationExtensions.ValidateExternalDrugKnowledgeOptions(externalDrugKnowledgeOptions);

var operationalAlertOptions = builder.Configuration.GetSection("OperationalAlerts").Get<OperationalAlertOptions>() ?? new OperationalAlertOptions();
ConfigurationValidationExtensions.ValidateOperationalAlertOptions(operationalAlertOptions);

var openTelemetryTracingOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryTracingOptions>() ?? new OpenTelemetryTracingOptions();
ConfigurationValidationExtensions.ValidateOpenTelemetryTracingOptions(openTelemetryTracingOptions);

builder.Services.AddHttpClient("operational-alert-webhook", (_, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, operationalAlertOptions.Webhook.TimeoutSeconds));
});

// ── Observability & OpenTelemetry ─────────────────────────────────────────────
builder.Services.AddApiObservability(builder.Configuration, openTelemetryTracingOptions);

// ── Database Context ──────────────────────────────────────────────────────────
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

// ── JWT Authentication & Permissions Authorization ───────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
ConfigurationValidationExtensions.ValidateJwtOptions(jwtKey, jwtIssuer, jwtAudience, builder.Environment.IsDevelopment());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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

// ── Distributed / Memory Cache ────────────────────────────────────────────────
var distributedCacheRuntimeOptions = builder.Configuration.GetSection("Redis").Get<DistributedCacheRuntimeOptions>() ?? new DistributedCacheRuntimeOptions();
if (distributedCacheRuntimeOptions.Enabled)
{
    if (string.IsNullOrWhiteSpace(distributedCacheRuntimeOptions.ConnectionString))
    {
        if (!distributedCacheRuntimeOptions.AllowInMemoryFallback)
        {
            throw new InvalidOperationException("Redis cache is enabled but Redis:ConnectionString is missing and fallback is disabled.");
        }

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSingleton(new DistributedCacheRuntimeInfo(
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

        builder.Services.AddSingleton(new DistributedCacheRuntimeInfo(
            provider: "redis",
            isFallback: false,
            reason: "Redis distributed cache is active."));
    }
}
else
{
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSingleton(new DistributedCacheRuntimeInfo(
        provider: "memory",
        isFallback: false,
        reason: "Redis is disabled in configuration. Using in-memory distributed cache."));
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks()
    .AddCheck<DependencyReadinessHealthCheck>("dependency-readiness", tags: ["ready"]);

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddApiRateLimiting(builder.Configuration);

// ── OpenAPI / Swagger ─────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Clean Architecture Layer Registrations ────────────────────────────────────
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();

// ── External Integration HttpClients ──────────────────────────────────────────
builder.Services.AddHttpClient<IAiSymptomChatService, GeminiSymptomChatService>();
builder.Services.AddHttpClient<IExternalDrugKnowledgeProvider, ConfigurableExternalDrugKnowledgeProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, externalDrugKnowledgeOptions.TimeoutSeconds));
});

// ── CORS Policy ───────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                   ?? ["http://localhost:3000", "http://localhost:3001"];

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

                if (host.StartsWith("192.168.", StringComparison.Ordinal) ||
                    host.StartsWith("10.", StringComparison.Ordinal))
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

// ── MVC Controllers & JSON Formatter ──────────────────────────────────────────
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

// ── HTTP Middleware Pipeline ──────────────────────────────────────────────────
app.UseCors("AllowFrontend");

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
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ─────────────────────────────────────────────────────────────────
app.MapObservabilityEndpoints();
app.MapControllers();

app.Run();
