"use client";

import { useState } from "react";
import toast from "react-hot-toast";
import { useAuth } from "@/hooks/useAuth";
import { useTranslation } from "@/hooks/useTranslation";
import { getApiErrorMessage } from "@/services/error";
import { hospitalAppointmentWorklistService } from "@/services/hospitalAppointmentWorklistService";
import { HospitalAppointmentWorklistItem } from "../types";
import { formatDateInput, formatTimeInput } from "../utils/appointmentFormatters";

export function useAppointmentActions(onSuccess: () => Promise<void>) {
  const { role } = useAuth();
  const { t } = useTranslation();

  const [isCheckInModalOpen, setIsCheckInModalOpen] = useState(false);
  const [isCancelModalOpen, setIsCancelModalOpen] = useState(false);
  const [isRescheduleModalOpen, setIsRescheduleModalOpen] = useState(false);
  const [selectedAppointment, setSelectedAppointment] = useState<HospitalAppointmentWorklistItem | null>(null);
  const [counterLabel, setCounterLabel] = useState("");
  const [cancelReason, setCancelReason] = useState("");
  const [rescheduleReason, setRescheduleReason] = useState("");
  const [rescheduleDate, setRescheduleDate] = useState("");
  const [rescheduleTime, setRescheduleTime] = useState("");
  const [actionId, setActionId] = useState<string | null>(null);

  const canCheckIn = role === "Admin" || role === "Cashier";
  const canManageAppointment = role === "Admin" || role === "Cashier";

  const openCheckInModal = (appointment: HospitalAppointmentWorklistItem) => {
    setSelectedAppointment(appointment);
    setCounterLabel(appointment.counterLabel ?? "");
    setIsCheckInModalOpen(true);
  };

  const openCancelModal = (appointment: HospitalAppointmentWorklistItem) => {
    setSelectedAppointment(appointment);
    setCancelReason("");
    setIsCancelModalOpen(true);
  };

  const openRescheduleModal = (appointment: HospitalAppointmentWorklistItem) => {
    setSelectedAppointment(appointment);
    setRescheduleReason("");
    setRescheduleDate(formatDateInput(appointment.appointmentStartLocal));
    setRescheduleTime(formatTimeInput(appointment.appointmentStartLocal));
    setIsRescheduleModalOpen(true);
  };

  const resetCheckInModal = () => {
    setIsCheckInModalOpen(false);
    setSelectedAppointment(null);
    setCounterLabel("");
  };

  const resetCancelModal = () => {
    setIsCancelModalOpen(false);
    setSelectedAppointment(null);
    setCancelReason("");
  };

  const resetRescheduleModal = () => {
    setIsRescheduleModalOpen(false);
    setSelectedAppointment(null);
    setRescheduleReason("");
    setRescheduleDate("");
    setRescheduleTime("");
  };

  const handleCheckIn = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedAppointment) return;

    setActionId(selectedAppointment.appointmentId);
    try {
      await hospitalAppointmentWorklistService.checkIn(selectedAppointment.appointmentId, {
        counterLabel: counterLabel.trim() || undefined,
      });

      toast.success(t("common.messages.updateSuccess"));
      resetCheckInModal();
      await onSuccess();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setActionId(null);
    }
  };

  const handleComplete = async (appointment: HospitalAppointmentWorklistItem) => {
    setActionId(appointment.appointmentId);
    try {
      await hospitalAppointmentWorklistService.updateStatus(appointment.appointmentId, "Completed");
      toast.success(t("common.messages.updateSuccess"));
      await onSuccess();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setActionId(null);
    }
  };

  const handleCancel = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedAppointment) return;

    setActionId(selectedAppointment.appointmentId);
    try {
      await hospitalAppointmentWorklistService.cancel(selectedAppointment.appointmentId, {
        reason: cancelReason.trim() || undefined,
      });
      toast.success(t("common.messages.updateSuccess"));
      resetCancelModal();
      await onSuccess();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setActionId(null);
    }
  };

  const handleReschedule = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedAppointment || !rescheduleDate || !rescheduleTime) return;

    setActionId(selectedAppointment.appointmentId);
    try {
      await hospitalAppointmentWorklistService.reschedule(selectedAppointment.appointmentId, {
        preferredDate: rescheduleDate,
        preferredTime: rescheduleTime,
        reason: rescheduleReason.trim() || undefined,
      });

      toast.success(t("common.messages.updateSuccess"));
      resetRescheduleModal();
      await onSuccess();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setActionId(null);
    }
  };

  return {
    canCheckIn,
    canManageAppointment,
    isCheckInModalOpen,
    isCancelModalOpen,
    isRescheduleModalOpen,
    selectedAppointment,
    counterLabel,
    setCounterLabel,
    cancelReason,
    setCancelReason,
    rescheduleReason,
    setRescheduleReason,
    rescheduleDate,
    setRescheduleDate,
    rescheduleTime,
    setRescheduleTime,
    actionId,
    openCheckInModal,
    openCancelModal,
    openRescheduleModal,
    resetCheckInModal,
    resetCancelModal,
    resetRescheduleModal,
    handleCheckIn,
    handleComplete,
    handleCancel,
    handleReschedule,
  };
}
