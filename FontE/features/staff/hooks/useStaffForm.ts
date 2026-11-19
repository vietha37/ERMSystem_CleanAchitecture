"use client";

import { FormEvent, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { useTranslation } from "@/hooks/useTranslation";
import { getApiErrorMessage } from "@/services/error";
import { staffUserService } from "@/services/staffUserService";
import { initialStaffForm, StaffFormState, StaffUser, UpdateStaffUserPayload } from "../types";

export function useStaffForm(onSuccess: () => void) {
  const { t } = useTranslation();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [mode, setMode] = useState<"create" | "edit">("create");
  const [selected, setSelected] = useState<StaffUser | null>(null);
  const [form, setForm] = useState<StaffFormState>(initialStaffForm);
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const openCreateModal = () => {
    setMode("create");
    setSelected(null);
    setForm(initialStaffForm);
    setShowPassword(false);
    setIsModalOpen(true);
  };

  const openEditModal = (user: StaffUser) => {
    if (user.role === "Patient") return;

    setMode("edit");
    setSelected(user);
    setForm({
      username: user.username,
      name: user.name,
      password: "",
      role: user.role as "Doctor" | "Cashier",
    });
    setShowPassword(false);
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setIsSubmitting(true);
    try {
      if (mode === "create") {
        await staffUserService.create({
          username: form.username.trim(),
          name: form.name.trim(),
          password: form.password,
          role: form.role,
        });
        toast.success(t("common.messages.createSuccess"));
      } else if (selected) {
        const payload: UpdateStaffUserPayload = {
          username: form.username.trim(),
          name: form.name.trim(),
          role: form.role,
        };
        if (form.password.trim()) {
          payload.password = form.password.trim();
        }
        await staffUserService.update(selected.id, payload);
        toast.success(t("common.messages.updateSuccess"));
      }

      setIsModalOpen(false);
      onSuccess();
    } catch (error) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setIsSubmitting(false);
    }
  };

  const titleByMode = useMemo(
    () => (mode === "create" ? t("staff.createStaff") : t("common.actions.edit")),
    [mode, t]
  );

  return {
    isModalOpen,
    mode,
    selected,
    form,
    setForm,
    showPassword,
    setShowPassword,
    isSubmitting,
    titleByMode,
    openCreateModal,
    openEditModal,
    closeModal,
    handleSubmit,
  };
}
