import api from "./api";
import {
  CreateStaffUserPayload,
  HospitalInternalUserSyncResult,
  PaginatedResult,
  StaffUser,
  UpdateStaffUserPayload,
} from "./types";

export const staffUserService = {
  getAll: async (
    pageNumber = 1,
    pageSize = 10,
    role: "Doctor" | "Cashier" | "Patient" | "" = "",
    textSearch = ""
  ): Promise<PaginatedResult<StaffUser>> => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());

    if (role) {
      params.set("role", role);
    }

    if (textSearch.trim()) {
      const keyword = textSearch.trim();
      params.set("textSearch", keyword);
      params.set("textSeach", keyword);
    }

    const response = await api.get<PaginatedResult<StaffUser>>(
      `/admin/users?${params.toString()}`
    );
    return response.data;
  },

  create: async (payload: CreateStaffUserPayload): Promise<StaffUser> => {
    const response = await api.post<StaffUser>("/admin/users", payload);
    return response.data;
  },

  update: async (id: string, payload: UpdateStaffUserPayload): Promise<void> => {
    await api.put(`/admin/users/${id}`, payload);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/admin/users/${id}`);
  },

  syncHospitalIdentity: async (): Promise<HospitalInternalUserSyncResult> => {
    const response = await api.post<HospitalInternalUserSyncResult>(
      "/admin/users/sync-hospital-identity"
    );
    return response.data;
  },
};
