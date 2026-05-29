using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ERMSystem.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ERMSystem.API.Services;

public class PaymentGatewayCallbackVerifier
{
    private readonly PaymentGatewayCallbackOptions _options;

    public PaymentGatewayCallbackVerifier(IOptions<PaymentGatewayCallbackOptions> options)
    {
        _options = options.Value;
    }

    public string GetDefaultProvider()
        => string.IsNullOrWhiteSpace(_options.DefaultProvider)
            ? "MockGateway"
            : _options.DefaultProvider.Trim();

    public bool TryValidate(ConfirmHospitalPaymentCallbackDto request, IHeaderDictionary headers, out string failureReason)
    {
        if (!_options.RequireSignature)
        {
            failureReason = string.Empty;
            return true;
        }

        var signatureHeaderName = string.IsNullOrWhiteSpace(_options.SignatureHeaderName)
            ? "X-ERM-Gateway-Signature"
            : _options.SignatureHeaderName.Trim();
        var providedSignature = headers[signatureHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedSignature))
        {
            failureReason = "Missing signature header.";
            return false;
        }

        var provider = string.IsNullOrWhiteSpace(request.GatewayProvider)
            ? GetDefaultProvider()
            : request.GatewayProvider.Trim();
        var eventId = request.GatewayEventId?.Trim();
        if (string.IsNullOrWhiteSpace(eventId))
        {
            failureReason = "Missing gateway event id.";
            return false;
        }

        if (!request.GatewayTimestampUtc.HasValue)
        {
            failureReason = "Missing gateway timestamp.";
            return false;
        }

        var timestampUtc = request.GatewayTimestampUtc.Value.ToUniversalTime();
        var tolerance = Math.Max(1, _options.TimestampToleranceMinutes);
        if (Math.Abs((DateTime.UtcNow - timestampUtc).TotalMinutes) > tolerance)
        {
            failureReason = "Gateway timestamp is outside the allowed tolerance window.";
            return false;
        }

        var expectedSignature = ComputeSignature(request, provider, eventId, timestampUtc);
        if (!FixedTimeEquals(expectedSignature, providedSignature.Trim()))
        {
            failureReason = "Gateway signature is invalid.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private string ComputeSignature(
        ConfirmHospitalPaymentCallbackDto request,
        string provider,
        string eventId,
        DateTime timestampUtc)
    {
        var payload = string.Join("|", new[]
        {
            provider,
            request.InvoiceId.ToString("D"),
            request.PaymentReference.Trim(),
            request.ExternalTransactionId?.Trim() ?? string.Empty,
            request.GatewayStatus.Trim(),
            request.Amount?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty,
            eventId,
            timestampUtc.ToString("O", CultureInfo.InvariantCulture)
        });

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.HmacSecret ?? string.Empty));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
