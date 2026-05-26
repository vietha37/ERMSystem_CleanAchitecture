import api from "./api";
import {
  ApproveHospitalEncounterPayload,
  CreateHospitalEncounterPayload,
  HospitalEncounterAttachmentDownloadTicket,
  HospitalEncounterDetail,
  HospitalEncounterEligibleAppointment,
  HospitalEncounterSummary,
  HospitalEncounterWorklistQuery,
  PaginatedResult,
  SignHospitalEncounterPayload,
  UpdateHospitalEncounterPayload,
} from "./types";

export const hospitalEncounterService = {
  getAll: async (
    query: HospitalEncounterWorklistQuery = {}
  ): Promise<PaginatedResult<HospitalEncounterSummary>> => {
    const params = new URLSearchParams();
    params.set("pageNumber", String(query.pageNumber ?? 1));
    params.set("pageSize", String(query.pageSize ?? 10));

    if (query.encounterStatus && query.encounterStatus !== "All") {
      params.set("encounterStatus", query.encounterStatus);
    }

    if (query.appointmentDate) {
      params.set("appointmentDate", query.appointmentDate);
    }

    if (query.textSearch?.trim()) {
      params.set("textSearch", query.textSearch.trim());
    }

    const response = await api.get<PaginatedResult<HospitalEncounterSummary>>(
      `/hospital-encounters?${params.toString()}`
    );

    return response.data;
  },

  getById: async (encounterId: string): Promise<HospitalEncounterDetail> => {
    const response = await api.get<HospitalEncounterDetail>(
      `/hospital-encounters/${encounterId}`
    );

    return response.data;
  },

  getEligibleAppointments: async (): Promise<HospitalEncounterEligibleAppointment[]> => {
    const response = await api.get<HospitalEncounterEligibleAppointment[]>(
      "/hospital-encounters/eligible-appointments"
    );

    return response.data;
  },

  create: async (
    payload: CreateHospitalEncounterPayload
  ): Promise<HospitalEncounterDetail> => {
    const response = await api.post<HospitalEncounterDetail>(
      "/hospital-encounters",
      payload
    );

    return response.data;
  },

  update: async (
    encounterId: string,
    payload: UpdateHospitalEncounterPayload
  ): Promise<HospitalEncounterDetail> => {
    const response = await api.put<HospitalEncounterDetail>(
      `/hospital-encounters/${encounterId}`,
      payload
    );

    return response.data;
  },

  approve: async (
    encounterId: string,
    payload: ApproveHospitalEncounterPayload = {}
  ): Promise<HospitalEncounterDetail> => {
    const response = await api.post<HospitalEncounterDetail>(
      `/hospital-encounters/${encounterId}/approve`,
      payload
    );

    return response.data;
  },

  sign: async (
    encounterId: string,
    payload: SignHospitalEncounterPayload = {}
  ): Promise<HospitalEncounterDetail> => {
    const response = await api.post<HospitalEncounterDetail>(
      `/hospital-encounters/${encounterId}/sign`,
      payload
    );

    return response.data;
  },

  uploadAttachment: async (
    encounterId: string,
    file: File,
    documentType = "EncounterAttachment"
  ): Promise<HospitalEncounterDetail> => {
    const formData = new FormData();
    formData.append("documentType", documentType);
    formData.append("file", file);

    const response = await api.post<HospitalEncounterDetail>(
      `/hospital-encounters/${encounterId}/attachments/upload`,
      formData,
      {
        headers: {
          "Content-Type": "multipart/form-data",
        },
      }
    );

    return response.data;
  },

  downloadAttachment: async (
    encounterId: string,
    attachmentId: string
  ): Promise<Blob> => {
    const response = await api.get(
      `/hospital-encounters/${encounterId}/attachments/${attachmentId}/content`,
      {
        responseType: "blob",
      }
    );

    return response.data as Blob;
  },

  createAttachmentDownloadTicket: async (
    encounterId: string,
    attachmentId: string
  ): Promise<HospitalEncounterAttachmentDownloadTicket> => {
    const response = await api.get<HospitalEncounterAttachmentDownloadTicket>(
      `/hospital-encounters/${encounterId}/attachments/${attachmentId}/download-ticket`
    );

    return response.data;
  },
};
