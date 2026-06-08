namespace ERMSystem.Infrastructure.Services;

public sealed class ExternalDrugKnowledgeOptions
{
    public bool Enabled { get; set; } = false;
    public string ProviderName { get; set; } = "LicensedDrugKnowledgeProvider";
    public string BaseUrl { get; set; } = string.Empty;
    public string EvaluationPath { get; set; } = "/clinical-decision-support/prescriptions/evaluate";
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
    public int TimeoutSeconds { get; set; } = 10;
    public bool FailOnProviderError { get; set; } = false;
    public bool IncludeInternalWarnings { get; set; } = true;
    public bool IncludeProviderAvailabilityWarning { get; set; } = true;
}
