"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalNotificationDeliveryService } from "@/services/hospitalNotificationDeliveryService";
import {
  HospitalCrmEngagementSummary,
  NotificationDelivery,
  NotificationDeliverySummary,
  NotificationDeliveryStatus,
} from "@/services/types";

type DeliveryStatusFilter = NotificationDeliveryStatus | "All";

const STATUS_OPTIONS: Array<{ value: DeliveryStatusFilter; label: string }> = [
  { value: "All", label: "Tất cả" },
  { value: "Queued", label: "Đang chờ gửi" },
  { value: "Delivered", label: "Đã gửi" },
  { value: "Failed", label: "Thất bại" },
  { value: "Skipped", label: "Bỏ qua" },
];

const STATUS_STYLES: Record<NotificationDeliveryStatus, string> = {
  Queued: "border border-amber-200 bg-amber-50 text-amber-700",
  Delivered: "border border-emerald-200 bg-emerald-50 text-emerald-700",
  Failed: "border border-rose-200 bg-rose-50 text-rose-700",
  Skipped: "border border-slate-200 bg-slate-100 text-slate-700",
};

function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

function getStatusLabel(status: NotificationDeliveryStatus): string {
  switch (status) {
    case "Queued":
      return "Đang chờ gửi";
    case "Delivered":
      return "Đã gửi";
    case "Failed":
      return "Thất bại";
    case "Skipped":
      return "Bỏ qua";
    default:
      return status;
  }
}

function canRetry(status: NotificationDeliveryStatus): boolean {
  return status === "Failed" || status === "Skipped";
}

