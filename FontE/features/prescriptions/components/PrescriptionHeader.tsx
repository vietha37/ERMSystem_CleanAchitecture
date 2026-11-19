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
    <div className="rounded-3xl border border-sky-100 bg-gradient-to-br from-cyan-50 via-white to-blue-50 p-6 shadow-sm">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.25em] text-cyan-700">
            Quản lý dược
          </p>
          <h1 className="mt-3 text-3xl font-bold text-slate-900">
            Đơn thuốc bệnh viện
          </h1>
          <p className="mt-2 text-sm text-slate-600">
            Phát hành, theo dõi cấp phát và quản lý danh mục đơn thuốc khám chữa bệnh.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2.5">
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            placeholder="Tìm theo mã đơn, bệnh nhân, bác sĩ..."
            className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
          />
          <select
            value={statusFilter}
            onChange={(e) =>
              onStatusChange(e.target.value as HospitalPrescriptionStatus | "All")
            }
            className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-700 shadow-2xs outline-none focus:border-slate-500"
          >
            {STATUS_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
          <Button
            variant="secondary"
            size="sm"
            onClick={onRefresh}
            disabled={isRefreshing}
          >
            {isRefreshing ? "Đang làm mới..." : "Làm mới"}
          </Button>
          <Button size="sm" onClick={onCreateOpen}>
            + Phát hành đơn thuốc
          </Button>
        </div>
      </div>
    </div>
  );
}
