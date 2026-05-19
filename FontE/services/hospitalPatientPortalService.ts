import api from "./api";
import {
  HospitalPatientPortalOverview,
  HospitalPatientVisitHistoryResult,
} from "./types";

export const hospitalPatientPortalService = {
  getMyOverview: async (): Promise<HospitalPatientPortalOverview> => {
    const response = await api.get<HospitalPatientPortalOverview>(
      "/hospital-patient-portal/me"
    );

    return response.data;
  },

  getMyVisitHistory: async (
    pageNumber = 1,
    pageSize = 5
  ): Promise<HospitalPatientVisitHistoryResult> => {
    const response = await api.get<HospitalPatientVisitHistoryResult>(
      `/hospital-patient-portal/me/visit-history?pageNumber=${pageNumber}&pageSize=${pageSize}`
    );

    return response.data;
  },
};
