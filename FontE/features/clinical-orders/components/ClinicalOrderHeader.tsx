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
    <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.25em] text-violet-700">
            Chỉ định
          </p>
          <h1 className="mt-2 text-2xl font-bold text-slate-900">
            Chỉ định cận lâm sàng
          </h1>
          <p className="mt-1 max-w-3xl text-sm leading-6 text-slate-600">
            Xét nghiệm và chẩn đoán hình ảnh theo hồ sơ khám bệnh.
          </p>
        </div>

        <div className="flex flex-col gap-3 md:flex-row md:items-center">
          <input
            type="text"
            value={searchQuery}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Tìm theo mã chỉ định, bệnh nhân, dịch vụ..."
            className="min-w-[260px] rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-violet-600 focus:ring-2 focus:ring-violet-100"
          />
          <select
            value={categoryFilter}
            onChange={(event) =>
              onCategoryChange(event.target.value as HospitalClinicalOrderCategory | "All")
            }
            aria-label="Lọc loại chỉ định"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-violet-600 focus:ring-2 focus:ring-violet-100"
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
            aria-label="Lọc trạng thái chỉ định"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-violet-600 focus:ring-2 focus:ring-violet-100"
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
