namespace ERMSystem.API.Services;

public class OpenTelemetryTracingOptions
{
    public bool Enabled { get; set; } = false;
    public string ServiceName { get; set; } = "ERMSystem.API";
    public string ServiceVersion { get; set; } = "1.0.0";
    public bool UseConsoleExporter { get; set; } = false;
    public string? OtlpEndpoint { get; set; }
}
