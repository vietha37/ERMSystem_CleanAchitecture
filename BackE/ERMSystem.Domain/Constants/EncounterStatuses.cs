namespace ERMSystem.Domain.Constants;

/// <summary>
/// Các hằng số trạng thái lượt khám (Encounter) của bệnh nhân tại bệnh viện.
/// </summary>
public static class EncounterStatuses
{
    public const string InProgress = "InProgress";
    public const string Finalized = "Finalized";
    public const string Approved = "Approved";

    public static readonly string[] AllowedStatuses = [InProgress, Finalized, Approved];
}
