import {
  HospitalAppointmentWorklistItem,
  HospitalEncounterSummary,
  Patient,
} from "@/services/types";

export type PatientViewModel = Patient & {
  status?: string;
  medicalRecordNumber?: string;
  latestDiagnosis?: string;
  latestVisitDate?: string;
  doctorName?: string;
};

export type DrawerData = {
  appointments: HospitalAppointmentWorklistItem[];
  encounters: HospitalEncounterSummary[];
  isLoading: boolean;
};

export type PatientFormState = {
  dateOfBirth: string;
  gender: string;
  phone: string;
  address: string;
};

export type PatientMetrics = {
  activePatients: number;
  withRecentVisit: number;
};

export const DEFAULT_FORM_STATE: PatientFormState = {
  dateOfBirth: "",
  gender: "Male",
  phone: "",
  address: "",
};
