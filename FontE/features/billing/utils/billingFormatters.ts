import { formatDateTimeValue } from "@/lib/dateFormatting";
import { HospitalInvoiceStatus } from "../types";

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(value);
}

export function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

export function getStatusClass(status: HospitalInvoiceStatus): string {
  switch (status) {
    case "Issued":
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
    case "PartiallyPaid":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "Paid":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Cancelled":
      return "border border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

export function getStatusLabel(status: HospitalInvoiceStatus): string {
  switch (status) {
    case "Issued":
      return "Đã phát hành";
    case "PartiallyPaid":
      return "Thanh toán một phần";
    case "Paid":
      return "Đã thanh toán";
    case "Cancelled":
      return "Đã hủy";
    default:
      return status;
  }
}
