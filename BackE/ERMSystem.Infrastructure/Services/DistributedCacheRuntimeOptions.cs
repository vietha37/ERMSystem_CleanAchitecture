namespace ERMSystem.Infrastructure.Services;

public sealed class DistributedCacheRuntimeOptions
{
    public bool Enabled { get; set; } = false;
    public bool AllowInMemoryFallback { get; set; } = true;
    public string? ConnectionString { get; set; }
    public string InstanceName { get; set; } = "ERMSystem";
}
