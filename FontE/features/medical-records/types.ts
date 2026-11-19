import {
  CreateHospitalEncounterPayload,
  HospitalEncounterAttachment,
  HospitalEncounterDetail,
  HospitalEncounterEligibleAppointment,
  HospitalEncounterStatus,
  HospitalEncounterSummary,
  UpdateHospitalEncounterPayload,
} from "@/services/types";

export type {
  CreateHospitalEncounterPayload,
  HospitalEncounterAttachment,
  HospitalEncounterDetail,
  HospitalEncounterEligibleAppointment,
  HospitalEncounterStatus,
  HospitalEncounterSummary,
  UpdateHospitalEncounterPayload,
};

export const ENCOUNTER_STATUS_OPTIONS: Array<{
  value: HospitalEncounterStatus | "All";
  label: string;
}> = [
  { value: "All", label: "Tất cả" },
  { value: "InProgress", label: "Đang khám" },
  { value: "Finalized", label: "Đã chốt hồ sơ" },
  { value: "Approved", label: "Đã duyệt hồ sơ" },
];

export type EncounterFormState = {
  appointmentId: string;
  diagnosisName: string;
  diagnosisCode: string;
  diagnosisType: string;
  encounterStatus: HospitalEncounterStatus;
  summary: string;
  subjective: string;
  objective: string;
  assessment: string;
  carePlan: string;
  heightCm: string;
  weightKg: string;
  temperatureC: string;
  pulseRate: string;
  respiratoryRate: string;
  systolicBp: string;
  diastolicBp: string;
  oxygenSaturation: string;
};

export const EMPTY_FORM: EncounterFormState = {
  appointmentId: "",
  diagnosisName: "",
  diagnosisCode: "",
  diagnosisType: "Working",
  encounterStatus: "InProgress",
  summary: "",
  subjective: "",
  objective: "",
  assessment: "",
  carePlan: "",
  heightCm: "",
  weightKg: "",
  temperatureC: "",
  pulseRate: "",
  respiratoryRate: "",
  systolicBp: "",
  diastolicBp: "",
  oxygenSaturation: "",
};
