namespace ERMSystem.Infrastructure.Services;

public class HospitalPaymentGatewayOptions
{
    public string DefaultProvider { get; set; } = "MockGateway";
    public Dictionary<string, HospitalPaymentGatewayProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class HospitalPaymentGatewayProviderOptions
{
    public bool Enabled { get; set; } = true;
    public string DisplayName { get; set; } = string.Empty;
    public string ProviderType { get; set; } = "GenericRedirect";
    public string CheckoutMode { get; set; } = "HostedUrl";
    public string CallbackMode { get; set; } = "SignedWebhook";
    public string CheckoutBaseUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
    public string MerchantCode { get; set; } = string.Empty;
    public bool RequireSignature { get; set; } = true;
    public bool SignCheckoutParameters { get; set; } = false;
    public string CheckoutSignatureParameterName { get; set; } = "signature";
    public string SignatureHeaderName { get; set; } = "X-ERM-Gateway-Signature";
    public string WebhookSecret { get; set; } = string.Empty;
    public int TimestampToleranceMinutes { get; set; } = 10;
    public List<string> SupportedPaymentMethods { get; set; } = new();
    public Dictionary<string, string> StatusMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> StaticCheckoutParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
