import api from "./api";
import {
  HospitalAppointmentCancelPayload,
  HospitalAppointmentCheckInPayload,
  HospitalAppointmentReschedulePayload,
  HospitalAppointmentWorklistItem,
  HospitalAppointmentWorklistQuery,
  HospitalAppointmentWorklistStatus,
  PaginatedResult,
} from "./types";

export const hospitalAppointmentWorklistService = {
  getAll: async (
    query: HospitalAppointmentWorklistQuery = {}
  ): Promise<PaginatedResult<HospitalAppointmentWorklistItem>> => {
    const pageSize = Math.min(Math.max(query.pageSize ?? 10, 1), 100);
    const params = new URLSearchParams();
    params.set("pageNumber", String(query.pageNumber ?? 1));
    params.set("pageSize", String(pageSize));

    if (query.status && query.status !== "All") {
      params.set("status", query.status);
    }

    if (query.appointmentDate) {
      params.set("appointmentDate", query.appointmentDate);
    }

    if (query.textSearch?.trim()) {
      params.set("textSearch", query.textSearch.trim());
    }

    const response = await api.get<PaginatedResult<HospitalAppointmentWorklistItem>>(
      `/hospital-appointments?${params.toString()}`
    );

    return response.data;
  },

  checkIn: async (
    appointmentId: string,
    payload: HospitalAppointmentCheckInPayload
  ): Promise<HospitalAppointmentWorklistItem> => {
    const response = await api.post<HospitalAppointmentWorklistItem>(
      `/hospital-appointments/${appointmentId}/check-in`,
      payload
    );

    return response.data;
  },

  updateStatus: async (
    appointmentId: string,
    status: HospitalAppointmentWorklistStatus
  ): Promise<HospitalAppointmentWorklistItem> => {
    const response = await api.post<HospitalAppointmentWorklistItem>(
      `/hospital-appointments/${appointmentId}/status`,
      { status }
    );

    return response.data;
  },

  cancel: async (
    appointmentId: string,
    payload: HospitalAppointmentCancelPayload
  ): Promise<HospitalAppointmentWorklistItem> => {
    const response = await api.post<HospitalAppointmentWorklistItem>(
      `/hospital-appointments/${appointmentId}/cancel`,
      payload
    );

    return response.data;
  },

  reschedule: async (
    appointmentId: string,
    payload: HospitalAppointmentReschedulePayload
  ): Promise<HospitalAppointmentWorklistItem> => {
    const response = await api.post<HospitalAppointmentWorklistItem>(
      `/hospital-appointments/${appointmentId}/reschedule`,
      payload
    );

    return response.data;
  },
};
