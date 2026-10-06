using System;
using System.Linq;
using ERMSystem.API.Services;
using ERMSystem.Infrastructure.Services;

namespace ERMSystem.API.Extensions;

/// <summary>
/// Các phương thức kiểm tra tính hợp lệ của cấu hình hệ thống khi khởi động ứng dụng (Fail-fast startup validation).
/// Tách khỏi Program.cs nhằm giữ cho điểm khởi động ứng dụng gọn gàng, rõ ràng và dễ bảo trì.
/// </summary>
public static class ConfigurationValidationExtensions
{
    public static void ValidatePaymentGatewayOptions(HospitalPaymentGatewayOptions options)
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

    public static void ValidateDocumentStorageOptions(HospitalDocumentStorageOptions options)
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

    public static void ValidateExternalDrugKnowledgeOptions(ExternalDrugKnowledgeOptions options)
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

    public static void ValidateOpenTelemetryTracingOptions(OpenTelemetryTracingOptions options)
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

    public static void ValidateOperationalAlertOptions(OperationalAlertOptions options)
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

    public static void ValidateThresholdPair(int warning, int critical, string label)
    {
        if (warning < 0 || critical < 0 || critical < warning)
        {
            throw new InvalidOperationException($"{label} must be non-negative and critical >= warning.");
        }
    }

    private static void ValidateOptionalPaymentGatewayUrl(string providerName, string optionName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !IsAbsoluteHttpUrl(value))
        {
            throw new InvalidOperationException($"PaymentGateway:Providers:{providerName}:{optionName} must be an absolute http/https URL.");
        }
    }

    public static bool IsAbsoluteHttpUrl(string value)
        => Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
           uri.Scheme is "http" or "https";

    public static void ValidateJwtOptions(string? jwtKey, string? issuer, string? audience, bool isDevelopment)
    {
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be configured and contain at least 32 characters (256 bits).");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Jwt:Audience must not be empty.");
        }

        if (!isDevelopment && string.Equals(jwtKey, "ERMSystem_JWT_Secret_Key_2026_32c", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "CRITICAL SECURITY ERROR: The default development JWT secret key cannot be used in production. Set the 'Jwt__Key' environment variable to a strong, randomly generated 256-bit secret.");
        }
    }
}
