using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Webhook.Url.Trim())
        {
            Content = JsonContent.Create(envelope)
        };

        if (!string.IsNullOrWhiteSpace(_options.Webhook.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Webhook.BearerToken.Trim());
        }

        using var response = await client.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Da day operational alert ra webhook. EventType={EventType}, Code={Code}, Severity={Severity}",
            envelope.EventType,
            envelope.Alert.Code,
            envelope.Alert.Severity);
    }
}
