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
        return new Patient
        {
            Id = patient.Id,
            AppUserId = appUserId,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth.ToDateTime(TimeOnly.MinValue),
            Gender = patient.Gender,
            Phone = patient.Phone ?? string.Empty,
            Address = BuildAddress(patient),
            EmergencyContactName = emergencyContact?.FullName,
            EmergencyContactPhone = emergencyContact?.Phone,
            EmergencyContactRelationship = emergencyContact?.Relationship,
            CreatedAt = patient.CreatedAtUtc
        };
    }

    public static Doctor MapDoctor(HospitalDoctorProfileEntity doctorProfile)
    {
        return new Doctor
        {
            Id = doctorProfile.Id,
            FullName = doctorProfile.StaffProfile.FullName,
            Specialty = doctorProfile.Specialty.Name
        };
    }

    public static Appointment MapAppointment(HospitalAppointmentEntity appointment)
    {
        return new Appointment
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorProfileId,
            AppointmentDate = appointment.AppointmentStartUtc,
            Status = MapAppointmentStatus(appointment.Status)
        };
    }

    public static MedicalRecord MapMedicalRecord(
        HospitalEncounterEntity encounter,
        HospitalClinicalNoteEntity? note,
        HospitalDiagnosisEntity? diagnosis)
    {
        return new MedicalRecord
        {
            Id = encounter.Id,
            AppointmentId = encounter.AppointmentId ?? Guid.Empty,
            CreatedAt = encounter.CreatedAtUtc,
            Symptoms = FirstNonEmpty(note?.Subjective, encounter.Appointment?.ChiefComplaint),
            Diagnosis = diagnosis?.DiagnosisName ?? string.Empty,
            Notes = FirstNonEmpty(note?.CarePlan, note?.Assessment, encounter.Summary)
        };
    }

    public static Prescription MapPrescription(HospitalPrescriptionEntity prescription)
    {
        return new Prescription
        {
            Id = prescription.Id,
            MedicalRecordId = prescription.OrderHeader.EncounterId,
            CreatedAt = prescription.CreatedAtUtc,
            PrescriptionItems = prescription.PrescriptionItems
                .Select(MapPrescriptionItem)
                .ToList()
        };
    }

    public static PrescriptionItem MapPrescriptionItem(HospitalPrescriptionItemEntity item)
    {
        return new PrescriptionItem
        {
            Id = item.Id,
            PrescriptionId = item.PrescriptionId,
            MedicineId = item.MedicineId,
            Dosage = item.DoseInstruction,
            Duration = BuildDuration(item)
        };
    }

    public static Medicine MapMedicine(HospitalMedicineEntity medicine)
    {
        return new Medicine
        {
            Id = medicine.Id,
            Name = medicine.Name,
            Description = BuildMedicineDescription(medicine)
        };
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
