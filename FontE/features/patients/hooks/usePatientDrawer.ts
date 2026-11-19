"use client";

import { useState } from "react";
import { hospitalAppointmentWorklistService } from "@/services/hospitalAppointmentWorklistService";
import { hospitalEncounterService } from "@/services/hospitalEncounterService";
import { DrawerData, PatientViewModel } from "../types";

export function usePatientDrawer() {
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [selectedPatient, setSelectedPatient] = useState<PatientViewModel | null>(null);
  const [drawerData, setDrawerData] = useState<DrawerData>({
    appointments: [],
    encounters: [],
    isLoading: false,
  });

  const openDrawer = async (patient: PatientViewModel) => {
    setSelectedPatient(patient);
    setIsDrawerOpen(true);
    setDrawerData({
      appointments: [],
      encounters: [],
      isLoading: true,
    });

    try {
      const [appointmentData, encounterData] = await Promise.all([
        hospitalAppointmentWorklistService.getAll({
          pageNumber: 1,
          pageSize: 100,
          textSearch: patient.medicalRecordNumber || patient.fullName,
        }),
        hospitalEncounterService.getAll({
          pageNumber: 1,
          pageSize: 100,
          textSearch: patient.medicalRecordNumber || patient.fullName,
        }),
      ]);

      const patientAppointments = appointmentData.items
        .filter((item) => item.patientId === patient.id)
        .sort(
          (left, right) =>
            new Date(right.appointmentStartLocal).getTime() -
            new Date(left.appointmentStartLocal).getTime()
        );

      const patientEncounters = encounterData.items
        .filter((item) => item.patientId === patient.id)
        .sort(
          (left, right) =>
            new Date(right.updatedAtLocal).getTime() -
            new Date(left.updatedAtLocal).getTime()
        );

      setDrawerData({
        appointments: patientAppointments,
        encounters: patientEncounters,
        isLoading: false,
      });
    } catch {
      setDrawerData({
        appointments: [],
        encounters: [],
        isLoading: false,
      });
    }
  };

  const closeDrawer = () => setIsDrawerOpen(false);

  const latestDiagnosis =
    drawerData.encounters.find((encounter) => encounter.primaryDiagnosisName)
      ?.primaryDiagnosisName ?? "Chưa có";

  return {
    isDrawerOpen,
    selectedPatient,
    drawerData,
    latestDiagnosis,
    openDrawer,
    closeDrawer,
  };
}
