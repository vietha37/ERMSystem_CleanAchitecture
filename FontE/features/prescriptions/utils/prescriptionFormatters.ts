import { formatDateTimeValue } from "@/lib/dateFormatting";
import {
  CreateHospitalPrescriptionItemPayload,
  HospitalPrescriptionStatus,
  PrescriptionItemForm,
} from "../types";

export function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

export function getStatusLabel(status: HospitalPrescriptionStatus): string {
  switch (status) {
    case "Issued":
      return "Đã phát hành";
    case "Dispensed":
      return "Đã cấp thuốc";
    case "Cancelled":
      return "Đã hủy";
    default:
      return status;
  }
}

export function getStatusClass(status: HospitalPrescriptionStatus): string {
  switch (status) {
    case "Issued":
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
    case "Dispensed":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Cancelled":
      return "border border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

export function buildCreateItems(
  items: PrescriptionItemForm[]
): CreateHospitalPrescriptionItemPayload[] {
  return items
    .filter((item) => item.medicineId && item.doseInstruction.trim() && item.quantity.trim())
    .map((item) => ({
      medicineId: item.medicineId,
      doseInstruction: item.doseInstruction.trim(),
      route: item.route.trim() || undefined,
      frequency: item.frequency.trim() || undefined,
      durationDays: item.durationDays.trim() ? Number(item.durationDays) : undefined,
      quantity: Number(item.quantity),
    }));
}
