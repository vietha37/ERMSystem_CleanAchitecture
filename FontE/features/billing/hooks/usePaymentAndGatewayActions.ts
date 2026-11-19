"use client";

import { useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalBillingService } from "@/services/hospitalBillingService";
import {
  HospitalInvoiceDetail,
  HospitalInvoiceSummary,
  HospitalPaymentIntent,
} from "../types";

export function usePaymentAndGatewayActions(
  onRefresh: () => Promise<void>,
  onUpdateDetail: (detail: HospitalInvoiceDetail) => void
) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Modals visibility
  const [isPaymentModalOpen, setIsPaymentModalOpen] = useState(false);
  const [isGatewayModalOpen, setIsGatewayModalOpen] = useState(false);
  const [isGatewayCallbackModalOpen, setIsGatewayCallbackModalOpen] = useState(false);

  // Selected targets
  const [paymentInvoice, setPaymentInvoice] = useState<HospitalInvoiceSummary | null>(null);
  const [gatewayInvoice, setGatewayInvoice] = useState<HospitalInvoiceSummary | null>(null);
  const [gatewayIntent, setGatewayIntent] = useState<HospitalPaymentIntent | null>(null);

  // Payment form
  const [paymentMethod, setPaymentMethod] = useState("Cash");
  const [paymentAmount, setPaymentAmount] = useState("");
  const [paymentReference, setPaymentReference] = useState("");

  // Gateway form
  const [gatewayMethod, setGatewayMethod] = useState("Transfer");
  const [gatewayAmount, setGatewayAmount] = useState("");
  const [gatewayStatus, setGatewayStatus] = useState("Captured");

  const openPaymentModal = (invoice: HospitalInvoiceSummary) => {
    setPaymentInvoice(invoice);
    setPaymentAmount(invoice.balanceAmount.toString());
    setPaymentReference("");
    setPaymentMethod("Cash");
    setIsPaymentModalOpen(true);
  };

  const handleReceivePayment = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!paymentInvoice) return;

    setIsSubmitting(true);
    try {
      const updated = await hospitalBillingService.receivePayment(paymentInvoice.invoiceId, {
        paymentMethod,
        paymentReference: paymentReference.trim() || undefined,
        amount: Number(paymentAmount),
      });

      toast.success("Đã ghi nhận thanh toán.");
      onUpdateDetail(updated);
      setPaymentInvoice(null);
      setPaymentAmount("");
      setPaymentReference("");
      setIsPaymentModalOpen(false);
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể ghi nhận thanh toán."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const openGatewayModal = (invoice: HospitalInvoiceSummary) => {
    setGatewayInvoice(invoice);
    setGatewayMethod("Transfer");
    setGatewayAmount(invoice.balanceAmount.toString());
    setGatewayIntent(null);
    setIsGatewayModalOpen(true);
  };

  const handleCreateGatewayIntent = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!gatewayInvoice) return;

    setIsSubmitting(true);
    try {
      const intent = await hospitalBillingService.createPaymentIntent(gatewayInvoice.invoiceId, {
        gatewayProvider: "MockGateway",
        paymentMethod: gatewayMethod,
        amount: Number(gatewayAmount),
      });

      setGatewayIntent(intent);
      setGatewayStatus("Captured");
      setIsGatewayModalOpen(false);
      setIsGatewayCallbackModalOpen(true);
      toast.success("Đã tạo giao dịch.");
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tạo giao dịch thanh toán."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleConfirmGatewayCallback = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!gatewayIntent) return;

    setIsSubmitting(true);
    try {
      const updated = await hospitalBillingService.simulatePaymentCallback({
        invoiceId: gatewayIntent.invoiceId,
        gatewayProvider: gatewayIntent.gatewayProvider,
        paymentReference: gatewayIntent.paymentReference,
        externalTransactionId: gatewayIntent.externalTransactionId || undefined,
        gatewayStatus,
        amount: gatewayIntent.amount,
      });

      onUpdateDetail(updated);
      setGatewayIntent(null);
      setIsGatewayCallbackModalOpen(false);
      toast.success(
        gatewayStatus === "Captured"
          ? "Xác nhận thanh toán thành công."
          : "Giao dịch thất bại."
      );
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể xử lý callback thanh toán."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    isSubmitting,
    isPaymentModalOpen,
    setIsPaymentModalOpen,
    isGatewayModalOpen,
    setIsGatewayModalOpen,
    isGatewayCallbackModalOpen,
    setIsGatewayCallbackModalOpen,
    paymentInvoice,
    gatewayIntent,
    paymentMethod,
    setPaymentMethod,
    paymentAmount,
    setPaymentAmount,
    paymentReference,
    setPaymentReference,
    gatewayMethod,
    setGatewayMethod,
    gatewayAmount,
    setGatewayAmount,
    gatewayStatus,
    setGatewayStatus,
    openPaymentModal,
    handleReceivePayment,
    openGatewayModal,
    handleCreateGatewayIntent,
    handleConfirmGatewayCallback,
  };
}
