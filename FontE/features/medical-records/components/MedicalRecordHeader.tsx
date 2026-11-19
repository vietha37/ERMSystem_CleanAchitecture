"use client";

import { Button } from "@/components/ui/Button";
import {
  ENCOUNTER_STATUS_OPTIONS,
  HospitalEncounterStatus,
} from "../types";

interface MedicalRecordHeaderProps {
  searchQuery: string;
  onSearchChange: (val: string) => void;
  appointmentDate: string;
  onAppointmentDateChange: (val: string) => void;
  statusFilter: HospitalEncounterStatus | "All";
  onStatusChange: (val: HospitalEncounterStatus | "All") => void;
  isRefreshing: boolean;
  onRefresh: () => void;
  onCreateOpen: () => void;
}

export function MedicalRecordHeader({
  searchQuery,
  onSearchChange,
  appointmentDate,
  onAppointmentDateChange,
  statusFilter,
  onStatusChange,
  isRefreshing,
  onRefresh,
  onCreateOpen,
}: MedicalRecordHeaderProps) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-xs">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.25em] text-emerald-700">
            Hồ sơ khám
          </p>
          <h1 className="mt-2 text-2xl font-bold text-slate-900">
            Hồ sơ khám bệnh
          </h1>
          <p className="mt-1 max-w-3xl text-sm leading-6 text-slate-600">
            Xem và quản lý hồ sơ khám bệnh, chẩn đoán và ghi chú lâm sàng.
          </p>
        </div>

        <div className="flex flex-col gap-3 md:flex-row md:items-center">
          <input
            type="text"
            value={searchQuery}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Tìm theo mã hồ sơ, mã lịch, bệnh nhân..."
            aria-label="Tìm kiếm hồ sơ"
            className="min-w-[260px] rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100"
          />

          <input
            type="date"
            value={appointmentDate}
            onChange={(event) => onAppointmentDateChange(event.target.value)}
            aria-label="Lọc theo ngày khám"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100"
          />

          <select
            value={statusFilter}
            onChange={(event) =>
              onStatusChange(event.target.value as HospitalEncounterStatus | "All")
            }
            aria-label="Lọc trạng thái hồ sơ"
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-emerald-600 focus:ring-2 focus:ring-emerald-100"
          >
            {ENCOUNTER_STATUS_OPTIONS.map((option) => (
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
          <Button onClick={onCreateOpen}>Mở hồ sơ khám</Button>
        </div>
      </div>
    </div>
  );
}
