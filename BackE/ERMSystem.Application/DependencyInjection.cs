using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ERMSystem.Application;

/// <summary>
/// Đăng ký toàn bộ các Service của Application Layer vào DI Container.
/// Giúp phân tách rõ ràng trách nhiệm cấu hình, giảm tải cho Program.cs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IMedicineService, MedicineService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IMedicalRecordService, MedicalRecordService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IPrescriptionItemService, PrescriptionItemService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<IHospitalCatalogService, HospitalCatalogService>();
        services.AddScoped<IHospitalDoctorService, HospitalDoctorService>();
        services.AddScoped<IHospitalDoctorWorklistService, HospitalDoctorWorklistService>();
        services.AddScoped<IHospitalAppointmentService, HospitalAppointmentService>();
        services.AddScoped<IHospitalEncounterService, HospitalEncounterService>();
        services.AddScoped<IHospitalPrescriptionService, HospitalPrescriptionService>();
        services.AddScoped<IHospitalClinicalOrderService, HospitalClinicalOrderService>();
        services.AddScoped<IHospitalBillingService, HospitalBillingService>();
        services.AddScoped<IHospitalPatientPortalService, HospitalPatientPortalService>();
        services.AddScoped<IHospitalNotificationDeliveryService, HospitalNotificationDeliveryService>();

        return services;
    }
}
