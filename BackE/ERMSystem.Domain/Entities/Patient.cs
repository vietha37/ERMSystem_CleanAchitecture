using System;
using System.Collections.Generic;
using ERMSystem.Domain.Common;
using ERMSystem.Domain.Events;
using ERMSystem.Domain.ValueObjects;

namespace ERMSystem.Domain.Entities;

/// <summary>
/// Aggregate Root đại diện cho Hồ sơ Bệnh nhân (Patient).
/// Bảo vệ các bất biến nghiệp vụ, quy tắc sáp nhập bệnh nhân và liên kết tài khoản.
/// </summary>
public sealed class Patient : AggregateRoot<Guid>
{
    public Guid? AppUserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public DateTime DateOfBirth { get; private set; }
    public string Gender { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public string? EmergencyContactRelationship { get; private set; }

    /// <summary>
    /// Đồng bộ với CreatedAtUtc của BaseEntity để tương thích backward.
    /// </summary>
    public DateTime CreatedAt
    {
        get => CreatedAtUtc;
        private set => CreatedAtUtc = value;
    }

    // Navigation property
    public ICollection<Appointment> Appointments { get; private set; } = new List<Appointment>();

    /// <summary>
    /// Thuộc tính chuyển đổi sang Value Object EmergencyContact khi có đầy đủ dữ liệu.
    /// </summary>
    public EmergencyContact? EmergencyContactInfo =>
        !string.IsNullOrWhiteSpace(EmergencyContactName) &&
        !string.IsNullOrWhiteSpace(EmergencyContactPhone) &&
        !string.IsNullOrWhiteSpace(EmergencyContactRelationship)
            ? new EmergencyContact(EmergencyContactName, EmergencyContactPhone, EmergencyContactRelationship)
            : null;

    // Parameterless constructor cho ORM và serialization
    private Patient() { }

    public Patient(
        Guid id,
        string fullName,
        DateTime dateOfBirth,
        string gender,
        string phone,
        string address,
        Guid? appUserId = null,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null,
        string? emergencyContactRelationship = null,
        DateTime? createdAtUtc = null)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        FullName = fullName ?? string.Empty;
        DateOfBirth = dateOfBirth;
        Gender = gender ?? string.Empty;
        Phone = phone ?? string.Empty;
        Address = address ?? string.Empty;
        AppUserId = appUserId;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        EmergencyContactRelationship = emergencyContactRelationship;
        CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method tạo một Patient mới, kiểm tra các bất biến nghiệp vụ và phát ra domain event.
    /// </summary>
    public static Patient Create(
        string fullName,
        DateTime dateOfBirth,
        string gender,
        string phone,
        string address,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null,
        string? emergencyContactRelationship = null,
        Guid? id = null,
        Guid? appUserId = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bệnh nhân không được để trống.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Số điện thoại bệnh nhân không được để trống.", nameof(phone));

        var patientId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();

        var patient = new Patient(
            patientId,
            fullName.Trim(),
            dateOfBirth,
            gender?.Trim() ?? string.Empty,
            phone.Trim(),
            address?.Trim() ?? string.Empty,
            appUserId,
            emergencyContactName?.Trim(),
            emergencyContactPhone?.Trim(),
            emergencyContactRelationship?.Trim(),
            DateTime.UtcNow);

        patient.RaiseDomainEvent(new PatientCreatedDomainEvent(patient.Id, patient.FullName, DateTime.UtcNow));
        return patient;
    }

    /// <summary>
    /// Khôi phục (reconstitute) Patient từ cơ sở dữ liệu mà không kích hoạt event tạo mới.
    /// </summary>
    public static Patient Reconstitute(
        Guid id,
        string fullName,
        DateTime dateOfBirth,
        string gender,
        string phone,
        string address,
        Guid? appUserId = null,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null,
        string? emergencyContactRelationship = null,
        DateTime? createdAtUtc = null)
    {
        return new Patient(
            id,
            fullName,
            dateOfBirth,
            gender,
            phone,
            address,
            appUserId,
            emergencyContactName,
            emergencyContactPhone,
            emergencyContactRelationship,
            createdAtUtc);
    }

    /// <summary>
    /// Cập nhật thông tin hành chính của bệnh nhân.
    /// </summary>
    public void UpdateProfile(
        string fullName,
        DateTime dateOfBirth,
        string gender,
        string phone,
        string address,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null,
        string? emergencyContactRelationship = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bệnh nhân không được để trống.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Số điện thoại bệnh nhân không được để trống.", nameof(phone));

        FullName = fullName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender?.Trim() ?? string.Empty;
        Phone = phone.Trim();
        Address = address?.Trim() ?? string.Empty;
        EmergencyContactName = emergencyContactName?.Trim();
        EmergencyContactPhone = emergencyContactPhone?.Trim();
        EmergencyContactRelationship = emergencyContactRelationship?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PatientUpdatedDomainEvent(Id, FullName, DateTime.UtcNow));
    }