export default function NotificationsPage() {
  const [deliveries, setDeliveries] = useState<NotificationDelivery[]>([]);
  const [statusFilter, setStatusFilter] = useState<DeliveryStatusFilter>("All");
  const [summary, setSummary] = useState<NotificationDeliverySummary | null>(null);
  const [engagementSummary, setEngagementSummary] =
    useState<HospitalCrmEngagementSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);
  const [retryingId, setRetryingId] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endItem = totalCount === 0 ? 0 : Math.min(pageNumber * pageSize, totalCount);

  const fetchDeliveries = useCallback(
    async (showRefreshState = false) => {
      if (showRefreshState) {
        setIsRefreshing(true);
      } else {
        setIsLoading(true);
      }

      try {
        const [response, summaryResponse, engagementResponse] = await Promise.all([
          hospitalNotificationDeliveryService.getAll(
            statusFilter,
            pageNumber,
            pageSize
          ),
          hospitalNotificationDeliveryService.getSummary(),
          hospitalNotificationDeliveryService.getEngagementSummary(),
        ]);

        setDeliveries(response.items);
        setTotalCount(response.totalCount);
        setSummary(summaryResponse);
        setEngagementSummary(engagementResponse);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, "Không thể tải danh sách gửi thông báo.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [pageNumber, pageSize, statusFilter]
  );

  useEffect(() => {
    void fetchDeliveries();
  }, [fetchDeliveries]);

  useEffect(() => {
    const timer = window.setInterval(() => {
      void fetchDeliveries(true);
    }, 15000);

    return () => window.clearInterval(timer);
  }, [fetchDeliveries]);

  const metrics = useMemo(() => {
    return {
      Queued: summary?.queuedCount ?? 0,
      Delivered: summary?.deliveredCount ?? 0,
      Failed: summary?.failedCount ?? 0,
      Skipped: summary?.skippedCount ?? 0,
    } as Record<NotificationDeliveryStatus, number>;
  }, [summary]);

  const handleRetry = async (delivery: NotificationDelivery) => {
    setRetryingId(delivery.id);

    try {
      await hospitalNotificationDeliveryService.retry(delivery.id);
      toast.success("Đã đưa thông báo về hàng đợi gửi lại.");
      await fetchDeliveries(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể gửi lại thông báo."));
    } finally {
      setRetryingId(null);
    }
  };

  const handleStatusChange = (nextStatus: DeliveryStatusFilter) => {
    setStatusFilter(nextStatus);
    setPageNumber(1);
  };

  const handleRetry = async (delivery: NotificationDelivery) => {
    setRetryingId(delivery.id);

    try {
      await hospitalNotificationDeliveryService.retry(delivery.id);
      toast.success("Đã đưa thông báo về hàng đợi gửi lại.");
      await fetchDeliveries(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể gửi lại thông báo."));
    } finally {
      setRetryingId(null);
    }
  };

  const handleStatusChange = (nextStatus: DeliveryStatusFilter) => {
    setStatusFilter(nextStatus);
    setPageNumber(1);
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <div className="rounded-3xl border border-sky-100 bg-gradient-to-br from-cyan-50 via-white to-blue-50 p-6 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.25em] text-cyan-700">
              Thông báo
            </p>
            <h1 className="mt-3 text-3xl font-bold text-slate-900">
              Quản lý thông báo
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">
              Theo dõi trạng thái gửi Email và SMS. Trang tự động cập nhật mỗi 15 giây.
            </p>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <select
              value={statusFilter}
              onChange={(event) =>
                handleStatusChange(event.target.value as DeliveryStatusFilter)
              }
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            >
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>

            <select
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value));
                setPageNumber(1);
              }}
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm font-medium text-slate-700 outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            >
              <option value={10}>10 dòng / trang</option>
              <option value={20}>20 dòng / trang</option>
              <option value={50}>50 dòng / trang</option>
            </select>

            <Button
              onClick={() => void fetchDeliveries(true)}
              disabled={isRefreshing}
              className="min-w-36"
            >
              {isRefreshing ? "Đang làm mới..." : "Làm mới"}
            </Button>
          </div>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <Card className="border border-amber-100 bg-amber-50/60 p-5 shadow-sm hover:shadow-sm">
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-amber-700">
            Đang chờ gửi
          </p>
          <p className="mt-3 text-3xl font-bold text-amber-950">
            {metrics.Queued}
          </p>
          <p className="mt-2 text-sm text-amber-800">
            Đang chờ xử lý.
          </p>
        </Card>

        <Card className="border border-emerald-100 bg-emerald-50/70 p-5 shadow-sm hover:shadow-sm">
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-emerald-700">
            Đã gửi
          </p>
          <p className="mt-3 text-3xl font-bold text-emerald-950">
            {metrics.Delivered}
          </p>
          <p className="mt-2 text-sm text-emerald-800">
            Đã gửi thành công.
          </p>
        </Card>

        <Card className="border border-rose-100 bg-rose-50/70 p-5 shadow-sm hover:shadow-sm">
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-rose-700">
            Thất bại
          </p>
          <p className="mt-3 text-3xl font-bold text-rose-950">
            {metrics.Failed}
          </p>
          <p className="mt-2 text-sm text-rose-800">
            Gửi thất bại, cần kiểm tra.
          </p>
        </Card>

        <Card className="border border-slate-200 bg-slate-50/80 p-5 shadow-sm hover:shadow-sm">
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-slate-600">
            Bỏ qua
          </p>
          <p className="mt-3 text-3xl font-bold text-slate-900">
            {metrics.Skipped}
          </p>
          <p className="mt-2 text-sm text-slate-600">
            Bị bỏ qua do thiếu thông tin.
          </p>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
            Cập nhật gần nhất: {formatDateTime(summary?.generatedAtUtc)}
          </p>
        </Card>
      </div>

      <Card className="overflow-hidden border border-cyan-100 p-0 shadow-sm">
        <div className="border-b border-cyan-100 bg-cyan-50/70 px-6 py-5">
          <div className="flex flex-col gap-2 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.2em] text-cyan-700">
                Chăm sóc bệnh nhân
              </p>
              <h2 className="mt-2 text-xl font-bold text-slate-900">
                Theo dõi sau khám
              </h2>
              <p className="mt-1 max-w-3xl text-sm text-slate-600">
                Nhắc tái khám, khảo sát hài lòng và chăm sóc sau khám.
              </p>
            </div>

            <p className="text-sm text-cyan-800">
              Cập nhật: {formatDateTime(engagementSummary?.generatedAtUtc)}
            </p>
          </div>
        </div>

        <div className="grid gap-4 px-6 py-5 md:grid-cols-2 xl:grid-cols-4">
          <div className="rounded-2xl border border-cyan-100 bg-white p-4">
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-cyan-700">
              Tổng chiến dịch
            </p>
            <p className="mt-3 text-3xl font-bold text-slate-950">
              {engagementSummary?.totalCampaignMessages ?? 0}
            </p>
            <p className="mt-2 text-sm text-slate-600">
              Tổng số đã tạo.
            </p>
          </div>

          <div className="rounded-2xl border border-emerald-100 bg-white p-4">
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-emerald-700">
              Đã gửi thành công
            </p>
            <p className="mt-3 text-3xl font-bold text-slate-950">
              {engagementSummary?.deliveredDeliveries ?? 0}
            </p>
            <p className="mt-2 text-sm text-slate-600">
              Đã gửi thành công.
            </p>
          </div>

          <div className="rounded-2xl border border-amber-100 bg-white p-4">
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-amber-700">
              Đang chờ / cần xử lý
            </p>
            <p className="mt-3 text-3xl font-bold text-slate-950">
              {(engagementSummary?.queuedDeliveries ?? 0) +
                (engagementSummary?.actionRequiredDeliveries ?? 0)}
            </p>
            <p className="mt-2 text-sm text-slate-600">
              Đang chờ hoặc cần xử lý.
            </p>
          </div>

          <div className="rounded-2xl border border-violet-100 bg-white p-4">
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-violet-700">
              Tổng người nhận
            </p>
            <p className="mt-3 text-3xl font-bold text-slate-950">
              {engagementSummary?.totalRecipients ?? 0}
            </p>
            <p className="mt-2 text-sm text-slate-600">
              Tổng Email/SMS đã tạo.
            </p>
          </div>
        </div>

        <div className="grid gap-4 border-t border-slate-100 px-6 py-5 lg:grid-cols-[1.1fr,1.4fr]">
          <div className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-3">
              <div className="rounded-2xl border border-sky-100 bg-sky-50/70 p-4">
                <p className="text-xs font-bold uppercase tracking-[0.16em] text-sky-700">
                  Nhắc tái khám
                </p>
                <p className="mt-2 text-2xl font-bold text-slate-950">
                  {engagementSummary?.revisitReminderMessages ?? 0}
                </p>
              </div>
              <div className="rounded-2xl border border-teal-100 bg-teal-50/70 p-4">
                <p className="text-xs font-bold uppercase tracking-[0.16em] text-teal-700">
                  Khảo sát hài lòng
                </p>
                <p className="mt-2 text-2xl font-bold text-slate-950">
                  {engagementSummary?.satisfactionSurveyMessages ?? 0}
                </p>
              </div>
              <div className="rounded-2xl border border-indigo-100 bg-indigo-50/70 p-4">
                <p className="text-xs font-bold uppercase tracking-[0.16em] text-indigo-700">
                  CSKH follow-up
                </p>
                <p className="mt-2 text-2xl font-bold text-slate-950">
                  {engagementSummary?.customerCareFollowUpMessages ?? 0}
                </p>
              </div>
            </div>

            <div className="rounded-2xl border border-slate-100 bg-slate-50/80 p-4">
              <h3 className="text-sm font-bold text-slate-900">
                Xu hướng 14 ngày gần nhất
              </h3>
              <div className="mt-4 space-y-3">
                {engagementSummary?.trendPoints?.slice(-7).map((point) => {
                  const total =
                    point.revisitReminderCount +
                    point.satisfactionSurveyCount +
                    point.customerCareFollowUpCount;

                  return (
                    <div key={point.date} className="space-y-2">
                      <div className="flex items-center justify-between text-sm">
                        <span className="font-medium text-slate-700">{point.label}</span>
                        <span className="text-slate-500">{total} chiến dịch</span>
                      </div>
                      <div className="flex h-2 overflow-hidden rounded-full bg-slate-200">
                        <div
                          className="bg-sky-500"
                          style={{ width: `${Math.min(100, point.revisitReminderCount * 12)}%` }}
                        />
                        <div
                          className="bg-teal-500"
                          style={{ width: `${Math.min(100, point.satisfactionSurveyCount * 12)}%` }}
                        />
                        <div
                          className="bg-indigo-500"
                          style={{ width: `${Math.min(100, point.customerCareFollowUpCount * 12)}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          <div className="rounded-2xl border border-slate-100 bg-white p-4">
            <h3 className="text-sm font-bold text-slate-900">
              Hoạt động CRM gần đây
            </h3>
            <div className="mt-4 space-y-3">
              {engagementSummary?.recentActivities?.length ? (
                engagementSummary.recentActivities.map((activity) => (
                  <div
                    key={activity.outboxMessageId}
                    className="rounded-2xl border border-slate-100 bg-slate-50/70 p-4"
                  >
                    <div className="flex flex-col gap-2 lg:flex-row lg:items-start lg:justify-between">
                      <div>
                        <p className="text-sm font-bold text-slate-900">
                          {activity.eventLabel} · {activity.patientName}
                        </p>
                        <p className="mt-1 text-xs text-slate-500">
                          MRN: {activity.medicalRecordNumber || "--"} ·{" "}
                          {activity.clinicName || "Chưa rõ phòng khám"} ·{" "}
                          {activity.doctorName || "Chưa rõ bác sĩ"}
                        </p>
                      </div>
                      <p className="text-xs font-medium text-slate-500">
                        {formatDateTime(activity.availableAtUtc)}
                      </p>
                    </div>

                    <div className="mt-3 flex flex-wrap gap-2 text-xs font-semibold">
                      <span className="rounded-full bg-sky-100 px-3 py-1 text-sky-700">
                        Người nhận: {activity.recipientCount}
                      </span>
                      <span className="rounded-full bg-amber-100 px-3 py-1 text-amber-700">
                        Queued: {activity.queuedCount}
                      </span>
                      <span className="rounded-full bg-emerald-100 px-3 py-1 text-emerald-700">
                        Delivered: {activity.deliveredCount}
                      </span>
                      <span className="rounded-full bg-rose-100 px-3 py-1 text-rose-700">
                        Failed/Skipped: {activity.failedCount + activity.skippedCount}
                      </span>
                    </div>
                  </div>
                ))
              ) : (
                <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 p-6 text-sm text-slate-500">
                  Chưa có hoạt động CRM nào được ghi nhận.
                </div>
              )}
            </div>
          </div>
        </div>
      </Card>

      <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
        <div className="flex flex-col gap-3 border-b border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-bold text-slate-900">
              Hàng đợi gửi
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              Tổng cộng {totalCount} thông báo theo bộ lọc hiện tại.
            </p>
          </div>

          <div className="rounded-full bg-slate-100 px-4 py-2 text-xs font-bold uppercase tracking-[0.2em] text-slate-600">
            Trang {pageNumber}/{totalPages}
          </div>
        </div>

        {isLoading ? (
          <LoadingState title="Đang tải..." tone="cyan" />
        ) : listError ? (
          <ErrorState
            title="Không thể tải hàng đợi gửi"
            description={listError}
            onAction={() => void fetchDeliveries(true)}
          />
        ) : deliveries.length === 0 ? (
          <EmptyState
            title="Không có thông báo nào khớp bộ lọc hiện tại."
            description="Thay đổi bộ lọc để xem thông báo khác."
            tone="cyan"
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-[1200px] w-full border-collapse text-left">
              <thead>
                <tr className="bg-slate-50">
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.15em] text-slate-500">
                    Kênh
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.15em] text-slate-500">
                    Người nhận
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.15em] text-slate-500">
                    Trạng thái
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.15em] text-slate-500">
                    Số lần thử
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.15em] text-slate-500">
                    Lần thử gần nhất
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.15em] text-slate-500">
                    Thời điểm gửi
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.15em] text-slate-500">
                    Provider message id
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.15em] text-slate-500">
                    Lỗi gần nhất
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.15em] text-slate-500">
                    Tác vụ
                  </th>
                </tr>
              </thead>
              <tbody>
                {deliveries.map((delivery) => (
                  <tr
                    key={delivery.id}
                    className="border-t border-slate-100 align-top transition-colors hover:bg-cyan-50/40"
                  >
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">
                        {delivery.channelCode}
                      </div>
                      <div className="mt-1 text-xs text-slate-500">
                        Outbox: {delivery.outboxMessageId}
                      </div>
                    </td>
                    <td className="px-6 py-4 text-sm font-medium text-slate-700">
                      {delivery.recipient}
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${STATUS_STYLES[delivery.deliveryStatus]}`}
                      >
                        {getStatusLabel(delivery.deliveryStatus)}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      {delivery.attemptCount}
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      {formatDateTime(delivery.lastAttemptAtUtc)}
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      {formatDateTime(delivery.deliveredAtUtc)}
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      <span className="break-all">
                        {delivery.providerMessageId || "--"}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      <p className="max-w-xs break-words text-rose-700">
                        {delivery.errorMessage || "--"}
                      </p>
                    </td>
                    <td className="px-6 py-4">
                      {canRetry(delivery.deliveryStatus) ? (
                        <Button
                          variant="secondary"
                          onClick={() => void handleRetry(delivery)}
                          disabled={retryingId === delivery.id}
                          className="min-w-28 border-cyan-200 text-cyan-700 hover:bg-cyan-50"
                        >
                          {retryingId === delivery.id ? "Đang gửi lại..." : "Gửi lại"}
                        </Button>
                      ) : (
                        <span className="text-sm font-medium text-slate-400">
                          Không cần gửi lại
                        </span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="flex flex-col gap-3 border-t border-slate-100 px-6 py-4 md:flex-row md:items-center md:justify-between">
          <p className="text-sm text-slate-500">
            Hiển thị {startItem}-{endItem} / {totalCount} dòng.
          </p>

          <div className="flex items-center gap-2">
            <Button
              variant="secondary"
              onClick={() => setPageNumber(1)}
              disabled={pageNumber === 1}
            >
              Đầu
            </Button>
            <Button
              variant="secondary"
              onClick={() => setPageNumber((current) => current - 1)}
              disabled={pageNumber === 1}
            >
              Trước
            </Button>
            <Button
              variant="secondary"
              onClick={() => setPageNumber((current) => current + 1)}
              disabled={pageNumber >= totalPages}
            >
              Sau
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
}
