import {
  CreateHospitalPrescriptionItemPayload,
  HospitalMedicineCatalog,
  HospitalPrescriptionDetail,
  HospitalPrescriptionEligibleEncounter,
  HospitalPrescriptionStatus,
  HospitalPrescriptionSummary,
} from "@/services/types";

export type {
  CreateHospitalPrescriptionItemPayload,
  HospitalMedicineCatalog,
  HospitalPrescriptionDetail,
  HospitalPrescriptionEligibleEncounter,
  HospitalPrescriptionStatus,
  HospitalPrescriptionSummary,
};

export const STATUS_OPTIONS: Array<{
  value: HospitalPrescriptionStatus | "All";
  label: string;
}> = [
  { value: "All", label: "Tất cả" },
  { value: "Issued", label: "Đã phát hành" },
  { value: "Dispensed", label: "Đã cấp thuốc" },
  { value: "Cancelled", label: "Đã hủy" },
];

export type PrescriptionItemForm = {
  medicineId: string;
  doseInstruction: string;
  route: string;
  frequency: string;
  durationDays: string;
  quantity: string;
};

export const EMPTY_ITEM: PrescriptionItemForm = {
  medicineId: "",
  doseInstruction: "",
  route: "",
  frequency: "",
  durationDays: "",
  quantity: "1",
};
