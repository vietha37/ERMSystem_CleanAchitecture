import {
  CreateHospitalClinicalOrderPayload,
  HospitalClinicalOrderCategory,
  HospitalClinicalOrderCatalogItem,
  HospitalClinicalOrderDetail,
  HospitalClinicalOrderEligibleEncounter,
  HospitalClinicalOrderStatus,
  HospitalClinicalOrderSummary,
  RecordHospitalLabResultItemPayload,
} from "@/services/types";

export type {
  CreateHospitalClinicalOrderPayload,
  HospitalClinicalOrderCategory,
  HospitalClinicalOrderCatalogItem,
  HospitalClinicalOrderDetail,
  HospitalClinicalOrderEligibleEncounter,
  HospitalClinicalOrderStatus,
  HospitalClinicalOrderSummary,
  RecordHospitalLabResultItemPayload,
};

export const CATEGORY_OPTIONS: Array<{
  value: HospitalClinicalOrderCategory | "All";
  label: string;
}> = [
  { value: "All", label: "Tất cả" },
  { value: "Lab", label: "Xét nghiệm" },
  { value: "Imaging", label: "Chẩn đoán hình ảnh" },
];

export const STATUS_OPTIONS: Array<{
  value: HospitalClinicalOrderStatus | "All";
  label: string;
}> = [
  { value: "All", label: "Tất cả" },
  { value: "Requested", label: "Đang chờ kết quả" },
  { value: "Completed", label: "Đã hoàn thành" },
];

export const EMPTY_LAB_RESULT_ITEM: RecordHospitalLabResultItemPayload = {
  analyteCode: "",
  analyteName: "",
  resultValue: "",
  unit: "",
  referenceRange: "",
  abnormalFlag: "",
};
