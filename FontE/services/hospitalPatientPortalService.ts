import api from "./api";
import {
  ConfirmHospitalPaymentCallbackPayload,
  HospitalInvoiceDetail,
  HospitalPaymentIntent,
  HospitalPatientPortalQrPaymentIntentPayload,
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

  createQrPaymentIntent: async (
    invoiceId: string,
    payload: HospitalPatientPortalQrPaymentIntentPayload
  ): Promise<HospitalPaymentIntent> => {
    const response = await api.post<HospitalPaymentIntent>(
      `/hospital-patient-portal/me/invoices/${invoiceId}/qr-payment-intents`,
      payload
    );

    return response.data;
  },

  simulateQrPaymentCallback: async (
    payload: ConfirmHospitalPaymentCallbackPayload
  ): Promise<HospitalInvoiceDetail> => {
    const response = await api.post<HospitalInvoiceDetail>(
      "/hospital-patient-portal/me/qr-payment-callbacks/simulate",
      payload
    );

    return response.data;
  },
};
