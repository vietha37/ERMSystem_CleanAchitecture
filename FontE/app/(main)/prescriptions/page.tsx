"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { Modal } from "@/components/ui/Modal";
import { useAuth } from "@/hooks/useAuth";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalPrescriptionService } from "@/services/hospitalPrescriptionService";
import {
  CreateHospitalPrescriptionItemPayload,
  HospitalMedicineCatalog,
  HospitalPrescriptionDetail,
  HospitalPrescriptionEligibleEncounter,
  HospitalPrescriptionStatus,
  HospitalPrescriptionSummary,
} from "@/services/types";
import toast from "react-hot-toast";

const STATUS_OPTIONS: Array<{
  value: HospitalPrescriptionStatus | "All";
  label: string;
}> = [
  { value: "All", label: "Tất cả" },
  { value: "Issued", label: "Đã phát hành" },
  { value: "Dispensed", label: "Đã cấp thuốc" },
  { value: "Cancelled", label: "Đã hủy" },
];

type PrescriptionItemForm = {
  medicineId: string;
  doseInstruction: string;
  route: string;
  frequency: string;
  durationDays: string;
  quantity: string;
};

const EMPTY_ITEM: PrescriptionItemForm = {
  medicineId: "",
  doseInstruction: "",
  route: "",
  frequency: "",
  durationDays: "",
  quantity: "1",
};

function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

function getStatusLabel(status: HospitalPrescriptionStatus): string {
  switch (status) {
    case "Issued":
      return "Đã phát hành";
    case "Dispensed":
      return "Đã cấp thuốc";
    case "Cancelled":
      return "Đã hủy";
    default:
      return status;
  }
}

