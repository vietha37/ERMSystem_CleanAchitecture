import {
  HospitalCrmEngagementSummary,
  NotificationDelivery,
  NotificationDeliverySummary,
  NotificationDeliveryStatus,
} from "@/services/types";

export type DeliveryStatusFilter = NotificationDeliveryStatus | "All";

export const STATUS_OPTIONS: Array<{ value: DeliveryStatusFilter; label: string }> = [
  { value: "All", label: "Tất cả" },
  { value: "Queued", label: "Đang chờ gửi" },
  { value: "Delivered", label: "Đã gửi" },
  { value: "Failed", label: "Thất bại" },
  { value: "Skipped", label: "Bỏ qua" },
];

export const STATUS_STYLES: Record<NotificationDeliveryStatus, string> = {
  Queued: "border border-amber-200 bg-amber-50 text-amber-700",
  Delivered: "border border-emerald-200 bg-emerald-50 text-emerald-700",
  Failed: "border border-rose-200 bg-rose-50 text-rose-700",
  Skipped: "border border-slate-200 bg-slate-100 text-slate-700",
};

export function getStatusLabel(status: NotificationDeliveryStatus): string {
  switch (status) {
    case "Queued":
      return "Đang chờ gửi";
    case "Delivered":
      return "Đã gửi";
    case "Failed":
      return "Thất bại";
    case "Skipped":
      return "Bỏ qua";
    default:
      return status;
  }
}

export function canRetry(status: NotificationDeliveryStatus): boolean {
  return status === "Failed" || status === "Skipped";
}

export type {
  HospitalCrmEngagementSummary,
  NotificationDelivery,
  NotificationDeliverySummary,
  NotificationDeliveryStatus,
};
