import {
  HospitalBillingEligibleEncounter,
  HospitalBillingEncounterPreview,
  HospitalInvoiceDetail,
  HospitalInvoiceStatus,
  HospitalInvoiceSummary,
  HospitalPaymentIntent,
  HospitalPaymentReconciliationSummary,
} from "@/services/types";

export type {
  HospitalBillingEligibleEncounter,
  HospitalBillingEncounterPreview,
  HospitalInvoiceDetail,
  HospitalInvoiceStatus,
  HospitalInvoiceSummary,
  HospitalPaymentIntent,
  HospitalPaymentReconciliationSummary,
};

export const STATUS_OPTIONS: Array<{ value: HospitalInvoiceStatus | "All"; label: string }> = [
  { value: "All", label: "Tất cả" },
  { value: "Issued", label: "Đã phát hành" },
  { value: "PartiallyPaid", label: "Thanh toán một phần" },
  { value: "Paid", label: "Đã thanh toán" },
  { value: "Cancelled", label: "Đã hủy" },
];
