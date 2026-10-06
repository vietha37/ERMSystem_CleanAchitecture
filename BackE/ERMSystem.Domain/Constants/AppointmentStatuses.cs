namespace ERMSystem.Domain.Constants;

/// <summary>
/// Các hằng số trạng thái lịch hẹn khám bệnh.
/// </summary>
public static class AppointmentStatuses
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Pending, Completed, Cancelled];
}
