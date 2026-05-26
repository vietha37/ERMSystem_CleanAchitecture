namespace ERMSystem.Infrastructure.Services;

public sealed class DistributedCacheRuntimeInfo
{
    public DistributedCacheRuntimeInfo(string provider, bool isFallback, string reason)
    {
        Provider = provider;
        IsFallback = isFallback;
        Reason = reason;
    }

    public string Provider { get; }
    public bool IsFallback { get; }
    public string Reason { get; }
}
