"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { TablePagination } from "@/shared/ui/Table/TablePagination";
import {
  canRetry,
  getStatusLabel,
  NotificationDelivery,
  STATUS_STYLES,
} from "../types";

interface NotificationTableProps {
  deliveries: NotificationDelivery[];
  isLoading: boolean;
  listError: string | null;
  retryingId: string | null;
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  onRefresh: () => void;
  onRetry: (delivery: NotificationDelivery) => void;
}

export function NotificationTable({
  deliveries,
  isLoading,
  listError,
  retryingId,
  pageNumber,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
  onRefresh,
  onRetry,
}: NotificationTableProps) {
  return (
    <Card className="overflow-hidden border border-slate-200 bg-white p-0 shadow-xs">
      <div className="flex flex-col gap-3 border-b border-slate-200 px-6 py-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-base font-bold text-slate-900">
            Hàng đợi gửi thông báo
          </h2>
          <p className="text-xs text-slate-500">
            Tổng cộng {totalCount} thông báo theo bộ lọc hiện tại.
          </p>
        </div>

        <div className="rounded-md bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
          Trang {pageNumber} / {Math.max(1, totalPages)}
        </div>
      </div>

      {isLoading ? (
        <LoadingState title="Đang tải danh sách thông báo..." tone="cyan" />
      ) : listError ? (
        <ErrorState
          title="Không thể tải hàng đợi gửi"
          description={listError}
          onAction={onRefresh}
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
              <tr className="border-b border-slate-200 bg-slate-50/75">
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Kênh
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Người nhận
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Trạng thái
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Số lần thử
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Lần thử gần nhất
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Thời điểm gửi
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Provider ID
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Lỗi gần nhất
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Tác vụ
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {deliveries.map((delivery) => (
                <tr
                  key={delivery.id}
                  className="align-top transition-colors hover:bg-slate-50/80"
                >
                  <td className="px-6 py-3.5">
                    <div className="text-sm font-semibold text-slate-900">
                      {delivery.channelCode}
                    </div>
                    <div className="mt-0.5 text-xs text-slate-400">
                      ID: {delivery.outboxMessageId.slice(0, 8)}...
                    </div>
                  </td>
                  <td className="px-6 py-3.5 text-sm font-medium text-slate-800">
                    {delivery.recipient}
                  </td>
                  <td className="px-6 py-3.5">
                    <span
                      className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ${STATUS_STYLES[delivery.deliveryStatus]}`}
                    >
                      {getStatusLabel(delivery.deliveryStatus)}
                    </span>
                  </td>
                  <td className="px-6 py-3.5 text-sm text-slate-600">
                    {delivery.attemptCount}
                  </td>
                  <td className="px-6 py-3.5 text-xs text-slate-600">
                    {formatDateTimeValue(delivery.lastAttemptAtUtc)}
                  </td>
                  <td className="px-6 py-3.5 text-xs text-slate-600">
                    {formatDateTimeValue(delivery.deliveredAtUtc)}
                  </td>
                  <td className="px-6 py-3.5 text-xs text-slate-600">
                    <span className="font-mono text-xs break-all">
                      {delivery.providerMessageId || "--"}
                    </span>
                  </td>
                  <td className="px-6 py-3.5 text-xs text-slate-600">
                    <p className="max-w-xs break-words text-rose-600">
                      {delivery.errorMessage || "--"}
                    </p>
                  </td>
                  <td className="px-6 py-3.5">
                    {canRetry(delivery.deliveryStatus) ? (
                      <Button
                        variant="secondary"
                        onClick={() => onRetry(delivery)}
                        disabled={retryingId === delivery.id}
                        className="text-xs font-medium text-cyan-700 hover:bg-cyan-50"
                      >
                        {retryingId === delivery.id ? "Đang gửi..." : "Gửi lại"}
                      </Button>
                    ) : (
                      <span className="text-xs text-slate-400">
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

      <div className="border-t border-slate-200 px-6 py-3">
        <TablePagination
          currentPage={pageNumber}
          totalPages={totalPages}
          totalItems={totalCount}
          pageSize={pageSize}
          onPageChange={onPageChange}
          onPageSizeChange={onPageSizeChange}
          pageSizeOptions={[10, 20, 50]}
          itemName="thông báo"
        />
      </div>
    </Card>
  );
}
