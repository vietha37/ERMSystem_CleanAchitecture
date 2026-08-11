"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import toast from "react-hot-toast";
import ProtectedLayout from "@/components/layout/ProtectedLayout";
import { ErrorState } from "@/components/ui/DataState";
import { useAuth } from "@/hooks/useAuth";
import { authService } from "@/services/authService";
import { formatDateTimeValue, formatDateValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalPatientPortalService } from "@/services/hospitalPatientPortalService";
import {
  HospitalPatientPortalAppointment,
  HospitalPatientPortalClinicalOrder,
  HospitalPatientPortalInvoice,
  HospitalPatientPortalOverview,
  HospitalPatientPortalPrescription,
  HospitalPaymentIntent,
  HospitalPatientVisitHistoryItem,
  HospitalPatientVisitHistoryResult,
} from "@/services/types";

function formatDate(value?: string | null): string {
  return formatDateValue(value);
}

function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

function formatCurrency(value?: number | null): string {
  if (value == null || Number.isNaN(value)) {
    return "--";
  }

  return value.toLocaleString("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  });
}

function getAppointmentStatusLabel(status: string): string {
  switch (status) {
    case "Scheduled":
      return "Đã xếp lịch";
    case "CheckedIn":
      return "Đã check-in";
    case "Completed":
      return "Đã hoàn thành";
    case "Cancelled":
      return "Đã hủy";
    case "Pending":
      return "Đang chờ";
    default:
      return status;
  }
}

function getAppointmentStatusStyle(status: string): string {
  switch (status) {
    case "Scheduled":
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
    case "CheckedIn":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "Completed":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Cancelled":
      return "border border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

function getPrescriptionStatusLabel(status: string): string {
  switch (status) {
    case "Issued":
      return "Đã phát hành";
    case "Dispensed":
      return "Đã cấp thuốc";
    case "Cancelled":
      return "Đã hủy";
    default:
      return status;
  }
}

function getPrescriptionStatusStyle(status: string): string {
  switch (status) {
    case "Issued":
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
    case "Dispensed":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Cancelled":
      return "border border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

function getClinicalOrderStatusLabel(status: string): string {
  switch (status) {
    case "Requested":
      return "Đang chờ kết quả";
    case "Completed":
      return "Đã hoàn thành";
    default:
      return status;
  }
}

function getClinicalOrderCategoryLabel(category: string): string {
  switch (category) {
    case "Lab":
      return "Xét nghiệm";
    case "Imaging":
      return "Chẩn đoán hình ảnh";
    default:
      return category;
  }
}

function getInvoiceStatusLabel(status: string): string {
  switch (status) {
    case "Issued":
      return "Đã phát hành";
    case "PartiallyPaid":
      return "Thanh toán một phần";
    case "Paid":
      return "Đã thanh toán";
    case "Cancelled":
      return "Đã hủy";
    default:
      return status;
  }
}

function getInvoiceStatusStyle(status: string): string {
  switch (status) {
    case "Issued":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "PartiallyPaid":
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
    case "Paid":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Cancelled":
      return "border border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

export default function PatientPortalPage() {
  const { logout } = useAuth();
  const [overview, setOverview] = useState<HospitalPatientPortalOverview | null>(null);
  const [visitHistory, setVisitHistory] = useState<HospitalPatientVisitHistoryResult | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isVisitHistoryLoading, setIsVisitHistoryLoading] = useState(true);
  const [portalError, setPortalError] = useState<string | null>(null);
  const [visitHistoryPage, setVisitHistoryPage] = useState(1);
  const [qrInvoice, setQrInvoice] = useState<HospitalPatientPortalInvoice | null>(null);
  const [qrIntent, setQrIntent] = useState<HospitalPaymentIntent | null>(null);
  const [isQrModalOpen, setIsQrModalOpen] = useState(false);
  const [qrLoadingInvoiceId, setQrLoadingInvoiceId] = useState<string | null>(null);
  const [isQrSubmitting, setIsQrSubmitting] = useState(false);
  const [dismissedReminderIds, setDismissedReminderIds] = useState<Set<string>>(new Set());
  const [now, setNow] = useState<Date | null>(null);

  const visitHistoryPageSize = 5;

  const loadPortalData = useCallback(
    async (showMainLoading = true) => {
      if (showMainLoading) {
        setIsLoading(true);
      }
      setIsVisitHistoryLoading(true);

      try {
        const [overviewData, visitHistoryData] = await Promise.all([
          hospitalPatientPortalService.getMyOverview(),
          hospitalPatientPortalService.getMyVisitHistory(visitHistoryPage, visitHistoryPageSize),
        ]);

        setOverview(overviewData);
        setVisitHistory(visitHistoryData);
        setPortalError(null);
      } catch (error: unknown) {
        setPortalError(getApiErrorMessage(error, "Không thể tải cổng thông tin bệnh nhân."));
        toast.error(getApiErrorMessage(error, "Không thể tải cổng thông tin bệnh nhân."));
      } finally {
        if (showMainLoading) {
          setIsLoading(false);
        }

        setIsVisitHistoryLoading(false);
      }
    },
    [visitHistoryPage, visitHistoryPageSize]
  );

  useEffect(() => {
    void loadPortalData();
  }, [loadPortalData]);

  // Cập nhật "now" trên client sau khi mount để tránh lệch Hydration SSR
  useEffect(() => {
    setNow(new Date());
    const timer = setInterval(() => setNow(new Date()), 60_000);
    return () => clearInterval(timer);
  }, []);

  const profile = overview?.profile;
  const upcomingAppointments = useMemo(
    () => overview?.upcomingAppointments ?? [],
    [overview]
  );
  const recentAppointments = overview?.recentAppointments ?? [];
  const recentPrescriptions = overview?.recentPrescriptions ?? [];
  const recentClinicalOrders = overview?.recentClinicalOrders ?? [];
  const recentInvoices = overview?.recentInvoices ?? [];
  const visitHistoryItems = visitHistory?.items ?? [];

  // Lịch hẹn cần nhắc nhở: trong vòng 24 giờ, chưa bị dismiss
  const reminderAppointments = useMemo(() => {
    if (!now) return [];
    const cutoff = new Date(now.getTime() + 24 * 60 * 60 * 1000);
    return upcomingAppointments
      .filter((appt) => {
        if (appt.status === "Cancelled" || appt.status === "Completed") return false;
        if (dismissedReminderIds.has(appt.appointmentId)) return false;
        const start = new Date(appt.appointmentStartLocal);
        return start > now && start <= cutoff;
      })
      .sort((a, b) =>
        new Date(a.appointmentStartLocal).getTime() -
        new Date(b.appointmentStartLocal).getTime()
      );
  }, [upcomingAppointments, dismissedReminderIds, now]);

  const visitHistoryTotalPages = Math.max(
    1,
    Math.ceil((visitHistory?.totalCount ?? 0) / (visitHistory?.pageSize ?? visitHistoryPageSize))
  );

  const stats = {
    totalUpcoming: upcomingAppointments.length,
    totalRecent: recentAppointments.length,
    nextAppointment: upcomingAppointments[0]?.appointmentStartLocal ?? null,
  };

  const handleCreateQrPayment = async (invoice: HospitalPatientPortalInvoice) => {
    setQrInvoice(invoice);
    setQrIntent(null);
    setIsQrModalOpen(true);
    setQrLoadingInvoiceId(invoice.invoiceId);

    try {
      const intent = await hospitalPatientPortalService.createQrPaymentIntent(invoice.invoiceId, {
        gatewayProvider: "MockGateway",
        paymentMethod: "QR",
        amount: invoice.balanceAmount,
      });

      setQrIntent(intent);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tạo mã QR thanh toán."));
      setIsQrModalOpen(false);
      setQrInvoice(null);
    } finally {
      setQrLoadingInvoiceId(null);
    }
  };

  const handleConfirmQrPayment = async () => {
    if (!qrIntent) {
      return;
    }

    setIsQrSubmitting(true);

    try {
      await hospitalPatientPortalService.simulateQrPaymentCallback({
        invoiceId: qrIntent.invoiceId,
        gatewayProvider: qrIntent.gatewayProvider,
        gatewayEventId: `PORTAL-${Date.now()}`,
        gatewayTimestampUtc: new Date().toISOString(),
        paymentReference: qrIntent.paymentReference,
        externalTransactionId: qrIntent.externalTransactionId ?? undefined,
        gatewayStatus: "Captured",
        amount: qrIntent.amount,
      });

      toast.success("Đã ghi nhận thanh toán QR.");
      setIsQrModalOpen(false);
      setQrInvoice(null);
      setQrIntent(null);
      await loadPortalData(false);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể xác nhận thanh toán QR."));
    } finally {
      setIsQrSubmitting(false);
    }
  };

  return (
    <ProtectedLayout>
      <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,_rgba(6,182,212,0.16),_transparent_24%),linear-gradient(180deg,_#eff8ff_0%,_#ffffff_100%)] px-4 py-8 md:px-6">
        <div className="mx-auto max-w-7xl space-y-6">
          {portalError && !isLoading && (
            <section className="rounded-[2rem] border border-rose-100 bg-white/90 shadow-sm">
              <ErrorState
                title="Không thể tải cổng thông tin bệnh nhân"
                description={portalError}
                onAction={() => window.location.reload()}
              />
            </section>
          )}

          <section className="sticky top-4 z-30 rounded-[1.75rem] border border-white/70 bg-white/86 px-5 py-4 shadow-[0_18px_45px_rgba(15,23,42,0.08)] backdrop-blur">
            <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-cyan-700">
                  Điều hướng nhanh
                </p>
                <p className="mt-1 text-sm text-slate-600">
                  Bạn có thể quay lại website công khai để xem bác sĩ, chuyên khoa hoặc đặt lịch mới.
                </p>
              </div>

              <div className="flex flex-wrap gap-2 text-sm">
                <Link
                  href="/"
                  className="rounded-full border border-slate-200 px-4 py-2 font-semibold text-slate-700 transition hover:border-cyan-300 hover:text-cyan-700"
                >
                  Về trang chủ
                </Link>
                <Link
                  href="/doctors"
                  className="rounded-full border border-slate-200 px-4 py-2 font-semibold text-slate-700 transition hover:border-cyan-300 hover:text-cyan-700"
                >
                  Xem bác sĩ
                </Link>
                <Link
                  href="/booking"
                  className="rounded-full bg-slate-950 px-5 py-2 font-semibold text-white transition hover:bg-cyan-700"
                >
                  Đặt lịch mới
                </Link>
              </div>
            </div>
          </section>

          {/* Banner nhắc lịch khám */}
          {!isLoading && reminderAppointments.length > 0 && (
            <AppointmentReminderBanner
              appointments={reminderAppointments}
              now={now ?? new Date()}
              onDismiss={(id) =>
                setDismissedReminderIds((prev) => new Set([...prev, id]))
              }
            />
          )}

          <section className="overflow-hidden rounded-[2.5rem] border border-cyan-100 bg-white/90 shadow-[0_30px_90px_rgba(15,23,42,0.08)] backdrop-blur">
            <div className="grid gap-8 px-8 py-8 lg:grid-cols-[1.2fr_0.8fr] lg:px-10 lg:py-10">
              <div>
                <p className="text-sm font-semibold uppercase tracking-[0.3em] text-cyan-700">
                  Cổng thông tin bệnh nhân
                </p>
                <h1 className="mt-4 text-4xl font-bold tracking-tight text-slate-950 md:text-5xl">
                  Xin chào {profile?.fullName ?? authService.getUsername() ?? "bạn"}.
                </h1>
                <p className="mt-4 max-w-2xl text-base leading-8 text-slate-600">
                  Theo dõi hồ sơ cá nhân, lịch hẹn, đơn thuốc, kết quả cận lâm sàng
                  và hóa đơn trên một giao diện riêng cho người bệnh.
                </p>

                <div className="mt-8 grid gap-4 md:grid-cols-3">
                  <StatCard label="Mã bệnh án" value={profile?.medicalRecordNumber ?? "--"} />
                  <StatCard label="Lịch sắp tới" value={stats.totalUpcoming.toString()} />
                  <StatCard
                    label="Lần khám tiếp theo"
                    value={stats.nextAppointment ? formatDateTime(stats.nextAppointment) : "Chưa có"}
                  />
                </div>
              </div>

              <div className="rounded-[2rem] bg-slate-950 p-6 text-white shadow-[0_20px_55px_rgba(15,23,42,0.14)]">
                <p className="text-xs font-semibold uppercase tracking-[0.28em] text-cyan-200">
                  Trạng thái tài khoản
                </p>
                <div className="mt-5 rounded-[1.5rem] border border-white/10 bg-white/5 p-5">
                  <p className="text-sm text-slate-300">Trạng thái tài khoản</p>
                  <p className="mt-2 text-2xl font-bold text-white">
                    {profile?.portalStatus ?? "Đang đồng bộ"}
                  </p>
                  <p className="mt-3 text-sm leading-7 text-slate-300">
                    Kích hoạt từ: {profile?.activatedAtUtc ? formatDateTime(profile.activatedAtUtc) : "--"}
                  </p>
                </div>

                <div className="mt-6 space-y-3 text-sm leading-7 text-slate-300">
                  <p>Portal này tách riêng khỏi dashboard vận hành nội bộ.</p>
                  <p>
                    Bạn có thể theo dõi nhanh lịch hẹn, lịch sử khám, đơn thuốc,
                    cận lâm sàng và trạng thái thanh toán tại đây.
                  </p>
                </div>

                <button
                  onClick={() => void logout()}
                  className="mt-8 w-full rounded-full border border-cyan-300/40 px-5 py-3 text-sm font-semibold text-cyan-100 transition hover:border-cyan-200 hover:bg-white/10"
                >
                  Đăng xuất
                </button>
              </div>
            </div>
          </section>

          <div className="grid gap-6 xl:grid-cols-[1.05fr_0.95fr]">
            <section className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-sm">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-cyan-700">
                    Hồ sơ của tôi
                  </p>
                  <h2 className="mt-2 text-2xl font-bold text-slate-900">
                    Thông tin cơ bản
                  </h2>
                </div>
                {isLoading && (
                  <div className="rounded-full bg-cyan-50 px-4 py-2 text-xs font-bold uppercase tracking-[0.2em] text-cyan-700">
                    Đang tải
                  </div>
                )}
              </div>

              {isLoading ? (
                <div className="mt-6 grid gap-4 md:grid-cols-2">
                  {[1, 2, 3, 4, 5, 6].map((item) => (
                    <div
                      key={item}
                      className="h-24 animate-pulse rounded-[1.4rem] border border-slate-100 bg-slate-100"
                    />
                  ))}
                </div>
              ) : profile ? (
                <div className="mt-6 grid gap-4 md:grid-cols-2">
                  <InfoCard label="Họ và tên" value={profile.fullName} />
                  <InfoCard label="Ngày sinh" value={formatDate(profile.dateOfBirth)} />
                  <InfoCard label="Giới tính" value={profile.gender} />
                  <InfoCard label="Số điện thoại" value={profile.phone ?? "--"} />
                  <InfoCard label="Email" value={profile.email ?? "--"} />
                  <InfoCard label="Địa chỉ" value={profile.address ?? "--"} className="md:col-span-2" />
                </div>
              ) : (
                <div className="mt-6 rounded-[1.5rem] border border-rose-200 bg-rose-50 p-5 text-sm text-rose-700">
                  Không tìm thấy hồ sơ cổng bệnh nhân của tài khoản này.
                </div>
              )}
            </section>

            <section className="space-y-6">
              <AppointmentPanel
                title="Lịch hẹn sắp tới"
                description="Các lịch hẹn sẽ diễn ra trong những ngày tiếp theo."
                appointments={upcomingAppointments}
                emptyMessage="Bạn chưa có lịch hẹn sắp tới."
              />

              <AppointmentPanel
                title="Lịch sử gần đây"
                description="Tổng hợp những lần khám gần nhất của bạn."
                appointments={recentAppointments}
                emptyMessage="Chưa có lịch sử khám nào trong cổng bệnh nhân."
              />
            </section>
          </div>

          <VisitHistoryPanel
            historyItems={visitHistoryItems}
            isLoading={isVisitHistoryLoading}
            pageNumber={visitHistory?.pageNumber ?? visitHistoryPage}
            totalPages={visitHistoryTotalPages}
            totalCount={visitHistory?.totalCount ?? 0}
            onPrevious={() => setVisitHistoryPage((current) => Math.max(1, current - 1))}
            onNext={() => setVisitHistoryPage((current) => Math.min(visitHistoryTotalPages, current + 1))}
          />

          <div className="grid gap-6 xl:grid-cols-3">
            <PrescriptionsPanel prescriptions={recentPrescriptions} />
            <ClinicalOrdersPanel orders={recentClinicalOrders} />
            <InvoicesPanel
              invoices={recentInvoices}
              onPayQr={handleCreateQrPayment}
              payingInvoiceId={qrLoadingInvoiceId}
            />
          </div>
        </div>
      </div>
      {isQrModalOpen && qrInvoice && (
        <QrPaymentModal
          invoice={qrInvoice}
          intent={qrIntent}
          isLoading={qrLoadingInvoiceId === qrInvoice.invoiceId && !qrIntent}
          isSubmitting={isQrSubmitting}
          onClose={() => {
            if (!isQrSubmitting) {
              setIsQrModalOpen(false);
              setQrInvoice(null);
              setQrIntent(null);
            }
          }}
          onConfirm={handleConfirmQrPayment}
        />
      )}
    </ProtectedLayout>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-[1.6rem] border border-cyan-100 bg-cyan-50/70 px-5 py-4">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-cyan-700">
        {label}
      </p>
      <p className="mt-3 text-lg font-bold text-slate-950">{value}</p>
    </div>
  );
}

function InfoCard({
  label,
  value,
  className = "",
}: {
  label: string;
  value: string;
  className?: string;
}) {
  return (
    <div className={`rounded-[1.4rem] border border-slate-100 bg-slate-50 px-4 py-4 ${className}`.trim()}>
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-400">
        {label}
      </p>
      <p className="mt-2 text-sm font-medium leading-7 text-slate-800">{value}</p>
    </div>
  );
}

function AppointmentPanel({
  title,
  description,
  appointments,
  emptyMessage,
}: {
  title: string;
  description: string;
  appointments: HospitalPatientPortalAppointment[];
  emptyMessage: string;
}) {
  return (
    <section className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-cyan-700">
        {title}
      </p>
      <p className="mt-2 text-sm leading-7 text-slate-500">{description}</p>

      {appointments.length === 0 ? (
        <div className="mt-5 rounded-[1.4rem] border border-dashed border-slate-200 bg-slate-50 px-5 py-8 text-sm text-slate-500">
          {emptyMessage}
        </div>
      ) : (
        <div className="mt-5 space-y-4">
          {appointments.map((appointment) => (
            <article
              key={appointment.appointmentId}
              className="rounded-[1.5rem] border border-slate-100 bg-slate-50/80 p-5"
            >
              <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-400">
                    {appointment.appointmentNumber}
                  </p>
                  <h3 className="mt-2 text-lg font-bold text-slate-900">
                    {appointment.doctorName}
                  </h3>
                  <p className="mt-1 text-sm text-slate-600">
                    {appointment.specialtyName} / {appointment.clinicName}
                  </p>
                </div>

                <span
                  className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getAppointmentStatusStyle(
                    appointment.status
                  )}`}
                >
                  {getAppointmentStatusLabel(appointment.status)}
                </span>
              </div>

              <div className="mt-4 grid gap-3 md:grid-cols-2">
                <MiniInfo label="Thời gian" value={formatDateTime(appointment.appointmentStartLocal)} />
                <MiniInfo label="Kênh đặt lịch" value={appointment.bookingChannel} />
                <MiniInfo label="Loại lịch hẹn" value={appointment.appointmentType} />
                <MiniInfo label="Lý do khám" value={appointment.chiefComplaint ?? "--"} />
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function VisitHistoryPanel({
  historyItems,
  isLoading,
  pageNumber,
  totalPages,
  totalCount,
  onPrevious,
  onNext,
}: {
  historyItems: HospitalPatientVisitHistoryItem[];
  isLoading: boolean;
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  onPrevious: () => void;
  onNext: () => void;
}) {
  return (
    <section className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-sm">
      <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-sky-700">
            Lịch sử khám tổng hợp
          </p>
          <h2 className="mt-2 text-2xl font-bold text-slate-900">
            Tổng hợp theo từng lần đến khám
          </h2>
          <p className="mt-2 text-sm leading-7 text-slate-500">
            Mỗi dòng gom lịch hẹn, hồ sơ khám, chẩn đoán, đơn thuốc, cận lâm sàng và thanh toán.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={onPrevious}
            disabled={pageNumber <= 1 || isLoading}
            className="rounded-full border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:border-sky-300 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Trước
          </button>
          <div className="rounded-full bg-slate-100 px-4 py-2 text-xs font-bold uppercase tracking-[0.2em] text-slate-600">
            Trang {pageNumber}/{totalPages}
          </div>
          <button
            onClick={onNext}
            disabled={pageNumber >= totalPages || isLoading}
            className="rounded-full border border-slate-200 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:border-sky-300 hover:text-sky-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Sau
          </button>
        </div>
      </div>

      {isLoading ? (
        <div className="mt-6 grid gap-4">
          {[1, 2, 3].map((item) => (
            <div
              key={item}
              className="h-40 animate-pulse rounded-[1.5rem] border border-slate-100 bg-slate-100"
            />
          ))}
        </div>
      ) : historyItems.length === 0 ? (
        <EmptyPanel message="Chưa có lịch sử khám nào để tổng hợp." />
      ) : (
        <div className="mt-6 space-y-4">
          {historyItems.map((item) => (
            <article
              key={item.appointmentId}
              className="rounded-[1.5rem] border border-slate-100 bg-slate-50/80 p-5"
            >
              <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-400">
                    {item.appointmentNumber}
                  </p>
                  <h3 className="mt-2 text-lg font-bold text-slate-900">
                    {item.primaryDiagnosisName || item.chiefComplaint || item.appointmentType}
                  </h3>
                  <p className="mt-1 text-sm text-slate-600">
                    {item.doctorName} / {item.specialtyName} / {item.clinicName}
                  </p>
                </div>

                <div className="flex flex-wrap gap-2">
                  <span
                    className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getAppointmentStatusStyle(
                      item.appointmentStatus
                    )}`}
                  >
                    {getAppointmentStatusLabel(item.appointmentStatus)}
                  </span>
                  <span className="inline-flex rounded-full border border-violet-200 bg-violet-50 px-3 py-1 text-xs font-bold text-violet-700">
                    {item.encounterStatus || "Chưa tạo hồ sơ khám"}
                  </span>
                </div>
              </div>

              <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                <MiniInfo label="Hẹn khám" value={formatDateTime(item.appointmentStartLocal)} />
                <MiniInfo label="Check-in" value={formatDateTime(item.checkInTimeLocal)} />
                <MiniInfo label="Hồ sơ khám" value={item.encounterNumber || "--"} />
                <MiniInfo label="Kết thúc" value={formatDateTime(item.encounterEndedLocal)} />
              </div>

              <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
                <MiniInfo label="Đơn thuốc" value={`${item.prescriptionCount}`} />
                <MiniInfo label="Cận lâm sàng" value={`${item.clinicalOrderCount}`} />
                <MiniInfo label="Hóa đơn" value={`${item.invoiceCount}`} />
                <MiniInfo label="Công nợ" value={formatCurrency(item.outstandingBalanceAmount)} />
              </div>

              {(item.clinicalSummary || item.chiefComplaint) && (
                <div className="mt-4 rounded-[1.2rem] border border-sky-100 bg-sky-50 px-4 py-3">
                  <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-sky-700">
                    Tóm tắt lần khám
                  </p>
                  <p className="mt-2 text-sm leading-7 text-slate-700">
                    {item.clinicalSummary || item.chiefComplaint}
                  </p>
                </div>
              )}

              <div className="mt-4 grid gap-3 md:grid-cols-3">
                <MiniInfo label="Tổng viện phí" value={formatCurrency(item.totalInvoiceAmount)} />
                <MiniInfo label="Đã thanh toán" value={formatCurrency(item.totalPaidAmount)} />
                <MiniInfo label="Kênh đặt lịch" value={`${item.bookingChannel} / ${item.appointmentType}`} />
              </div>
            </article>
          ))}
        </div>
      )}

      <p className="mt-5 text-sm text-slate-500">Tổng cộng {totalCount} lần khám trong lịch sử.</p>
    </section>
  );
}

function PrescriptionsPanel({
  prescriptions,
}: {
  prescriptions: HospitalPatientPortalPrescription[];
}) {
  return (
    <section className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-cyan-700">
        Đơn thuốc của tôi
      </p>
      <p className="mt-2 text-sm leading-7 text-slate-500">
        Theo dõi các đơn thuốc mới nhất gắn với hồ sơ khám gần đây.
      </p>

      {prescriptions.length === 0 ? (
        <EmptyPanel message="Chưa có đơn thuốc nào trong cổng bệnh nhân." />
      ) : (
        <div className="mt-5 space-y-4">
          {prescriptions.map((prescription) => (
            <article
              key={prescription.prescriptionId}
              className="rounded-[1.5rem] border border-slate-100 bg-slate-50/80 p-5"
            >
              <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-400">
                    {prescription.prescriptionNumber}
                  </p>
                  <h3 className="mt-2 text-lg font-bold text-slate-900">
                    {prescription.primaryDiagnosisName || prescription.encounterNumber}
                  </h3>
                  <p className="mt-1 text-sm text-slate-600">
                    {prescription.doctorName} / {prescription.specialtyName}
                  </p>
                </div>

                <span
                  className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getPrescriptionStatusStyle(
                    prescription.status
                  )}`}
                >
                  {getPrescriptionStatusLabel(prescription.status)}
                </span>
              </div>

              <div className="mt-4 grid gap-3 md:grid-cols-2">
                <MiniInfo label="Ngày tạo" value={formatDateTime(prescription.createdAtLocal)} />
                <MiniInfo label="Ngày cấp thuốc" value={formatDateTime(prescription.dispensedAtLocal)} />
                <MiniInfo label="Hồ sơ khám" value={prescription.encounterNumber} />
                <MiniInfo label="Số thuốc" value={`${prescription.totalItems} mục`} />
              </div>

              {prescription.items.length > 0 && (
                <div className="mt-4 space-y-2">
                  {prescription.items.map((item) => (
                    <div
                      key={item.prescriptionItemId}
                      className="rounded-[1.2rem] border border-white bg-white px-4 py-3 shadow-sm"
                    >
                      <p className="text-sm font-semibold text-slate-900">
                        {item.medicineName}
                      </p>
                      <p className="mt-1 text-xs text-slate-500">{item.drugCode}</p>
                      <p className="mt-2 text-sm text-slate-700">
                        {item.doseInstruction}
                        {item.route ? ` / ${item.route}` : ""}
                        {item.frequency ? ` / ${item.frequency}` : ""}
                        {item.durationDays ? ` / ${item.durationDays} ngày` : ""}
                      </p>
                      <p className="mt-1 text-xs text-slate-500">
                        Số lượng: {item.quantity} {item.unit || ""}
                      </p>
                    </div>
                  ))}
                </div>
              )}

              {prescription.notes && (
                <div className="mt-4 rounded-[1.2rem] border border-cyan-100 bg-cyan-50 px-4 py-3 text-sm text-cyan-900">
                  {prescription.notes}
                </div>
              )}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function ClinicalOrdersPanel({
  orders,
}: {
  orders: HospitalPatientPortalClinicalOrder[];
}) {
  return (
    <section className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-violet-700">
        Kết quả cận lâm sàng
      </p>
      <p className="mt-2 text-sm leading-7 text-slate-500">
        Các chỉ định xét nghiệm và chẩn đoán hình ảnh gần nhất của bạn.
      </p>

      {orders.length === 0 ? (
        <EmptyPanel message="Chưa có kết quả cận lâm sàng nào trong cổng bệnh nhân." />
      ) : (
        <div className="mt-5 space-y-4">
          {orders.map((order) => (
            <article
              key={order.clinicalOrderId}
              className="rounded-[1.5rem] border border-slate-100 bg-slate-50/80 p-5"
            >
              <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-400">
                    {order.orderNumber}
                  </p>
                  <h3 className="mt-2 text-lg font-bold text-slate-900">
                    {order.serviceName}
                  </h3>
                  <p className="mt-1 text-sm text-slate-600">
                    {getClinicalOrderCategoryLabel(order.category)} / {order.doctorName}
                  </p>
                </div>

                <span className="inline-flex rounded-full border border-violet-200 bg-violet-50 px-3 py-1 text-xs font-bold text-violet-700">
                  {getClinicalOrderStatusLabel(order.status)}
                </span>
              </div>

              <div className="mt-4 grid gap-3 md:grid-cols-2">
                <MiniInfo label="Ngày chỉ định" value={formatDateTime(order.requestedAtLocal)} />
                <MiniInfo label="Hoàn thành" value={formatDateTime(order.completedAtLocal)} />
                <MiniInfo label="Mã dịch vụ" value={order.serviceCode} />
                <MiniInfo label="Hồ sơ khám" value={order.encounterNumber} />
              </div>

              {order.category === "Lab" && order.resultItems.length > 0 && (
                <div className="mt-4 space-y-2">
                  {order.resultItems.map((item) => (
                    <div
                      key={item.resultItemId}
                      className="rounded-[1.2rem] border border-white bg-white px-4 py-3 shadow-sm"
                    >
                      <p className="text-sm font-semibold text-slate-900">
                        {item.analyteName}
                      </p>
                      <p className="mt-1 text-sm text-slate-700">
                        {item.resultValue || "--"} {item.unit || ""}
                      </p>
                      <p className="mt-1 text-xs text-slate-500">
                        Tham chiếu: {item.referenceRange || "--"}
                        {item.abnormalFlag ? ` / Bất thường: ${item.abnormalFlag}` : ""}
                      </p>
                    </div>
                  ))}
                </div>
              )}

              {order.category === "Imaging" && (
                <div className="mt-4 space-y-3">
                  <RichInfoBlock label="Mô tả" value={order.findings || order.summaryText} />
                  <RichInfoBlock label="Kết luận" value={order.impression} />
                  {order.reportUri && (
                    <div className="rounded-[1.2rem] border border-white bg-white px-4 py-3 text-sm shadow-sm">
                      <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">
                        Liên kết báo cáo
                      </p>
                      <p className="mt-2 break-all text-slate-700">{order.reportUri}</p>
                    </div>
                  )}
                </div>
              )}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function InvoicesPanel({
  invoices,
  onPayQr,
  payingInvoiceId,
}: {
  invoices: HospitalPatientPortalInvoice[];
  onPayQr: (invoice: HospitalPatientPortalInvoice) => void;
  payingInvoiceId?: string | null;
}) {
  return (
    <section className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-sm">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-emerald-700">
        Hóa đơn của tôi
      </p>
      <p className="mt-2 text-sm leading-7 text-slate-500">
        Theo dõi hóa đơn, công nợ và các lần thanh toán gần nhất.
      </p>

      {invoices.length === 0 ? (
        <EmptyPanel message="Chưa có hóa đơn nào trong cổng bệnh nhân." />
      ) : (
        <div className="mt-5 space-y-4">
          {invoices.map((invoice) => (
            <article
              key={invoice.invoiceId}
              className="rounded-[1.5rem] border border-slate-100 bg-slate-50/80 p-5"
            >
              <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-400">
                    {invoice.invoiceNumber}
                  </p>
                  <h3 className="mt-2 text-lg font-bold text-slate-900">
                    {formatCurrency(invoice.totalAmount)}
                  </h3>
                  <p className="mt-1 text-sm text-slate-600">
                    {invoice.encounterNumber || "Không gắn hồ sơ khám"}
                  </p>
                </div>

                <div className="flex flex-col items-start gap-2 md:items-end">
                  <span
                    className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getInvoiceStatusStyle(
                      invoice.invoiceStatus
                    )}`}
                  >
                    {getInvoiceStatusLabel(invoice.invoiceStatus)}
                  </span>
                  <button
                    type="button"
                    onClick={() => onPayQr(invoice)}
                    disabled={!canPayInvoiceByQr(invoice) || payingInvoiceId === invoice.invoiceId}
                    className="inline-flex min-h-9 items-center justify-center rounded-full bg-emerald-600 px-4 text-xs font-bold text-white shadow-sm transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:bg-slate-200 disabled:text-slate-500"
                  >
                    {getQrPaymentButtonLabel(invoice, payingInvoiceId === invoice.invoiceId)}
                  </button>
                </div>
              </div>

              <div className="mt-4 grid gap-3 md:grid-cols-2">
                <MiniInfo label="Ngày phát hành" value={formatDateTime(invoice.issuedAtLocal)} />
                <MiniInfo label="Hạn thanh toán" value={formatDateTime(invoice.dueAtLocal)} />
                <MiniInfo label="Đã thanh toán" value={formatCurrency(invoice.paidAmount)} />
                <MiniInfo label="Còn lại" value={formatCurrency(invoice.balanceAmount)} />
              </div>

              {invoice.items.length > 0 && (
                <div className="mt-4 space-y-2">
                  {invoice.items.map((item) => (
                    <div
                      key={item.invoiceItemId}
                      className="rounded-[1.2rem] border border-white bg-white px-4 py-3 shadow-sm"
                    >
                      <p className="text-sm font-semibold text-slate-900">{item.description}</p>
                      <p className="mt-1 text-xs text-slate-500">{item.itemType}</p>
                      <p className="mt-2 text-sm text-slate-700">
                        {item.quantity} x {formatCurrency(item.unitPrice)}
                      </p>
                      <p className="mt-1 text-sm font-semibold text-emerald-700">
                        {formatCurrency(item.lineAmount)}
                      </p>
                    </div>
                  ))}
                </div>
              )}

              {invoice.payments.length > 0 && (
                <div className="mt-4 rounded-[1.2rem] border border-emerald-100 bg-emerald-50 px-4 py-3">
                  <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-emerald-700">
                    Lịch sử thanh toán
                  </p>
                  <div className="mt-3 space-y-2">
                    {invoice.payments.map((payment) => (
                      <div
                        key={payment.paymentId}
                        className="flex flex-col gap-1 text-sm text-emerald-950"
                      >
                        <span>
                          {payment.paymentMethod} / {formatCurrency(payment.amount)}
                        </span>
                        <span className="text-xs text-emerald-700">
                          {payment.paymentReference} / {payment.paymentStatus} /{" "}
                          {formatDateTime(payment.paidAtLocal)}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function canPayInvoiceByQr(invoice: HospitalPatientPortalInvoice): boolean {
  return invoice.balanceAmount > 0 &&
    invoice.invoiceStatus !== "Paid" &&
    invoice.invoiceStatus !== "Cancelled";
}

function getQrPaymentButtonLabel(invoice: HospitalPatientPortalInvoice, isBusy: boolean): string {
  if (isBusy) {
    return "Đang tạo QR";
  }

  if (invoice.invoiceStatus === "Cancelled") {
    return "Đã hủy";
  }

  if (invoice.balanceAmount <= 0 || invoice.invoiceStatus === "Paid") {
    return "Đã thanh toán";
  }

  return "Thanh toán QR";
}

function QrPaymentModal({
  invoice,
  intent,
  isLoading,
  isSubmitting,
  onClose,
  onConfirm,
}: {
  invoice: HospitalPatientPortalInvoice;
  intent: HospitalPaymentIntent | null;
  isLoading: boolean;
  isSubmitting: boolean;
  onClose: () => void;
  onConfirm: () => void;
}) {
  const qrPayload = intent?.checkoutUrl || intent?.checkoutToken || intent?.paymentReference || invoice.invoiceNumber;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/55 px-4 py-6 backdrop-blur-sm">
      <div className="w-full max-w-lg rounded-[1.75rem] bg-white p-6 shadow-2xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-emerald-700">
              Thanh toán QR
            </p>
            <h2 className="mt-2 text-xl font-bold text-slate-950">{invoice.invoiceNumber}</h2>
            <p className="mt-1 text-sm text-slate-500">
              Số tiền: {formatCurrency(intent?.amount ?? invoice.balanceAmount)}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="inline-flex h-9 w-9 items-center justify-center rounded-full border border-slate-200 text-lg font-bold text-slate-500 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
            aria-label="Đóng"
          >
            ×
          </button>
        </div>

        <div className="mt-6 flex flex-col items-center rounded-[1.4rem] border border-emerald-100 bg-emerald-50/70 p-5">
          {isLoading ? (
            <div className="h-56 w-56 animate-pulse rounded-[1.2rem] bg-white/80" />
          ) : (
            <MockQrCode payload={qrPayload} />
          )}
          <p className="mt-4 text-center text-sm font-semibold text-slate-800">
            {intent?.gatewayProvider ?? "MockGateway"} / {intent?.paymentMethod ?? "QR"}
          </p>
          <p className="mt-2 max-w-full break-all text-center text-xs text-slate-500">
            {intent?.paymentReference ?? "Đang tạo mã thanh toán"}
          </p>
        </div>

        {intent?.instructionText && (
          <p className="mt-4 rounded-[1.1rem] border border-slate-100 bg-slate-50 px-4 py-3 text-sm leading-6 text-slate-600">
            {intent.instructionText}
          </p>
        )}

        <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="inline-flex min-h-11 items-center justify-center rounded-full border border-slate-200 px-5 text-sm font-bold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          >
            Đóng
          </button>
          <button
            type="button"
            onClick={onConfirm}
            disabled={!intent || isLoading || isSubmitting}
            className="inline-flex min-h-11 items-center justify-center rounded-full bg-emerald-600 px-5 text-sm font-bold text-white shadow-sm transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:bg-emerald-300"
          >
            {isSubmitting ? "Đang xác nhận" : "Xác nhận đã thanh toán"}
          </button>
        </div>
      </div>
    </div>
  );
}

function MockQrCode({ payload }: { payload: string }) {
  const size = 21;
  const modules = Array.from({ length: size * size }, (_, index) => {
    const x = index % size;
    const y = Math.floor(index / size);
    return isQrModuleFilled(payload, x, y, size);
  });

  return (
    <div className="grid h-56 w-56 grid-cols-[repeat(21,minmax(0,1fr))] rounded-[1rem] border-8 border-white bg-white shadow-sm">
      {modules.map((filled, index) => (
        <span
          key={`${payload}-${index}`}
          className={filled ? "bg-slate-950" : "bg-white"}
          aria-hidden="true"
        />
      ))}
    </div>
  );
}

function isQrModuleFilled(payload: string, x: number, y: number, size: number): boolean {
  if (isFinderModule(x, y) || isFinderModule(x - (size - 7), y) || isFinderModule(x, y - (size - 7))) {
    return true;
  }

  if ((x < 8 && y < 8) || (x >= size - 8 && y < 8) || (x < 8 && y >= size - 8)) {
    return false;
  }

  const seed = payload.split("").reduce((acc, char) => (acc * 31 + char.charCodeAt(0)) % 9973, 17);
  return (seed + x * 13 + y * 19 + x * y) % 7 < 3;
}

function isFinderModule(x: number, y: number): boolean {
  if (x < 0 || y < 0 || x > 6 || y > 6) {
    return false;
  }

  return x === 0 || y === 0 || x === 6 || y === 6 || (x >= 2 && x <= 4 && y >= 2 && y <= 4);
}

function EmptyPanel({ message }: { message: string }) {
  return (
    <div className="mt-5 rounded-[1.4rem] border border-dashed border-slate-200 bg-slate-50 px-5 py-8 text-sm text-slate-500">
      {message}
    </div>
  );
}

function MiniInfo({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-[1.2rem] border border-white bg-white px-4 py-3 shadow-sm">
      <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">
        {label}
      </p>
      <p className="mt-2 text-sm font-medium text-slate-700">{value}</p>
    </div>
  );
}

function RichInfoBlock({
  label,
  value,
}: {
  label: string;
  value?: string | null;
}) {
  return (
    <div className="rounded-[1.2rem] border border-white bg-white px-4 py-3 shadow-sm">
      <p className="text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-400">
        {label}
      </p>
      <p className="mt-2 whitespace-pre-line text-sm leading-7 text-slate-700">
        {value || "--"}
      </p>
    </div>
  );
}

// --- Appointment Reminder Banner --------------------------------------------

type ReminderUrgency = "critical" | "high" | "normal";

function getReminderUrgency(minutesUntil: number): ReminderUrgency {
  if (minutesUntil <= 120) return "critical";
  if (minutesUntil <= 360) return "high";
  return "normal";
}

function formatCountdown(minutesUntil: number): string {
  if (minutesUntil < 1) return "S?p b?t d?u";
  const hours = Math.floor(minutesUntil / 60);
  const mins = minutesUntil % 60;
  if (hours === 0) return `c�n ${mins} ph�t`;
  if (mins === 0) return `c�n ${hours} gi?`;
  return `c�n ${hours} gi? ${mins} ph�t`;
}

const urgencyConfig: Record<
  ReminderUrgency,
  { wrapperClass: string; badgeClass: string; timeClass: string; label: string }
> = {
  critical: {
    wrapperClass:
      "border-orange-300 bg-gradient-to-r from-orange-50 via-red-50 to-orange-50 shadow-[0_0_0_3px_rgba(251,146,60,0.22)]",
    badgeClass: "border border-orange-300 bg-orange-100 text-orange-700",
    timeClass: "text-orange-700 font-bold",
    label: "R?t g?p",
  },
  high: {
    wrapperClass:
      "border-amber-200 bg-gradient-to-r from-amber-50 via-yellow-50 to-amber-50 shadow-[0_0_0_2px_rgba(251,191,36,0.18)]",
    badgeClass: "border border-amber-300 bg-amber-100 text-amber-700",
    timeClass: "text-amber-700 font-semibold",
    label: "G?p",
  },
  normal: {
    wrapperClass:
      "border-cyan-200 bg-gradient-to-r from-cyan-50/60 via-sky-50/60 to-cyan-50/60",
    badgeClass: "border border-cyan-200 bg-cyan-50 text-cyan-700",
    timeClass: "text-cyan-700 font-semibold",
    label: "S?p t?i",
  },
};

const urgencyAccentClass: Record<ReminderUrgency, string> = {
  critical: "bg-orange-400",
  high: "bg-amber-400",
  normal: "bg-cyan-400",
};

function AppointmentReminderBanner({
  appointments,
  now,
  onDismiss,
}: {
  appointments: HospitalPatientPortalAppointment[];
  now: Date;
  onDismiss: (id: string) => void;
}) {
  return (
    <section aria-label="Nh?c l?ch kh�m s?p t?i">
      <div className="mb-3 flex items-center gap-3">
        <div className="flex h-9 w-9 shrink-0 animate-bounce items-center justify-center rounded-full bg-orange-100">
          <span className="text-lg leading-none">??</span>
        </div>
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.24em] text-orange-600">
            Nh?c nh? l?ch kh�m
          </p>
          <p className="mt-0.5 text-sm text-slate-600">
            B?n c� {appointments.length} l?ch h?n s?p di?n ra trong 24 gi? t?i.
          </p>
        </div>
      </div>

      <div className="space-y-3">
        {appointments.map((appt) => {
          const start = new Date(appt.appointmentStartLocal);
          const minutesUntil = Math.max(
            0,
            Math.round((start.getTime() - now.getTime()) / 60_000)
          );
          const urgency = getReminderUrgency(minutesUntil);
          const cfg = urgencyConfig[urgency];
          const accentClass = urgencyAccentClass[urgency];

          return (
            <article
              key={appt.appointmentId}
              className={`relative overflow-hidden rounded-[1.75rem] border p-5 transition-all ${cfg.wrapperClass}`}
            >
              <div
                className={`absolute left-0 top-0 h-full w-1 rounded-l-[1.75rem] ${accentClass}`}
              />
              <div className="flex flex-col gap-4 pl-3 sm:flex-row sm:items-start sm:justify-between">
                <div className="flex-1 space-y-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <span
                      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-bold ${cfg.badgeClass}`}
                    >
                      {urgency === "critical" && (
                        <span className="inline-block h-1.5 w-1.5 animate-pulse rounded-full bg-orange-500" />
                      )}
                      {cfg.label}
                    </span>
                    <span className={`text-sm ${cfg.timeClass}`}>
                      {formatCountdown(minutesUntil)}
                    </span>
                  </div>

                  <div>
                    <h3 className="text-base font-bold text-slate-900">
                      {appt.doctorName}
                    </h3>
                    <p className="mt-0.5 text-sm text-slate-600">
                      {appt.specialtyName}
                      {appt.clinicName ? ` � ${appt.clinicName}` : ""}
                    </p>
                  </div>

                  <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm">
                    <span className="flex items-center gap-1.5 text-slate-700">
                      <span className="text-base leading-none">??</span>
                      <span className="font-medium">
                        {start.toLocaleDateString("vi-VN", {
                          weekday: "long",
                          day: "2-digit",
                          month: "2-digit",
                          year: "numeric",
                        })}
                      </span>
                    </span>
                    <span className="flex items-center gap-1.5 text-slate-700">
                      <span className="text-base leading-none">??</span>
                      <span className="font-medium">
                        {start.toLocaleTimeString("vi-VN", {
                          hour: "2-digit",
                          minute: "2-digit",
                        })}
                      </span>
                    </span>
                    {appt.chiefComplaint && (
                      <span className="italic text-slate-500">
                        &ldquo;{appt.chiefComplaint}&rdquo;
                      </span>
                    )}
                  </div>

                  <p className="font-mono text-[11px] font-semibold tracking-wide text-slate-400">
                    {appt.appointmentNumber}
                  </p>
                </div>

                <button
                  aria-label={`��ng nh?c nh? l?ch h?n ${appt.appointmentNumber}`}
                  onClick={() => onDismiss(appt.appointmentId)}
                  className="shrink-0 self-start rounded-full p-2 text-slate-400 transition hover:bg-white/60 hover:text-slate-600"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                    className="h-4 w-4"
                  >
                    <path d="M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z" />
                  </svg>
                </button>
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
}
