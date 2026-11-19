import React from "react";
import { Button } from "@/components/ui/Button";
import { useTranslation } from "@/hooks/useTranslation";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import {
  HospitalAppointmentWorklistItem,
  HospitalAppointmentWorklistStatus,
} from "../types";
import { getStatusStyle } from "../utils/appointmentFormatters";

interface AppointmentTableRowProps {
  appointment: HospitalAppointmentWorklistItem;
  canCheckIn: boolean;
  canManageAppointment: boolean;
  actionId: string | null;
  onOpenCheckIn: (appointment: HospitalAppointmentWorklistItem) => void;
  onOpenReschedule: (appointment: HospitalAppointmentWorklistItem) => void;
  onComplete: (appointment: HospitalAppointmentWorklistItem) => void;
  onOpenCancel: (appointment: HospitalAppointmentWorklistItem) => void;
}

export function AppointmentTableRow({
  appointment,
  canCheckIn,
  canManageAppointment,
  actionId,
  onOpenCheckIn,
  onOpenReschedule,
  onComplete,
  onOpenCancel,
}: AppointmentTableRowProps) {
  const { t } = useTranslation();

  const getStatusLabel = (status: HospitalAppointmentWorklistStatus): string => {
    switch (status) {
      case "Scheduled":
        return t("common.status.scheduled") || "Đã xếp lịch";
      case "CheckedIn":
        return t("common.status.checkedIn") || "Đã check-in";
      case "Completed":
        return t("common.status.completed") || "Đã hoàn thành";
      case "Cancelled":
        return t("common.status.cancelled") || "Đã hủy";
      default:
        return status;
    }
  };

  const isBusy = actionId === appointment.appointmentId;

  return (
    <tr className="transition-colors hover:bg-slate-50/70">
      <td className="px-5 py-3.5">
        <div className="font-semibold text-slate-900 font-mono">
          {formatDateTimeValue(appointment.appointmentStartLocal)}
        </div>
        <div className="mt-0.5 text-[11px] text-slate-500">
          {appointment.appointmentNumber}
        </div>
      </td>
      <td className="px-5 py-3.5">
        <div className="font-semibold text-slate-900">{appointment.patientName}</div>
        <div className="mt-0.5 text-[11px] text-slate-500 font-mono">
          {appointment.patientPhone || "--"}
        </div>
      </td>
      <td className="px-5 py-3.5">
        <div className="font-medium text-slate-900">{appointment.doctorName}</div>
        <div className="mt-0.5 text-[11px] text-slate-500">
          {appointment.clinicName || appointment.specialtyName}
        </div>
      </td>
      <td className="px-5 py-3.5">
        <span
          className={`inline-flex items-center rounded-md px-2 py-0.5 text-[11px] font-semibold ${getStatusStyle(
            appointment.status
          )}`}
        >
          {getStatusLabel(appointment.status)}
        </span>
      </td>
      <td className="px-5 py-3.5">
        <div className="font-medium text-slate-800">
          {appointment.counterLabel ? `Quầy: ${appointment.counterLabel}` : "--"}
        </div>
        <div className="text-[11px] text-slate-500 font-mono">
          {appointment.queueNumber ? `STT: ${appointment.queueNumber}` : ""}
        </div>
      </td>
      <td className="px-5 py-3.5 text-right space-x-1.5">
        {canCheckIn && appointment.status === "Scheduled" && (
          <Button
            variant="secondary"
            size="sm"
            onClick={() => onOpenCheckIn(appointment)}
            disabled={isBusy}
          >
            Check-in
          </Button>
        )}
        {canManageAppointment && appointment.status === "Scheduled" && (
          <Button
            variant="secondary"
            size="sm"
            onClick={() => onOpenReschedule(appointment)}
            disabled={isBusy}
          >
            Đổi lịch
          </Button>
        )}
        {appointment.status !== "Completed" && appointment.status !== "Cancelled" && (
          <Button
            variant="secondary"
            size="sm"
            onClick={() => onComplete(appointment)}
            disabled={isBusy}
          >
            Hoàn thành
          </Button>
        )}
        {canManageAppointment && appointment.status === "Scheduled" && (
          <Button
            variant="danger"
            size="sm"
            onClick={() => onOpenCancel(appointment)}
            disabled={isBusy}
          >
            Hủy
          </Button>
        )}
      </td>
    </tr>
  );
}
