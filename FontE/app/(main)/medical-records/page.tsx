"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Modal } from "@/components/ui/Modal";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalEncounterService } from "@/services/hospitalEncounterService";
import {
  CreateHospitalEncounterPayload,
  HospitalEncounterDetail,
  HospitalEncounterEligibleAppointment,
  HospitalEncounterStatus,
  HospitalEncounterSummary,
  UpdateHospitalEncounterPayload,
} from "@/services/types";
import toast from "react-hot-toast";

const ENCOUNTER_STATUS_OPTIONS: Array<{
  value: HospitalEncounterStatus | "All";
  label: string;
}> = [
  { value: "All", label: "Tất cả" },
  { value: "InProgress", label: "Đang khám" },
  { value: "Finalized", label: "Đã chốt hồ sơ" },
];

type EncounterFormState = {
  appointmentId: string;
  diagnosisName: string;
  diagnosisCode: string;
  diagnosisType: string;
  encounterStatus: HospitalEncounterStatus;
  summary: string;
  subjective: string;
  objective: string;
  assessment: string;
  carePlan: string;
  heightCm: string;
  weightKg: string;
  temperatureC: string;
  pulseRate: string;
  respiratoryRate: string;
  systolicBp: string;
  diastolicBp: string;
  oxygenSaturation: string;
};

const EMPTY_FORM: EncounterFormState = {
  appointmentId: "",
  diagnosisName: "",
  diagnosisCode: "",
  diagnosisType: "Working",
  encounterStatus: "InProgress",
  summary: "",
  subjective: "",
  objective: "",
  assessment: "",
  carePlan: "",
  heightCm: "",
  weightKg: "",
  temperatureC: "",
  pulseRate: "",
  respiratoryRate: "",
  systolicBp: "",
  diastolicBp: "",
  oxygenSaturation: "",
};

function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

function getEncounterStatusLabel(status: HospitalEncounterStatus): string {
  switch (status) {
    case "InProgress":
      return "Đang khám";
    case "Finalized":
      return "Đã chốt hồ sơ";
    default:
      return status;
  }
}

