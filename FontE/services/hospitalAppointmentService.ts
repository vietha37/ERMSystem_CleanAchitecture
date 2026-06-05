import api from "./api";

export type PublicHospitalAppointmentBookingPayload = {
  fullName: string;
  phone: string;
  email?: string;
  dateOfBirth: string;
  gender: string;
  doctorProfileId: string;
  specialtyId?: string;
  serviceCode?: string;
  preferredDate: string;
  preferredTime: string;
  chiefComplaint?: string;
  notes?: string;
};

export type PublicHospitalAppointmentBookingResult = {
  appointmentId: string;
  appointmentNumber: string;
  patientId: string;
  doctorProfileId: string;
  doctorName: string;
  specialtyName: string;
  clinicName: string;
  appointmentStartLocal: string;
  appointmentEndLocal: string;
  status: string;
  isExistingPatient: boolean;
  notificationQueued: boolean;
};

export const hospitalAppointmentService = {
  async bookPublicAppointment(
    payload: PublicHospitalAppointmentBookingPayload
  ): Promise<PublicHospitalAppointmentBookingResult> {
    const response = await api.post<PublicHospitalAppointmentBookingResult>(
      "/hospital-appointments/public-booking",
      payload
    );

    return response.data;
  },
};
