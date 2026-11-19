import React from "react";
import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { useTranslation } from "@/hooks/useTranslation";
import { HospitalAppointmentWorklistItem } from "../types";

interface AppointmentModalsProps {
  selectedAppointment: HospitalAppointmentWorklistItem | null;
  actionId: string | null;
  // Check-In
  isCheckInModalOpen: boolean;
  counterLabel: string;
  onCounterLabelChange: (value: string) => void;
  onCheckInSubmit: (e: React.FormEvent) => void;
  onCheckInClose: () => void;
  // Reschedule
  isRescheduleModalOpen: boolean;
  rescheduleDate: string;
  onRescheduleDateChange: (value: string) => void;
  rescheduleTime: string;
  onRescheduleTimeChange: (value: string) => void;
  rescheduleReason: string;
  onRescheduleReasonChange: (value: string) => void;
  onRescheduleSubmit: (e: React.FormEvent) => void;
  onRescheduleClose: () => void;
  // Cancel
  isCancelModalOpen: boolean;
  cancelReason: string;
  onCancelReasonChange: (value: string) => void;
  onCancelSubmit: (e: React.FormEvent) => void;
  onCancelClose: () => void;
}

export function AppointmentModals({
  selectedAppointment,
  actionId,
  isCheckInModalOpen,
  counterLabel,
  onCounterLabelChange,
  onCheckInSubmit,
  onCheckInClose,
  isRescheduleModalOpen,
  rescheduleDate,
  onRescheduleDateChange,
  rescheduleTime,
  onRescheduleTimeChange,
  rescheduleReason,
  onRescheduleReasonChange,
  onRescheduleSubmit,
  onRescheduleClose,
  isCancelModalOpen,
  cancelReason,
  onCancelReasonChange,
  onCancelSubmit,
  onCancelClose,
}: AppointmentModalsProps) {
  const { t } = useTranslation();

  return (
    <>
      {/* Check-In Modal */}
      <Modal
        isOpen={isCheckInModalOpen}
        onClose={onCheckInClose}
        title={t("appointments.checkIn")}
        badge={selectedAppointment?.appointmentNumber}
      >
        <form onSubmit={onCheckInSubmit} className="space-y-4">
          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              Quầy tiếp đón / Số phòng
            </label>
            <input
              type="text"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
              placeholder="VD: Quầy A1 hoặc Phòng 102"
              value={counterLabel}
              onChange={(e) => onCounterLabelChange(e.target.value)}
            />
          </div>
          <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
            <Button type="button" variant="secondary" size="sm" onClick={onCheckInClose}>
              {t("common.actions.cancel")}
            </Button>
            <Button type="submit" variant="primary" size="sm" isLoading={actionId != null}>
              Xác nhận Check-in
            </Button>
          </div>
        </form>
      </Modal>

      {/* Reschedule Modal */}
      <Modal
        isOpen={isRescheduleModalOpen}
        onClose={onRescheduleClose}
        title={t("appointments.reschedule")}
        badge={selectedAppointment?.appointmentNumber}
      >
        <form onSubmit={onRescheduleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="mb-1 block text-xs font-semibold text-slate-700">
                Ngày khám mới <span className="text-rose-500">*</span>
              </label>
              <input
                type="date"
                required
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
                value={rescheduleDate}
                onChange={(e) => onRescheduleDateChange(e.target.value)}
              />
            </div>
            <div>
              <label className="mb-1 block text-xs font-semibold text-slate-700">
                Giờ khám mới <span className="text-rose-500">*</span>
              </label>
              <input
                type="time"
                required
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
                value={rescheduleTime}
                onChange={(e) => onRescheduleTimeChange(e.target.value)}
              />
            </div>
          </div>
          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              Lý do đổi lịch
            </label>
            <textarea
              rows={2}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
              placeholder="Nhập lý do thay đổi lịch hẹn..."
              value={rescheduleReason}
              onChange={(e) => onRescheduleReasonChange(e.target.value)}
            />
          </div>
          <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
            <Button type="button" variant="secondary" size="sm" onClick={onRescheduleClose}>
              {t("common.actions.cancel")}
            </Button>
            <Button type="submit" variant="primary" size="sm" isLoading={actionId != null}>
              Lưu lịch khám mới
            </Button>
          </div>
        </form>
      </Modal>

      {/* Cancel Modal */}
      <Modal
        isOpen={isCancelModalOpen}
        onClose={onCancelClose}
        title={t("appointments.cancelAppointment")}
        badge={selectedAppointment?.appointmentNumber}
      >
        <form onSubmit={onCancelSubmit} className="space-y-4">
          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              Lý do hủy lịch
            </label>
            <textarea
              rows={3}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
              placeholder="Nhập lý do hủy lịch hẹn..."
              value={cancelReason}
              onChange={(e) => onCancelReasonChange(e.target.value)}
            />
          </div>
          <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
            <Button type="button" variant="secondary" size="sm" onClick={onCancelClose}>
              {t("common.actions.cancel")}
            </Button>
            <Button type="submit" variant="danger" size="sm" isLoading={actionId != null}>
              Xác nhận Hủy lịch
            </Button>
          </div>
        </form>
      </Modal>
    </>
  );
}
