import { formatDateTimeValue } from "@/lib/dateFormatting";
import {
  CreateHospitalClinicalOrderPayload,
  HospitalClinicalOrderCategory,
  HospitalClinicalOrderStatus,
  RecordHospitalLabResultItemPayload,
} from "../types";

export function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

export function getCategoryLabel(category: HospitalClinicalOrderCategory): string {
  return category === "Lab" ? "Xét nghiệm" : "Chẩn đoán hình ảnh";
}

export function getCategoryClass(category: HospitalClinicalOrderCategory): string {
  return category === "Lab"
    ? "border border-cyan-200 bg-cyan-50 text-cyan-700"
    : "border border-violet-200 bg-violet-50 text-violet-700";
}

export function getStatusLabel(status: HospitalClinicalOrderStatus): string {
  return status === "Requested" ? "Đang chờ kết quả" : "Đã hoàn thành";
}

export function getStatusClass(status: HospitalClinicalOrderStatus): string {
  return status === "Requested"
    ? "border border-amber-200 bg-amber-50 text-amber-700"
    : "border border-emerald-200 bg-emerald-50 text-emerald-700";
}

export function buildCreatePayload(
  encounterId: string,
  category: HospitalClinicalOrderCategory,
  serviceId: string,
  priorityCode: string
): CreateHospitalClinicalOrderPayload {
  return {
    encounterId,
    category,
    serviceId,
    priorityCode: category === "Lab" && priorityCode.trim() ? priorityCode.trim() : undefined,
  };
}

export function buildLabResultItems(items: RecordHospitalLabResultItemPayload[]) {
  return items
    .filter((item) => item.analyteName.trim())
    .map((item) => ({
      analyteCode: item.analyteCode?.trim() || undefined,
      analyteName: item.analyteName.trim(),
      resultValue: item.resultValue?.trim() || undefined,
      unit: item.unit?.trim() || undefined,
      referenceRange: item.referenceRange?.trim() || undefined,
      abnormalFlag: item.abnormalFlag?.trim() || undefined,
    }));
}
