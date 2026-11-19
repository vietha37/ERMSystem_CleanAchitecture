"use client";

import { useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { patientService } from "@/services/patientService";
import { DEFAULT_FORM_STATE, PatientFormState, PatientViewModel } from "../types";

export function usePatientForm(isDoctor: boolean) {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<"create" | "edit">("create");
  const [selectedPatientId, setSelectedPatientId] = useState<string | null>(null);
  const [fullName, setFullName] = useState("");
  const [formData, setFormData] = useState<PatientFormState>(DEFAULT_FORM_STATE);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const openCreateModal = () => {
    if (isDoctor) {
      toast.error("Bác sĩ không có quyền tạo hồ sơ bệnh nhân từ màn này.");
      return;
    }

    setModalMode("create");
    setSelectedPatientId(null);
    setFullName("");
    setFormData(DEFAULT_FORM_STATE);
    setIsModalOpen(true);
  };

  const openEditModal = (patient: PatientViewModel, event?: React.MouseEvent) => {
    event?.stopPropagation();

    if (isDoctor) {
      toast.error("Bác sĩ chỉ được xem danh sách bệnh nhân liên quan.");
      return;
    }

    setModalMode("edit");
    setSelectedPatientId(patient.id);
    setFullName(patient.fullName || "");
    setFormData({
      dateOfBirth: patient.dateOfBirth ? patient.dateOfBirth.split("T")[0] : "",
      gender: patient.gender || "Male",
      phone: patient.phone || "",
      address: patient.address || "",
    });
    setIsModalOpen(true);
  };

  const closeModal = () => setIsModalOpen(false);

  const handleSubmit = async (
    event: React.FormEvent,
    onSuccess?: () => Promise<void>
  ) => {
    event.preventDefault();

    if (!fullName.trim() || !formData.dateOfBirth) {
      toast.error("Họ tên và ngày sinh là bắt buộc.");
      return;
    }

    setIsSubmitting(true);
    const payload = {
      fullName: fullName.trim(),
      ...formData,
    };

    try {
      if (modalMode === "create") {
        await patientService.create(payload);
        toast.success("Đã tạo hồ sơ bệnh nhân.");
      } else if (selectedPatientId) {
        await patientService.update(selectedPatientId, payload);
        toast.success("Đã cập nhật hồ sơ bệnh nhân.");
      }

      setIsModalOpen(false);
      await onSuccess?.();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể xử lý yêu cầu."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    isModalOpen,
    modalMode,
    selectedPatientId,
    fullName,
    setFullName,
    formData,
    setFormData,
    isSubmitting,
    openCreateModal,
    openEditModal,
    closeModal,
    handleSubmit,
  };
}
