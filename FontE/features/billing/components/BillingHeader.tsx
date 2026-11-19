"use client";

import { Button } from "@/components/ui/Button";
import { HospitalInvoiceStatus, STATUS_OPTIONS } from "../types";

interface BillingHeaderProps {
  searchQuery: string;
  onSearchChange: (val: string) => void;
  statusFilter: HospitalInvoiceStatus | "All";
  onStatusChange: (val: HospitalInvoiceStatus | "All") => void;
  isRefreshing: boolean;
  onRefresh: () => void;
  onCreateOpen: () => void;
}

export function BillingHeader({
  searchQuery,
  onSearchChange,
  statusFilter,
  onStatusChange,
  isRefreshing,
  onRefresh,
  onCreateOpen,
}: BillingHeaderProps) {
  return (
    <section className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.25em] text-emerald-700">
            Tài chính
          </p>
          <h1 className="mt-2 text-2xl font-bold text-slate-900">
            Hóa đơn và thanh toán
          </h1>
          <p className="mt-1 max-w-3xl text-sm leading-6 text-slate-600">
            Quản lý hóa đơn viện phí, thu tiền và đối soát giao dịch.
          </p>
        </div>

        <div className="flex flex-col gap-3 md:flex-row md:items-center">
          <input
            type="text"
            value={searchQuery}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Tìm theo mã hóa đơn, bệnh nhân, hồ sơ khám..."
            aria-label="Tìm kiếm hóa đơn"
            className="min-w-[260px] rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100"
          />
          <select
            value={statusFilter}
            onChange={(event) =>
              onStatusChange(event.target.value as HospitalInvoiceStatus | "All")
            }
            aria-label="Lọc trạng thái hóa đơn"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100"
          >
            {STATUS_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <Button
            variant="secondary"
            onClick={onRefresh}
            disabled={isRefreshing}
          >
            {isRefreshing ? "Đang làm mới..." : "Làm mới"}
          </Button>
          <Button onClick={onCreateOpen}>Lập hóa đơn</Button>
        </div>
      </div>
    </section>
  );
}
