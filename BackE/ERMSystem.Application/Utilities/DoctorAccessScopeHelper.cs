using System;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;

namespace ERMSystem.Application.Utilities;

/// <summary>
/// Helper kiểm tra phạm vi dữ liệu của Bác sĩ (Doctor Scoping Authorization).
/// Giúp loại bỏ code trùng lặp tại 4 service lớn (Billing, Encounter, ClinicalOrder, Prescription).
/// Đảm bảo nguyên tắc bảo mật: Bác sĩ chỉ được thao tác trên dữ liệu/bệnh nhân mà mình phụ trách.
/// </summary>
public static class DoctorAccessScopeHelper
{
    /// <summary>
    /// Nếu người dùng có role là Doctor, tra cứu DoctorProfileId tương ứng.
    /// Nếu là role khác (Admin, Cashier...), trả về null để cho phép xem toàn bộ.
    /// </summary>
    public static async Task<Guid?> ResolveScopedDoctorProfileIdAsync(
        IHospitalDoctorWorklistRepository doctorWorklistRepository,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
    {
        if (!string.Equals(currentRole, AppRole.Doctor, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(currentUsername))
        {
            return null;
        }

        var doctorProfile = await doctorWorklistRepository.ResolveDoctorByUsernameAsync(currentUsername, ct);
        return doctorProfile?.DoctorProfileId;
    }

    /// <summary>
    /// Kiểm tra quyền truy cập bản ghi y tế theo profile bác sĩ.
    /// Trả về true nếu người dùng không bị giới hạn (không phải Doctor), hoặc DoctorProfileId khớp với bản ghi.
    /// </summary>
    public static async Task<bool> CanAccessDoctorScopedDataAsync(
        IHospitalDoctorWorklistRepository doctorWorklistRepository,
        Guid? targetDoctorProfileId,
        string currentRole,
        string? currentUsername,
        CancellationToken ct = default)
    {
        var scopedDoctorProfileId = await ResolveScopedDoctorProfileIdAsync(
            doctorWorklistRepository,
            currentRole,
            currentUsername,
            ct);

        return !scopedDoctorProfileId.HasValue ||
               (targetDoctorProfileId.HasValue && scopedDoctorProfileId.Value == targetDoctorProfileId.Value);
    }
}
