namespace ERMSystem.Domain.Constants;

/// <summary>
/// Các hằng số trạng thái hóa đơn viện phí.
/// </summary>
public static class InvoiceStatuses
{
    public const string Issued = "Issued";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";

    public static readonly string[] AllowedStatuses = [Issued, PartiallyPaid, Paid, Cancelled];
}
