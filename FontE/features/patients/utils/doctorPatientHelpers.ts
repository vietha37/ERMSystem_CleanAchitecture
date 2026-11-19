import {
  HospitalAppointmentWorklistItem,
  HospitalEncounterSummary,
} from "@/services/types";
import { PatientViewModel } from "../types";

export function filterDoctorPatients(
  items: PatientViewModel[],
  debouncedSearch: string
): PatientViewModel[] {
  const query = debouncedSearch.trim().toLowerCase();
  if (!query) return items;

  return items.filter((patient) =>
    [
      patient.fullName,
      patient.phone,
      patient.address,
      patient.medicalRecordNumber,
      patient.latestDiagnosis,
      patient.doctorName,
    ]
      .filter(Boolean)
      .some((value) => value!.toLowerCase().includes(query))
  );
}

export function mapDoctorPatients(
  appointments: HospitalAppointmentWorklistItem[],
  encounters: HospitalEncounterSummary[]
): PatientViewModel[] {
  const patientMap = new Map<string, PatientViewModel>();
  const latestEncounterByPatient = new Map<string, HospitalEncounterSummary>();

  encounters.forEach((encounter) => {
    const existing = latestEncounterByPatient.get(encounter.patientId);
    if (
      !existing ||
      new Date(encounter.updatedAtLocal).getTime() >
        new Date(existing.updatedAtLocal).getTime()
    ) {
      latestEncounterByPatient.set(encounter.patientId, encounter);
    }
  });

  appointments.forEach((appointment) => {
    const linkedEncounter = latestEncounterByPatient.get(appointment.patientId);
    const existing = patientMap.get(appointment.patientId);
    const candidateDate =
      linkedEncounter?.updatedAtLocal ?? appointment.appointmentStartLocal;

    if (
      !existing ||
      new Date(candidateDate).getTime() >
        new Date(existing.latestVisitDate ?? "1970-01-01T00:00:00Z").getTime()
    ) {
      patientMap.set(appointment.patientId, {
        id: appointment.patientId,
        fullName: appointment.patientName,
        dateOfBirth: "",
        gender: "",
        phone: appointment.patientPhone ?? "",
        address: "",
        createdAt: appointment.appointmentStartLocal,
        medicalRecordNumber: appointment.medicalRecordNumber,
        latestDiagnosis: linkedEncounter?.primaryDiagnosisName ?? "",
        latestVisitDate: candidateDate,
        doctorName: appointment.doctorName,
        status:
          appointment.status === "Cancelled" ? "Tạm dừng" : "Đang theo dõi",
      });
    }
  });

  return Array.from(patientMap.values()).sort(
    (a, b) =>
      new Date(b.latestVisitDate ?? b.createdAt).getTime() -
      new Date(a.latestVisitDate ?? a.createdAt).getTime()
  );
}
