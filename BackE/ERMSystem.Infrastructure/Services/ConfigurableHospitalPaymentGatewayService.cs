using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ERMSystem.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public class ConfigurableHospitalPaymentGatewayService : IHospitalPaymentGatewayService
{
    private readonly HospitalPaymentGatewayOptions _options;

    public ConfigurableHospitalPaymentGatewayService(IOptions<HospitalPaymentGatewayOptions> options)
    {
        _options = options.Value;
    }

    public string GetDefaultProvider()
    {
        var providerName = string.IsNullOrWhiteSpace(_options.DefaultProvider)
            ? "MockGateway"
            : _options.DefaultProvider.Trim();

        if (_options.Providers.ContainsKey(providerName))
        {
            return providerName;
        }

        return _options.Providers.Keys.FirstOrDefault() ?? providerName;
    }

    public HospitalPaymentGatewayIntentPreparation PreparePaymentIntent(HospitalPaymentGatewayIntentRequest request)
    {
        var (providerName, providerOptions) = ResolveProvider(request.RequestedProvider);
        ValidatePaymentMethod(providerOptions, request.PaymentMethod);

        var externalTransactionId = Normalize(request.ExistingExternalTransactionId)
            ?? $"{providerName.ToUpperInvariant()}-{Guid.NewGuid():N}";
        var checkoutToken = $"{providerName.ToUpperInvariant()}-{Guid.NewGuid():N}";
        var checkoutUrl = BuildCheckoutUrl(providerOptions, request, externalTransactionId, checkoutToken);
        var providerLabel = string.IsNullOrWhiteSpace(providerOptions.DisplayName)
            ? providerName
            : providerOptions.DisplayName.Trim();

        return new HospitalPaymentGatewayIntentPreparation
        {
            ProviderName = providerName,
            ExternalTransactionId = externalTransactionId,
            CheckoutToken = checkoutToken,
            CheckoutUrl = checkoutUrl,
            CallbackMode = string.IsNullOrWhiteSpace(providerOptions.CallbackMode)
                ? "SignedWebhook"
                : providerOptions.CallbackMode.Trim(),
            InstructionText = BuildInstructionText(providerLabel, providerOptions, request.PaymentReference, request.InvoiceNumber, checkoutUrl)
        };
    }

    public HospitalPaymentGatewayCallbackValidationResult ValidateCallback(HospitalPaymentGatewayCallbackValidationRequest request)
    {
        try
        {
            var (providerName, providerOptions) = ResolveProvider(request.ProviderName);
            if (!providerOptions.RequireSignature)
            {
                return new HospitalPaymentGatewayCallbackValidationResult
                {
                    IsValid = true,
                    ResolvedProviderName = providerName
                };
            }

            var signatureHeaderName = string.IsNullOrWhiteSpace(providerOptions.SignatureHeaderName)
                ? "X-ERM-Gateway-Signature"
                : providerOptions.SignatureHeaderName.Trim();

            if (!request.Headers.TryGetValue(signatureHeaderName, out var providedSignature) ||
                string.IsNullOrWhiteSpace(providedSignature))
            {
                return Invalid(providerName, "Missing signature header.");
            }

            var eventId = Normalize(request.GatewayEventId);
            if (eventId is null)
            {
                return Invalid(providerName, "Missing gateway event id.");
            }

            if (!request.GatewayTimestampUtc.HasValue)
            {
                return Invalid(providerName, "Missing gateway timestamp.");
            }

            var timestampUtc = request.GatewayTimestampUtc.Value.ToUniversalTime();
            var toleranceMinutes = Math.Max(1, providerOptions.TimestampToleranceMinutes);
            if (Math.Abs((DateTime.UtcNow - timestampUtc).TotalMinutes) > toleranceMinutes)
            {
                return Invalid(providerName, "Gateway timestamp is outside the allowed tolerance window.");
            }

            var expectedSignature = ComputeSignature(
                providerOptions.WebhookSecret,
                providerName,
                request.InvoiceId,
                request.PaymentReference,
                request.ExternalTransactionId,
                request.GatewayStatus,
                request.Amount,
                eventId,
                timestampUtc);

            var normalizedProvidedSignature = NormalizeSignature(providedSignature);
            if (normalizedProvidedSignature is null ||
                !FixedTimeEquals(expectedSignature, normalizedProvidedSignature))
            {
                return Invalid(providerName, "Gateway signature is invalid.");
            }

            return new HospitalPaymentGatewayCallbackValidationResult
            {
                IsValid = true,
                ResolvedProviderName = providerName
            };
        }
        catch (InvalidOperationException ex)
        {
            return new HospitalPaymentGatewayCallbackValidationResult
            {
                IsValid = false,
                FailureReason = ex.Message,
                ResolvedProviderName = Normalize(request.ProviderName) ?? string.Empty
            };
        }
    }

    public string NormalizeGatewayStatus(string? providerName, string gatewayStatus)
    {
        var (resolvedProviderName, providerOptions) = ResolveProvider(providerName);
        var normalizedStatus = Normalize(gatewayStatus)
            ?? throw new InvalidOperationException("Gateway status khong hop le cho callback thanh toan.");

        if (providerOptions.StatusMappings.TryGetValue(normalizedStatus, out var mappedStatus))
        {
            return NormalizeCanonicalPaymentStatus(resolvedProviderName, gatewayStatus, mappedStatus);
        }

        if (string.Equals(normalizedStatus, "Captured", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedStatus, "Success", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedStatus, "Succeeded", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedStatus, "Paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return "Captured";
        }

        if (string.Equals(normalizedStatus, "Failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedStatus, "Declined", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedStatus, "Canceled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedStatus, "Expired", StringComparison.OrdinalIgnoreCase))
        {
            return "Failed";
        }

        throw new InvalidOperationException($"Gateway status '{gatewayStatus}' khong hop le cho provider {resolvedProviderName}.");
    }

    private static string NormalizeCanonicalPaymentStatus(
        string providerName,
        string originalGatewayStatus,
        string? mappedStatus)
    {
        var normalizedMappedStatus = Normalize(mappedStatus)
            ?? throw new InvalidOperationException(
                $"Gateway status '{originalGatewayStatus}' cua provider {providerName} map toi trang thai rong.");

        if (string.Equals(normalizedMappedStatus, "Captured", StringComparison.OrdinalIgnoreCase))
        {
            return "Captured";
        }

        if (string.Equals(normalizedMappedStatus, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            return "Failed";
        }

        throw new InvalidOperationException(
            $"Gateway status '{originalGatewayStatus}' cua provider {providerName} map toi trang thai noi bo khong hop le '{mappedStatus}'.");
    }

    private (string ProviderName, HospitalPaymentGatewayProviderOptions Options) ResolveProvider(string? requestedProvider)
    {
        var providerName = Normalize(requestedProvider) ?? GetDefaultProvider();
        if (!_options.Providers.TryGetValue(providerName, out var providerOptions) || providerOptions is null)
        {
            throw new InvalidOperationException($"Payment gateway provider '{providerName}' chua duoc cau hinh.");
        }

        if (!providerOptions.Enabled)
        {
            throw new InvalidOperationException($"Payment gateway provider '{providerName}' dang bi tat.");
        }

        if (providerOptions.RequireSignature && string.IsNullOrWhiteSpace(providerOptions.WebhookSecret))
        {
            throw new InvalidOperationException(
                $"Payment gateway provider '{providerName}' yeu cau chu ky callback nhung chua cau hinh WebhookSecret.");
        }

        if (providerOptions.SignCheckoutParameters && string.IsNullOrWhiteSpace(providerOptions.WebhookSecret))
        {
            throw new InvalidOperationException(
                $"Payment gateway provider '{providerName}' bat ky checkout nhung chua cau hinh WebhookSecret.");
        }

        return (providerName, providerOptions);
    }

    private static void ValidatePaymentMethod(HospitalPaymentGatewayProviderOptions providerOptions, string paymentMethod)
    {
        if (providerOptions.SupportedPaymentMethods.Count == 0)
        {
            return;
        }

        var normalizedMethod = Normalize(paymentMethod)
            ?? throw new InvalidOperationException("Payment method khong hop le.");
        if (providerOptions.SupportedPaymentMethods.Any(x => string.Equals(x, normalizedMethod, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        throw new InvalidOperationException($"Payment method '{normalizedMethod}' khong duoc ho tro boi gateway da chon.");
    }

    private static string? BuildCheckoutUrl(
        HospitalPaymentGatewayProviderOptions providerOptions,
        HospitalPaymentGatewayIntentRequest request,
        string externalTransactionId,
        string checkoutToken)
    {
        var baseUrl = Normalize(providerOptions.CheckoutBaseUrl);
        if (baseUrl is null)
        {
            return null;
        }

        var queryValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["invoiceId"] = request.InvoiceId.ToString("D"),
            ["invoiceNumber"] = request.InvoiceNumber,
            ["paymentReference"] = request.PaymentReference,
            ["paymentMethod"] = request.PaymentMethod,
            ["amount"] = request.Amount.ToString("0.##", CultureInfo.InvariantCulture),
            ["transactionId"] = externalTransactionId,
            ["checkoutToken"] = checkoutToken
        };

        var providerType = Normalize(providerOptions.ProviderType);
        if (providerType is not null)
        {
            queryValues["providerType"] = providerType;
        }

        if (string.Equals(providerType, "VNPay", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(providerOptions.MerchantCode) ||
                string.IsNullOrWhiteSpace(providerOptions.ReturnUrl))
            {
                throw new InvalidOperationException("VNPay gateway requires MerchantCode and ReturnUrl.");
            }

            queryValues["vnp_TmnCode"] = providerOptions.MerchantCode.Trim();
            queryValues["vnp_TxnRef"] = request.PaymentReference;
            queryValues["vnp_OrderInfo"] = $"Thanh toan hoa don {request.InvoiceNumber}";
            queryValues["vnp_Amount"] = (request.Amount * 100m).ToString("0", CultureInfo.InvariantCulture);
            queryValues["vnp_CreateDate"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            queryValues["vnp_ReturnUrl"] = providerOptions.ReturnUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(providerOptions.MerchantCode))
        {
            queryValues["merchantCode"] = providerOptions.MerchantCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(providerOptions.ReturnUrl))
        {
            queryValues["returnUrl"] = providerOptions.ReturnUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(providerOptions.IpnUrl))
        {
            queryValues["ipnUrl"] = providerOptions.IpnUrl.Trim();
        }

        foreach (var pair in providerOptions.StaticCheckoutParameters)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            {
                queryValues[pair.Key.Trim()] = pair.Value.Trim();
            }
        }

        if (providerOptions.SignCheckoutParameters)
        {
            var signatureParameterName = Normalize(providerOptions.CheckoutSignatureParameterName) ?? "signature";
            queryValues[signatureParameterName] = ComputeCheckoutSignature(providerOptions.WebhookSecret, queryValues);
        }

        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return baseUrl + separator + string.Join("&", queryValues.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }

    private static string BuildInstructionText(
        string providerLabel,
        HospitalPaymentGatewayProviderOptions providerOptions,
        string paymentReference,
        string invoiceNumber,
        string? checkoutUrl)
    {
        if (!string.IsNullOrWhiteSpace(checkoutUrl))
        {
            return $"Mo cong thanh toan {providerLabel} de xu ly giao dich {paymentReference} cho hoa don {invoiceNumber}.";
        }

        return $"Gateway {providerLabel} se gui callback {providerOptions.CallbackMode} de xac nhan giao dich {paymentReference} cho hoa don {invoiceNumber}.";
    }

    private static HospitalPaymentGatewayCallbackValidationResult Invalid(string providerName, string message)
        => new()
        {
            IsValid = false,
            FailureReason = message,
            ResolvedProviderName = providerName
        };

    private static string ComputeSignature(
        string? webhookSecret,
        string providerName,
        Guid invoiceId,
        string paymentReference,
        string? externalTransactionId,
        string gatewayStatus,
        decimal? amount,
        string gatewayEventId,
        DateTime timestampUtc)
    {
        var payload = string.Join("|", new[]
        {
            providerName,
            invoiceId.ToString("D"),
            paymentReference.Trim(),
            Normalize(externalTransactionId) ?? string.Empty,
            gatewayStatus.Trim(),
            amount?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty,
            gatewayEventId,
            timestampUtc.ToString("O", CultureInfo.InvariantCulture)
        });

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret ?? string.Empty));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string ComputeCheckoutSignature(
        string? webhookSecret,
        IReadOnlyDictionary<string, string> queryValues)
    {
        var canonicalPayload = string.Join("&", queryValues
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret ?? string.Empty));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonicalPayload))).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string? NormalizeSignature(string? value)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return null;
        }

        const string sha256Prefix = "sha256=";
        if (normalized.StartsWith(sha256Prefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[sha256Prefix.Length..].Trim();
        }

        return normalized.Length == 64 && normalized.All(Uri.IsHexDigit)
            ? normalized.ToLowerInvariant()
            : null;
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
