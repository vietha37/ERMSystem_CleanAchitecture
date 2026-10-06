using System;

namespace ERMSystem.Application.Utilities;

/// <summary>
/// Tiện ích xử lý múi giờ và thời gian chuẩn cho phòng khám / bệnh viện (mặc định GMT+7 "SE Asia Standard Time").
/// Giúp loại bỏ hoàn toàn code trùng lặp tại hơn 16 file trong hệ thống, tối ưu hiệu năng bằng Lazy caching.
/// </summary>
public static class ClinicDateTimeHelper
{
    public const string ClinicTimeZoneId = "SE Asia Standard Time";

    private static readonly Lazy<TimeZoneInfo> ClinicTimeZoneLazy = new(() =>
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(ClinicTimeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    });

    /// <summary>
    /// Lấy đối tượng TimeZoneInfo của phòng khám, tự động fallback sang UTC nếu hệ điều hành không hỗ trợ.
    /// </summary>
    public static TimeZoneInfo ResolveClinicTimeZone() => ClinicTimeZoneLazy.Value;

    /// <summary>
    /// Chuyển đổi thời gian từ chuẩn UTC sang giờ địa phương của phòng khám.
    /// </summary>
    public static DateTime ConvertUtcToClinicLocal(DateTime utcDateTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
            ResolveClinicTimeZone());
    }

    /// <summary>
    /// Chuyển đổi thời gian từ giờ địa phương phòng khám sang chuẩn UTC.
    /// </summary>
    public static DateTime ConvertClinicLocalToUtc(DateTime localDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified),
            ResolveClinicTimeZone());
    }

    /// <summary>
    /// Chuyển đổi thời gian nullable từ chuẩn UTC sang giờ địa phương của phòng khám.
    /// </summary>
    public static DateTime? ConvertUtcToClinicLocal(DateTime? utcDateTime)
    {
        return utcDateTime.HasValue ? ConvertUtcToClinicLocal(utcDateTime.Value) : null;
    }
}
