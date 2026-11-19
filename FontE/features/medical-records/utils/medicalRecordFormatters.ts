import { formatDateTimeValue } from "@/lib/dateFormatting";
import {
  CreateHospitalEncounterPayload,
  EncounterFormState,
  HospitalEncounterDetail,
  HospitalEncounterStatus,
  UpdateHospitalEncounterPayload,
} from "../types";

export function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

export function getEncounterStatusLabel(status: HospitalEncounterStatus): string {
  switch (status) {
    case "InProgress":
      return "Đang khám";
    case "Finalized":
      return "Đã chốt hồ sơ";
    case "Approved":
      return "Đã duyệt hồ sơ";
    default:
      return status;
  }
}

export function getEncounterStatusClass(status: HospitalEncounterStatus): string {
  switch (status) {
    case "InProgress":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "Finalized":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Approved":
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

export function parseOptionalNumber(value: string): number | null {
  if (!value.trim()) {
    return null;
  }
  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

export function buildPayload(form: EncounterFormState): CreateHospitalEncounterPayload {
  return {
    appointmentId: form.appointmentId,
    diagnosisName: form.diagnosisName.trim(),
    diagnosisCode: form.diagnosisCode.trim() || undefined,
    diagnosisType: form.diagnosisType.trim() || "Working",
    encounterStatus: form.encounterStatus,
    summary: form.summary.trim() || undefined,
    subjective: form.subjective.trim() || undefined,
    objective: form.objective.trim() || undefined,
    assessment: form.assessment.trim() || undefined,
    carePlan: form.carePlan.trim() || undefined,
    heightCm: parseOptionalNumber(form.heightCm),
    weightKg: parseOptionalNumber(form.weightKg),
    temperatureC: parseOptionalNumber(form.temperatureC),
    pulseRate: parseOptionalNumber(form.pulseRate),
    respiratoryRate: parseOptionalNumber(form.respiratoryRate),
    systolicBp: parseOptionalNumber(form.systolicBp),
    diastolicBp: parseOptionalNumber(form.diastolicBp),
    oxygenSaturation: parseOptionalNumber(form.oxygenSaturation),
  };
}

export function buildUpdatePayload(form: EncounterFormState): UpdateHospitalEncounterPayload {
  const payload = buildPayload(form);
  return {
    diagnosisName: payload.diagnosisName,
    diagnosisCode: payload.diagnosisCode,
    diagnosisType: payload.diagnosisType,
    encounterStatus: payload.encounterStatus,
    summary: payload.summary,
    subjective: payload.subjective,
    objective: payload.objective,
    assessment: payload.assessment,
    carePlan: payload.carePlan,
    heightCm: payload.heightCm,
    weightKg: payload.weightKg,
    temperatureC: payload.temperatureC,
    pulseRate: payload.pulseRate,
    respiratoryRate: payload.respiratoryRate,
    systolicBp: payload.systolicBp,
    diastolicBp: payload.diastolicBp,
    oxygenSaturation: payload.oxygenSaturation,
  };
}

export function mapDetailToForm(detail: HospitalEncounterDetail): EncounterFormState {
  return {
    appointmentId: detail.appointmentId ?? "",
    diagnosisName: detail.primaryDiagnosisName ?? "",
    diagnosisCode: detail.diagnosisCode ?? "",
    diagnosisType: detail.diagnosisType ?? "Working",
    encounterStatus: detail.encounterStatus,
    summary: detail.summary ?? "",
    subjective: detail.subjective ?? "",
    objective: detail.objective ?? "",
    assessment: detail.assessment ?? "",
    carePlan: detail.carePlan ?? "",
    heightCm: detail.heightCm?.toString() ?? "",
    weightKg: detail.weightKg?.toString() ?? "",
    temperatureC: detail.temperatureC?.toString() ?? "",
    pulseRate: detail.pulseRate?.toString() ?? "",
    respiratoryRate: detail.respiratoryRate?.toString() ?? "",
    systolicBp: detail.systolicBp?.toString() ?? "",
    diastolicBp: detail.diastolicBp?.toString() ?? "",
    oxygenSaturation: detail.oxygenSaturation?.toString() ?? "",
  };
}
