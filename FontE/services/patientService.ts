import api from "./api";
import {
  AuthResponse,
  CreatePatientPayload,
  PaginatedResult,
  Patient,
  PatientRegisterPayload,
  UpdatePatientPayload,
} from "./types";

export const patientService = {
  getAll: async (
    pageNumber = 1,
    pageSize = 10,
    search = ""
  ): Promise<PaginatedResult<Patient>> => {
    let url = `/patients?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (search) {
      const encoded = encodeURIComponent(search.trim());
      url += `&textSearch=${encoded}`;
    }

    const response = await api.get<PaginatedResult<Patient>>(url);
    return response.data;
  },

  getById: async (id: string): Promise<Patient> => {
    const response = await api.get<Patient>(`/patients/${id}`);
    return response.data;
  },

  getMe: async (): Promise<Patient> => {
    const response = await api.get<Patient>("/patients/me");
    return response.data;
  },

  create: async (data: CreatePatientPayload): Promise<Patient> => {
    const response = await api.post<Patient>("/patients", data);
    return response.data;
  },

  register: async (data: PatientRegisterPayload): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>("/auth/patient-register", data);
    return response.data;
  },

  update: async (id: string, data: UpdatePatientPayload): Promise<void> => {
    await api.put(`/patients/${id}`, data);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/patients/${id}`);
  },
};
