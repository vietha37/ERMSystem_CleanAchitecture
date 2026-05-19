"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { getApiErrorMessage } from "@/services/error";
import { hospitalNotificationDeliveryService } from "@/services/hospitalNotificationDeliveryService";
import {
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
  if (!value) {
    return "--";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "--";
  }

  return date.toLocaleString("vi-VN");
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
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
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
        const [response, summaryResponse] = await Promise.all([
          hospitalNotificationDeliveryService.getAll(
            statusFilter,
            pageNumber,
            pageSize
          ),
          hospitalNotificationDeliveryService.getSummary(),
        ]);

        setDeliveries(response.items);
        setTotalCount(response.totalCount);
        setSummary(summaryResponse);
      } catch (error: unknown) {
        toast.error(getApiErrorMessage(error, "Không thể tải danh sách gửi thông báo."));
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
      toast.error(getApiErrorMessage(error, "Không thể retry thông báo."));
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
              Vận hành thông báo
            </p>
            <h1 className="mt-3 text-3xl font-bold text-slate-900">
              Theo dõi gửi thông báo
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">
              Giám sát trạng thái gửi Email và SMS từ pipeline notification.
              Trang này tự động làm mới 15 giây để lễ tân và admin theo dõi
              sự cố delivery ngay trong ca trực.
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
            Cần publisher/worker xử lý tiếp.
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
            Đã có provider message id hoặc đã xác nhận gửi.
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
            Cần kiểm tra lỗi và retry nếu phù hợp.
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
            Thường do thiếu template hoặc không đủ dữ liệu người nhận.
          </p>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card className="border border-violet-100 bg-violet-50/70 p-5 shadow-sm hover:shadow-sm">
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-violet-700">
            Cần can thiệp
          </p>
          <p className="mt-3 text-3xl font-bold text-violet-950">
            {summary?.actionRequiredCount ?? 0}
          </p>
          <p className="mt-2 text-sm text-violet-800">
            Failed + Skipped cần operator kiểm tra hoặc retry.
          </p>
        </Card>

        <Card className="border border-orange-100 bg-orange-50/70 p-5 shadow-sm hover:shadow-sm">
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-orange-700">
            Queued quá 15 phút
          </p>
          <p className="mt-3 text-3xl font-bold text-orange-950">
            {summary?.staleQueuedCount ?? 0}
          </p>
          <p className="mt-2 text-sm text-orange-800">
            Dùng như backlog monitor cơ bản cho queue dispatch.
          </p>
        </Card>

        <Card className="border border-cyan-100 bg-cyan-50/70 p-5 shadow-sm hover:shadow-sm">
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-cyan-700">
            Queued lâu nhất
          </p>
          <p className="mt-3 text-lg font-bold text-cyan-950">
            {formatDateTime(summary?.oldestQueuedAtUtc)}
          </p>
          <p className="mt-2 text-sm text-cyan-800">
            Cập nhật gần nhất: {formatDateTime(summary?.generatedAtUtc)}
          </p>
        </Card>
      </div>

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
          <div className="flex flex-col items-center justify-center p-16">
            <div className="mb-4 h-10 w-10 animate-spin rounded-full border-4 border-cyan-100 border-t-cyan-600" />
            <p className="text-sm font-medium text-slate-500">
              Đang tải dữ liệu notification...
            </p>
          </div>
        ) : deliveries.length === 0 ? (
          <div className="p-16 text-center">
            <p className="text-base font-semibold text-slate-700">
              Không có delivery nào khớp bộ lọc hiện tại.
            </p>
            <p className="mt-2 text-sm text-slate-500">
              Thử đổi trạng thái lọc hoặc chờ worker tạo thêm dữ liệu.
            </p>
          </div>
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