function getEncounterStatusClass(status: HospitalEncounterStatus): string {
  switch (status) {
    case "InProgress":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "Finalized":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

function parseOptionalNumber(value: string): number | null {
  if (!value.trim()) {
    return null;
  }

  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

function buildPayload(form: EncounterFormState): CreateHospitalEncounterPayload {
  return {
    appointmentId: form.appointmentId,
    diagnosisName: form.diagnosisName.trim(),
    diagnosisCode: form.diagnosisCode.trim() || undefined,
    diagnosisType: form.diagnosisType.trim() || "Working",
    encounterStatus: form.encounterStatus,
    summary: form.summary.trim() || undefined,
    subjective: form.subjective.trim() || undefined,
    objective: form.objective.trim() || undefined,
    assessment: form.assessment.trim() || undefined,
    carePlan: form.carePlan.trim() || undefined,
    heightCm: parseOptionalNumber(form.heightCm),
    weightKg: parseOptionalNumber(form.weightKg),
    temperatureC: parseOptionalNumber(form.temperatureC),
    pulseRate: parseOptionalNumber(form.pulseRate),
    respiratoryRate: parseOptionalNumber(form.respiratoryRate),
    systolicBp: parseOptionalNumber(form.systolicBp),
    diastolicBp: parseOptionalNumber(form.diastolicBp),
    oxygenSaturation: parseOptionalNumber(form.oxygenSaturation),
  };
}

function buildUpdatePayload(form: EncounterFormState): UpdateHospitalEncounterPayload {
  const payload = buildPayload(form);

  return {
    diagnosisName: payload.diagnosisName,
    diagnosisCode: payload.diagnosisCode,
    diagnosisType: payload.diagnosisType,
    encounterStatus: payload.encounterStatus,
    summary: payload.summary,
    subjective: payload.subjective,
    objective: payload.objective,
    assessment: payload.assessment,
    carePlan: payload.carePlan,
    heightCm: payload.heightCm,
    weightKg: payload.weightKg,
    temperatureC: payload.temperatureC,
    pulseRate: payload.pulseRate,
    respiratoryRate: payload.respiratoryRate,
    systolicBp: payload.systolicBp,
    diastolicBp: payload.diastolicBp,
    oxygenSaturation: payload.oxygenSaturation,
  };
}

function mapDetailToForm(detail: HospitalEncounterDetail): EncounterFormState {
  return {
    appointmentId: detail.appointmentId ?? "",
    diagnosisName: detail.primaryDiagnosisName ?? "",
    diagnosisCode: detail.diagnosisCode ?? "",
    diagnosisType: detail.diagnosisType ?? "Working",
    encounterStatus: detail.encounterStatus,
    summary: detail.summary ?? "",
    subjective: detail.subjective ?? "",
    objective: detail.objective ?? "",
    assessment: detail.assessment ?? "",
    carePlan: detail.carePlan ?? "",
    heightCm: detail.heightCm?.toString() ?? "",
    weightKg: detail.weightKg?.toString() ?? "",
    temperatureC: detail.temperatureC?.toString() ?? "",
    pulseRate: detail.pulseRate?.toString() ?? "",
    respiratoryRate: detail.respiratoryRate?.toString() ?? "",
    systolicBp: detail.systolicBp?.toString() ?? "",
    diastolicBp: detail.diastolicBp?.toString() ?? "",
    oxygenSaturation: detail.oxygenSaturation?.toString() ?? "",
  };
}

export default function MedicalRecordsPage() {
  const [encounters, setEncounters] = useState<HospitalEncounterSummary[]>([]);
  const [eligibleAppointments, setEligibleAppointments] = useState<
    HospitalEncounterEligibleAppointment[]
  >([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<HospitalEncounterStatus | "All">(
    "All"
  );
  const [appointmentDate, setAppointmentDate] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEncounterId, setEditingEncounterId] = useState<string | null>(null);
  const [form, setForm] = useState<EncounterFormState>(EMPTY_FORM);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(searchQuery.trim());
      setPageNumber(1);
    }, 400);

    return () => window.clearTimeout(timer);
  }, [searchQuery]);

  const fetchData = useCallback(
    async (showRefreshState = false) => {
      if (showRefreshState) {
        setIsRefreshing(true);
      } else {
        setIsLoading(true);
      }

      try {
        const [worklist, appointments] = await Promise.all([
          hospitalEncounterService.getAll({
            pageNumber,
            pageSize,
            encounterStatus: statusFilter,
            appointmentDate: appointmentDate || undefined,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalEncounterService.getEligibleAppointments(),
        ]);

        setEncounters(worklist.items);
        setTotalCount(worklist.totalCount);
        setEligibleAppointments(appointments);
      } catch (error: unknown) {
        toast.error(getApiErrorMessage(error, "Không thể tải dữ liệu hồ sơ khám."));
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [appointmentDate, debouncedSearch, pageNumber, pageSize, statusFilter]
  );

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const metrics = useMemo(
    () =>
      encounters.reduce(
        (acc, item) => {
          acc[item.encounterStatus] += 1;
          return acc;
        },
        {
          InProgress: 0,
          Finalized: 0,
        } as Record<HospitalEncounterStatus, number>
      ),
    [encounters]
  );

  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endItem = totalCount === 0 ? 0 : Math.min(pageNumber * pageSize, totalCount);

  const availableAppointments = eligibleAppointments.filter(
    (item) => !item.existingEncounterId || item.existingEncounterId === editingEncounterId
  );

  const openCreateModal = () => {
    setEditingEncounterId(null);
    setForm(EMPTY_FORM);
    setIsModalOpen(true);
  };

  const openEditModal = async (encounterId: string) => {
    try {
      const detail = await hospitalEncounterService.getById(encounterId);
      setEditingEncounterId(encounterId);
      setForm(mapDetailToForm(detail));
      setIsModalOpen(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải chi tiết hồ sơ khám."));
    }
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
        await hospitalEncounterService.update(editingEncounterId, buildUpdatePayload(form));
        toast.success("Đã cập nhật hồ sơ khám.");
      } else {
        await hospitalEncounterService.create(buildPayload(form));
        toast.success("Đã tạo hồ sơ khám mới.");
      }

      setIsModalOpen(false);
      setEditingEncounterId(null);
      setForm(EMPTY_FORM);
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể lưu hồ sơ khám."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <div className="rounded-[2rem] border border-emerald-100 bg-gradient-to-br from-emerald-50 via-white to-cyan-50 p-6 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.26em] text-emerald-700">
              Hồ sơ lâm sàng
            </p>
            <h1 className="mt-3 text-3xl font-bold text-slate-950">
              Hồ sơ khám bệnh hospital
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-7 text-slate-600">
              Module này đã chuyển sang hospital database mới. Mỗi hồ sơ được lưu
              theo mô hình hồ sơ khám, gồm chẩn đoán, ghi chú lâm sàng và dấu hiệu sinh tồn.
            </p>
          </div>

          <div className="flex flex-col gap-3 md:flex-row md:items-center">
            <input
              type="text"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Tìm theo mã hồ sơ, mã lịch, bệnh nhân..."
              className="min-w-[260px] rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            />

            <input
              type="date"
              value={appointmentDate}
              onChange={(event) => {
                setAppointmentDate(event.target.value);
                setPageNumber(1);
              }}
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            />

            <select
              value={statusFilter}
              onChange={(event) => {
                setStatusFilter(event.target.value as HospitalEncounterStatus | "All");
                setPageNumber(1);
              }}
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            >
              {ENCOUNTER_STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>

            <Button
              variant="secondary"
              onClick={() => void fetchData(true)}
              disabled={isRefreshing}
            >
              {isRefreshing ? "Đang làm mới..." : "Làm mới"}
            </Button>
            <Button onClick={openCreateModal}>Mở hồ sơ khám</Button>
          </div>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <MetricCard label="Đang khám" value={metrics.InProgress} tone="amber" />
        <MetricCard label="Đã chốt hồ sơ" value={metrics.Finalized} tone="emerald" />
        <MetricCard
          label="Lịch chờ mở hồ sơ"
          value={availableAppointments.length}
          tone="cyan"
        />
      </div>

      <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
        <div className="flex flex-col gap-3 border-b border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-bold text-slate-900">Danh sách hồ sơ khám</h2>
            <p className="mt-1 text-sm text-slate-500">
              Hiển thị {startItem}-{endItem} / {totalCount} hồ sơ.
            </p>
          </div>

          <select
            value={pageSize}
            onChange={(event) => {
              setPageSize(Number(event.target.value));
              setPageNumber(1);
            }}
            className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          >
            <option value={10}>10 dòng / trang</option>
            <option value={20}>20 dòng / trang</option>
            <option value={50}>50 dòng / trang</option>
          </select>
        </div>

        {isLoading ? (
          <div className="flex flex-col items-center justify-center p-16">
            <div className="mb-4 h-10 w-10 animate-spin rounded-full border-4 border-emerald-100 border-t-emerald-600" />
            <p className="text-sm font-medium text-slate-500">Đang tải hồ sơ khám...</p>
          </div>
        ) : encounters.length === 0 ? (
          <div className="p-16 text-center text-sm text-slate-500">
            Chưa có hồ sơ khám nào khớp bộ lọc hiện tại.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-[1260px] w-full border-collapse text-left">
              <thead>
                <tr className="bg-slate-50">
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Hồ sơ khám
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Bệnh nhân
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Bác sĩ
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Chẩn đoán
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Trạng thái
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Cập nhật
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Tác vụ
                  </th>
                </tr>
              </thead>
              <tbody>
                {encounters.map((encounter) => (
                  <tr
                    key={encounter.encounterId}
                    className="border-t border-slate-100 align-top transition-colors hover:bg-emerald-50/30"
                  >
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">
                        {encounter.encounterNumber}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {encounter.appointmentNumber || "--"}
                      </div>
                      <div className="mt-1 text-xs text-slate-400">
                        Bắt đầu: {formatDateTime(encounter.startedAtLocal)}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{encounter.patientName}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        {encounter.medicalRecordNumber}
                      </div>
                      <div className="mt-1 text-xs text-slate-400">
                        Lịch khám: {formatDateTime(encounter.appointmentStartLocal)}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{encounter.doctorName}</div>
                      <div className="mt-1 text-sm text-slate-500">{encounter.specialtyName}</div>
                      <div className="mt-1 text-sm text-slate-500">{encounter.clinicName}</div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-emerald-700">
                        {encounter.primaryDiagnosisName || "--"}
                      </div>
                      <div className="mt-1 max-w-[280px] text-sm text-slate-500">
                        {encounter.summary || "Chưa có tóm tắt bệnh án."}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getEncounterStatusClass(
                          encounter.encounterStatus
                        )}`}
                      >
                        {getEncounterStatusLabel(encounter.encounterStatus)}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      <div>{formatDateTime(encounter.updatedAtLocal)}</div>
                      <div className="mt-1 text-xs text-slate-400">
                        Kết thúc: {formatDateTime(encounter.endedAtLocal)}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <Button
                        variant="secondary"
                        className="border-emerald-200 text-emerald-700 hover:bg-emerald-50"
                        onClick={() => void openEditModal(encounter.encounterId)}
                      >
                        Cập nhật
                      </Button>
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
            <Button
              variant="secondary"
              onClick={() => setPageNumber(1)}
              disabled={pageNumber === 1}
            >
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

      <Modal
        isOpen={isModalOpen}
        onClose={() => {
          setIsModalOpen(false);
          setEditingEncounterId(null);
          setForm(EMPTY_FORM);
        }}
        title={editingEncounterId ? "Cập nhật hồ sơ khám" : "Mở hồ sơ khám mới"}
      >
        <form onSubmit={handleSubmit} className="space-y-5">
          {!editingEncounterId && (
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                Lịch hẹn đã check-in / đã hoàn thành
              </label>
              <select
                value={form.appointmentId}
                onChange={(event) =>
                  setForm((current) => ({ ...current, appointmentId: event.target.value }))
                }
                className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
                required
              >
                <option value="">-- Chọn lịch hẹn --</option>
                {availableAppointments.map((appointment) => (
                  <option key={appointment.appointmentId} value={appointment.appointmentId}>
                    {appointment.appointmentNumber} - {appointment.patientName} -{" "}
                    {formatDateTime(appointment.appointmentStartLocal)}
                  </option>
                ))}
              </select>
            </div>
          )}

          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                Chẩn đoán
              </label>
              <input
                type="text"
                value={form.diagnosisName}
                onChange={(event) =>
                  setForm((current) => ({ ...current, diagnosisName: event.target.value }))
                }
                className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
                placeholder="Ví dụ: Tăng huyết áp"
                required
              />
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                Mã chẩn đoán
              </label>
              <input
                type="text"
                value={form.diagnosisCode}
                onChange={(event) =>
                  setForm((current) => ({ ...current, diagnosisCode: event.target.value }))
                }
                className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
                placeholder="ICD-10 nếu có"
              />
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                Loại chẩn đoán
              </label>
              <input
                type="text"
                value={form.diagnosisType}
                onChange={(event) =>
                  setForm((current) => ({ ...current, diagnosisType: event.target.value }))
                }
                className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
                placeholder="Working / Final"
              />
            </div>

            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                Trạng thái hồ sơ khám
              </label>
              <select
                value={form.encounterStatus}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    encounterStatus: event.target.value as HospitalEncounterStatus,
                  }))
                }
                className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
              >
                <option value="InProgress">Đang khám</option>
                <option value="Finalized">Đã chốt hồ sơ</option>
              </select>
            </div>
          </div>

          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              Tóm tắt hồ sơ
            </label>
            <textarea
              rows={3}
              value={form.summary}
              onChange={(event) =>
                setForm((current) => ({ ...current, summary: event.target.value }))
              }
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
              placeholder="Tóm tắt diễn biến và kết luận chung"
            />
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <TextAreaField
              label="Triệu chứng / Subjective"
              value={form.subjective}
              onChange={(value) => setForm((current) => ({ ...current, subjective: value }))}
            />
            <TextAreaField
              label="Khám thực thể / Objective"
              value={form.objective}
              onChange={(value) => setForm((current) => ({ ...current, objective: value }))}
            />
            <TextAreaField
              label="Đánh giá / Assessment"
              value={form.assessment}
              onChange={(value) => setForm((current) => ({ ...current, assessment: value }))}
            />
            <TextAreaField
              label="Hướng điều trị / Care plan"
              value={form.carePlan}
              onChange={(value) => setForm((current) => ({ ...current, carePlan: value }))}
            />
          </div>

          <div className="grid gap-4 md:grid-cols-4">
            <NumberField
              label="Chiều cao (cm)"
              value={form.heightCm}
              onChange={(value) => setForm((current) => ({ ...current, heightCm: value }))}
            />
            <NumberField
              label="Cân nặng (kg)"
              value={form.weightKg}
              onChange={(value) => setForm((current) => ({ ...current, weightKg: value }))}
            />
            <NumberField
              label="Nhiệt độ (C)"
              value={form.temperatureC}
              onChange={(value) =>
                setForm((current) => ({ ...current, temperatureC: value }))
              }
            />
            <NumberField
              label="Mạch"
              value={form.pulseRate}
              onChange={(value) => setForm((current) => ({ ...current, pulseRate: value }))}
            />
            <NumberField
              label="Nhịp thở"
              value={form.respiratoryRate}
              onChange={(value) =>
                setForm((current) => ({ ...current, respiratoryRate: value }))
              }
            />
            <NumberField
              label="HA tâm thu"
              value={form.systolicBp}
              onChange={(value) => setForm((current) => ({ ...current, systolicBp: value }))}
            />
            <NumberField
              label="HA tâm trương"
              value={form.diastolicBp}
              onChange={(value) =>
                setForm((current) => ({ ...current, diastolicBp: value }))
              }
            />
            <NumberField
              label="SpO2 (%)"
              value={form.oxygenSaturation}
              onChange={(value) =>
                setForm((current) => ({ ...current, oxygenSaturation: value }))
              }
            />
          </div>

          <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setIsModalOpen(false);
                setEditingEncounterId(null);
                setForm(EMPTY_FORM);
              }}
            >
              Đóng
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting
                ? "Đang lưu..."
                : editingEncounterId
                  ? "Cập nhật hồ sơ"
                  : "Tạo hồ sơ"}
            </Button>
          </div>
        </form>
      </Modal>
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
  tone: "amber" | "emerald" | "cyan";
}) {
  const toneClasses = {
    amber: "border-amber-100 bg-amber-50/70 text-amber-700",
    emerald: "border-emerald-100 bg-emerald-50/70 text-emerald-700",
    cyan: "border-cyan-100 bg-cyan-50/70 text-cyan-700",
  };

  return (
    <Card className={`border p-5 shadow-sm hover:shadow-sm ${toneClasses[tone]}`}>
      <p className="text-xs font-bold uppercase tracking-[0.2em]">{label}</p>
      <p className="mt-3 text-3xl font-bold text-slate-950">{value}</p>
    </Card>
  );
}

function TextAreaField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div>
      <label className="mb-2 block text-sm font-medium text-slate-700">{label}</label>
      <textarea
        rows={4}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
      />
    </div>
  );
}

function NumberField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div>
      <label className="mb-2 block text-sm font-medium text-slate-700">{label}</label>
      <input
        type="number"
        step="0.1"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
      />
    </div>
  );
}
