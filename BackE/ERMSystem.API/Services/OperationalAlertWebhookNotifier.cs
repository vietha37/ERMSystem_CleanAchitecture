using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ERMSystem.API.Services;

public class OperationalAlertWebhookNotifier
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OperationalAlertOptions _options;
    private readonly ILogger<OperationalAlertWebhookNotifier> _logger;

    public OperationalAlertWebhookNotifier(
        IHttpClientFactory httpClientFactory,
        IOptions<OperationalAlertOptions> options,
        ILogger<OperationalAlertWebhookNotifier> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled()
        => _options.DispatchEnabled
           && _options.Webhook.Enabled
           && !string.IsNullOrWhiteSpace(_options.Webhook.Url);

    public async Task SendAsync(OperationalAlertNotificationEnvelope envelope, CancellationToken ct)
    {
        if (!IsEnabled())
        {
            return;
        }

        var client = _httpClientFactory.CreateClient("operational-alert-webhook");
        var payload = JsonSerializer.Serialize(envelope, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Webhook.Url.Trim())
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_options.Webhook.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Webhook.BearerToken.Trim());
        }

        AddSignatureHeaders(request, payload);

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Da day operational alert ra webhook. EventType={EventType}, Code={Code}, Severity={Severity}",
            envelope.EventType,
            envelope.Alert.Code,
            envelope.Alert.Severity);
    }

    private void AddSignatureHeaders(HttpRequestMessage request, string payload)
    {
        var signingSecret = _options.Webhook.SigningSecret?.Trim();
        if (string.IsNullOrWhiteSpace(signingSecret))
        {
            return;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signaturePayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        var signature = Convert.ToHexString(
                hmac.ComputeHash(Encoding.UTF8.GetBytes(signaturePayload)))
            .ToLowerInvariant();

        request.Headers.TryAddWithoutValidation(_options.Webhook.TimestampHeaderName.Trim(), timestamp);
        request.Headers.TryAddWithoutValidation(
            _options.Webhook.SignatureHeaderName.Trim(),
            $"sha256={signature}");
    }
}
