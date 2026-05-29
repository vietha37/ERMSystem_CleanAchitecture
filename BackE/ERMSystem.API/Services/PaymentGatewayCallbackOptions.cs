namespace ERMSystem.API.Services;

public class PaymentGatewayCallbackOptions
{
    public string DefaultProvider { get; set; } = "MockGateway";
    public bool RequireSignature { get; set; } = true;
    public string SignatureHeaderName { get; set; } = "X-ERM-Gateway-Signature";
    public string HmacSecret { get; set; } = "dev-only-payment-webhook-secret";
    public int TimestampToleranceMinutes { get; set; } = 10;
}
