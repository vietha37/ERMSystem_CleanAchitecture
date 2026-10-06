using ERMSystem.Application.Interfaces;
using ERMSystem.Infrastructure.Messaging;
using ERMSystem.Infrastructure.Repositories;
using ERMSystem.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ERMSystem.Infrastructure;

/// <summary>
/// Đăng ký toàn bộ Repositories, Messaging và Infrastructure Services vào DI Container.
/// Đảm bảo nguyên tắc Clean Architecture: Tách rời cấu hình chi tiết của tầng hạ tầng.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // ── Security & Passwords ───────────────────────────────────────────────────
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IAuthSecurityMonitor, AuthSecurityMonitor>();
        services.AddScoped<IHospitalIdentityBridgeService, HospitalIdentityBridgeService>();
        services.AddScoped<IComplianceAuditRecorder, ComplianceAuditRecorder>();

        // ── Domain Repositories ───────────────────────────────────────────────────
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IMedicineRepository, MedicineRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IPrescriptionItemRepository, PrescriptionItemRepository>();
        services.AddScoped<IDashboardQueryCache, DashboardQueryCache>();

        // ── Hospital Specialized Repositories ─────────────────────────────────────
        services.AddScoped<IHospitalCatalogRepository, HospitalCatalogRepository>();
        services.AddScoped<IHospitalDoctorRepository, HospitalDoctorRepository>();
        services.AddScoped<IHospitalDoctorWorklistRepository, HospitalDoctorWorklistRepository>();
        services.AddScoped<IHospitalAppointmentRepository, HospitalAppointmentRepository>();
        services.AddScoped<IHospitalEncounterRepository, HospitalEncounterRepository>();
        services.AddScoped<IHospitalPrescriptionRepository, HospitalPrescriptionRepository>();
        services.AddScoped<IHospitalClinicalOrderRepository, HospitalClinicalOrderRepository>();
        services.AddScoped<IHospitalBillingRepository, HospitalBillingRepository>();
        services.AddScoped<IHospitalPatientPortalRepository, HospitalPatientPortalRepository>();
        services.AddScoped<IHospitalNotificationDeliveryRepository, HospitalNotificationDeliveryRepository>();

        // ── Infrastructure Document & Payment Services ────────────────────────────
        services.AddSingleton<IHospitalDocumentStorageService, ConfigurableHospitalDocumentStorageService>();
        services.AddSingleton<IHospitalPaymentGatewayService, ConfigurableHospitalPaymentGatewayService>();

        // ── Notification Senders & Metrics ────────────────────────────────────────
        services.AddSingleton<INotificationChannelSender, MockEmailNotificationSender>();
        services.AddSingleton<INotificationChannelSender, MockSmsNotificationSender>();
        services.AddSingleton<BackgroundWorkerHealthRegistry>();
        services.AddSingleton<DashboardCacheMetricsRegistry>();
        services.AddSingleton<NotificationPipelineMetricsReader>();

        // ── Background Hosted Services ────────────────────────────────────────────
        services.AddHostedService<HospitalOutboxPublisherService>();
        services.AddHostedService<HospitalNotificationConsumerService>();
        services.AddHostedService<HospitalNotificationDispatchService>();
        services.AddHostedService<RetentionCleanupService>();
        services.AddHostedService<RevisitReminderCampaignService>();
        services.AddHostedService<SatisfactionSurveyCampaignService>();
        services.AddHostedService<CustomerCareFollowUpCampaignService>();
        services.AddHostedService<DocumentStorageCleanupService>();

        return services;
    }
}
