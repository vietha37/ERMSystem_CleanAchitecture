import React from "react";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { useTranslation } from "@/hooks/useTranslation";
import { HospitalAppointmentWorklistItem } from "../types";
import { AppointmentTableRow } from "./AppointmentTableRow";

interface AppointmentTableProps {
  appointments: HospitalAppointmentWorklistItem[];
  isLoading: boolean;
  listError: string | null;
  totalCount: number;
  pageSize: number;
  onPageSizeChange: (size: number) => void;
  canCheckIn: boolean;
  canManageAppointment: boolean;
  actionId: string | null;
  onRefresh: () => void;
  onOpenCheckIn: (appointment: HospitalAppointmentWorklistItem) => void;
  onOpenReschedule: (appointment: HospitalAppointmentWorklistItem) => void;
  onComplete: (appointment: HospitalAppointmentWorklistItem) => void;
  onOpenCancel: (appointment: HospitalAppointmentWorklistItem) => void;
  children?: React.ReactNode;
}

export function AppointmentTable({
  appointments,
  isLoading,
  listError,
  totalCount,
  pageSize,
  onPageSizeChange,
  canCheckIn,
  canManageAppointment,
  actionId,
  onRefresh,
  onOpenCheckIn,
  onOpenReschedule,
  onComplete,
  onOpenCancel,
  children,
}: AppointmentTableProps) {
  const { t } = useTranslation();

  return (
    <Card padding="none">
      <div className="p-4 sm:p-5 border-b border-slate-100 flex items-center justify-between">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">
            {t("appointments.title")}
          </h2>
          <p className="text-xs text-slate-500 mt-0.5">
            {t("common.labels.totalRecords", { count: totalCount })}
          </p>
        </div>

        <select
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
          className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-700 shadow-2xs outline-none focus:border-slate-500"
        >
          <option value={10}>10 / trang</option>
          <option value={20}>20 / trang</option>
          <option value={50}>50 / trang</option>
        </select>
      </div>

      {isLoading ? (
        <LoadingState title={t("common.actions.loading")} />
      ) : listError ? (
        <ErrorState
          title={t("common.messages.error")}
          description={listError}
          onAction={onRefresh}
        />
      ) : appointments.length === 0 ? (
        <EmptyState
          title={t("common.labels.empty")}
          description={t("common.labels.noResults")}
        />
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-left text-xs">
            <thead className="border-b border-slate-200 bg-slate-50 text-slate-600 font-semibold">
              <tr>
                <th className="px-5 py-3">{t("appointments.fields.date")}</th>
                <th className="px-5 py-3">{t("appointments.fields.patient")}</th>
                <th className="px-5 py-3">{t("appointments.fields.doctor")}</th>
                <th className="px-5 py-3">{t("appointments.fields.status")}</th>
                <th className="px-5 py-3">Quầy / STT</th>
                <th className="px-5 py-3 text-right">{t("common.labels.actions")}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-800">
              {appointments.map((appointment) => (
                <AppointmentTableRow
                  key={appointment.appointmentId}
                  appointment={appointment}
                  canCheckIn={canCheckIn}
                  canManageAppointment={canManageAppointment}
                  actionId={actionId}
                  onOpenCheckIn={onOpenCheckIn}
                  onOpenReschedule={onOpenReschedule}
                  onComplete={onComplete}
                  onOpenCancel={onOpenCancel}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {children}
    </Card>
  );
}