function getStatusClass(status: HospitalPrescriptionStatus): string {
  switch (status) {
    case "Issued":
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
    case "Dispensed":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Cancelled":
      return "border border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

function buildCreateItems(
  items: PrescriptionItemForm[]
): CreateHospitalPrescriptionItemPayload[] {
  return items
    .filter((item) => item.medicineId && item.doseInstruction.trim() && item.quantity.trim())
    .map((item) => ({
      medicineId: item.medicineId,
      doseInstruction: item.doseInstruction.trim(),
      route: item.route.trim() || undefined,
      frequency: item.frequency.trim() || undefined,
      durationDays: item.durationDays.trim() ? Number(item.durationDays) : undefined,
      quantity: Number(item.quantity),
    }));
}

export default function PrescriptionsPage() {
  const { role } = useAuth();
  const [prescriptions, setPrescriptions] = useState<HospitalPrescriptionSummary[]>([]);
  const [eligibleEncounters, setEligibleEncounters] = useState<
    HospitalPrescriptionEligibleEncounter[]
  >([]);
  const [medicines, setMedicines] = useState<HospitalMedicineCatalog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<HospitalPrescriptionStatus | "All">(
    "All"
  );
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [selectedPrescription, setSelectedPrescription] =
    useState<HospitalPrescriptionDetail | null>(null);
  const [selectedEncounterId, setSelectedEncounterId] = useState("");
  const [notes, setNotes] = useState("");
  const [prescriptionItems, setPrescriptionItems] = useState<PrescriptionItemForm[]>([
    EMPTY_ITEM,
  ]);

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
        const [worklist, encounters, catalog] = await Promise.all([
          hospitalPrescriptionService.getAll({
            pageNumber,
            pageSize,
            status: statusFilter,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalPrescriptionService.getEligibleEncounters(),
          hospitalPrescriptionService.getMedicineCatalog(),
        ]);

        setPrescriptions(worklist.items);
        setTotalCount(worklist.totalCount);
        setEligibleEncounters(encounters);
        setMedicines(catalog);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, "Không thể tải dữ liệu đơn thuốc.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [debouncedSearch, pageNumber, pageSize, statusFilter]
  );

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const metrics = useMemo(
    () =>
      prescriptions.reduce(
        (acc, item) => {
          acc[item.status] += 1;
          return acc;
        },
        {
          Issued: 0,
          Dispensed: 0,
          Cancelled: 0,
        } as Record<HospitalPrescriptionStatus, number>
      ),
    [prescriptions]
  );

  const availableEncounters = eligibleEncounters.filter((item) => !item.existingPrescriptionId);
  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endItem = totalCount === 0 ? 0 : Math.min(pageNumber * pageSize, totalCount);

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

  const handleDelete = async (prescriptionId: string) => {
    if (!window.confirm("Bạn có chắc chắn muốn xóa đơn thuốc này?")) return;
    try {
      await hospitalPrescriptionService.delete(prescriptionId);
      toast.success("Đã xóa đơn thuốc.");
      await fetchData(true);
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
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tạo đơn thuốc."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <div className="rounded-3xl border border-sky-100 bg-gradient-to-br from-cyan-50 via-white to-blue-50 p-6 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.25em] text-cyan-700">
              Quản lý dược
            </p>
            <h1 className="mt-3 text-3xl font-bold text-slate-900">Đơn thuốc bệnh viện</h1>
            <p className="mt-2 text-sm text-slate-600">
              Phát hành, theo dõi cấp phát và quản lý danh mục đơn thuốc khám chữa bệnh.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <Button
              variant="secondary"
              onClick={() => void fetchData(true)}
              disabled={isRefreshing}
            >
              {isRefreshing ? "Đang làm mới..." : "Làm mới"}
            </Button>
            <Button onClick={() => setIsCreateModalOpen(true)}>Phát hành đơn thuốc</Button>
          </div>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        <MetricCard label="Đã phát hành" value={metrics.Issued} tone="cyan" />
        <MetricCard label="Đã cấp thuốc" value={metrics.Dispensed} tone="emerald" />
        <MetricCard label="Đã hủy" value={metrics.Cancelled} tone="rose" />
        <MetricCard
          label="Hồ sơ chờ kê đơn"
          value={availableEncounters.length}
          tone="amber"
        />
      </div>

      <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
        <div className="flex flex-col gap-3 border-b border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-bold text-slate-900">Danh sách đơn thuốc</h2>
            <p className="mt-1 text-sm text-slate-500">
              Hiển thị {startItem}-{endItem} / {totalCount} đơn thuốc.
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
          <LoadingState title="Đang tải đơn thuốc..." tone="cyan" />
        ) : listError ? (
          <ErrorState
            title="Không thể tải dữ liệu đơn thuốc"
            description={listError}
            onAction={() => void fetchData(true)}
          />
        ) : prescriptions.length === 0 ? (
          <EmptyState
            title="Chưa có đơn thuốc nào khớp bộ lọc hiện tại."
            description="Thay đổi bộ lọc hoặc tạo đơn thuốc mới."
            tone="cyan"
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-[1260px] w-full border-collapse text-left">
              <thead>
                <tr className="bg-slate-50">
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Đơn thuốc
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
                    Ngày tạo
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Tác vụ
                  </th>
                </tr>
              </thead>
              <tbody>
                {prescriptions.map((prescription) => (
                  <tr
                    key={prescription.prescriptionId}
                    className="border-t border-slate-100 align-top transition-colors hover:bg-cyan-50/30"
                  >
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">
                        {prescription.prescriptionNumber}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {prescription.encounterNumber}
                      </div>
                      <div className="mt-1 text-xs text-slate-400">
                        {prescription.totalItems} thuốc
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">
                        {prescription.patientName}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {prescription.medicalRecordNumber}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">
                        {prescription.doctorName}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {prescription.specialtyName}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">{prescription.clinicName}</div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-cyan-700">
                        {prescription.primaryDiagnosisName || "--"}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {prescription.notes || "Không có ghi chú thêm."}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getStatusClass(
                          prescription.status
                        )}`}
                      >
                        {getStatusLabel(prescription.status)}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      {formatDateTime(prescription.createdAtLocal)}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap gap-2">
                        <Button
                          variant="secondary"
                          className="border-cyan-200 text-cyan-700 hover:bg-cyan-50"
                          onClick={() => void handleOpenDetail(prescription.prescriptionId)}
                        >
                          Xem
                        </Button>
                        {role !== "Doctor" && (
                          <Button
                            variant="secondary"
                            className="border-rose-200 text-rose-700 hover:bg-rose-50"
                            onClick={() => void handleDelete(prescription.prescriptionId)}
                          >
                            Xóa
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
        isOpen={isCreateModalOpen}
        onClose={() => {
          setIsCreateModalOpen(false);
          resetCreateForm();
        }}
        title="Phát hành đơn thuốc"
      >
        <form onSubmit={handleCreatePrescription} className="space-y-5">
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              Chọn hồ sơ khám
            </label>
            <select
              value={selectedEncounterId}
              onChange={(event) => setSelectedEncounterId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
              required
            >
              <option value="">-- Chọn hồ sơ khám --</option>
              {availableEncounters.map((encounter) => (
                <option key={encounter.encounterId} value={encounter.encounterId}>
                  {encounter.encounterNumber} - {encounter.patientName} -{" "}
                  {encounter.primaryDiagnosisName || "Chưa có chẩn đoán"}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">Ghi chú</label>
            <textarea
              rows={3}
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
              placeholder="Lưu ý sử dụng thuốc, hướng dẫn bổ sung..."
            />
          </div>

          <div className="space-y-4 border-t border-slate-100 pt-5">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-bold text-slate-800">Danh sách thuốc</h3>
              <Button type="button" variant="secondary" onClick={handleAddRow}>
                Thêm dòng
              </Button>
            </div>

            {prescriptionItems.map((item, index) => (
              <div
                key={index}
                className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/70 p-4"
              >
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="md:col-span-2">
                    <label className="mb-2 block text-sm font-medium text-slate-700">
                      Thuốc
                    </label>
                    <select
                      value={item.medicineId}
                      onChange={(event) =>
                        handleUpdateRow(index, "medicineId", event.target.value)
                      }
                      className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                      required
                    >
                      <option value="">-- Chọn thuốc --</option>
                      {medicines.map((medicine) => (
                        <option key={medicine.medicineId} value={medicine.medicineId}>
                          {medicine.name} ({medicine.drugCode}){" "}
                          {medicine.strength ? `- ${medicine.strength}` : ""}
                        </option>
                      ))}
                    </select>
                  </div>

                  <InputField
                    label="Liều dùng"
                    value={item.doseInstruction}
                    onChange={(value) => handleUpdateRow(index, "doseInstruction", value)}
                    placeholder="1 viên/lần"
                  />
                  <InputField
                    label="Đường dùng"
                    value={item.route}
                    onChange={(value) => handleUpdateRow(index, "route", value)}
                    placeholder="Uống"
                  />
                  <InputField
                    label="Tần suất"
                    value={item.frequency}
                    onChange={(value) => handleUpdateRow(index, "frequency", value)}
                    placeholder="Ngày 2 lần"
                  />
                  <InputField
                    label="Số ngày"
                    value={item.durationDays}
                    onChange={(value) => handleUpdateRow(index, "durationDays", value)}
                    placeholder="5"
                    type="number"
                  />
                  <InputField
                    label="Số lượng"
                    value={item.quantity}
                    onChange={(value) => handleUpdateRow(index, "quantity", value)}
                    placeholder="10"
                    type="number"
                  />
                </div>

                <div className="mt-4 flex justify-end">
                  <Button
                    type="button"
                    variant="secondary"
                    className="border-rose-200 text-rose-700 hover:bg-rose-50"
                    onClick={() => handleRemoveRow(index)}
                    disabled={prescriptionItems.length === 1}
                  >
                    Xóa dòng
                  </Button>
                </div>
              </div>
            ))}
          </div>

          <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setIsCreateModalOpen(false);
                resetCreateForm();
              }}
            >
              Đóng
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Đang phát hành..." : "Xác nhận đơn thuốc"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        isOpen={isDetailModalOpen}
        onClose={() => {
          setIsDetailModalOpen(false);
          setSelectedPrescription(null);
        }}
        title="Chi tiết đơn thuốc"
      >
        {selectedPrescription ? (
          <div className="space-y-5">
            <div className="grid gap-3 rounded-2xl border border-cyan-100 bg-cyan-50/70 p-4 text-sm md:grid-cols-2">
              <InfoLine label="Đơn thuốc" value={selectedPrescription.prescriptionNumber} />
              <InfoLine label="Hồ sơ khám" value={selectedPrescription.encounterNumber} />
              <InfoLine label="Bệnh nhân" value={selectedPrescription.patientName} />
              <InfoLine label="Bác sĩ" value={selectedPrescription.doctorName} />
              <InfoLine
                label="Chẩn đoán"
                value={selectedPrescription.primaryDiagnosisName || "--"}
              />
              <InfoLine
                label="Ngày tạo"
                value={formatDateTime(selectedPrescription.createdAtLocal)}
              />
            </div>

            {(selectedPrescription.warningDetails.length > 0 || selectedPrescription.warnings.length > 0) && (
              <div className="rounded-2xl border border-amber-200 bg-amber-50/80 p-4">
                <p className="text-sm font-bold text-amber-900">Cảnh báo lâm sàng</p>
                <div className="mt-3 space-y-2">
                  {selectedPrescription.warningDetails.length > 0
                    ? selectedPrescription.warningDetails.map((warning) => (
                        <div
                          key={warning.code}
                          className="rounded-2xl border border-amber-100 bg-white/80 px-4 py-3 text-sm leading-6 text-amber-900"
                        >
                          <div className="flex flex-wrap items-center gap-2">
                            <span
                              className={`rounded-full px-2.5 py-1 text-[11px] font-bold uppercase tracking-[0.16em] ${
                                warning.severity === "critical"
                                  ? "bg-red-100 text-red-700"
                                  : "bg-amber-100 text-amber-700"
                              }`}
                            >
                              {warning.severity === "critical" ? "Nghiêm trọng" : "Cảnh báo"}
                            </span>
                            <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.14em] text-slate-600">
                              {warning.category}
                            </span>
                          </div>
                          <p className="mt-3">{warning.message}</p>
                          {warning.relatedMedicines.length > 0 && (
                            <p className="mt-2 text-xs text-amber-800">
                              Thuốc liên quan: {warning.relatedMedicines.join(", ")}
                            </p>
                          )}
                          {warning.recommendation && (
                            <p className="mt-2 text-xs text-slate-600">
                              Khuyến nghị: {warning.recommendation}
                            </p>
                          )}
                        </div>
                      ))
                    : selectedPrescription.warnings.map((warning, index) => (
                        <div
                          key={`${index}-${warning}`}
                          className="rounded-2xl border border-amber-100 bg-white/80 px-4 py-3 text-sm leading-6 text-amber-900"
                        >
                          {warning}
                        </div>
                      ))}
                </div>
              </div>
            )}

            <div className="overflow-hidden rounded-2xl border border-slate-100">
              <table className="w-full border-collapse text-left text-sm">
                <thead className="bg-slate-50">
                  <tr>
                    <th className="px-4 py-3 font-semibold text-slate-700">Thuốc</th>
                    <th className="px-4 py-3 font-semibold text-slate-700">Liều dùng</th>
                    <th className="px-4 py-3 font-semibold text-slate-700">Tần suất</th>
                    <th className="px-4 py-3 font-semibold text-slate-700">Số lượng</th>
                  </tr>
                </thead>
                <tbody>
                  {selectedPrescription.items.map((item) => (
                    <tr key={item.prescriptionItemId} className="border-t border-slate-100">
                      <td className="px-4 py-3">
                        <div className="font-semibold text-slate-900">{item.medicineName}</div>
                        <div className="mt-1 text-xs text-slate-500">
                          {item.drugCode}
                          {item.strength ? ` / ${item.strength}` : ""}
                          {item.dosageForm ? ` / ${item.dosageForm}` : ""}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-slate-600">{item.doseInstruction}</td>
                      <td className="px-4 py-3 text-slate-600">
                        {[
                          item.route,
                          item.frequency,
                          item.durationDays ? `${item.durationDays} ngày` : null,
                        ]
                          .filter(Boolean)
                          .join(" / ") || "--"}
                      </td>
                      <td className="px-4 py-3 text-slate-600">
                        {item.quantity} {item.unit || ""}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="flex justify-end">
              <Button
                variant="secondary"
                onClick={() => {
                  setIsDetailModalOpen(false);
                  setSelectedPrescription(null);
                }}
              >
                Đóng
              </Button>
            </div>
          </div>
        ) : null}
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
  tone: "cyan" | "emerald" | "rose" | "amber";
}) {
  const toneClasses = {
    cyan: "border-cyan-100 bg-cyan-50/70 text-cyan-700",
    emerald: "border-emerald-100 bg-emerald-50/70 text-emerald-700",
    rose: "border-rose-100 bg-rose-50/70 text-rose-700",
    amber: "border-amber-100 bg-amber-50/70 text-amber-700",
  };

  return (
    <Card className={`border p-5 shadow-sm hover:shadow-sm ${toneClasses[tone]}`}>
      <p className="text-xs font-bold uppercase tracking-[0.2em]">{label}</p>
      <p className="mt-3 text-3xl font-bold text-slate-950">{value}</p>
    </Card>
  );
}

function InputField({
  label,
  value,
  onChange,
  placeholder,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  type?: "text" | "number";
}) {
  return (
    <div>
      <label className="mb-2 block text-sm font-medium text-slate-700">{label}</label>
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
      />
    </div>
  );
}

function InfoLine({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-slate-500">{label}</p>
      <p className="mt-1 font-semibold text-slate-900">{value}</p>
    </div>
  );
}
