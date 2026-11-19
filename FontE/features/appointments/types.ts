import {
  HospitalAppointmentWorklistItem,
  HospitalAppointmentWorklistStatus,
} from "@/services/types";

export type {
  HospitalAppointmentWorklistItem,
  HospitalAppointmentWorklistStatus,
};

export type AppointmentMetrics = Record<HospitalAppointmentWorklistStatus, number>;
