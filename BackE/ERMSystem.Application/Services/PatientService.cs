using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Domain.Entities;

namespace ERMSystem.Application.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IComplianceAuditRecorder _complianceAuditRecorder;

        public PatientService(
            IPatientRepository patientRepository,
            IComplianceAuditRecorder complianceAuditRecorder)
        {
            _patientRepository = patientRepository;
            _complianceAuditRecorder = complianceAuditRecorder;
        }

        public async Task<PaginatedResult<PatientDto>> GetAllPatientsAsync(PaginationRequest request, CancellationToken ct = default)
        {
            var (items, totalCount) = await _patientRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.TextSearch,
                ct);
            return new PaginatedResult<PatientDto>(items.Select(MapToDto), totalCount, request.PageNumber, request.PageSize);
        }

        public async Task<PatientDto?> GetPatientByIdAsync(Guid id, CancellationToken ct = default)
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct);
            return patient == null ? null : MapToDto(patient);
        }

        public async Task<PatientDto?> GetPatientByAppUserIdAsync(Guid appUserId, CancellationToken ct = default)
        {
            var patient = await _patientRepository.GetByAppUserIdAsync(appUserId, ct);
            return patient == null ? null : MapToDto(patient);
        }

        public async Task<IReadOnlyCollection<PotentialDuplicatePatientDto>> GetPotentialDuplicatesAsync(Guid patientId, CancellationToken ct = default)
        {
            var patient = await _patientRepository.GetByIdAsync(patientId, ct);
            if (patient == null)
            {
                throw new KeyNotFoundException($"Patient with ID {patientId} not found.");
            }

            var candidates = await _patientRepository.GetPotentialDuplicateCandidatesAsync(
                patient.Id,
                patient.FullName,
                patient.DateOfBirth,
                patient.Phone,
                patient.EmergencyContactPhone,
                ct);

            return candidates
                .Select(candidate => new PotentialDuplicatePatientDto
                {
                    Patient = MapToDto(candidate),
                    MatchReasons = BuildMatchReasons(patient, candidate)
                })
                .Where(x => x.MatchReasons.Count > 0)
                .OrderByDescending(x => x.MatchReasons.Count)
                .ThenBy(x => x.Patient.FullName)
                .ToArray();
        }

        public async Task<PatientDto> CreatePatientAsync(CreatePatientDto dto, CancellationToken ct = default)
        {
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                AppUserId = null,
                FullName = dto.FullName,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Phone = dto.Phone,
                Address = dto.Address,
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                EmergencyContactRelationship = dto.EmergencyContactRelationship,
                CreatedAt = DateTime.UtcNow
            };

            await _patientRepository.AddAsync(patient, ct);
            return MapToDto(patient);
        }

        public async Task UpdatePatientAsync(Guid id, UpdatePatientDto dto, CancellationToken ct = default)
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct);
            if (patient == null)
                throw new KeyNotFoundException($"Patient with ID {id} not found.");

            patient.FullName = dto.FullName;
            patient.DateOfBirth = dto.DateOfBirth;
            patient.Gender = dto.Gender;
            patient.Phone = dto.Phone;
            patient.Address = dto.Address;
            patient.EmergencyContactName = dto.EmergencyContactName;
            patient.EmergencyContactPhone = dto.EmergencyContactPhone;
            patient.EmergencyContactRelationship = dto.EmergencyContactRelationship;

            await _patientRepository.UpdateAsync(patient, ct);
        }

        public async Task<MergePatientsResultDto> MergePatientsAsync(
            MergePatientsRequestDto request,
            Guid? actorUserId,
            string? actorUsername,
            CancellationToken ct = default)
        {
            if (request.SourcePatientId == request.TargetPatientId)
            {
                throw new InvalidOperationException("Source patient and target patient must be different.");
            }

            var sourcePatient = await _patientRepository.GetByIdAsync(request.SourcePatientId, ct);
            if (sourcePatient == null)
            {
                throw new KeyNotFoundException($"Source patient with ID {request.SourcePatientId} not found.");
            }

            var targetPatient = await _patientRepository.GetByIdAsync(request.TargetPatientId, ct);
            if (targetPatient == null)
            {
                throw new KeyNotFoundException($"Target patient with ID {request.TargetPatientId} not found.");
            }

            if (sourcePatient.AppUserId.HasValue &&
                targetPatient.AppUserId.HasValue &&
                sourcePatient.AppUserId != targetPatient.AppUserId)
            {
                throw new InvalidOperationException("Cannot merge patients that are linked to different app users.");
            }

            var appUserLinkMoved = false;
            if (!targetPatient.AppUserId.HasValue && sourcePatient.AppUserId.HasValue)
            {
                targetPatient.AppUserId = sourcePatient.AppUserId;
                appUserLinkMoved = true;
            }

            targetPatient.FullName = SelectPreferredRequiredValue(targetPatient.FullName, sourcePatient.FullName);
            targetPatient.Phone = SelectPreferredRequiredValue(targetPatient.Phone, sourcePatient.Phone);
            targetPatient.Address = SelectPreferredRequiredValue(targetPatient.Address, sourcePatient.Address);
            targetPatient.Gender = SelectPreferredRequiredValue(targetPatient.Gender, sourcePatient.Gender);
            targetPatient.EmergencyContactName = SelectPreferredValue(targetPatient.EmergencyContactName, sourcePatient.EmergencyContactName);
            targetPatient.EmergencyContactPhone = SelectPreferredValue(targetPatient.EmergencyContactPhone, sourcePatient.EmergencyContactPhone);
            targetPatient.EmergencyContactRelationship = SelectPreferredValue(targetPatient.EmergencyContactRelationship, sourcePatient.EmergencyContactRelationship);
            if (targetPatient.DateOfBirth == default && sourcePatient.DateOfBirth != default)
            {
                targetPatient.DateOfBirth = sourcePatient.DateOfBirth;
            }

            var reassignedAppointmentCount = await _patientRepository.ReassignAppointmentsAsync(
                sourcePatient.Id,
                targetPatient.Id,
                ct);

            await _patientRepository.MergeAsync(sourcePatient, targetPatient, ct);

            await _complianceAuditRecorder.RecordAsync(
                actorUserId,
                actorUsername ?? "unknown",
                "PatientMerged",
                "Warning",
                $"SourcePatientId={sourcePatient.Id}; TargetPatientId={targetPatient.Id}; ReassignedAppointments={reassignedAppointmentCount}; AppUserLinkMoved={appUserLinkMoved}.",
                ct);

            return new MergePatientsResultDto
            {
                SourcePatientId = sourcePatient.Id,
                TargetPatientId = targetPatient.Id,
                ReassignedAppointmentCount = reassignedAppointmentCount,
                AppUserLinkMoved = appUserLinkMoved
            };
        }

        public async Task DeletePatientAsync(Guid id, CancellationToken ct = default)
        {
            var patient = await _patientRepository.GetByIdAsync(id, ct);
            if (patient == null)
                throw new KeyNotFoundException($"Patient with ID {id} not found.");

            var appointmentCount = await _patientRepository.GetAppointmentCountAsync(id, ct);
            if (appointmentCount > 0)
            {
                throw new InvalidOperationException(
                    $"Cannot delete patient with ID {id} because there are {appointmentCount} linked appointments.");
            }

            await _patientRepository.DeleteAsync(patient, ct);
        }

        private static PatientDto MapToDto(Patient p) => new PatientDto
        {
            Id = p.Id,
            FullName = p.FullName,
            DateOfBirth = p.DateOfBirth,
            Gender = p.Gender,
            Phone = p.Phone,
            Address = p.Address,
            EmergencyContactName = p.EmergencyContactName,
            EmergencyContactPhone = p.EmergencyContactPhone,
            EmergencyContactRelationship = p.EmergencyContactRelationship,
            CreatedAt = p.CreatedAt
        };

        private static IReadOnlyCollection<string> BuildMatchReasons(Patient source, Patient candidate)
        {
            var reasons = new List<string>();

            if (AreSamePhone(source.Phone, candidate.Phone))
            {
                reasons.Add("same_phone");
            }

            if (AreSameNameAndDob(source, candidate))
            {
                reasons.Add("same_name_and_dob");
            }

            if (!string.IsNullOrWhiteSpace(source.EmergencyContactPhone) &&
                AreSamePhone(source.EmergencyContactPhone, candidate.EmergencyContactPhone))
            {
                reasons.Add("same_emergency_contact_phone");
            }

            return reasons
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static bool AreSameNameAndDob(Patient source, Patient candidate)
            => string.Equals(
                   NormalizeText(source.FullName),
                   NormalizeText(candidate.FullName),
                   StringComparison.Ordinal)
               && source.DateOfBirth.Date == candidate.DateOfBirth.Date;

        private static bool AreSamePhone(string? left, string? right)
            => !string.IsNullOrWhiteSpace(left)
               && !string.IsNullOrWhiteSpace(right)
               && string.Equals(NormalizePhone(left), NormalizePhone(right), StringComparison.Ordinal);

        private static string NormalizePhone(string value)
            => new string(value.Where(char.IsDigit).ToArray());

        private static string NormalizeText(string value)
            => (value ?? string.Empty).Trim().ToUpperInvariant();

        private static string SelectPreferredRequiredValue(string currentValue, string fallbackValue)
            => string.IsNullOrWhiteSpace(currentValue) ? fallbackValue : currentValue;

        private static string? SelectPreferredValue(string? currentValue, string? fallbackValue)
            => string.IsNullOrWhiteSpace(currentValue) ? fallbackValue : currentValue;
    }
}
