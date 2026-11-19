import React from "react";
import { Button } from "@/components/ui/Button";
import { useTranslation } from "@/hooks/useTranslation";
import { HospitalAppointmentWorklistStatus } from "../types";

interface AppointmentHeaderProps {
  searchQuery: string;
  onSearchChange: (value: string) => void;
  appointmentDate: string;
  onDateChange: (value: string) => void;
  statusFilter: HospitalAppointmentWorklistStatus | "All";
  onStatusFilterChange: (value: HospitalAppointmentWorklistStatus | "All") => void;
  isRefreshing: boolean;
  onRefresh: () => void;
}

export function AppointmentHeader({
  searchQuery,
  onSearchChange,
  appointmentDate,
  onDateChange,
  statusFilter,
  onStatusFilterChange,
  isRefreshing,
  onRefresh,
}: AppointmentHeaderProps) {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border-b border-slate-200 pb-5">
      <div>
        <h1 className="text-xl font-bold tracking-tight text-slate-900">
          {t("appointments.title")}
        </h1>
        <p className="mt-1 text-xs text-slate-500">
          {t("appointments.subtitle")}
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Tìm mã lịch, bệnh nhân, bác sĩ..."
          className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
        />

        <input
          type="date"
          value={appointmentDate}
          onChange={(e) => onDateChange(e.target.value)}
          className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
        />

        <select
          value={statusFilter}
          onChange={(e) =>
            onStatusFilterChange(e.target.value as HospitalAppointmentWorklistStatus | "All")
          }
          className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-700 shadow-2xs outline-none focus:border-slate-500"
        >
          <option value="All">{t("common.status.all")}</option>
          <option value="Scheduled">Đã xếp lịch</option>
          <option value="CheckedIn">Đã check-in</option>
          <option value="Completed">Đã hoàn thành</option>
          <option value="Cancelled">Đã hủy</option>
        </select>

        <Button
          variant="secondary"
          size="sm"
          onClick={onRefresh}
          disabled={isRefreshing}
        >
          {isRefreshing ? t("common.actions.processing") : t("common.actions.refresh")}
        </Button>
      </div>
    </div>
  );
}
