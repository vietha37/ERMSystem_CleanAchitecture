using System.Diagnostics;

namespace ERMSystem.API.Services;

public static class ErmTelemetry
{
    public const string ActivitySourceName = "ERMSystem";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
