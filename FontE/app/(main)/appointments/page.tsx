"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { Modal } from "@/components/ui/Modal";
import { useAuth } from "@/hooks/useAuth";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalAppointmentWorklistService } from "@/services/hospitalAppointmentWorklistService";
import {
  HospitalAppointmentWorklistItem,
  HospitalAppointmentWorklistStatus,
} from "@/services/types";

const STATUS_OPTIONS: Array<{
  value: HospitalAppointmentWorklistStatus | "All";
  label: string;
}> = [
  { value: "All", label: "Tất cả" },
  { value: "Scheduled", label: "Đã xếp lịch" },
  { value: "CheckedIn", label: "Đã check-in" },
  { value: "Completed", label: "Đã hoàn thành" },
  { value: "Cancelled", label: "Đã hủy" },
];

function getStatusLabel(status: HospitalAppointmentWorklistStatus): string {
  switch (status) {
    case "Scheduled":
      return "Đã xếp lịch";
    case "CheckedIn":
      return "Đã check-in";
    case "Completed":
      return "Đã hoàn thành";
    case "Cancelled":
      return "Đã hủy";
    default:
      return status;
  }
}

function getStatusStyle(status: HospitalAppointmentWorklistStatus): string {
  switch (status) {
    case "Scheduled":
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
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
  if (!value) {
    return "";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return date.toISOString().slice(0, 10);
}

function formatTimeInput(value?: string | null): string {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return date.toISOString().slice(11, 16);
}

export default function AppointmentsPage() {
  const { role } = useAuth();
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
        const message = getApiErrorMessage(error, "Không thể tải danh sách lịch hẹn.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [appointmentDate, debouncedSearch, pageNumber, pageSize, statusFilter]
  );

  useEffect(() => {
    void fetchAppointments();
  }, [fetchAppointments]);

  const metrics = useMemo(() => {
    return appointments.reduce(
      (acc, item) => {
        acc[item.status] += 1;
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
    if (!selectedAppointment) {
      return;
    }

    setActionId(selectedAppointment.appointmentId);

    try {
      await hospitalAppointmentWorklistService.checkIn(selectedAppointment.appointmentId, {
        counterLabel: counterLabel.trim() || undefined,
      });

      toast.success("Đã check-in bệnh nhân và cấp số thứ tự.");
      resetCheckInModal();
      await fetchAppointments(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể check-in lịch hẹn."));
    } finally {
      setActionId(null);
    }
  };

  const handleComplete = async (appointment: HospitalAppointmentWorklistItem) => {
    setActionId(appointment.appointmentId);

    try {
      await hospitalAppointmentWorklistService.updateStatus(appointment.appointmentId, "Completed");
      toast.success("Đã cập nhật trạng thái sang Đã hoàn thành.");
      await fetchAppointments(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể cập nhật trạng thái lịch hẹn."));
    } finally {
      setActionId(null);
    }
  };

  const handleCancel = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedAppointment) {
      return;
    }

    setActionId(selectedAppointment.appointmentId);

    try {
      await hospitalAppointmentWorklistService.cancel(selectedAppointment.appointmentId, {
        reason: cancelReason.trim() || undefined,
      });
      toast.success("Đã hủy lịch hẹn.");
      resetCancelModal();
      await fetchAppointments(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể hủy lịch hẹn."));
    } finally {
      setActionId(null);
    }
  };

  const handleReschedule = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedAppointment || !rescheduleDate || !rescheduleTime) {
      return;
    }

    setActionId(selectedAppointment.appointmentId);

    try {
      await hospitalAppointmentWorklistService.reschedule(selectedAppointment.appointmentId, {
        preferredDate: rescheduleDate,
        preferredTime: rescheduleTime,
        reason: rescheduleReason.trim() || undefined,
      });
      toast.success("Đã đổi lịch hẹn.");
      resetRescheduleModal();
      await fetchAppointments(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể đổi lịch hẹn."));
    } finally {
      setActionId(null);
    }
  };

  const canCheckIn = role === "Admin" || role === "Receptionist";
  const canManageAppointment = role === "Admin" || role === "Receptionist";
  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endItem = totalCount === 0 ? 0 : Math.min(pageNumber * pageSize, totalCount);

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <div className="rounded-[2rem] border border-sky-100 bg-gradient-to-br from-cyan-50 via-white to-blue-50 p-6 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.26em] text-cyan-700">
              Dịch vụ lịch hẹn
            </p>
            <h1 className="mt-3 text-3xl font-bold text-slate-950">
              Điều phối lịch hẹn nội bộ
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-7 text-slate-600">
              Tách rõ nhánh check-in, hoàn thành, hủy lịch và đổi lịch để lễ tân thao tác đúng chính sách.
            </p>
          </div>

          <div className="flex flex-col gap-3 md:flex-row md:items-center">
            <input
              type="text"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Tìm theo mã lịch, bệnh nhân, bác sĩ..."
              className="min-w-[260px] rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            />

            <input
              type="date"
              value={appointmentDate}
              onChange={(event) => {
                setAppointmentDate(event.target.value);
                setPageNumber(1);
              }}
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            />

            <select
              value={statusFilter}
              onChange={(event) => {
                setStatusFilter(event.target.value as HospitalAppointmentWorklistStatus | "All");
                setPageNumber(1);
              }}
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            >
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>

            <Button onClick={() => void fetchAppointments(true)} disabled={isRefreshing}>
              {isRefreshing ? "Đang làm mới..." : "Làm mới"}
            </Button>
          </div>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Đã xếp lịch" value={metrics.Scheduled} tone="cyan" />
        <MetricCard label="Đã check-in" value={metrics.CheckedIn} tone="amber" />
        <MetricCard label="Đã hoàn thành" value={metrics.Completed} tone="emerald" />
        <MetricCard label="Đã hủy" value={metrics.Cancelled} tone="rose" />
      </div>

      <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
        <div className="flex flex-col gap-3 border-b border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-bold text-slate-900">Danh sách lịch hẹn</h2>
            <p className="mt-1 text-sm text-slate-500">
              Hiển thị {startItem}-{endItem} / {totalCount} lịch hẹn.
            </p>
          </div>

          <select
            value={pageSize}
            onChange={(event) => {
              setPageSize(Number(event.target.value));
              setPageNumber(1);
            }}
            className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
          >
            <option value={10}>10 dòng / trang</option>
            <option value={20}>20 dòng / trang</option>
            <option value={50}>50 dòng / trang</option>
          </select>
        </div>

        {isLoading ? (
          <LoadingState title="Đang tải danh sách lịch hẹn..." tone="cyan" />
        ) : listError ? (
          <ErrorState
            title="Không thể tải danh sách lịch hẹn"
            description={listError}
            onAction={() => void fetchAppointments(true)}
          />
        ) : appointments.length === 0 ? (
          <EmptyState
            title="Không có lịch hẹn nào khớp bộ lọc hiện tại."
            description="Thử đổi ngày, trạng thái hoặc từ khóa tìm kiếm để mở rộng kết quả."
            tone="cyan"
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-[1380px] w-full border-collapse text-left">
              <thead>
                <tr className="bg-slate-50">
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Lịch hẹn</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Bệnh nhân</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Bác sĩ</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Thời gian</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Trạng thái</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Check-in</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Tác vụ</th>
                </tr>
              </thead>
              <tbody>
                {appointments.map((appointment) => (
                  <tr
                    key={appointment.appointmentId}
                    className="border-t border-slate-100 align-top transition-colors hover:bg-cyan-50/30"
                  >
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{appointment.appointmentNumber}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        {appointment.appointmentType} / {appointment.bookingChannel}
                      </div>
                      <div className="mt-1 text-xs text-slate-400">
                        {appointment.chiefComplaint || "Không có lý do khám"}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{appointment.patientName}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        {appointment.medicalRecordNumber}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {appointment.patientPhone || "--"}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{appointment.doctorName}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        {appointment.specialtyName}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {appointment.clinicName}
                        {(appointment.floorLabel || appointment.roomLabel) &&
                          ` / ${appointment.floorLabel ?? "--"} / ${appointment.roomLabel ?? "--"}`}
                      </div>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      <div>{formatDateTime(appointment.appointmentStartLocal)}</div>
                      <div className="mt-1 text-xs text-slate-400">
                        Kết thúc: {formatDateTime(appointment.appointmentEndLocal)}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getStatusStyle(appointment.status)}`}
                      >
                        {getStatusLabel(appointment.status)}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      <div>Quầy: {appointment.counterLabel || "--"}</div>
                      <div className="mt-1">Số thứ tự: {appointment.queueNumber || "--"}</div>
                      <div className="mt-1 text-xs text-slate-400">
                        {appointment.checkInTimeLocal
                          ? `Lúc ${formatDateTime(appointment.checkInTimeLocal)}`
                          : "Chưa check-in"}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap gap-2">
                        {canCheckIn && appointment.status === "Scheduled" && (
                          <Button
                            variant="secondary"
                            onClick={() => openCheckInModal(appointment)}
                            disabled={actionId === appointment.appointmentId}
                            className="border-amber-200 text-amber-700 hover:bg-amber-50"
                          >
                            Check-in
                          </Button>
                        )}

                        {canManageAppointment && appointment.status === "Scheduled" && (
                          <Button
                            variant="secondary"
                            onClick={() => openRescheduleModal(appointment)}
                            disabled={actionId === appointment.appointmentId}
                            className="border-sky-200 text-sky-700 hover:bg-sky-50"
                          >
                            Đổi lịch
                          </Button>
                        )}

                        {appointment.status !== "Completed" && appointment.status !== "Cancelled" && (
                          <Button
                            variant="secondary"
                            onClick={() => void handleComplete(appointment)}
                            disabled={actionId === appointment.appointmentId}
                            className="border-emerald-200 text-emerald-700 hover:bg-emerald-50"
                          >
                            Hoàn thành
                          </Button>
                        )}

                        {canManageAppointment && appointment.status === "Scheduled" && (
                          <Button
                            variant="secondary"
                            onClick={() => openCancelModal(appointment)}
                            disabled={actionId === appointment.appointmentId}
                            className="border-rose-200 text-rose-700 hover:bg-rose-50"
                          >
                            Hủy lịch
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="flex flex-col gap-3 border-t border-slate-100 px-6 py-4 md:flex-row md:items-center md:justify-between">
          <p className="text-sm text-slate-500">
            Trang {pageNumber}/{totalPages}
          </p>

          <div className="flex items-center gap-2">
            <Button variant="secondary" onClick={() => setPageNumber(1)} disabled={pageNumber === 1}>
              Đầu
            </Button>
            <Button
              variant="secondary"
              onClick={() => setPageNumber((current) => current - 1)}
              disabled={pageNumber === 1}
            >
              Trước
            </Button>
            <Button
              variant="secondary"
              onClick={() => setPageNumber((current) => current + 1)}
              disabled={pageNumber >= totalPages}
            >
              Sau
            </Button>
          </div>
        </div>
      </Card>

      <Modal isOpen={isCheckInModalOpen} onClose={resetCheckInModal} title="Check-in bệnh nhân">
        <form onSubmit={handleCheckIn} className="space-y-5">
          <ModalAppointmentSummary appointment={selectedAppointment} />

          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              Quầy tiếp nhận
            </label>
            <input
              type="text"
              value={counterLabel}
              onChange={(event) => setCounterLabel(event.target.value)}
              placeholder="Ví dụ: Quầy 1"
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            />
          </div>

          <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
            <Button type="button" variant="secondary" onClick={resetCheckInModal}>
              Đóng
            </Button>
            <Button type="submit" disabled={!selectedAppointment || actionId === selectedAppointment.appointmentId}>
              {actionId === selectedAppointment?.appointmentId ? "Đang xử lý..." : "Xác nhận check-in"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={isCancelModalOpen} onClose={resetCancelModal} title="Hủy lịch hẹn">
        <form onSubmit={handleCancel} className="space-y-5">
          <ModalAppointmentSummary appointment={selectedAppointment} />

          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              Lý do hủy
            </label>
            <textarea
              value={cancelReason}
              onChange={(event) => setCancelReason(event.target.value)}
              rows={4}
              placeholder="Ví dụ: bệnh nhân xin dời sang ngày khác"
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            />
          </div>

          <div className="rounded-2xl border border-rose-100 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            Chính sách hiện tại: chỉ hủy được lịch đang Scheduled. Lịch đã check-in sẽ phải xử lý tiếp ở quầy thay vì hủy trực tiếp.
          </div>

          <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
            <Button type="button" variant="secondary" onClick={resetCancelModal}>
              Đóng
            </Button>
            <Button type="submit" disabled={!selectedAppointment || actionId === selectedAppointment.appointmentId}>
              {actionId === selectedAppointment?.appointmentId ? "Đang xử lý..." : "Xác nhận hủy lịch"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={isRescheduleModalOpen} onClose={resetRescheduleModal} title="Đổi lịch hẹn">
        <form onSubmit={handleReschedule} className="space-y-5">
          <ModalAppointmentSummary appointment={selectedAppointment} />

          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                Ngày mới
              </label>
              <input
                type="date"
                value={rescheduleDate}
                onChange={(event) => setRescheduleDate(event.target.value)}
                className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
              />
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                Giờ mới
              </label>
              <input
                type="time"
                value={rescheduleTime}
                onChange={(event) => setRescheduleTime(event.target.value)}
                className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
              />
            </div>
          </div>

          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              Lý do đổi lịch
            </label>
            <textarea
              value={rescheduleReason}
              onChange={(event) => setRescheduleReason(event.target.value)}
              rows={4}
              placeholder="Ví dụ: bệnh nhân xin đổi sang buổi chiều"
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            />
          </div>

          <div className="rounded-2xl border border-sky-100 bg-sky-50 px-4 py-3 text-sm text-sky-700">
            Chính sách hiện tại: chỉ đổi được lịch đang Scheduled, giữ nguyên bác sĩ, phải khớp lịch làm việc và không trùng slot hiện có.
          </div>

          <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
            <Button type="button" variant="secondary" onClick={resetRescheduleModal}>
              Đóng
            </Button>
            <Button
              type="submit"
              disabled={
                !selectedAppointment ||
                !rescheduleDate ||
                !rescheduleTime ||
                actionId === selectedAppointment.appointmentId
              }
            >
              {actionId === selectedAppointment?.appointmentId ? "Đang xử lý..." : "Xác nhận đổi lịch"}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

function ModalAppointmentSummary({
  appointment,
}: {
  appointment: HospitalAppointmentWorklistItem | null;
}) {
  return (
    <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
      <p className="text-sm font-semibold text-slate-900">
        {appointment?.patientName}
      </p>
      <p className="mt-1 text-sm text-slate-500">
        {appointment?.appointmentNumber} / {appointment?.doctorName}
      </p>
      <p className="mt-1 text-xs text-slate-400">
        {formatDateTime(appointment?.appointmentStartLocal)}
      </p>
    </div>
  );
}

function MetricCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone: "cyan" | "amber" | "emerald" | "rose";
}) {
  const toneClasses = {
    cyan: "border-cyan-100 bg-cyan-50/70 text-cyan-700",
    amber: "border-amber-100 bg-amber-50/70 text-amber-700",
    emerald: "border-emerald-100 bg-emerald-50/70 text-emerald-700",
    rose: "border-rose-100 bg-rose-50/70 text-rose-700",
  };

  return (
    <Card className={`border p-5 shadow-sm hover:shadow-sm ${toneClasses[tone]}`}>
      <p className="text-xs font-bold uppercase tracking-[0.2em]">{label}</p>
      <p className="mt-3 text-3xl font-bold text-slate-950">{value}</p>
    </Card>
  );
}
