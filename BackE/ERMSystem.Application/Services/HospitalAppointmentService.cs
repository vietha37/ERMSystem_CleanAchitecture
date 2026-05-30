using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.DTOs.Common;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Utilities;

namespace ERMSystem.Application.Services
{
    public class HospitalAppointmentService : IHospitalAppointmentService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IHospitalDoctorRepository _hospitalDoctorRepository;
        private readonly IHospitalDoctorWorklistRepository _hospitalDoctorWorklistRepository;
        private readonly IHospitalAppointmentRepository _hospitalAppointmentRepository;
        private readonly IBusinessMetricsRecorder _businessMetricsRecorder;

        public HospitalAppointmentService(
            IHospitalDoctorRepository hospitalDoctorRepository,
            IHospitalDoctorWorklistRepository hospitalDoctorWorklistRepository,
            IHospitalAppointmentRepository hospitalAppointmentRepository,
            IBusinessMetricsRecorder businessMetricsRecorder)
        {
            _hospitalDoctorRepository = hospitalDoctorRepository;
            _hospitalDoctorWorklistRepository = hospitalDoctorWorklistRepository;
            _hospitalAppointmentRepository = hospitalAppointmentRepository;
            _businessMetricsRecorder = businessMetricsRecorder;
        }

        public async Task<HospitalAppointmentBookingResultDto> BookPublicAppointmentAsync(
            PublicHospitalAppointmentBookingRequestDto request,
            CancellationToken ct = default)
        {
            var doctor = await _hospitalDoctorRepository.GetDoctorByIdAsync(request.DoctorProfileId, ct);
            if (doctor == null)
            {
                throw new KeyNotFoundException("Khong tim thay bac si duoc chon.");
            }

            if (!doctor.IsBookable)
            {
                throw new InvalidOperationException("Bac si hien khong nhan dat lich online.");
            }

            if (request.SpecialtyId.HasValue && request.SpecialtyId.Value != doctor.SpecialtyId)
            {
                throw new InvalidOperationException("Chuyen khoa va bac si duoc chon khong khop nhau.");
            }

            var matchingSchedule = doctor.Schedules
                .Where(x => x.DayOfWeek == (byte)request.PreferredDate.DayOfWeek)
                .Where(x => x.ValidFrom <= request.PreferredDate && (!x.ValidTo.HasValue || x.ValidTo.Value >= request.PreferredDate))
                .Where(x => x.StartTime <= request.PreferredTime)
                .Where(x => request.PreferredTime.AddMinutes(x.SlotMinutes) <= x.EndTime)
                .OrderBy(x => x.StartTime)
                .FirstOrDefault();

            if (matchingSchedule == null)
            {
                throw new InvalidOperationException("Khung gio duoc chon khong nam trong lich lam viec cua bac si.");
            }

            var appointmentStartLocal = request.PreferredDate.ToDateTime(request.PreferredTime);
            var appointmentEndLocal = appointmentStartLocal.AddMinutes(matchingSchedule.SlotMinutes);
            var appointmentStartUtc = ConvertLocalClinicTimeToUtc(appointmentStartLocal);
            var appointmentEndUtc = ConvertLocalClinicTimeToUtc(appointmentEndLocal);

            var hasConflict = await _hospitalAppointmentRepository.HasDoctorConflictAsync(
                doctor.DoctorProfileId,
                appointmentStartUtc,
                appointmentEndUtc,
                null,
                ct);

            if (hasConflict)
            {
                throw new InvalidOperationException("Khung gio nay da co lich hen. Vui long chon gio khac.");
            }

            var existingPatient = await _hospitalAppointmentRepository.FindMatchingPatientAsync(
                request.FullName.Trim(),
                request.DateOfBirth,
                request.Phone.Trim(),
                string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                ct);

            var patientId = existingPatient?.PatientId ?? Guid.NewGuid();
            var isExistingPatient = existingPatient != null;
            var nowUtc = DateTime.UtcNow;

            if (!isExistingPatient)
            {
                await _hospitalAppointmentRepository.AddPatientAsync(new HospitalBookingPatientCreateCommand
                {
                    PatientId = patientId,
                    MedicalRecordNumber = GenerateMedicalRecordNumber(nowUtc),
                    FullName = request.FullName.Trim(),
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender.Trim(),
                    Phone = request.Phone.Trim(),
                    Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                    CreatedAtUtc = nowUtc,
                    UpdatedAtUtc = nowUtc
                }, ct);
            }

            var appointmentId = Guid.NewGuid();
            var appointmentNumber = GenerateAppointmentNumber(nowUtc);
            var normalizedNotes = BuildNotes(request.Notes, request.ServiceCode);

            await _hospitalAppointmentRepository.AddAppointmentAsync(new HospitalBookingAppointmentCreateCommand
            {
                AppointmentId = appointmentId,
                AppointmentNumber = appointmentNumber,
                PatientId = patientId,
                DoctorProfileId = doctor.DoctorProfileId,
                ClinicId = matchingSchedule.ClinicId,
                AppointmentType = "Outpatient",
                BookingChannel = "Website",
                Status = "Scheduled",
                AppointmentStartUtc = appointmentStartUtc,
                AppointmentEndUtc = appointmentEndUtc,
                ChiefComplaint = string.IsNullOrWhiteSpace(request.ChiefComplaint) ? null : request.ChiefComplaint.Trim(),
                Notes = normalizedNotes,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            }, ct);

            await _hospitalAppointmentRepository.AddOutboxMessageAsync(new HospitalOutboxMessageCreateCommand
            {
                OutboxMessageId = Guid.NewGuid(),
                AggregateType = "Appointment",
                AggregateId = appointmentId,
                EventType = "AppointmentCreated.v1",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    appointmentId,
                    appointmentNumber,
                    patientId,
                    patientName = request.FullName.Trim(),
                    phone = request.Phone.Trim(),
                    email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                    doctorProfileId = doctor.DoctorProfileId,
                    doctorName = doctor.FullName,
                    specialtyName = doctor.SpecialtyName,
                    clinicName = matchingSchedule.ClinicName,
                    appointmentStartLocal,
                    appointmentEndLocal,
                    channel = "Website"
                }, JsonOptions),
                Status = "Pending",
                AvailableAtUtc = nowUtc
            }, ct);

            await _hospitalAppointmentRepository.SaveChangesAsync(ct);

            _businessMetricsRecorder.IncrementEvent("hospital_appointment", "booked", new Dictionary<string, string?>
            {
                ["channel"] = "website",
                ["patient_type"] = isExistingPatient ? "existing" : "new"
            });

            return new HospitalAppointmentBookingResultDto
            {
                AppointmentId = appointmentId,
                AppointmentNumber = appointmentNumber,
                PatientId = patientId,
                DoctorProfileId = doctor.DoctorProfileId,
                DoctorName = doctor.FullName,
                SpecialtyName = doctor.SpecialtyName,
                ClinicName = matchingSchedule.ClinicName,
                AppointmentStartLocal = appointmentStartLocal,
                AppointmentEndLocal = appointmentEndLocal,
                Status = "Scheduled",
                IsExistingPatient = isExistingPatient,
                NotificationQueued = true
            };
        }

        public Task<PaginatedResult<HospitalAppointmentWorklistItemDto>> GetWorklistAsync(
            HospitalAppointmentWorklistRequestDto request,
            string currentRole,
            string? currentUsername,
            CancellationToken ct = default)
            => GetScopedWorklistAsync(request, currentRole, currentUsername, ct);

        public async Task<HospitalAppointmentWorklistItemDto?> CheckInAsync(
            Guid appointmentId,
            HospitalAppointmentCheckInRequestDto request,
            CancellationToken ct = default)
        {
            var appointment = await _hospitalAppointmentRepository.GetAppointmentAggregateAsync(appointmentId, ct);
            if (appointment == null)
            {
                return null;
            }

            if (appointment.Status is "Cancelled" or "Completed")
            {
                throw new InvalidOperationException("Lich hen nay khong the check-in o trang thai hien tai.");
            }

            if (appointment.CheckInId == null)
            {
                await _hospitalAppointmentRepository.AddCheckInAsync(new HospitalAppointmentCheckInCommand
                {
                    CheckInId = Guid.NewGuid(),
                    AppointmentId = appointment.AppointmentId,
                    CheckInTimeUtc = DateTime.UtcNow,
                    CounterLabel = string.IsNullOrWhiteSpace(request.CounterLabel) ? null : request.CounterLabel.Trim(),
                    CheckInStatus = "CheckedIn"
                }, ct);
            }

            if (string.IsNullOrWhiteSpace(appointment.QueueNumber))
            {
                var nextSequence = await _hospitalAppointmentRepository.GetNextQueueSequenceAsync(appointment.AppointmentStartUtc, ct);
                await _hospitalAppointmentRepository.AddQueueTicketAsync(new HospitalAppointmentQueueTicketCreateCommand
                {
                    QueueTicketId = Guid.NewGuid(),
                    AppointmentId = appointment.AppointmentId,
                    QueueNumber = $"Q{nextSequence:000}",
                    QueueStatus = "Waiting"
                }, ct);
            }

            await _hospitalAppointmentRepository.UpdateStatusAsync(appointment.AppointmentId, "CheckedIn", ct);
            await _hospitalAppointmentRepository.SaveChangesAsync(ct);

            _businessMetricsRecorder.IncrementEvent("hospital_appointment", "checked_in");

            var refreshed = await _hospitalAppointmentRepository.GetAppointmentAggregateAsync(appointmentId, ct);
            return refreshed == null ? null : MapAggregateToWorklistItem(refreshed);
        }

        public async Task<HospitalAppointmentWorklistItemDto?> UpdateStatusAsync(
            Guid appointmentId,
            HospitalAppointmentStatusUpdateRequestDto request,
            CancellationToken ct = default)
        {
            var normalizedStatus = request.Status.Trim();
            if (!string.Equals(normalizedStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Endpoint nay chi dung de chuyen lich hen sang Completed.");
            }

            var appointment = await _hospitalAppointmentRepository.GetAppointmentAggregateAsync(appointmentId, ct);
            if (appointment == null)
            {
                return null;
            }

            if (appointment.Status == "Cancelled" && !string.Equals(normalizedStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Lich hen da huy khong the chuyen sang trang thai khac.");
            }

            if (string.Equals(appointment.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Lich hen da hoan thanh.");
            }

            var previousStatus = appointment.Status;
            await _hospitalAppointmentRepository.UpdateStatusAsync(appointmentId, normalizedStatus, ct);

            if (!string.Equals(previousStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase))
            {
                var appointmentStartLocal = ConvertUtcToClinicLocal(appointment.AppointmentStartUtc);
                var appointmentEndLocal = appointment.AppointmentEndUtc.HasValue
                    ? ConvertUtcToClinicLocal(appointment.AppointmentEndUtc.Value)
                    : (DateTime?)null;
                var eventType = string.Equals(normalizedStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)
                    ? "AppointmentCancelled.v1"
                    : "AppointmentUpdated.v1";

                await _hospitalAppointmentRepository.AddOutboxMessageAsync(new HospitalOutboxMessageCreateCommand
                {
                    OutboxMessageId = Guid.NewGuid(),
                    AggregateType = "Appointment",
                    AggregateId = appointment.AppointmentId,
                    EventType = eventType,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        appointmentId = appointment.AppointmentId,
                        appointmentNumber = appointment.AppointmentNumber,
                        patientId = appointment.PatientId,
                        patientName = appointment.PatientName,
                        phone = appointment.PatientPhone,
                        email = appointment.PatientEmail,
                        doctorProfileId = appointment.DoctorProfileId,
                        doctorName = appointment.DoctorName,
                        specialtyName = appointment.SpecialtyName,
                        clinicName = appointment.ClinicName,
                        appointmentStartLocal,
                        appointmentEndLocal,
                        previousStatus,
                        currentStatus = normalizedStatus,
                        channel = appointment.BookingChannel
                    }, JsonOptions),
                    Status = "Pending",
                    AvailableAtUtc = DateTime.UtcNow
                }, ct);
            }

            await _hospitalAppointmentRepository.SaveChangesAsync(ct);

            if (!string.Equals(previousStatus, normalizedStatus, StringComparison.OrdinalIgnoreCase))
            {
                _businessMetricsRecorder.IncrementEvent("hospital_appointment", "status_updated", new Dictionary<string, string?>
                {
                    ["current_status"] = normalizedStatus,
                    ["previous_status"] = previousStatus
                });
            }

            var refreshed = await _hospitalAppointmentRepository.GetAppointmentAggregateAsync(appointmentId, ct);
            return refreshed == null ? null : MapAggregateToWorklistItem(refreshed);
        }

        public async Task<HospitalAppointmentWorklistItemDto?> CancelAsync(
            Guid appointmentId,
            HospitalAppointmentCancelRequestDto request,
            CancellationToken ct = default)
        {
            var appointment = await _hospitalAppointmentRepository.GetAppointmentAggregateAsync(appointmentId, ct);
            if (appointment == null)
            {
                return null;
            }

            if (!string.Equals(appointment.Status, "Scheduled", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chi duoc huy lich hen dang Scheduled.");
            }

            var nowUtc = DateTime.UtcNow;
            await _hospitalAppointmentRepository.UpdateStatusAsync(appointmentId, "Cancelled", ct);

            var appointmentStartLocal = ConvertUtcToClinicLocal(appointment.AppointmentStartUtc);
            var appointmentEndLocal = appointment.AppointmentEndUtc.HasValue
                ? ConvertUtcToClinicLocal(appointment.AppointmentEndUtc.Value)
                : (DateTime?)null;
            var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

            await _hospitalAppointmentRepository.AddOutboxMessageAsync(new HospitalOutboxMessageCreateCommand
            {
                OutboxMessageId = Guid.NewGuid(),
                AggregateType = "Appointment",
                AggregateId = appointment.AppointmentId,
                EventType = "AppointmentCancelled.v1",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    appointmentId = appointment.AppointmentId,
                    appointmentNumber = appointment.AppointmentNumber,
                    patientId = appointment.PatientId,
                    patientName = appointment.PatientName,
                    phone = appointment.PatientPhone,
                    email = appointment.PatientEmail,
                    doctorProfileId = appointment.DoctorProfileId,
                    doctorName = appointment.DoctorName,
                    specialtyName = appointment.SpecialtyName,
                    clinicName = appointment.ClinicName,
                    appointmentStartLocal,
                    appointmentEndLocal,
                    previousStatus = appointment.Status,
                    currentStatus = "Cancelled",
                    channel = appointment.BookingChannel,
                    reason
                }, JsonOptions),
                Status = "Pending",
                AvailableAtUtc = nowUtc
            }, ct);

            await _hospitalAppointmentRepository.SaveChangesAsync(ct);

            _businessMetricsRecorder.IncrementEvent("hospital_appointment", "cancelled", new Dictionary<string, string?>
            {
                ["channel"] = appointment.BookingChannel,
                ["had_reason"] = string.IsNullOrWhiteSpace(reason) ? "false" : "true"
            });

            var refreshed = await _hospitalAppointmentRepository.GetAppointmentAggregateAsync(appointmentId, ct);
            return refreshed == null ? null : MapAggregateToWorklistItem(refreshed);
        }

        public async Task<HospitalAppointmentWorklistItemDto?> RescheduleAsync(
            Guid appointmentId,
            HospitalAppointmentRescheduleRequestDto request,
            CancellationToken ct = default)
        {
            var appointment = await _hospitalAppointmentRepository.GetAppointmentAggregateAsync(appointmentId, ct);
            if (appointment == null)
            {
                return null;
            }

            if (!string.Equals(appointment.Status, "Scheduled", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chi duoc doi lich hen dang Scheduled.");
            }

            var doctor = await _hospitalDoctorRepository.GetDoctorByIdAsync(appointment.DoctorProfileId, ct);
            if (doctor == null)
            {
                throw new InvalidOperationException("Khong tim thay thong tin bac si cua lich hen.");
            }

            var matchingSchedule = doctor.Schedules
                .Where(x => x.DayOfWeek == (byte)request.PreferredDate.DayOfWeek)
                .Where(x => x.ValidFrom <= request.PreferredDate && (!x.ValidTo.HasValue || x.ValidTo.Value >= request.PreferredDate))
                .Where(x => x.StartTime <= request.PreferredTime)
                .Where(x => request.PreferredTime.AddMinutes(x.SlotMinutes) <= x.EndTime)
                .OrderBy(x => x.StartTime)
                .FirstOrDefault();

            if (matchingSchedule == null)
            {
                throw new InvalidOperationException("Khung gio doi lich khong nam trong lich lam viec cua bac si.");
            }

            var nextStartLocal = request.PreferredDate.ToDateTime(request.PreferredTime);
            var nextEndLocal = nextStartLocal.AddMinutes(matchingSchedule.SlotMinutes);
            var nextStartUtc = ConvertLocalClinicTimeToUtc(nextStartLocal);
            var nextEndUtc = ConvertLocalClinicTimeToUtc(nextEndLocal);
            if (nextStartUtc <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Khong the doi lich sang thoi diem trong qua khu.");
            }

            var hasConflict = await _hospitalAppointmentRepository.HasDoctorConflictAsync(
                appointment.DoctorProfileId,
                nextStartUtc,
                nextEndUtc,
                appointment.AppointmentId,
                ct);
            if (hasConflict)
            {
                throw new InvalidOperationException("Khung gio doi lich da co lich hen khac. Vui long chon gio khac.");
            }

            var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
            var mergedNotes = BuildRescheduleNotes(reason, appointment.AppointmentStartUtc, nextStartUtc);
            await _hospitalAppointmentRepository.UpdateScheduleAsync(
                appointment.AppointmentId,
                matchingSchedule.ClinicId,
                nextStartUtc,
                nextEndUtc,
                mergedNotes,
                ct);

            await _hospitalAppointmentRepository.AddOutboxMessageAsync(new HospitalOutboxMessageCreateCommand
            {
                OutboxMessageId = Guid.NewGuid(),
                AggregateType = "Appointment",
                AggregateId = appointment.AppointmentId,
                EventType = "AppointmentUpdated.v1",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    appointmentId = appointment.AppointmentId,
                    appointmentNumber = appointment.AppointmentNumber,
                    patientId = appointment.PatientId,
                    patientName = appointment.PatientName,
                    phone = appointment.PatientPhone,
                    email = appointment.PatientEmail,
                    doctorProfileId = appointment.DoctorProfileId,
                    doctorName = appointment.DoctorName,
                    specialtyName = appointment.SpecialtyName,
                    clinicName = matchingSchedule.ClinicName,
                    previousStatus = appointment.Status,
                    currentStatus = appointment.Status,
                    previousAppointmentStartLocal = ConvertUtcToClinicLocal(appointment.AppointmentStartUtc),
                    previousAppointmentEndLocal = appointment.AppointmentEndUtc.HasValue
                        ? ConvertUtcToClinicLocal(appointment.AppointmentEndUtc.Value)
                        : (DateTime?)null,
                    appointmentStartLocal = nextStartLocal,
                    appointmentEndLocal = nextEndLocal,
                    channel = appointment.BookingChannel,
                    changeType = "Rescheduled",
                    reason
                }, JsonOptions),
                Status = "Pending",
                AvailableAtUtc = DateTime.UtcNow
            }, ct);

            await _hospitalAppointmentRepository.SaveChangesAsync(ct);

            _businessMetricsRecorder.IncrementEvent("hospital_appointment", "rescheduled", new Dictionary<string, string?>
            {
                ["channel"] = appointment.BookingChannel,
                ["had_reason"] = string.IsNullOrWhiteSpace(reason) ? "false" : "true"
            });

            var refreshed = await _hospitalAppointmentRepository.GetAppointmentAggregateAsync(appointmentId, ct);
            return refreshed == null ? null : MapAggregateToWorklistItem(refreshed);
        }

        private static TimeZoneInfo ResolveClinicTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }

        private static DateTime ConvertLocalClinicTimeToUtc(DateTime localDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), ResolveClinicTimeZone());
        }

        private static DateTime ConvertUtcToClinicLocal(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), ResolveClinicTimeZone());
        }

        private async Task<PaginatedResult<HospitalAppointmentWorklistItemDto>> GetScopedWorklistAsync(
            HospitalAppointmentWorklistRequestDto request,
            string currentRole,
            string? currentUsername,
            CancellationToken ct)
        {
            if (string.Equals(currentRole, "Doctor", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(currentUsername))
                {
                    return new PaginatedResult<HospitalAppointmentWorklistItemDto>(
                        Array.Empty<HospitalAppointmentWorklistItemDto>(),
                        0,
                        request.PageNumber,
                        request.PageSize);
                }

                var doctorProfile = await _hospitalDoctorWorklistRepository.ResolveDoctorByUsernameAsync(currentUsername, ct);
                if (doctorProfile == null)
                {
                    return new PaginatedResult<HospitalAppointmentWorklistItemDto>(
                        Array.Empty<HospitalAppointmentWorklistItemDto>(),
                        0,
                        request.PageNumber,
                        request.PageSize);
                }

                request.DoctorProfileId = doctorProfile.DoctorProfileId;
            }

            return await _hospitalAppointmentRepository.GetWorklistAsync(request, ct);
        }

        private static string GenerateMedicalRecordNumber(DateTime nowUtc)
            => CompactCodeGenerator.Generate("MR", nowUtc);

        private static string GenerateAppointmentNumber(DateTime nowUtc)
            => CompactCodeGenerator.Generate("AP", nowUtc);

        private static string? BuildNotes(string? notes, string? serviceCode)
        {
            var parts = new[]
            {
                string.IsNullOrWhiteSpace(serviceCode) ? null : $"ServiceCode: {serviceCode.Trim()}",
                string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            }.Where(x => !string.IsNullOrWhiteSpace(x));

            var merged = string.Join(" | ", parts);
            return string.IsNullOrWhiteSpace(merged) ? null : merged;
        }

        private static string BuildRescheduleNotes(string? reason, DateTime previousStartUtc, DateTime nextStartUtc)
        {
            var parts = new[]
            {
                $"RescheduledFromUtc: {previousStartUtc:O}",
                $"RescheduledToUtc: {nextStartUtc:O}",
                string.IsNullOrWhiteSpace(reason) ? null : $"RescheduleReason: {reason}"
            }.Where(x => !string.IsNullOrWhiteSpace(x));

            return string.Join(" | ", parts);
        }

        private static HospitalAppointmentWorklistItemDto MapAggregateToWorklistItem(HospitalAppointmentAggregateSnapshot aggregate)
        {
            return new HospitalAppointmentWorklistItemDto
            {
                AppointmentId = aggregate.AppointmentId,
                AppointmentNumber = aggregate.AppointmentNumber,
                PatientId = aggregate.PatientId,
                PatientName = aggregate.PatientName,
                MedicalRecordNumber = aggregate.MedicalRecordNumber,
                PatientPhone = aggregate.PatientPhone,
                DoctorProfileId = aggregate.DoctorProfileId,
                DoctorName = aggregate.DoctorName,
                SpecialtyName = aggregate.SpecialtyName,
                ClinicName = aggregate.ClinicName,
                FloorLabel = aggregate.FloorLabel,
                RoomLabel = aggregate.RoomLabel,
                AppointmentType = aggregate.AppointmentType,
                BookingChannel = aggregate.BookingChannel,
                Status = aggregate.Status,
                AppointmentStartLocal = ConvertUtcToClinicLocal(aggregate.AppointmentStartUtc),
                AppointmentEndLocal = aggregate.AppointmentEndUtc.HasValue
                    ? ConvertUtcToClinicLocal(aggregate.AppointmentEndUtc.Value)
                    : null,
                ChiefComplaint = aggregate.ChiefComplaint,
                CounterLabel = aggregate.CounterLabel,
                QueueNumber = aggregate.QueueNumber,
                CheckInTimeLocal = aggregate.CheckInTimeUtc.HasValue
                    ? ConvertUtcToClinicLocal(aggregate.CheckInTimeUtc.Value)
                    : null
            };
        }
    }
}
