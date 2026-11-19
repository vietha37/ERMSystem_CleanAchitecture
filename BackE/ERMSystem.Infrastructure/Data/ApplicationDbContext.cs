using Microsoft.EntityFrameworkCore;
using ERMSystem.Domain.Entities;

namespace ERMSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<MedicalRecord> MedicalRecords { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;
        public DbSet<AppUser> AppUsers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Ignore<ERMSystem.Domain.Common.IDomainEvent>();
            modelBuilder.Entity<Patient>().Ignore(p => p.DomainEvents);
            modelBuilder.Entity<Patient>().Ignore(p => p.EmergencyContactInfo);
            modelBuilder.Entity<Appointment>().Ignore(a => a.DomainEvents);
            modelBuilder.Entity<Prescription>().Ignore(p => p.DomainEvents);

            // A patient has many appointments.
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // A doctor has many appointments.
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // An appointment has exactly one medical record (one-to-one).
            modelBuilder.Entity<MedicalRecord>()
                .HasOne(m => m.Appointment)
                .WithOne(a => a.MedicalRecord)
                .HasForeignKey<MedicalRecord>(m => m.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // A medical record has at most one prescription (enforced by unique index).
            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.MedicalRecord)
                .WithMany(m => m.Prescriptions)
                .HasForeignKey(p => p.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            // DB-level guarantee: one prescription per medical record.
            modelBuilder.Entity<Prescription>()
                .HasIndex(p => p.MedicalRecordId)
                .IsUnique();

            // A prescription has many prescription items.
            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Prescription)
                .WithMany(p => p.PrescriptionItems)
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            // A medicine can appear in many prescription items.
            modelBuilder.Entity<PrescriptionItem>()
                .HasOne(pi => pi.Medicine)
                .WithMany(m => m.PrescriptionItems)
                .HasForeignKey(pi => pi.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            // Username must be unique.
            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}