    /// <summary>
    /// Gán người liên hệ khẩn cấp qua Value Object.
    /// </summary>
    public void SetEmergencyContact(EmergencyContact contact)
    {
        ArgumentNullException.ThrowIfNull(contact);

        EmergencyContactName = contact.FullName;
        EmergencyContactPhone = contact.Phone;
        EmergencyContactRelationship = contact.Relationship;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Xóa thông tin người liên hệ khẩn cấp.
    /// </summary>
    public void ClearEmergencyContact()
    {
        EmergencyContactName = null;
        EmergencyContactPhone = null;
        EmergencyContactRelationship = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Liên kết bệnh nhân với tài khoản người dùng portal.
    /// </summary>
    public void LinkAppUser(Guid appUserId)
    {
        if (appUserId == Guid.Empty)
            throw new ArgumentException("AppUserId không hợp lệ.", nameof(appUserId));

        if (AppUserId.HasValue && AppUserId.Value != appUserId)
            throw new InvalidOperationException($"Bệnh nhân đã được liên kết với người dùng khác ({AppUserId.Value}).");

        AppUserId = appUserId;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PatientAppUserLinkedDomainEvent(Id, appUserId, DateTime.UtcNow));
    }

    /// <summary>
    /// Hủy liên kết tài khoản portal khỏi bệnh nhân.
    /// </summary>
    public void UnlinkAppUser()
    {
        if (!AppUserId.HasValue) return;

        AppUserId = null;
        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PatientAppUserUnlinkedDomainEvent(Id, DateTime.UtcNow));
    }

    /// <summary>
    /// Sáp nhập thông tin từ một hồ sơ bệnh nhân trùng lặp (source) vào hồ sơ hiện tại (target/this).
    /// Tuân thủ quy tắc bảo toàn dữ liệu và bảo vệ bất biến Aggregate.
    /// </summary>
    public void MergeWith(Patient sourcePatient, string actor = "System")
    {
        ArgumentNullException.ThrowIfNull(sourcePatient);

        if (sourcePatient.Id == Id)
            throw new InvalidOperationException("Không thể sáp nhập một hồ sơ bệnh nhân vào chính nó.");

        if (sourcePatient.AppUserId.HasValue &&
            AppUserId.HasValue &&
            sourcePatient.AppUserId != AppUserId)
        {
            throw new InvalidOperationException("Không thể sáp nhập hai hồ sơ bệnh nhân đang liên kết với hai tài khoản người dùng khác nhau.");
        }

        // Chuyển liên kết portal nếu target chưa có nhưng source đã có
        if (!AppUserId.HasValue && sourcePatient.AppUserId.HasValue)
        {
            AppUserId = sourcePatient.AppUserId;
        }

        // Cập nhật các trường thông tin ưu tiên
        FullName = SelectPreferredRequiredValue(FullName, sourcePatient.FullName);
        Phone = SelectPreferredRequiredValue(Phone, sourcePatient.Phone);
        Address = SelectPreferredRequiredValue(Address, sourcePatient.Address);
        Gender = SelectPreferredRequiredValue(Gender, sourcePatient.Gender);
        EmergencyContactName = SelectPreferredValue(EmergencyContactName, sourcePatient.EmergencyContactName);
        EmergencyContactPhone = SelectPreferredValue(EmergencyContactPhone, sourcePatient.EmergencyContactPhone);
        EmergencyContactRelationship = SelectPreferredValue(EmergencyContactRelationship, sourcePatient.EmergencyContactRelationship);

        if (DateOfBirth == default && sourcePatient.DateOfBirth != default)
        {
            DateOfBirth = sourcePatient.DateOfBirth;
        }

        UpdatedAtUtc = DateTime.UtcNow;

        RaiseDomainEvent(new PatientMergedDomainEvent(Id, sourcePatient.Id, actor, DateTime.UtcNow));
    }

    private static string SelectPreferredRequiredValue(string targetValue, string sourceValue)
    {
        if (!string.IsNullOrWhiteSpace(targetValue))
            return targetValue.Trim();

        return !string.IsNullOrWhiteSpace(sourceValue) ? sourceValue.Trim() : string.Empty;
    }

    private static string? SelectPreferredValue(string? targetValue, string? sourceValue)
    {
        if (!string.IsNullOrWhiteSpace(targetValue))
            return targetValue.Trim();

        return !string.IsNullOrWhiteSpace(sourceValue) ? sourceValue.Trim() : null;
    }
}
