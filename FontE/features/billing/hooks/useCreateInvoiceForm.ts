"use client";

import { useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalBillingService } from "@/services/hospitalBillingService";
import { HospitalBillingEncounterPreview } from "../types";

export function useCreateInvoiceForm(onRefresh: () => Promise<void>) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedEncounterId, setSelectedEncounterId] = useState("");
  const [discountAmount, setDiscountAmount] = useState("0");
  const [encounterPreview, setEncounterPreview] = useState<HospitalBillingEncounterPreview | null>(null);
  const [isLoadingPreview, setIsLoadingPreview] = useState(false);

  useEffect(() => {
    if (!selectedEncounterId) {
      setEncounterPreview(null);
      return;
    }

    setIsLoadingPreview(true);
    hospitalBillingService
      .getEncounterPreview(selectedEncounterId)
      .then((preview) => setEncounterPreview(preview))
      .catch(() => setEncounterPreview(null))
      .finally(() => setIsLoadingPreview(false));
  }, [selectedEncounterId]);

  const previewBreakdown = useMemo(() => {
    if (!encounterPreview) return { medTotal: 0, otherTotal: 0, total: 0 };
    let medTotal = 0;
    let otherTotal = 0;
    for (const line of encounterPreview.billableLines) {
      if (line.itemType === "Medication" || line.itemType === "Pharmacy") {
        medTotal += line.lineAmount;
      } else {
        otherTotal += line.lineAmount;
      }
    }
    return { medTotal, otherTotal, total: medTotal + otherTotal };
  }, [encounterPreview]);

  const openCreateModal = () => {
    setSelectedEncounterId("");
    setEncounterPreview(null);
    setDiscountAmount("0");
    setIsCreateModalOpen(true);
  };

  const handleCreateInvoice = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedEncounterId) {
      toast.error("Cần chọn hồ sơ khám để lập hóa đơn.");
      return;
    }

    setIsSubmitting(true);
    try {
      await hospitalBillingService.createInvoice({
        encounterId: selectedEncounterId,
        discountAmount: Number(discountAmount || 0),
        insuranceAmount: 0,
      });

      toast.success("Đã tạo hóa đơn.");
      setIsCreateModalOpen(false);
      setSelectedEncounterId("");
      setDiscountAmount("0");
      setEncounterPreview(null);
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tạo hóa đơn."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    isSubmitting,
    isCreateModalOpen,
    setIsCreateModalOpen,
    selectedEncounterId,
    setSelectedEncounterId,
    discountAmount,
    setDiscountAmount,
    encounterPreview,
    isLoadingPreview,
    previewBreakdown,
    openCreateModal,
    handleCreateInvoice,
  };
}
