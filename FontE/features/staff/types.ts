import { StaffUser, UpdateStaffUserPayload, CreateStaffUserPayload } from "@/services/types";

export type { StaffUser, UpdateStaffUserPayload, CreateStaffUserPayload };

export type StaffRole = "Doctor" | "Cashier";
export type StaffRoleFilter = "" | "Doctor" | "Cashier" | "Patient";

export type StaffFormState = {
  username: string;
  name: string;
  password: string;
  role: StaffRole;
};

export const initialStaffForm: StaffFormState = {
  username: "",
  name: "",
  password: "",
  role: "Doctor",
};
