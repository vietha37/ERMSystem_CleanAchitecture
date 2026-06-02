namespace ERMSystem.Application.Interfaces;

public interface IHospitalPaymentGatewayService
{
    string GetDefaultProvider();

    HospitalPaymentGatewayIntentPreparation PreparePaymentIntent(HospitalPaymentGatewayIntentRequest request);

    HospitalPaymentGatewayCallbackValidationResult ValidateCallback(HospitalPaymentGatewayCallbackValidationRequest request);

    string NormalizeGatewayStatus(string? providerName, string gatewayStatus);
}

public class HospitalPaymentGatewayIntentRequest
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? RequestedProvider { get; set; }
    public string? ExistingExternalTransactionId { get; set; }
}

public class HospitalPaymentGatewayIntentPreparation
{
    public string ProviderName { get; set; } = string.Empty;
    public string ExternalTransactionId { get; set; } = string.Empty;
    public string CheckoutToken { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string InstructionText { get; set; } = string.Empty;
    public string CallbackMode { get; set; } = string.Empty;
}

public class HospitalPaymentGatewayCallbackValidationRequest
{
    public Guid InvoiceId { get; set; }
    public string? ProviderName { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string? ExternalTransactionId { get; set; }
    public string GatewayStatus { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string? GatewayEventId { get; set; }
    public DateTime? GatewayTimestampUtc { get; set; }
    public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class HospitalPaymentGatewayCallbackValidationResult
{
    public bool IsValid { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public string ResolvedProviderName { get; set; } = string.Empty;
}
