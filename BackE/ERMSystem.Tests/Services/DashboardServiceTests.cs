using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ERMSystem.Application.DTOs;
using ERMSystem.Application.Interfaces;
using ERMSystem.Application.Services;
using Moq;
using Xunit;

namespace ERMSystem.Tests.Services
{
    public class DashboardServiceTests
    {
        private readonly Mock<IPatientRepository> _patientRepoMock;
        private readonly Mock<IAppointmentRepository> _appointmentRepoMock;
        private readonly Mock<IMedicalRecordRepository> _medicalRecordRepoMock;
        private readonly Mock<IPrescriptionRepository> _prescriptionRepoMock;
        private readonly Mock<IHospitalBillingRepository> _billingRepoMock;
        private readonly Mock<IDashboardQueryCache> _cacheMock;
        private readonly DashboardService _service;

        public DashboardServiceTests()
        {
            _patientRepoMock = new Mock<IPatientRepository>();
            _appointmentRepoMock = new Mock<IAppointmentRepository>();
            _medicalRecordRepoMock = new Mock<IMedicalRecordRepository>();
            _prescriptionRepoMock = new Mock<IPrescriptionRepository>();
            _billingRepoMock = new Mock<IHospitalBillingRepository>();
            _cacheMock = new Mock<IDashboardQueryCache>();

            _service = new DashboardService(
                _patientRepoMock.Object,
                _appointmentRepoMock.Object,
                _medicalRecordRepoMock.Object,
                _prescriptionRepoMock.Object,
                _billingRepoMock.Object,
                _cacheMock.Object
            );
        }

        [Fact]
        public async Task GetDashboardStatsAsync_ReturnsCachedValue_WhenCacheHit()
        {
            var cachedStats = new DashboardStatsDto { TotalPatients = 100, AppointmentsToday = 10 };
            _cacheMock.Setup(c => c.GetStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cachedStats);

            var result = await _service.GetDashboardStatsAsync();

            Assert.Equal(100, result.TotalPatients);
            Assert.Equal(10, result.AppointmentsToday);
            _patientRepoMock.Verify(p => p.GetTotalCountAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetDashboardStatsAsync_CalculatesStatsAndCaches_WhenCacheMiss()
        {
            _cacheMock.Setup(c => c.GetStatsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((DashboardStatsDto?)null);
            _patientRepoMock.Setup(p => p.GetTotalCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(50);
            _appointmentRepoMock.Setup(a => a.GetAppointmentsTodayCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10);
            _appointmentRepoMock.Setup(a => a.GetPendingAppointmentsTodayCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(3);
            _appointmentRepoMock.Setup(a => a.GetCompletedAppointmentsTodayCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(5);
            _appointmentRepoMock.Setup(a => a.GetCancelledAppointmentsTodayCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
            _appointmentRepoMock.Setup(a => a.GetRevisitAppointmentsTodayCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _medicalRecordRepoMock.Setup(m => m.GetTopDiagnosesAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, int> { { "Flu", 15 } });
            _billingRepoMock.Setup(b => b.GetDashboardSnapshotAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HospitalBillingDashboardSnapshot
                {
                    TotalInvoices = 20,
                    PaidInvoices = 18,
                    IssuedAmountInRange = 1000m,
                    CollectedAmountInRange = 900m,
                    OutstandingBalanceAmount = 100m
                });

            var result = await _service.GetDashboardStatsAsync();

            Assert.NotNull(result);
            Assert.Equal(50, result.TotalPatients);
            Assert.Equal(10, result.AppointmentsToday);
            Assert.Equal(50.0m, result.CompletionRatePercent); // 5/10 * 100
            Assert.Equal(20.0m, result.CancellationRatePercent); // 2/10 * 100
            Assert.Equal(90.0m, result.CollectionRatePercent); // 900/1000 * 100
            _cacheMock.Verify(c => c.SetStatsAsync(result, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetDashboardTrendsAsync_Daily_ReturnsTrendPoints()
        {
            _cacheMock.Setup(c => c.GetTrendsAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DashboardTrendsDto?)null);

            _patientRepoMock.Setup(p => p.GetCreatedCountByDayAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<DateTime, int>());
            _appointmentRepoMock.Setup(a => a.GetScheduledCountByDayAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<DateTime, int>());
            _prescriptionRepoMock.Setup(pr => pr.GetCreatedCountByDayAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<DateTime, int>());

            var result = await _service.GetDashboardTrendsAsync("daily");

            Assert.NotNull(result);
            Assert.Equal("daily", result.Period);
            Assert.NotEmpty(result.Points);
        }

        [Fact]
        public async Task GetDashboardTrendsAsync_Monthly_ReturnsMonthlyTrendPoints()
        {
            _cacheMock.Setup(c => c.GetTrendsAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DashboardTrendsDto?)null);

            _patientRepoMock.Setup(p => p.GetCreatedCountByDayAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<DateTime, int>());
            _appointmentRepoMock.Setup(a => a.GetScheduledCountByDayAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<DateTime, int>());
            _prescriptionRepoMock.Setup(pr => pr.GetCreatedCountByDayAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<DateTime, int>());

            var result = await _service.GetDashboardTrendsAsync("monthly");

            Assert.NotNull(result);
            Assert.Equal("monthly", result.Period);
            Assert.NotEmpty(result.Points);
        }
    }
}
