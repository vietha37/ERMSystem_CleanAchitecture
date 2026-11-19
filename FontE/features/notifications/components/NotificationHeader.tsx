"use client";

import { Button } from "@/components/ui/Button";
import { DeliveryStatusFilter, STATUS_OPTIONS } from "../types";

interface NotificationHeaderProps {
  statusFilter: DeliveryStatusFilter;
  onStatusChange: (status: DeliveryStatusFilter) => void;
  pageSize: number;
  onPageSizeChange: (size: number) => void;
  isRefreshing: boolean;
  onRefresh: () => void;
}

export function NotificationHeader({
  statusFilter,
  onStatusChange,
  pageSize,
  onPageSizeChange,
  isRefreshing,
  onRefresh,
}: NotificationHeaderProps) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.25em] text-cyan-700">
            Hệ thống thông báo
          </p>
          <h1 className="mt-2 text-2xl font-bold text-slate-900">
            Quản lý thông báo
          </h1>
          <p className="mt-1 max-w-3xl text-sm leading-6 text-slate-600">
            Theo dõi trạng thái gửi Email và SMS. Hệ thống tự động làm mới định kỳ mỗi 15 giây.
          </p>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <select
            value={statusFilter}
            onChange={(event) =>
              onStatusChange(event.target.value as DeliveryStatusFilter)
            }
            aria-label="Lọc trạng thái gửi"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 outline-none transition focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100"
          >
            {STATUS_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>

          <select
            value={pageSize}
            onChange={(event) => onPageSizeChange(Number(event.target.value))}
            aria-label="Số dòng trên mỗi trang"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 outline-none transition focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100"
          >
            <option value={10}>10 dòng / trang</option>
            <option value={20}>20 dòng / trang</option>
            <option value={50}>50 dòng / trang</option>
          </select>

          <Button
            onClick={onRefresh}
            disabled={isRefreshing}
            className="min-w-36"
          >
            {isRefreshing ? "Đang làm mới..." : "Làm mới"}
          </Button>
        </div>
      </div>
    </div>
  );
}
