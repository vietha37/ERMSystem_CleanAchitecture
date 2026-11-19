"use client";

import { Button } from "@/components/ui/Button";
import {
  CATEGORY_OPTIONS,
  HospitalClinicalOrderCategory,
  HospitalClinicalOrderStatus,
  STATUS_OPTIONS,
} from "../types";

interface ClinicalOrderHeaderProps {
  searchQuery: string;
  onSearchChange: (val: string) => void;
  categoryFilter: HospitalClinicalOrderCategory | "All";
  onCategoryChange: (val: HospitalClinicalOrderCategory | "All") => void;
  statusFilter: HospitalClinicalOrderStatus | "All";
  onStatusChange: (val: HospitalClinicalOrderStatus | "All") => void;
  isRefreshing: boolean;
  onRefresh: () => void;
  onCreateOpen: () => void;
}

export function ClinicalOrderHeader({
  searchQuery,
  onSearchChange,
  categoryFilter,
  onCategoryChange,
  statusFilter,
  onStatusChange,
  isRefreshing,
  onRefresh,
  onCreateOpen,
}: ClinicalOrderHeaderProps) {
  return (
    <div className="rounded-[2rem] border border-violet-100 bg-gradient-to-br from-violet-50 via-white to-cyan-50 p-6 shadow-sm">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.26em] text-violet-700">
            Chỉ định
          </p>
          <h1 className="mt-3 text-3xl font-bold text-slate-950">
            Chỉ định cận lâm sàng
          </h1>
          <p className="mt-2 max-w-3xl text-sm leading-7 text-slate-600">
            Xét nghiệm và chẩn đoán hình ảnh theo hồ sơ khám.
          </p>
        </div>

        <div className="flex flex-col gap-3 md:flex-row md:items-center">
          <input
            type="text"
            value={searchQuery}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Tìm theo mã chỉ định, bệnh nhân, dịch vụ..."
            className="min-w-[260px] rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
          />
          <select
            value={categoryFilter}
            onChange={(event) =>
              onCategoryChange(event.target.value as HospitalClinicalOrderCategory | "All")
            }
            className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
          >
            {CATEGORY_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>

          <select
            value={statusFilter}
            onChange={(event) =>
              onStatusChange(event.target.value as HospitalClinicalOrderStatus | "All")
            }
            className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
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
          <Button onClick={onCreateOpen}>Tạo chỉ định</Button>
        </div>
      </div>
    </div>
  );
}
