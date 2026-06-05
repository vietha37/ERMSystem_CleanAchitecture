using ERMSystem.Domain.Entities;

namespace ERMSystem.Application.Authorization;

public static class AppPermissions
{
    public const string ClaimType = "permission";

    public static class Dashboard
    {
        public const string View = "dashboard.view";
    }

    public static class Patients
    {
        public const string Read = "patients.read";
        public const string Create = "patients.create";
        public const string Update = "patients.update";
        public const string Delete = "patients.delete";
        public const string Merge = "patients.merge";
        public const string SelfRead = "patients.self.read";
    }

    public static class AdminUsers
    {
        public const string Read = "adminusers.read";
        public const string Create = "adminusers.create";
        public const string Update = "adminusers.update";
        public const string Delete = "adminusers.delete";
        public const string SyncIdentity = "adminusers.syncidentity";
    }

    public static class Appointments
    {
        public const string Read = "appointments.read";
        public const string Create = "appointments.create";
        public const string Update = "appointments.update";
        public const string Delete = "appointments.delete";
        public const string CheckIn = "appointments.checkin";
        public const string StatusUpdate = "appointments.statusupdate";
    }

    public static class Doctors
    {
        public const string Read = "doctors.read";
        public const string Create = "doctors.create";
        public const string Update = "doctors.update";
        public const string Delete = "doctors.delete";
    }

    public static class MedicalRecords
    {
        public const string Read = "medicalrecords.read";
        public const string Create = "medicalrecords.create";
        public const string Update = "medicalrecords.update";
        public const string Delete = "medicalrecords.delete";
    }

    public static class Medicines
    {
        public const string Read = "medicines.read";
        public const string Create = "medicines.create";
        public const string Update = "medicines.update";
        public const string Delete = "medicines.delete";
    }

    public static class Prescriptions
    {
        public const string Read = "prescriptions.read";
        public const string Create = "prescriptions.create";
        public const string Update = "prescriptions.update";
        public const string Delete = "prescriptions.delete";
        public const string Dispense = "prescriptions.dispense";
    }

    public static class HospitalDoctorWorklist
    {
        public const string Read = "hospitaldoctorworklist.read";
    }

    public static class HospitalClinicalOrders
    {
        public const string Read = "hospitalclinicalorders.read";
        public const string Create = "hospitalclinicalorders.create";
        public const string Update = "hospitalclinicalorders.update";
    }

    public static class HospitalEncounters
    {
        public const string Read = "hospitalencounters.read";
        public const string Create = "hospitalencounters.create";
        public const string Update = "hospitalencounters.update";
    }

    public static class HospitalBilling
    {
        public const string Read = "hospitalbilling.read";
        public const string Create = "hospitalbilling.create";
        public const string CollectPayment = "hospitalbilling.collectpayment";
        public const string Refund = "hospitalbilling.refund";
    }

    public static class HospitalNotifications
    {
        public const string Read = "hospitalnotifications.read";
        public const string Retry = "hospitalnotifications.retry";
    }

    public static class Notifications
    {
        public const string Read = "notifications.read";
    }

    public static class HospitalPortal
    {
        public const string View = "hospitalportal.view";
    }

    public static readonly string[] All =
    [
        Dashboard.View,
        Patients.Read,
        Patients.Create,
        Patients.Update,
        Patients.Delete,
        Patients.Merge,
        Patients.SelfRead,
        AdminUsers.Read,
        AdminUsers.Create,
        AdminUsers.Update,
        AdminUsers.Delete,
        AdminUsers.SyncIdentity,
        Appointments.Read,
        Appointments.Create,
        Appointments.Update,
        Appointments.Delete,
        Appointments.CheckIn,
        Appointments.StatusUpdate,
        Doctors.Read,
        Doctors.Create,
        Doctors.Update,
        Doctors.Delete,
        MedicalRecords.Read,
        MedicalRecords.Create,
        MedicalRecords.Update,
        MedicalRecords.Delete,
        Medicines.Read,
        Medicines.Create,
        Medicines.Update,
        Medicines.Delete,
        Prescriptions.Read,
        Prescriptions.Create,
        Prescriptions.Update,
        Prescriptions.Delete,
        Prescriptions.Dispense,
        HospitalDoctorWorklist.Read,
        HospitalClinicalOrders.Read,
        HospitalClinicalOrders.Create,
        HospitalClinicalOrders.Update,
        HospitalEncounters.Read,
        HospitalEncounters.Create,
        HospitalEncounters.Update,
        HospitalBilling.Read,
        HospitalBilling.Create,
        HospitalBilling.CollectPayment,
        HospitalBilling.Refund,
        HospitalNotifications.Read,
        HospitalNotifications.Retry,
        Notifications.Read,
        HospitalPortal.View
    ];

    public static IReadOnlyList<string> GetPermissionsForRole(string role)
        => role switch
        {
            AppRole.Admin => All,
            AppRole.Doctor =>
            [
                Dashboard.View,
                Patients.Read,
                Appointments.Read,
                Appointments.Create,
                Appointments.Update,
                Appointments.StatusUpdate,
                Doctors.Read,
                MedicalRecords.Read,
                MedicalRecords.Create,
                MedicalRecords.Update,
                Medicines.Read,
                Prescriptions.Read,
                Prescriptions.Create,
                Prescriptions.Update,
                HospitalDoctorWorklist.Read,
                HospitalClinicalOrders.Read,
                HospitalClinicalOrders.Create,
                HospitalClinicalOrders.Update,
                HospitalEncounters.Read,
                HospitalEncounters.Create,
                HospitalEncounters.Update,
                Notifications.Read
            ],
            AppRole.Cashier =>
            [
                Dashboard.View,
                Patients.Read,
                Patients.Create,
                Patients.Update,
                Appointments.Read,
                Appointments.Create,
                Appointments.Update,
                Appointments.Delete,
                Appointments.CheckIn,
                Appointments.StatusUpdate,
                Doctors.Read,
                MedicalRecords.Read,
                Medicines.Read,
                Prescriptions.Read,
                HospitalDoctorWorklist.Read,
                HospitalEncounters.Read,
                HospitalBilling.Read,
                HospitalBilling.Create,
                HospitalBilling.CollectPayment,
                HospitalBilling.Refund,
                HospitalNotifications.Read,
                HospitalNotifications.Retry,
                Notifications.Read
            ],
            AppRole.Patient =>
            [
                Patients.SelfRead,
                HospitalPortal.View
            ],
            _ => Array.Empty<string>()
        };
}
