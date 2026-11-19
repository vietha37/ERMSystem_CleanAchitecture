"use client";

import { useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalPrescriptionService } from "@/services/hospitalPrescriptionService";
import {
  EMPTY_ITEM,
  HospitalPrescriptionDetail,
  PrescriptionItemForm,
} from "../types";
import { buildCreateItems } from "../utils/prescriptionFormatters";

export function usePrescriptionActions(onRefresh: () => Promise<void>) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Create Modal state
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedEncounterId, setSelectedEncounterId] = useState("");
  const [notes, setNotes] = useState("");
  const [prescriptionItems, setPrescriptionItems] = useState<PrescriptionItemForm[]>([
    EMPTY_ITEM,
  ]);

  // Detail Modal state
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [selectedPrescription, setSelectedPrescription] =
    useState<HospitalPrescriptionDetail | null>(null);

  // Delete Confirm Dialog state
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);

  const resetCreateForm = () => {
    setSelectedEncounterId("");
    setNotes("");
    setPrescriptionItems([EMPTY_ITEM]);
  };

  const handleAddRow = () => {
    setPrescriptionItems((current) => [...current, { ...EMPTY_ITEM }]);
  };

  const handleRemoveRow = (index: number) => {
    setPrescriptionItems((current) =>
      current.length === 1 ? current : current.filter((_, itemIndex) => itemIndex !== index)
    );
  };

  const handleUpdateRow = (
    index: number,
    field: keyof PrescriptionItemForm,
    value: string
  ) => {
    setPrescriptionItems((current) =>
      current.map((item, itemIndex) =>
        itemIndex === index ? { ...item, [field]: value } : item
      )
    );
  };

  const handleOpenDetail = async (prescriptionId: string) => {
    try {
      const detail = await hospitalPrescriptionService.getById(prescriptionId);
      setSelectedPrescription(detail);
      setIsDetailModalOpen(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải chi tiết đơn thuốc."));
    }
  };

  const requestDelete = (prescriptionId: string) => {
    setDeleteTargetId(prescriptionId);
    setIsConfirmDeleteOpen(true);
  };

  const cancelDelete = () => {
    setDeleteTargetId(null);
    setIsConfirmDeleteOpen(false);
  };

  const confirmDelete = async () => {
    if (!deleteTargetId) return;

    try {
      await hospitalPrescriptionService.delete(deleteTargetId);
      toast.success("Đã xóa đơn thuốc.");
      cancelDelete();
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể xóa đơn thuốc."));
    }
  };

  const handleCreatePrescription = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!selectedEncounterId) {
      toast.error("Cần chọn hồ sơ khám để phát hành đơn thuốc.");
      return;
    }

    const items = buildCreateItems(prescriptionItems);
    if (items.length === 0) {
      toast.error("Cần có ít nhất một thuốc hợp lệ.");
      return;
    }

    setIsSubmitting(true);
    try {
      await hospitalPrescriptionService.create({
        encounterId: selectedEncounterId,
        status: "Issued",
        notes: notes.trim() || undefined,
        items,
      });

      toast.success("Đã phát hành đơn thuốc.");
      setIsCreateModalOpen(false);
      resetCreateForm();
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tạo đơn thuốc."));
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
    notes,
    setNotes,
    prescriptionItems,
    isDetailModalOpen,
    setIsDetailModalOpen,
    selectedPrescription,
    setSelectedPrescription,
    isConfirmDeleteOpen,
    deleteTargetId,
    resetCreateForm,
    handleAddRow,
    handleRemoveRow,
    handleUpdateRow,
    handleOpenDetail,
    requestDelete,
    cancelDelete,
    confirmDelete,
    handleCreatePrescription,
  };
}
