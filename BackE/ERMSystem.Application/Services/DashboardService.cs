using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;

namespace ERMSystem.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IMedicalRecordRepository _medicalRecordRepository;
        private readonly IPrescriptionRepository _prescriptionRepository;
        private readonly IHospitalBillingRepository _hospitalBillingRepository;
        private readonly IDashboardQueryCache _dashboardQueryCache;

        public DashboardService(
            IPatientRepository patientRepository,
            IAppointmentRepository appointmentRepository,
            IMedicalRecordRepository medicalRecordRepository,
            IPrescriptionRepository prescriptionRepository,
            IHospitalBillingRepository hospitalBillingRepository,
            IDashboardQueryCache dashboardQueryCache)
        {
            _patientRepository = patientRepository;
            _appointmentRepository = appointmentRepository;
            _medicalRecordRepository = medicalRecordRepository;
            _prescriptionRepository = prescriptionRepository;
            _hospitalBillingRepository = hospitalBillingRepository;
            _dashboardQueryCache = dashboardQueryCache;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken ct = default)
        {
            var cached = await _dashboardQueryCache.GetStatsAsync(ct);
            if (cached is not null)
            {
                return cached;
            }

            var patientsCount = await _patientRepository.GetTotalCountAsync(ct);
            var todayAppointmentsCount = await _appointmentRepository.GetAppointmentsTodayCountAsync(ct);
            var pendingAppointmentsCount = await _appointmentRepository.GetPendingAppointmentsTodayCountAsync(ct);
            var completedAppointmentsCount = await _appointmentRepository.GetCompletedAppointmentsTodayCountAsync(ct);
            var cancelledAppointmentsCount = await _appointmentRepository.GetCancelledAppointmentsTodayCountAsync(ct);
            var revisitAppointmentsCount = await _appointmentRepository.GetRevisitAppointmentsTodayCountAsync(ct);
            var topDiagnoses = await _medicalRecordRepository.GetTopDiagnosesAsync(5, ct);
            var monthStartUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nowUtc = DateTime.UtcNow;
            var billingSnapshot = await _hospitalBillingRepository.GetDashboardSnapshotAsync(monthStartUtc, nowUtc, ct);

            var completionRate = todayAppointmentsCount == 0
                ? 0m
                : Math.Round((decimal)completedAppointmentsCount * 100m / todayAppointmentsCount, 2);
            var cancellationRate = todayAppointmentsCount == 0
                ? 0m
                : Math.Round((decimal)cancelledAppointmentsCount * 100m / todayAppointmentsCount, 2);
            var revisitRate = todayAppointmentsCount == 0
                ? 0m
                : Math.Round((decimal)revisitAppointmentsCount * 100m / todayAppointmentsCount, 2);
            var collectionRate = billingSnapshot.IssuedAmountInRange == 0
                ? 0m
                : Math.Round(billingSnapshot.CollectedAmountInRange * 100m / billingSnapshot.IssuedAmountInRange, 2);

            var result = new DashboardStatsDto
            {
                TotalPatients = patientsCount,
                AppointmentsToday = todayAppointmentsCount,
                PendingAppointments = pendingAppointmentsCount,
                CompletedAppointments = completedAppointmentsCount,
                CancelledAppointments = cancelledAppointmentsCount,
                RevisitAppointmentsToday = revisitAppointmentsCount,
                CompletionRatePercent = completionRate,
                CancellationRatePercent = cancellationRate,
                RevisitRatePercent = revisitRate,
                TotalInvoices = billingSnapshot.TotalInvoices,
                PaidInvoices = billingSnapshot.PaidInvoices,
                IssuedAmountThisMonth = billingSnapshot.IssuedAmountInRange,
                CollectedAmountThisMonth = billingSnapshot.CollectedAmountInRange,
                OutstandingBalanceAmount = billingSnapshot.OutstandingBalanceAmount,
                CollectionRatePercent = collectionRate,
                TopDiagnoses = topDiagnoses
            };

            await _dashboardQueryCache.SetStatsAsync(result, ct);

            return result;
        }

        public async Task<DashboardTrendsDto> GetDashboardTrendsAsync(
            string period,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var normalizedPeriod = string.Equals(period, "monthly", StringComparison.OrdinalIgnoreCase)
                ? "monthly"
                : "daily";

            if (normalizedPeriod == "monthly")
            {
                return await BuildMonthlyTrendsAsync(fromDate, toDate, ct);
            }

            return await BuildDailyTrendsAsync(fromDate, toDate, ct);
        }

        private async Task<DashboardTrendsDto> BuildDailyTrendsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken ct)
        {
            var today = DateTime.UtcNow.Date;
            var effectiveTo = (toDate ?? today).Date;
            var effectiveFrom = (fromDate ?? effectiveTo.AddDays(-29)).Date;

            if (effectiveFrom > effectiveTo)
            {
                (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
            }

            var dayCount = (effectiveTo - effectiveFrom).Days + 1;
            if (dayCount > 366)
            {
                effectiveFrom = effectiveTo.AddDays(-365);
                dayCount = 366;
            }

            var cached = await _dashboardQueryCache.GetTrendsAsync("daily", effectiveFrom, effectiveTo, ct);
            if (cached is not null)
            {
                return cached;
            }

            var previousFrom = effectiveFrom.AddDays(-dayCount);
            var previousTo = effectiveFrom.AddDays(-1);

            var patientMap = await _patientRepository.GetCreatedCountByDayAsync(previousFrom, ct);
            var appointmentMap = await _appointmentRepository.GetScheduledCountByDayAsync(previousFrom, ct);
            var prescriptionMap = await _prescriptionRepository.GetCreatedCountByDayAsync(previousFrom, ct);

            var points = new List<DashboardTrendPointDto>(dayCount);
            for (var i = 0; i < dayCount; i++)
            {
                var date = effectiveFrom.AddDays(i);
                patientMap.TryGetValue(date, out var patients);
                appointmentMap.TryGetValue(date, out var appointments);
                prescriptionMap.TryGetValue(date, out var prescriptions);

                points.Add(new DashboardTrendPointDto
                {
                    Label = date.ToString("dd/MM"),
                    PatientsCount = patients,
                    AppointmentsCount = appointments,
                    PrescriptionsCount = prescriptions
                });
            }

            var currentPatientsTotal = points.Sum(x => x.PatientsCount);
            var currentAppointmentsTotal = points.Sum(x => x.AppointmentsCount);
            var currentPrescriptionsTotal = points.Sum(x => x.PrescriptionsCount);
            var previousPatientsTotal = SumByRange(patientMap, previousFrom, previousTo);
            var previousAppointmentsTotal = SumByRange(appointmentMap, previousFrom, previousTo);
            var previousPrescriptionsTotal = SumByRange(prescriptionMap, previousFrom, previousTo);

            var result = new DashboardTrendsDto
            {
                Period = "daily",
                FromDate = effectiveFrom,
                ToDate = effectiveTo,
                CurrentPatientsTotal = currentPatientsTotal,
                CurrentAppointmentsTotal = currentAppointmentsTotal,
                CurrentPrescriptionsTotal = currentPrescriptionsTotal,
                PreviousPatientsTotal = previousPatientsTotal,
                PreviousAppointmentsTotal = previousAppointmentsTotal,
                PreviousPrescriptionsTotal = previousPrescriptionsTotal,
                Points = points
            };

            await _dashboardQueryCache.SetTrendsAsync("daily", effectiveFrom, effectiveTo, result, ct);

            return result;
        }

        private async Task<DashboardTrendsDto> BuildMonthlyTrendsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow.Date;
            var defaultTo = new DateTime(now.Year, now.Month, 1);
            var effectiveTo = new DateTime((toDate ?? defaultTo).Year, (toDate ?? defaultTo).Month, 1);
            var fromSource = fromDate ?? defaultTo.AddMonths(-11);
            var effectiveFrom = new DateTime(fromSource.Year, fromSource.Month, 1);

            if (effectiveFrom > effectiveTo)
            {
                (effectiveFrom, effectiveTo) = (effectiveTo, effectiveFrom);
            }

            var monthCount = ((effectiveTo.Year - effectiveFrom.Year) * 12) + effectiveTo.Month - effectiveFrom.Month + 1;
            if (monthCount > 24)
            {
                effectiveFrom = effectiveTo.AddMonths(-23);
                monthCount = 24;
            }

            var cached = await _dashboardQueryCache.GetTrendsAsync("monthly", effectiveFrom, effectiveTo, ct);
            if (cached is not null)
            {
                return cached;
            }

            var previousFrom = effectiveFrom.AddMonths(-monthCount);
            var previousTo = effectiveFrom.AddMonths(-1);

            var patientDailyMap = await _patientRepository.GetCreatedCountByDayAsync(previousFrom, ct);
            var appointmentDailyMap = await _appointmentRepository.GetScheduledCountByDayAsync(previousFrom, ct);
            var prescriptionDailyMap = await _prescriptionRepository.GetCreatedCountByDayAsync(previousFrom, ct);

            var patientMonthlyMap = GroupByMonth(patientDailyMap);
            var appointmentMonthlyMap = GroupByMonth(appointmentDailyMap);
            var prescriptionMonthlyMap = GroupByMonth(prescriptionDailyMap);

            var points = new List<DashboardTrendPointDto>(monthCount);
            for (var i = 0; i < monthCount; i++)
            {
                var month = effectiveFrom.AddMonths(i);
                patientMonthlyMap.TryGetValue(month, out var patients);
                appointmentMonthlyMap.TryGetValue(month, out var appointments);
                prescriptionMonthlyMap.TryGetValue(month, out var prescriptions);

                points.Add(new DashboardTrendPointDto
                {
                    Label = month.ToString("MM/yyyy"),
                    PatientsCount = patients,
                    AppointmentsCount = appointments,
                    PrescriptionsCount = prescriptions
                });
            }

            var currentPatientsTotal = points.Sum(x => x.PatientsCount);
            var currentAppointmentsTotal = points.Sum(x => x.AppointmentsCount);
            var currentPrescriptionsTotal = points.Sum(x => x.PrescriptionsCount);
            var previousPatientsTotal = SumByMonthRange(patientMonthlyMap, previousFrom, previousTo);
            var previousAppointmentsTotal = SumByMonthRange(appointmentMonthlyMap, previousFrom, previousTo);
            var previousPrescriptionsTotal = SumByMonthRange(prescriptionMonthlyMap, previousFrom, previousTo);

            var result = new DashboardTrendsDto
            {
                Period = "monthly",
                FromDate = effectiveFrom,
                ToDate = effectiveTo,
                CurrentPatientsTotal = currentPatientsTotal,
                CurrentAppointmentsTotal = currentAppointmentsTotal,
                CurrentPrescriptionsTotal = currentPrescriptionsTotal,
                PreviousPatientsTotal = previousPatientsTotal,
                PreviousAppointmentsTotal = previousAppointmentsTotal,
                PreviousPrescriptionsTotal = previousPrescriptionsTotal,
                Points = points
            };

            await _dashboardQueryCache.SetTrendsAsync("monthly", effectiveFrom, effectiveTo, result, ct);

            return result;
        }

        private static Dictionary<DateTime, int> GroupByMonth(Dictionary<DateTime, int> dailyMap)
        {
            return dailyMap
                .GroupBy(x => new DateTime(x.Key.Year, x.Key.Month, 1))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Value));
        }

        private static int SumByRange(Dictionary<DateTime, int> map, DateTime from, DateTime to)
        {
            if (from > to)
            {
                return 0;
            }

            var sum = 0;
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                if (map.TryGetValue(d, out var value))
                {
                    sum += value;
                }
            }

            return sum;
        }

        private static int SumByMonthRange(Dictionary<DateTime, int> map, DateTime fromMonth, DateTime toMonth)
        {
            if (fromMonth > toMonth)
            {
                return 0;
            }

            var sum = 0;
            for (var m = new DateTime(fromMonth.Year, fromMonth.Month, 1);
                 m <= new DateTime(toMonth.Year, toMonth.Month, 1);
                 m = m.AddMonths(1))
            {
                if (map.TryGetValue(m, out var value))
                {
                    sum += value;
                }
            }

            return sum;
        }
    }
}
