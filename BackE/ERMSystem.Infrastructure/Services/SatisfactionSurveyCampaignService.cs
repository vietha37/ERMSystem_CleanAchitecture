using System.Text.Json;
using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.HospitalData;
using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public class SatisfactionSurveyCampaignService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SatisfactionSurveyOptions _options;
    private readonly BackgroundWorkerHealthRegistry _workerHealthRegistry;
    private readonly IBusinessMetricsRecorder _businessMetricsRecorder;
    private readonly ILogger<SatisfactionSurveyCampaignService> _logger;

    public SatisfactionSurveyCampaignService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<SatisfactionSurveyOptions> options,
        BackgroundWorkerHealthRegistry workerHealthRegistry,
        IBusinessMetricsRecorder businessMetricsRecorder,
        ILogger<SatisfactionSurveyCampaignService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _workerHealthRegistry = workerHealthRegistry;
        _businessMetricsRecorder = businessMetricsRecorder;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Khoi dong worker gui khao sat hai long.");
        _workerHealthRegistry.Report("satisfaction-survey-campaign", "Starting", "Worker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromHours(Math.Max(1, _options.PollIntervalHours)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCampaignAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker gui khao sat hai long gap loi khong mong muon.");
                _workerHealthRegistry.Report("satisfaction-survey-campaign", "Unhealthy", ex.Message, errorAtUtc: DateTime.UtcNow);
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunCampaignAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var hospitalDbContext = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var nowUtc = DateTime.UtcNow;
        var eligibleCompletedBeforeUtc = nowUtc.AddHours(-Math.Max(1, _options.SurveyDelayHours));
        var lookbackCutoffUtc = nowUtc.AddDays(-Math.Max(1, _options.MaxCompletedLookbackDays));
        var cooldownCutoffUtc = nowUtc.AddDays(-Math.Max(1, _options.SurveyCooldownDays));

        var completedAppointments = await hospitalDbContext.Appointments
            .AsNoTracking()
            .Where(x => x.Status == "Completed")
            .Where(x => x.AppointmentStartUtc >= lookbackCutoffUtc && x.AppointmentStartUtc <= eligibleCompletedBeforeUtc)
            .Where(x => x.Patient.DeletedAtUtc == null)
            .Where(x => !string.IsNullOrWhiteSpace(x.Patient.Phone) || !string.IsNullOrWhiteSpace(x.Patient.Email))
            .Include(x => x.Patient)
            .Include(x => x.DoctorProfile)
                .ThenInclude(x => x.StaffProfile)
            .Include(x => x.DoctorProfile)
                .ThenInclude(x => x.Specialty)
            .Include(x => x.Clinic)
            .OrderByDescending(x => x.AppointmentStartUtc)
            .Take(Math.Max(1, _options.BatchSize) * 5)
            .ToListAsync(ct);

        var candidateAppointments = completedAppointments
            .GroupBy(x => x.PatientId)
            .Select(x => x.OrderByDescending(y => y.AppointmentStartUtc).First())
            .Take(Math.Max(1, _options.BatchSize))
            .ToList();

        if (candidateAppointments.Count == 0)
        {
            _workerHealthRegistry.Report("satisfaction-survey-campaign", "Healthy", "No eligible satisfaction survey candidates.", successAtUtc: nowUtc);
            return;
        }

        var candidatePatientIds = candidateAppointments.Select(x => x.PatientId).Distinct().ToArray();

        var patientsWithRecentSurvey = await hospitalDbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => candidatePatientIds.Contains(x.AggregateId))
            .Where(x => x.AggregateType == "Patient")
            .Where(x => x.EventType == "PatientSatisfactionSurvey.v1")
            .Where(x => x.AvailableAtUtc >= cooldownCutoffUtc)
            .Select(x => x.AggregateId)
            .Distinct()
            .ToListAsync(ct);

        var skippedCooldown = 0;
        var queuedCount = 0;

        foreach (var appointment in candidateAppointments)
        {
            if (patientsWithRecentSurvey.Contains(appointment.PatientId))
            {
                skippedCooldown += 1;
                continue;
            }

            var appointmentStartLocal = ConvertUtcToClinicLocal(appointment.AppointmentStartUtc);
            var appointmentEndLocal = appointment.AppointmentEndUtc.HasValue
                ? ConvertUtcToClinicLocal(appointment.AppointmentEndUtc.Value)
                : (DateTime?)null;

            hospitalDbContext.OutboxMessages.Add(new HospitalOutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                AggregateType = "Patient",
                AggregateId = appointment.PatientId,
                EventType = "PatientSatisfactionSurvey.v1",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    appointmentId = appointment.Id,
                    appointmentNumber = appointment.AppointmentNumber,
                    patientId = appointment.PatientId,
                    patientName = appointment.Patient.FullName,
                    phone = appointment.Patient.Phone,
                    email = appointment.Patient.Email,
                    medicalRecordNumber = appointment.Patient.MedicalRecordNumber,
                    diagnosisName = appointment.ChiefComplaint,
                    doctorProfileId = appointment.DoctorProfileId,
                    doctorName = appointment.DoctorProfile.StaffProfile.FullName,
                    specialtyName = appointment.DoctorProfile.Specialty.Name,
                    clinicName = appointment.Clinic.Name,
                    appointmentStartLocal,
                    appointmentEndLocal,
                    channel = "CRM",
                    surveyType = "Satisfaction",
                    surveyDelayHours = _options.SurveyDelayHours
                }, JsonOptions),
                Status = "Pending",
                AvailableAtUtc = nowUtc
            });

            patientsWithRecentSurvey.Add(appointment.PatientId);
            queuedCount += 1;
        }

        if (queuedCount > 0)
        {
            await hospitalDbContext.SaveChangesAsync(ct);
            _businessMetricsRecorder.IncrementEvent("hospital_crm", "satisfaction_survey_queued", new Dictionary<string, string?>
            {
                ["count"] = queuedCount.ToString()
            });
        }

        _workerHealthRegistry.Report(
            "satisfaction-survey-campaign",
            "Healthy",
            $"Queued={queuedCount}; SkippedCooldown={skippedCooldown}.",
            successAtUtc: nowUtc);
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

    private static DateTime ConvertUtcToClinicLocal(DateTime utcDateTime)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), ResolveClinicTimeZone());
}
