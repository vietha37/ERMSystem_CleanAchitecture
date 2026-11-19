"use client";

import { Button } from "@/components/ui/Button";
import { HospitalPrescriptionStatus, STATUS_OPTIONS } from "../types";

interface PrescriptionHeaderProps {
  searchQuery: string;
  onSearchChange: (val: string) => void;
  statusFilter: HospitalPrescriptionStatus | "All";
  onStatusChange: (val: HospitalPrescriptionStatus | "All") => void;
  isRefreshing: boolean;
  onRefresh: () => void;
  onCreateOpen: () => void;
}

export function PrescriptionHeader({
  searchQuery,
  onSearchChange,
  statusFilter,
  onStatusChange,
  isRefreshing,
  onRefresh,
  onCreateOpen,
}: PrescriptionHeaderProps) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.25em] text-cyan-700">
            Quản lý dược
          </p>
          <h1 className="mt-2 text-2xl font-bold text-slate-900">
            Đơn thuốc bệnh viện
          </h1>
          <p className="mt-1 text-sm text-slate-600">
            Phát hành, theo dõi cấp phát và quản lý danh mục đơn thuốc khám chữa bệnh.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2.5">
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            placeholder="Tìm theo mã đơn, bệnh nhân, bác sĩ..."
            aria-label="Tìm kiếm đơn thuốc"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100"
          />
          <select
            value={statusFilter}
            onChange={(e) =>
              onStatusChange(e.target.value as HospitalPrescriptionStatus | "All")
            }
            aria-label="Lọc trạng thái đơn thuốc"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100"
          >
            {STATUS_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
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
          <Button onClick={onCreateOpen}>
            Kê đơn mới
          </Button>
        </div>
      </div>
    </div>
  );
}
