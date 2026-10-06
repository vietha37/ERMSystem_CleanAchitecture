"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { Modal } from "@/components/ui/Modal";
import { useAuth } from "@/hooks/useAuth";
import { useTranslation } from "@/hooks/useTranslation";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalAppointmentWorklistService } from "@/services/hospitalAppointmentWorklistService";
import {
  HospitalAppointmentWorklistItem,
  HospitalAppointmentWorklistStatus,
} from "@/services/types";

function getStatusStyle(status: HospitalAppointmentWorklistStatus): string {
  switch (status) {
    case "Scheduled":
      return "border border-sky-200 bg-sky-50 text-sky-700";
    case "CheckedIn":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "Completed":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Cancelled":
      return "border border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

function formatDateInput(value?: string | null): string {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().slice(0, 10);
}

function formatTimeInput(value?: string | null): string {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  return date.toISOString().slice(11, 16);
}

export default function AppointmentsPage() {
  const { role } = useAuth();
  const { t } = useTranslation();
  const [appointments, setAppointments] = useState<HospitalAppointmentWorklistItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<HospitalAppointmentWorklistStatus | "All">("All");
  const [appointmentDate, setAppointmentDate] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

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

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(searchQuery.trim());
      setPageNumber(1);
    }, 400);

    return () => window.clearTimeout(timer);
  }, [searchQuery]);

  const fetchAppointments = useCallback(
    async (showRefreshState = false) => {
      if (showRefreshState) {
        setIsRefreshing(true);
      } else {
        setIsLoading(true);
      }

      try {
        const data = await hospitalAppointmentWorklistService.getAll({
          pageNumber,
          pageSize,
          status: statusFilter,
          appointmentDate: appointmentDate || undefined,
          textSearch: debouncedSearch || undefined,
        });

        setAppointments(data.items);
        setTotalCount(data.totalCount);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, t("common.messages.error"));
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [appointmentDate, debouncedSearch, pageNumber, pageSize, statusFilter, t]
  );

  useEffect(() => {
    void fetchAppointments();
  }, [fetchAppointments]);

  const metrics = useMemo(() => {
    return appointments.reduce(
      (acc, item) => {
        acc[item.status] = (acc[item.status] || 0) + 1;
        return acc;
      },
      {
        Scheduled: 0,
        CheckedIn: 0,
        Completed: 0,
        Cancelled: 0,
      } as Record<HospitalAppointmentWorklistStatus, number>
    );
  }, [appointments]);

  const getStatusLabel = (status: HospitalAppointmentWorklistStatus): string => {
    switch (status) {
      case "Scheduled":
        return t("common.status.scheduled") || "Đã xếp lịch";
      case "CheckedIn":
        return t("common.status.checkedIn") || "Đã check-in";
      case "Completed":
        return t("common.status.completed") || "Đã hoàn thành";
      case "Cancelled":
        return t("common.status.cancelled") || "Đã hủy";
      default:
        return status;
    }
  };

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

  const canCheckIn = role === "Admin" || role === "Cashier";
  const canManageAppointment = role === "Admin" || role === "Cashier";

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
      await fetchAppointments(true);
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
      await fetchAppointments(true);
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
      await fetchAppointments(true);
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
      await fetchAppointments(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setActionId(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header & Controls */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border-b border-slate-200 pb-5">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-slate-900">
            {t("appointments.title")}
          </h1>
          <p className="mt-1 text-xs text-slate-500">
            {t("appointments.subtitle")}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Tìm mã lịch, bệnh nhân, bác sĩ..."
            className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
          />

          <input
            type="date"
            value={appointmentDate}
            onChange={(e) => {
              setAppointmentDate(e.target.value);
              setPageNumber(1);
            }}
            className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
          />

          <select
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value as HospitalAppointmentWorklistStatus | "All");
              setPageNumber(1);
            }}
            className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-700 shadow-2xs outline-none focus:border-slate-500"
          >
            <option value="All">{t("common.status.all")}</option>
            <option value="Scheduled">Đã xếp lịch</option>
            <option value="CheckedIn">Đã check-in</option>
            <option value="Completed">Đã hoàn thành</option>
            <option value="Cancelled">Đã hủy</option>
          </select>

          <Button
            variant="secondary"
            size="sm"
            onClick={() => void fetchAppointments(true)}
            disabled={isRefreshing}
          >
            {isRefreshing ? t("common.actions.processing") : t("common.actions.refresh")}
          </Button>
        </div>
      </div>

      {/* KPI Metrics */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Card className="flex flex-col justify-between">
          <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
            Đã xếp lịch
          </p>
          <p className="mt-1 text-2xl font-bold tracking-tight text-sky-700">
            {metrics.Scheduled}
          </p>
        </Card>
        <Card className="flex flex-col justify-between">
          <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
            Đã check-in
          </p>
          <p className="mt-1 text-2xl font-bold tracking-tight text-amber-700">
            {metrics.CheckedIn}
          </p>
        </Card>
        <Card className="flex flex-col justify-between">
          <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
            Đã hoàn thành
          </p>
          <p className="mt-1 text-2xl font-bold tracking-tight text-emerald-700">
            {metrics.Completed}
          </p>
        </Card>
        <Card className="flex flex-col justify-between">
          <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
            Đã hủy
          </p>
          <p className="mt-1 text-2xl font-bold tracking-tight text-rose-700">
            {metrics.Cancelled}
          </p>
        </Card>
      </div>

      {/* Main Table Card */}
      <Card padding="none">
        <div className="p-4 sm:p-5 border-b border-slate-100 flex items-center justify-between">
          <div>
            <h2 className="text-sm font-semibold text-slate-900">
              {t("appointments.title")}
            </h2>
            <p className="text-xs text-slate-500 mt-0.5">
              {t("common.labels.totalRecords", { count: totalCount })}
            </p>
          </div>

          <select
            value={pageSize}
            onChange={(e) => {
              setPageSize(Number(e.target.value));
              setPageNumber(1);
            }}
            className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-700 shadow-2xs outline-none focus:border-slate-500"
          >
            <option value={10}>10 / trang</option>
            <option value={20}>20 / trang</option>
            <option value={50}>50 / trang</option>
          </select>
        </div>

        {isLoading ? (
          <LoadingState title={t("common.actions.loading")} />
        ) : listError ? (
          <ErrorState
            title={t("common.messages.error")}
            description={listError}
            onAction={() => void fetchAppointments(true)}
          />
        ) : appointments.length === 0 ? (
          <EmptyState
            title={t("common.labels.empty")}
            description={t("common.labels.noResults")}
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left text-xs">
              <thead className="border-b border-slate-200 bg-slate-50 text-slate-600 font-semibold">
                <tr>
                  <th className="px-5 py-3">{t("appointments.fields.date")}</th>
                  <th className="px-5 py-3">{t("appointments.fields.patient")}</th>
                  <th className="px-5 py-3">{t("appointments.fields.doctor")}</th>
                  <th className="px-5 py-3">{t("appointments.fields.status")}</th>
                  <th className="px-5 py-3">Quầy / STT</th>
                  <th className="px-5 py-3 text-right">{t("common.labels.actions")}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-slate-800">
                {appointments.map((appointment) => (
                  <tr
                    key={appointment.appointmentId}
                    className="transition-colors hover:bg-slate-50/70"
                  >
                    <td className="px-5 py-3.5">
                      <div className="font-semibold text-slate-900 font-mono">
                        {formatDateTime(appointment.appointmentStartLocal)}
                      </div>
                      <div className="mt-0.5 text-[11px] text-slate-500">
                        {appointment.appointmentNumber}
                      </div>
                    </td>
                    <td className="px-5 py-3.5">
                      <div className="font-semibold text-slate-900">
                        {appointment.patientName}
                      </div>
                      <div className="mt-0.5 text-[11px] text-slate-500 font-mono">
                        {appointment.patientPhone || "--"}
                      </div>
                    </td>
                    <td className="px-5 py-3.5">
                      <div className="font-medium text-slate-900">
                        {appointment.doctorName}
                      </div>
                      <div className="mt-0.5 text-[11px] text-slate-500">
                        {appointment.clinicName || appointment.specialtyName}
                      </div>
                    </td>
                    <td className="px-5 py-3.5">
                      <span
                        className={`inline-flex items-center rounded-md px-2 py-0.5 text-[11px] font-semibold ${getStatusStyle(
                          appointment.status
                        )}`}
                      >
                        {getStatusLabel(appointment.status)}
                      </span>
                    </td>
                    <td className="px-5 py-3.5">
                      <div className="font-medium text-slate-800">
                        {appointment.counterLabel ? `Quầy: ${appointment.counterLabel}` : "--"}
                      </div>
                      <div className="text-[11px] text-slate-500 font-mono">
                        {appointment.queueNumber ? `STT: ${appointment.queueNumber}` : ""}
                      </div>
                    </td>
                    <td className="px-5 py-3.5 text-right space-x-1.5">
                      {canCheckIn && appointment.status === "Scheduled" && (
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => openCheckInModal(appointment)}
                          disabled={actionId === appointment.appointmentId}
                        >
                          Check-in
                        </Button>
                      )}
                      {canManageAppointment && appointment.status === "Scheduled" && (
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => openRescheduleModal(appointment)}
                          disabled={actionId === appointment.appointmentId}
                        >
                          Đổi lịch
                        </Button>
                      )}
                      {appointment.status !== "Completed" && appointment.status !== "Cancelled" && (
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => void handleComplete(appointment)}
                          disabled={actionId === appointment.appointmentId}
                        >
                          Hoàn thành
                        </Button>
                      )}
                      {canManageAppointment && appointment.status === "Scheduled" && (
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => openCancelModal(appointment)}
                          disabled={actionId === appointment.appointmentId}
                        >
                          Hủy
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination Footer */}
        {!isLoading && totalPages > 0 && (
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between border-t border-slate-100 px-5 py-3.5 text-xs text-slate-600">
            <div>
              {t("common.labels.totalRecords", { count: totalCount })}
            </div>
            <div className="flex items-center gap-1.5">
              <Button
                variant="secondary"
                size="sm"
                disabled={pageNumber <= 1}
                onClick={() => setPageNumber((c) => Math.max(1, c - 1))}
              >
                {t("common.labels.previous")}
              </Button>
              <span className="px-2 font-medium">
                {pageNumber} / {totalPages}
              </span>
              <Button
                variant="secondary"
                size="sm"
                disabled={pageNumber >= totalPages}
                onClick={() => setPageNumber((c) => Math.min(totalPages, c + 1))}
              >
                {t("common.labels.next")}
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Check-In Modal */}
      <Modal
        isOpen={isCheckInModalOpen}
        onClose={resetCheckInModal}
        title={t("appointments.checkIn")}
        badge={selectedAppointment?.appointmentNumber}
      >
        <form onSubmit={handleCheckIn} className="space-y-4">
          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              Quầy tiếp đón / Số phòng
            </label>
            <input
              type="text"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
              placeholder="VD: Quầy A1 hoặc Phòng 102"
              value={counterLabel}
              onChange={(e) => setCounterLabel(e.target.value)}
            />
          </div>
          <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
            <Button type="button" variant="secondary" size="sm" onClick={resetCheckInModal}>
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
        onClose={resetRescheduleModal}
        title={t("appointments.reschedule")}
        badge={selectedAppointment?.appointmentNumber}
      >
        <form onSubmit={handleReschedule} className="space-y-4">
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
                onChange={(e) => setRescheduleDate(e.target.value)}
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
                onChange={(e) => setRescheduleTime(e.target.value)}
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
              onChange={(e) => setRescheduleReason(e.target.value)}
            />
          </div>
          <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
            <Button type="button" variant="secondary" size="sm" onClick={resetRescheduleModal}>
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
        onClose={resetCancelModal}
        title={t("appointments.cancelAppointment")}
        badge={selectedAppointment?.appointmentNumber}
      >
        <form onSubmit={handleCancel} className="space-y-4">
          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              Lý do hủy lịch
            </label>
            <textarea
              rows={3}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
              placeholder="Nhập lý do hủy lịch hẹn..."
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
            />
          </div>
          <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
            <Button type="button" variant="secondary" size="sm" onClick={resetCancelModal}>
              {t("common.actions.cancel")}
            </Button>
            <Button type="submit" variant="danger" size="sm" isLoading={actionId != null}>
              Xác nhận Hủy lịch
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
