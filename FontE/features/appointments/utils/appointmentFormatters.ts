import { HospitalAppointmentWorklistStatus } from "@/services/types";

export function getStatusStyle(status: HospitalAppointmentWorklistStatus): string {
  switch (status) {
    case "Scheduled":
      return "border border-sky-200 bg-sky-50 text-sky-700";
    case "CheckedIn":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "Completed":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Cancelled":
      return "border border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

export function formatDateInput(value?: string | null): string {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().slice(0, 10);
}

export function formatTimeInput(value?: string | null): string {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().slice(11, 16);
}
