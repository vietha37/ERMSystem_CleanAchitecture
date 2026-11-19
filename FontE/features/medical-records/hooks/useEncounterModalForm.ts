"use client";

import { useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalEncounterService } from "@/services/hospitalEncounterService";
import {
  EMPTY_FORM,
  EncounterFormState,
  HospitalEncounterDetail,
  HospitalEncounterEligibleAppointment,
} from "../types";
import {
  buildPayload,
  buildUpdatePayload,
  mapDetailToForm,
} from "../utils/medicalRecordFormatters";
import { useEncounterAttachments } from "./useEncounterAttachments";

interface UseEncounterModalFormProps {
  eligibleAppointments: HospitalEncounterEligibleAppointment[];
  onRefresh: () => Promise<void>;
}

export function useEncounterModalForm({
  eligibleAppointments,
  onRefresh,
}: UseEncounterModalFormProps) {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [editingEncounterId, setEditingEncounterId] = useState<string | null>(null);
  const [editingDetail, setEditingDetail] = useState<HospitalEncounterDetail | null>(null);
  const [form, setForm] = useState<EncounterFormState>(EMPTY_FORM);

  // Workflow actions
  const [signatureComment, setSignatureComment] = useState("");
  const [isSigningEncounter, setIsSigningEncounter] = useState(false);
  const [approvalComment, setApprovalComment] = useState("");
  const [isApprovingModal, setIsApprovingModal] = useState(false);

  const attachments = useEncounterAttachments((updated) => setEditingDetail(updated), onRefresh);

  const availableAppointments = eligibleAppointments.filter(
    (item) => !item.existingEncounterId || item.existingEncounterId === editingEncounterId
  );

  const isApprovedDetail = editingDetail?.encounterStatus === "Approved";
  const canSignDetail = !!editingEncounterId && editingDetail?.encounterStatus !== "InProgress";
  const canApproveDetail =
    !!editingEncounterId &&
    editingDetail?.encounterStatus === "Finalized" &&
    !!editingDetail?.isClinicalNoteSigned;

  const resetFormState = () => {
    setEditingEncounterId(null);
    setEditingDetail(null);
    setForm(EMPTY_FORM);
    attachments.resetAttachments();
    setSignatureComment("");
    setApprovalComment("");
  };

  const openCreateModal = () => {
    resetFormState();
    setIsModalOpen(true);
  };

  const openEditModal = async (encounterId: string) => {
    try {
      const detail = await hospitalEncounterService.getById(encounterId);
      setEditingEncounterId(encounterId);
      setEditingDetail(detail);
      setForm(mapDetailToForm(detail));
      attachments.resetAttachments();
      setSignatureComment("");
      setApprovalComment(detail.approvalComment ?? "");
      setIsModalOpen(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải chi tiết hồ sơ khám."));
    }
  };

  const closeModal = () => {
    setIsModalOpen(false);
    resetFormState();
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!editingEncounterId && !form.appointmentId) {
      toast.error("Cần chọn lịch hẹn để mở hồ sơ khám.");
      return;
    }
    if (!form.diagnosisName.trim()) {
      toast.error("Chẩn đoán là trường bắt buộc.");
      return;
    }

    setIsSubmitting(true);
    try {
      if (editingEncounterId) {
        const updated = await hospitalEncounterService.update(
          editingEncounterId,
          buildUpdatePayload(form)
        );
        setEditingDetail(updated);
        toast.success("Đã cập nhật hồ sơ khám.");
      } else {
        await hospitalEncounterService.create(buildPayload(form));
        toast.success("Đã tạo hồ sơ khám mới.");
      }
      closeModal();
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể lưu hồ sơ khám."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleSignEncounter = async () => {
    if (!editingEncounterId) return;

    setIsSigningEncounter(true);
    try {
      const updated = await hospitalEncounterService.sign(editingEncounterId, {
        attestationText: signatureComment.trim() || undefined,
      });
      setEditingDetail(updated);
      setSignatureComment("");
      toast.success("Đã ký xác nhận hồ sơ.");
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể ký xác nhận hồ sơ."));
    } finally {
      setIsSigningEncounter(false);
    }
  };

  const handleApproveFromModal = async () => {
    if (!editingEncounterId) return;

    setIsApprovingModal(true);
    try {
      const updated = await hospitalEncounterService.approve(editingEncounterId, {
        approvalComment: approvalComment.trim() || undefined,
      });
      setEditingDetail(updated);
      setApprovalComment(updated.approvalComment ?? "");
      toast.success("Đã duyệt hồ sơ khám.");
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể duyệt hồ sơ khám."));
    } finally {
      setIsApprovingModal(false);
    }
  };

  return {
    isModalOpen,
    isSubmitting,
    editingEncounterId,
    editingDetail,
    form,
    setForm,
    availableAppointments,
    isApprovedDetail,
    canSignDetail,
    canApproveDetail,
    signatureComment,
    setSignatureComment,
    isSigningEncounter,
    approvalComment,
    setApprovalComment,
    isApprovingModal,
    ...attachments,
    handleUploadAttachment: () => attachments.handleUploadAttachment(editingEncounterId),
    openCreateModal,
    openEditModal,
    closeModal,
    handleSubmit,
    handleSignEncounter,
    handleApproveFromModal,
  };
}
