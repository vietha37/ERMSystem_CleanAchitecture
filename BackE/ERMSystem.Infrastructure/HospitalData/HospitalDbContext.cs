using ERMSystem.Infrastructure.HospitalData.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERMSystem.Infrastructure.HospitalData;

public class HospitalDbContext : DbContext
{
    public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options)
    {
    }

    public DbSet<HospitalUserEntity> Users => Set<HospitalUserEntity>();
    public DbSet<HospitalRoleEntity> Roles => Set<HospitalRoleEntity>();
    public DbSet<HospitalUserRoleEntity> UserRoles => Set<HospitalUserRoleEntity>();
    public DbSet<HospitalRefreshTokenEntity> RefreshTokens => Set<HospitalRefreshTokenEntity>();
    public DbSet<HospitalUserSessionEntity> UserSessions => Set<HospitalUserSessionEntity>();
    public DbSet<HospitalSecurityEventEntity> SecurityEvents => Set<HospitalSecurityEventEntity>();

    public DbSet<HospitalDepartmentEntity> Departments => Set<HospitalDepartmentEntity>();
    public DbSet<HospitalSpecialtyEntity> Specialties => Set<HospitalSpecialtyEntity>();
    public DbSet<HospitalClinicEntity> Clinics => Set<HospitalClinicEntity>();
    public DbSet<HospitalStaffProfileEntity> StaffProfiles => Set<HospitalStaffProfileEntity>();
    public DbSet<HospitalDoctorProfileEntity> DoctorProfiles => Set<HospitalDoctorProfileEntity>();
    public DbSet<HospitalDoctorScheduleEntity> DoctorSchedules => Set<HospitalDoctorScheduleEntity>();

    public DbSet<HospitalPatientEntity> Patients => Set<HospitalPatientEntity>();
    public DbSet<HospitalPatientAccountEntity> PatientAccounts => Set<HospitalPatientAccountEntity>();
    public DbSet<HospitalPatientIdentifierEntity> PatientIdentifiers => Set<HospitalPatientIdentifierEntity>();
    public DbSet<HospitalPatientContactEntity> PatientContacts => Set<HospitalPatientContactEntity>();
    public DbSet<HospitalPatientEmergencyContactEntity> PatientEmergencyContacts => Set<HospitalPatientEmergencyContactEntity>();
    public DbSet<HospitalPatientInsurancePolicyEntity> PatientInsurancePolicies => Set<HospitalPatientInsurancePolicyEntity>();
    public DbSet<HospitalPatientConsentEntity> PatientConsents => Set<HospitalPatientConsentEntity>();

    public DbSet<HospitalAppointmentSlotEntity> AppointmentSlots => Set<HospitalAppointmentSlotEntity>();
    public DbSet<HospitalAppointmentEntity> Appointments => Set<HospitalAppointmentEntity>();
    public DbSet<HospitalCheckInEntity> CheckIns => Set<HospitalCheckInEntity>();
    public DbSet<HospitalQueueTicketEntity> QueueTickets => Set<HospitalQueueTicketEntity>();
    public DbSet<HospitalEncounterEntity> Encounters => Set<HospitalEncounterEntity>();
    public DbSet<HospitalVitalSignEntity> VitalSigns => Set<HospitalVitalSignEntity>();
    public DbSet<HospitalDiagnosisEntity> Diagnoses => Set<HospitalDiagnosisEntity>();
    public DbSet<HospitalClinicalNoteEntity> ClinicalNotes => Set<HospitalClinicalNoteEntity>();
    public DbSet<HospitalEncounterAttachmentEntity> EncounterAttachments => Set<HospitalEncounterAttachmentEntity>();
    public DbSet<HospitalOrderHeaderEntity> OrderHeaders => Set<HospitalOrderHeaderEntity>();
    public DbSet<HospitalLabServiceEntity> LabServices => Set<HospitalLabServiceEntity>();
    public DbSet<HospitalLabOrderEntity> LabOrders => Set<HospitalLabOrderEntity>();
    public DbSet<HospitalSpecimenEntity> Specimens => Set<HospitalSpecimenEntity>();
    public DbSet<HospitalLabResultItemEntity> LabResultItems => Set<HospitalLabResultItemEntity>();
    public DbSet<HospitalImagingServiceEntity> ImagingServices => Set<HospitalImagingServiceEntity>();
    public DbSet<HospitalImagingOrderEntity> ImagingOrders => Set<HospitalImagingOrderEntity>();
    public DbSet<HospitalImagingReportEntity> ImagingReports => Set<HospitalImagingReportEntity>();
    public DbSet<HospitalMedicineEntity> Medicines => Set<HospitalMedicineEntity>();
    public DbSet<HospitalPrescriptionEntity> Prescriptions => Set<HospitalPrescriptionEntity>();
    public DbSet<HospitalPrescriptionItemEntity> PrescriptionItems => Set<HospitalPrescriptionItemEntity>();
    public DbSet<HospitalDispensingEntity> Dispensings => Set<HospitalDispensingEntity>();

    public DbSet<HospitalServiceCatalogEntity> ServiceCatalog => Set<HospitalServiceCatalogEntity>();
    public DbSet<HospitalInvoiceEntity> Invoices => Set<HospitalInvoiceEntity>();
    public DbSet<HospitalInvoiceItemEntity> InvoiceItems => Set<HospitalInvoiceItemEntity>();
    public DbSet<HospitalPaymentEntity> Payments => Set<HospitalPaymentEntity>();
    public DbSet<HospitalOutboxMessageEntity> OutboxMessages => Set<HospitalOutboxMessageEntity>();
    public DbSet<HospitalNotificationTemplateEntity> NotificationTemplates => Set<HospitalNotificationTemplateEntity>();
    public DbSet<HospitalNotificationDeliveryEntity> NotificationDeliveries => Set<HospitalNotificationDeliveryEntity>();
    public DbSet<IntegrationInboxMessageEntity> InboxMessages => Set<IntegrationInboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HospitalUserEntity>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<HospitalPatientEntity>().Property(x => x.RowVersion).IsRowVersion();
        modelBuilder.Entity<HospitalDoctorProfileEntity>().Property(x => x.ConsultationFee).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalPatientInsurancePolicyEntity>().Property(x => x.CoveragePercent).HasPrecision(5, 2);
        modelBuilder.Entity<HospitalServiceCatalogEntity>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalInvoiceEntity>().Property(x => x.SubtotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalInvoiceEntity>().Property(x => x.DiscountAmount).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalInvoiceEntity>().Property(x => x.InsuranceAmount).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalInvoiceEntity>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalInvoiceItemEntity>().Property(x => x.Quantity).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalInvoiceItemEntity>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalInvoiceItemEntity>().Property(x => x.LineAmount).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalPaymentEntity>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalVitalSignEntity>().Property(x => x.HeightCm).HasPrecision(6, 2);
        modelBuilder.Entity<HospitalVitalSignEntity>().Property(x => x.WeightKg).HasPrecision(6, 2);
        modelBuilder.Entity<HospitalVitalSignEntity>().Property(x => x.TemperatureC).HasPrecision(4, 1);
        modelBuilder.Entity<HospitalVitalSignEntity>().Property(x => x.OxygenSaturation).HasPrecision(5, 2);
        modelBuilder.Entity<HospitalPrescriptionItemEntity>().Property(x => x.Quantity).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalPrescriptionItemEntity>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalLabServiceEntity>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<HospitalImagingServiceEntity>().Property(x => x.UnitPrice).HasPrecision(18, 2);

        modelBuilder.Entity<HospitalUserRoleEntity>().HasKey(x => new { x.UserId, x.RoleCode });

        modelBuilder.Entity<HospitalUserRoleEntity>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalUserRoleEntity>()
            .HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleCode)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalRefreshTokenEntity>()
            .HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalUserSessionEntity>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserSessions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalSecurityEventEntity>()
            .HasOne(x => x.User)
            .WithMany(x => x.SecurityEvents)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalSpecialtyEntity>()
            .HasOne(x => x.Department)
            .WithMany(x => x.Specialties)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalClinicEntity>()
            .HasOne(x => x.Department)
            .WithMany(x => x.Clinics)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalStaffProfileEntity>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalStaffProfileEntity>()
            .HasOne(x => x.Department)
            .WithMany(x => x.StaffProfiles)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalDoctorProfileEntity>()
            .HasOne(x => x.StaffProfile)
            .WithOne(x => x.DoctorProfile)
            .HasForeignKey<HospitalDoctorProfileEntity>(x => x.StaffProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalDoctorProfileEntity>()
            .HasOne(x => x.Specialty)
            .WithMany(x => x.DoctorProfiles)
            .HasForeignKey(x => x.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalDoctorScheduleEntity>()
            .HasOne(x => x.DoctorProfile)
            .WithMany(x => x.DoctorSchedules)
            .HasForeignKey(x => x.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalDoctorScheduleEntity>()
            .HasOne(x => x.Clinic)
            .WithMany(x => x.DoctorSchedules)
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPatientAccountEntity>()
            .HasOne(x => x.Patient)
            .WithOne(x => x.PatientAccount)
            .HasForeignKey<HospitalPatientAccountEntity>(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPatientAccountEntity>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPatientIdentifierEntity>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Identifiers)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPatientContactEntity>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Contacts)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPatientEmergencyContactEntity>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.EmergencyContacts)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPatientInsurancePolicyEntity>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.InsurancePolicies)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPatientConsentEntity>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Consents)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalAppointmentSlotEntity>()
            .HasOne(x => x.DoctorSchedule)
            .WithMany(x => x.AppointmentSlots)
            .HasForeignKey(x => x.DoctorScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalAppointmentEntity>()
            .HasOne(x => x.Patient)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalAppointmentEntity>()
            .HasOne(x => x.DoctorProfile)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalAppointmentEntity>()
            .HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalAppointmentEntity>()
            .HasOne(x => x.AppointmentSlot)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.AppointmentSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalAppointmentEntity>()
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalCheckInEntity>()
            .HasOne(x => x.Appointment)
            .WithOne(x => x.CheckIn)
            .HasForeignKey<HospitalCheckInEntity>(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalQueueTicketEntity>()
            .HasOne(x => x.Appointment)
            .WithMany(x => x.QueueTickets)
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalEncounterEntity>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalEncounterEntity>()
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalEncounterEntity>()
            .HasOne(x => x.DoctorProfile)
            .WithMany()
            .HasForeignKey(x => x.DoctorProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalEncounterEntity>()
            .HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalVitalSignEntity>()
            .HasOne(x => x.Encounter)
            .WithMany(x => x.VitalSigns)
            .HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalVitalSignEntity>()
            .HasOne(x => x.RecordedByUser)
            .WithMany()
            .HasForeignKey(x => x.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalDiagnosisEntity>()
            .HasOne(x => x.Encounter)
            .WithMany(x => x.Diagnoses)
            .HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalClinicalNoteEntity>()
            .HasOne(x => x.Encounter)
            .WithMany(x => x.ClinicalNotes)
            .HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalClinicalNoteEntity>()
            .HasOne(x => x.AuthoredByUser)
            .WithMany()
            .HasForeignKey(x => x.AuthoredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalEncounterAttachmentEntity>()
            .HasOne(x => x.Encounter)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalEncounterAttachmentEntity>()
            .HasOne(x => x.UploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalOrderHeaderEntity>()
            .HasOne(x => x.Encounter)
            .WithMany()
            .HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalOrderHeaderEntity>()
            .HasOne(x => x.OrderedByUser)
            .WithMany()
            .HasForeignKey(x => x.OrderedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalLabOrderEntity>()
            .HasOne(x => x.OrderHeader)
            .WithMany(x => x.LabOrders)
            .HasForeignKey(x => x.OrderHeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalLabOrderEntity>()
            .HasOne(x => x.LabService)
            .WithMany()
            .HasForeignKey(x => x.LabServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalSpecimenEntity>()
            .HasOne(x => x.LabOrder)
            .WithMany(x => x.Specimens)
            .HasForeignKey(x => x.LabOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalLabResultItemEntity>()
            .HasOne(x => x.LabOrder)
            .WithMany(x => x.ResultItems)
            .HasForeignKey(x => x.LabOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalImagingOrderEntity>()
            .HasOne(x => x.OrderHeader)
            .WithMany(x => x.ImagingOrders)
            .HasForeignKey(x => x.OrderHeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalImagingOrderEntity>()
            .HasOne(x => x.ImagingService)
            .WithMany()
            .HasForeignKey(x => x.ImagingServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalImagingReportEntity>()
            .HasOne(x => x.ImagingOrder)
            .WithOne(x => x.ImagingReport)
            .HasForeignKey<HospitalImagingReportEntity>(x => x.ImagingOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalImagingReportEntity>()
            .HasOne(x => x.SignedByUser)
            .WithMany()
            .HasForeignKey(x => x.SignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalInvoiceEntity>()
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalInvoiceEntity>()
            .HasOne(x => x.Encounter)
            .WithMany()
            .HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalInvoiceItemEntity>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.InvoiceItems)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalInvoiceItemEntity>()
            .HasOne(x => x.ServiceCatalog)
            .WithMany()
            .HasForeignKey(x => x.ServiceCatalogId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPaymentEntity>()
            .HasOne(x => x.Invoice)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPaymentEntity>()
            .HasOne(x => x.ReceivedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPrescriptionEntity>()
            .HasOne(x => x.OrderHeader)
            .WithMany(x => x.Prescriptions)
            .HasForeignKey(x => x.OrderHeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPrescriptionItemEntity>()
            .HasOne(x => x.Prescription)
            .WithMany(x => x.PrescriptionItems)
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalPrescriptionItemEntity>()
            .HasOne(x => x.Medicine)
            .WithMany(x => x.PrescriptionItems)
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalDispensingEntity>()
            .HasOne(x => x.Prescription)
            .WithMany(x => x.Dispensings)
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalDispensingEntity>()
            .HasOne(x => x.DispensedByUser)
            .WithMany()
            .HasForeignKey(x => x.DispensedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalNotificationDeliveryEntity>()
            .HasOne(x => x.OutboxMessage)
            .WithMany()
            .HasForeignKey(x => x.OutboxMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HospitalAppointmentEntity>().HasIndex(x => x.CreatedAtUtc);
        modelBuilder.Entity<HospitalAppointmentEntity>().HasIndex(x => new { x.Status, x.AppointmentStartUtc });

        modelBuilder.Entity<HospitalEncounterEntity>().HasIndex(x => x.CreatedAtUtc);
        modelBuilder.Entity<HospitalEncounterEntity>().HasIndex(x => new { x.PatientId, x.CreatedAtUtc });
        modelBuilder.Entity<HospitalEncounterEntity>().HasIndex(x => new { x.DoctorProfileId, x.CreatedAtUtc });

        modelBuilder.Entity<HospitalInvoiceEntity>().HasIndex(x => x.IssuedAtUtc);
        modelBuilder.Entity<HospitalInvoiceEntity>().HasIndex(x => new { x.InvoiceStatus, x.IssuedAtUtc });

        modelBuilder.Entity<HospitalPatientEntity>().HasIndex(x => x.CreatedAtUtc);
        modelBuilder.Entity<HospitalPatientEntity>().HasIndex(x => new { x.MedicalRecordNumber, x.Phone, x.FullName });

        modelBuilder.Entity<HospitalOrderHeaderEntity>().HasIndex(x => x.EncounterId);
        modelBuilder.Entity<HospitalLabOrderEntity>().HasIndex(x => new { x.OrderHeaderId, x.OrderStatus });
        modelBuilder.Entity<HospitalImagingOrderEntity>().HasIndex(x => new { x.OrderHeaderId, x.OrderStatus });
        modelBuilder.Entity<HospitalPrescriptionEntity>().HasIndex(x => new { x.OrderHeaderId, x.Status });
        modelBuilder.Entity<HospitalPrescriptionItemEntity>().HasIndex(x => x.PrescriptionId);
        modelBuilder.Entity<HospitalNotificationDeliveryEntity>().HasIndex(x => new { x.DeliveryStatus, x.LastAttemptAtUtc });
    }
}
