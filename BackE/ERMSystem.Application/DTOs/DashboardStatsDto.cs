using System.Collections.Generic;

namespace ERMSystem.Application.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalPatients { get; set; }
        public int AppointmentsToday { get; set; }
        public int PendingAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public int RevisitAppointmentsToday { get; set; }
        public decimal CompletionRatePercent { get; set; }
        public decimal CancellationRatePercent { get; set; }
        public decimal RevisitRatePercent { get; set; }
        public int TotalInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public decimal IssuedAmountThisMonth { get; set; }
        public decimal CollectedAmountThisMonth { get; set; }
        public decimal OutstandingBalanceAmount { get; set; }
        public decimal CollectionRatePercent { get; set; }
        public Dictionary<string, int> TopDiagnoses { get; set; } = new Dictionary<string, int>();
    }
}
