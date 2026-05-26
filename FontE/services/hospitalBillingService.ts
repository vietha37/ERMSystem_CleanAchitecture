import api from "./api";
import {
  ConfirmHospitalPaymentCallbackPayload,
  CreateHospitalPaymentIntentPayload,
  CreateHospitalInvoicePayload,
  HospitalBillingEligibleEncounter,
  HospitalInvoiceDetail,
  HospitalPaymentIntent,
  HospitalPaymentReconciliationSummary,
  HospitalInvoiceSummary,
  HospitalInvoiceWorklistQuery,
  PaginatedResult,
  ReceiveHospitalPaymentPayload,
} from "./types";

export const hospitalBillingService = {
  getAll: async (
    query: HospitalInvoiceWorklistQuery = {}
  ): Promise<PaginatedResult<HospitalInvoiceSummary>> => {
    const params = new URLSearchParams();
    params.set("pageNumber", String(query.pageNumber ?? 1));
    params.set("pageSize", String(query.pageSize ?? 10));

    if (query.invoiceStatus && query.invoiceStatus !== "All") {
      params.set("invoiceStatus", query.invoiceStatus);
    }

    if (query.textSearch?.trim()) {
      params.set("textSearch", query.textSearch.trim());
    }

    const response = await api.get<PaginatedResult<HospitalInvoiceSummary>>(
      `/hospital-billing?${params.toString()}`
    );

    return response.data;
  },

  getById: async (invoiceId: string): Promise<HospitalInvoiceDetail> => {
    const response = await api.get<HospitalInvoiceDetail>(`/hospital-billing/${invoiceId}`);
    return response.data;
  },

  getEligibleEncounters: async (): Promise<HospitalBillingEligibleEncounter[]> => {
    const response = await api.get<HospitalBillingEligibleEncounter[]>(
      "/hospital-billing/eligible-encounters"
    );
    return response.data;
  },

  createInvoice: async (payload: CreateHospitalInvoicePayload): Promise<HospitalInvoiceDetail> => {
    const response = await api.post<HospitalInvoiceDetail>("/hospital-billing", payload);
    return response.data;
  },

  createPaymentIntent: async (
    invoiceId: string,
    payload: CreateHospitalPaymentIntentPayload
  ): Promise<HospitalPaymentIntent> => {
    const response = await api.post<HospitalPaymentIntent>(
      `/hospital-billing/${invoiceId}/payment-intents`,
      payload
    );
    return response.data;
  },

  receivePayment: async (
    invoiceId: string,
    payload: ReceiveHospitalPaymentPayload
  ): Promise<HospitalInvoiceDetail> => {
    const response = await api.post<HospitalInvoiceDetail>(
      `/hospital-billing/${invoiceId}/payments`,
      payload
    );
    return response.data;
  },

  confirmPaymentCallback: async (
    payload: ConfirmHospitalPaymentCallbackPayload
  ): Promise<HospitalInvoiceDetail> => {
    const response = await api.post<HospitalInvoiceDetail>(
      "/hospital-billing/payment-callbacks",
      payload
    );
    return response.data;
  },

  getReconciliationSummary: async (): Promise<HospitalPaymentReconciliationSummary> => {
    const response = await api.get<HospitalPaymentReconciliationSummary>(
      "/hospital-billing/reconciliation/summary"
    );
    return response.data;
  },
};
