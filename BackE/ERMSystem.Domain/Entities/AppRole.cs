namespace ERMSystem.Domain.Entities
{
    public static class AppRole
    {
        public const string Admin = "Admin";
        public const string Doctor = "Doctor";
        public const string Cashier = "Cashier";
        public const string Patient = "Patient";

        public static readonly string[] All = { Admin, Doctor, Cashier, Patient };
        public static readonly string[] Internal = { Admin, Doctor, Cashier };
    }
}
