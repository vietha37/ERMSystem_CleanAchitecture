using ERMSystem.Domain.Entities;
using ERMSystem.Infrastructure.HospitalData.Entities;

namespace ERMSystem.Infrastructure.Repositories;

internal static class HospitalLegacyRepositoryMapper
{
    public static Patient MapPatient(
        HospitalPatientEntity patient,
        Guid? appUserId = null,
        HospitalPatientEmergencyContactEntity? emergencyContact = null)
    {
        return Patient.Reconstitute(
            patient.Id,
            patient.FullName,
            patient.DateOfBirth.ToDateTime(TimeOnly.MinValue),
            patient.Gender,
            patient.Phone ?? string.Empty,
            BuildAddress(patient),
            appUserId,
            emergencyContact?.FullName,
            emergencyContact?.Phone,
            emergencyContact?.Relationship,
            patient.CreatedAtUtc);
    }

    public static Doctor MapDoctor(HospitalDoctorProfileEntity doctorProfile)
    {
        return Doctor.Reconstitute(
            doctorProfile.Id,
            doctorProfile.StaffProfile.FullName,
            doctorProfile.Specialty.Name);
    }

    public static Appointment MapAppointment(HospitalAppointmentEntity appointment)
    {
        return Appointment.Reconstitute(
            appointment.Id,
            appointment.PatientId,
            appointment.DoctorProfileId,
            appointment.AppointmentStartUtc,
            MapAppointmentStatus(appointment.Status));
    }

    public static MedicalRecord MapMedicalRecord(
        HospitalEncounterEntity encounter,
        HospitalClinicalNoteEntity? note,
        HospitalDiagnosisEntity? diagnosis)
    {
        return MedicalRecord.Reconstitute(
            encounter.Id,
            encounter.AppointmentId ?? Guid.Empty,
            FirstNonEmpty(note?.Subjective, encounter.Appointment?.ChiefComplaint),
            diagnosis?.DiagnosisName ?? string.Empty,
            FirstNonEmpty(note?.CarePlan, note?.Assessment, encounter.Summary),
            encounter.CreatedAtUtc);
    }

    public static Prescription MapPrescription(HospitalPrescriptionEntity prescription)
    {
        var items = prescription.PrescriptionItems
            .Select(MapPrescriptionItem)
            .ToList();

        return Prescription.Reconstitute(
            prescription.Id,
            prescription.OrderHeader.EncounterId,
            prescription.CreatedAtUtc,
            items);
    }

    public static PrescriptionItem MapPrescriptionItem(HospitalPrescriptionItemEntity item)
    {
        return PrescriptionItem.Reconstitute(
            item.Id,
            item.PrescriptionId,
            item.MedicineId,
            item.DoseInstruction,
            BuildDuration(item));
    }

    public static Medicine MapMedicine(HospitalMedicineEntity medicine)
    {
        return Medicine.Reconstitute(
            medicine.Id,
            medicine.Name,
            BuildMedicineDescription(medicine));
    }

    public static string MapAppointmentStatus(string status)
    {
        return status switch
        {
            "Completed" => "Completed",
            "Cancelled" => "Cancelled",
            _ => "Pending"
        };
    }

    public static string BuildAddress(HospitalPatientEntity patient)
    {
        var parts = new[]
        {
            patient.AddressLine1,
            patient.AddressLine2,
            patient.Ward,
            patient.District,
            patient.Province
        };

        return string.Join(", ", parts.Where(static x => !string.IsNullOrWhiteSpace(x)));
    }

    public static string BuildMedicineDescription(HospitalMedicineEntity medicine)
    {
        var parts = new[]
        {
            medicine.GenericName,
            medicine.Strength,
            medicine.DosageForm,
            medicine.Unit
        };

        return string.Join(" | ", parts.Where(static x => !string.IsNullOrWhiteSpace(x)));
    }

    public static string BuildDuration(HospitalPrescriptionItemEntity item)
    {
        if (item.DurationDays is int durationDays && durationDays > 0)
        {
            return $"{durationDays} day(s)";
        }

        return item.Frequency ?? string.Empty;
    }

    public static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(static x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
    }
}
