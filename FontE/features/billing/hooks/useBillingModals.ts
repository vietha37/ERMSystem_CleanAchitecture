"use client";

import { useMemo, useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalBillingService } from "@/services/hospitalBillingService";
import { HospitalInvoiceDetail } from "../types";
import { useCreateInvoiceForm } from "./useCreateInvoiceForm";
import { usePaymentAndGatewayActions } from "./usePaymentAndGatewayActions";

export function useBillingModals(onRefresh: () => Promise<void>) {
  // Detail Modal
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState<HospitalInvoiceDetail | null>(null);

  const createForm = useCreateInvoiceForm(onRefresh);
  const paymentActions = usePaymentAndGatewayActions(onRefresh, (updated) => setSelectedInvoice(updated));

  const detailBreakdown = useMemo(() => {
    if (!selectedInvoice) return { medItems: [], otherItems: [], medTotal: 0, otherTotal: 0 };
    const medItems = selectedInvoice.items.filter(
      (item) => item.itemType === "Medication" || item.itemType === "Pharmacy"
    );
    const otherItems = selectedInvoice.items.filter(
      (item) => item.itemType !== "Medication" && item.itemType !== "Pharmacy"
    );
    const medTotal = medItems.reduce((sum, item) => sum + item.lineAmount, 0);
    const otherTotal = otherItems.reduce((sum, item) => sum + item.lineAmount, 0);
    return { medItems, otherItems, medTotal, otherTotal };
  }, [selectedInvoice]);

  const openDetail = async (invoiceId: string) => {
    try {
      const detail = await hospitalBillingService.getById(invoiceId);
      setSelectedInvoice(detail);
      setIsDetailModalOpen(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải chi tiết hóa đơn."));
    }
  };

  return {
    ...createForm,
    ...paymentActions,
    isSubmitting: createForm.isSubmitting || paymentActions.isSubmitting,
    isDetailModalOpen,
    setIsDetailModalOpen,
    selectedInvoice,
    detailBreakdown,
    openDetail,
  };
}
