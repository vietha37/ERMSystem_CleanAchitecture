namespace ERMSystem.Domain.Constants;

/// <summary>
/// Các hằng số trạng thái đơn thuốc.
/// </summary>
public static class PrescriptionStatuses
{
    public const string Issued = "Issued";
    public const string Dispensed = "Dispensed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] AllowedStatuses = [Issued, Dispensed, Cancelled];
}
