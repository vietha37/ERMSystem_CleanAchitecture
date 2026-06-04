"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { Modal } from "@/components/ui/Modal";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalClinicalOrderService } from "@/services/hospitalClinicalOrderService";
import {
  CreateHospitalClinicalOrderPayload,
  HospitalClinicalOrderCategory,
  HospitalClinicalOrderCatalogItem,
  HospitalClinicalOrderDetail,
  HospitalClinicalOrderEligibleEncounter,
  HospitalClinicalOrderStatus,
  HospitalClinicalOrderSummary,
  RecordHospitalLabResultItemPayload,
} from "@/services/types";
import toast from "react-hot-toast";

const CATEGORY_OPTIONS: Array<{
  value: HospitalClinicalOrderCategory | "All";
  label: string;
}> = [
  { value: "All", label: "Tất cả" },
  { value: "Lab", label: "Xét nghiệm" },
  { value: "Imaging", label: "Chẩn đoán hình ảnh" },
];

const STATUS_OPTIONS: Array<{
  value: HospitalClinicalOrderStatus | "All";
  label: string;
}> = [
  { value: "All", label: "Tất cả" },
  { value: "Requested", label: "Đang chờ kết quả" },
  { value: "Completed", label: "Đã hoàn thành" },
];

const EMPTY_LAB_RESULT_ITEM: RecordHospitalLabResultItemPayload = {
  analyteCode: "",
  analyteName: "",
  resultValue: "",
  unit: "",
  referenceRange: "",
  abnormalFlag: "",
};

function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

function getCategoryLabel(category: HospitalClinicalOrderCategory): string {
  return category === "Lab" ? "Xét nghiệm" : "Chẩn đoán hình ảnh";
}

function getCategoryClass(category: HospitalClinicalOrderCategory): string {
  return category === "Lab"
    ? "border border-cyan-200 bg-cyan-50 text-cyan-700"
    : "border border-violet-200 bg-violet-50 text-violet-700";
}

function getStatusLabel(status: HospitalClinicalOrderStatus): string {
  return status === "Requested" ? "Đang chờ kết quả" : "Đã hoàn thành";
}

function getStatusClass(status: HospitalClinicalOrderStatus): string {
  return status === "Requested"
    ? "border border-amber-200 bg-amber-50 text-amber-700"
    : "border border-emerald-200 bg-emerald-50 text-emerald-700";
}

function buildCreatePayload(
  encounterId: string,
  category: HospitalClinicalOrderCategory,
  serviceId: string,
  priorityCode: string
): CreateHospitalClinicalOrderPayload {
  return {
    encounterId,
    category,
    serviceId,
    priorityCode: category === "Lab" && priorityCode.trim() ? priorityCode.trim() : undefined,
  };
}

function buildLabResultItems(items: RecordHospitalLabResultItemPayload[]) {
  return items
    .filter((item) => item.analyteName.trim())
    .map((item) => ({
      analyteCode: item.analyteCode?.trim() || undefined,
      analyteName: item.analyteName.trim(),
      resultValue: item.resultValue?.trim() || undefined,
      unit: item.unit?.trim() || undefined,
      referenceRange: item.referenceRange?.trim() || undefined,
      abnormalFlag: item.abnormalFlag?.trim() || undefined,
    }));
}

export default function ClinicalOrdersPage() {
  const [orders, setOrders] = useState<HospitalClinicalOrderSummary[]>([]);
  const [eligibleEncounters, setEligibleEncounters] = useState<
    HospitalClinicalOrderEligibleEncounter[]
  >([]);
  const [catalog, setCatalog] = useState<HospitalClinicalOrderCatalogItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState<
    HospitalClinicalOrderCategory | "All"
  >("All");
  const [statusFilter, setStatusFilter] = useState<HospitalClinicalOrderStatus | "All">(
    "All"
  );
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [isLabResultModalOpen, setIsLabResultModalOpen] = useState(false);
  const [isImagingReportModalOpen, setIsImagingReportModalOpen] = useState(false);

  const [selectedDetail, setSelectedDetail] = useState<HospitalClinicalOrderDetail | null>(
    null
  );
  const [labResultTarget, setLabResultTarget] = useState<HospitalClinicalOrderSummary | null>(
    null
  );
  const [imagingReportTarget, setImagingReportTarget] =
    useState<HospitalClinicalOrderSummary | null>(null);

  const [selectedEncounterId, setSelectedEncounterId] = useState("");
  const [createCategory, setCreateCategory] = useState<HospitalClinicalOrderCategory>("Lab");
  const [selectedServiceId, setSelectedServiceId] = useState("");
  const [priorityCode, setPriorityCode] = useState("Routine");

  const [specimenCode, setSpecimenCode] = useState("");
  const [labResultItems, setLabResultItems] = useState<RecordHospitalLabResultItemPayload[]>([
    EMPTY_LAB_RESULT_ITEM,
  ]);
  const [findings, setFindings] = useState("");
  const [impression, setImpression] = useState("");
  const [reportUri, setReportUri] = useState("");

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
        const [worklist, encounterData, catalogData] = await Promise.all([
          hospitalClinicalOrderService.getAll({
            pageNumber,
            pageSize,
            category: categoryFilter,
            status: statusFilter,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalClinicalOrderService.getEligibleEncounters(),
          hospitalClinicalOrderService.getCatalog(),
        ]);

        setOrders(worklist.items);
        setTotalCount(worklist.totalCount);
        setEligibleEncounters(encounterData);
        setCatalog(catalogData);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, "Không thể tải dữ liệu chỉ định cận lâm sàng.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [categoryFilter, debouncedSearch, pageNumber, pageSize, statusFilter]
  );

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const metrics = useMemo(
    () =>
      orders.reduce(
        (acc, item) => {
          acc.total += 1;
          acc[item.category] += 1;
          acc[item.status] += 1;
          return acc;
        },
        {
          total: 0,
          Lab: 0,
          Imaging: 0,
          Requested: 0,
          Completed: 0,
        } as Record<"total" | HospitalClinicalOrderCategory | HospitalClinicalOrderStatus, number>
      ),
    [orders]
  );

  const availableServices = useMemo(
    () => catalog.filter((item) => item.category === createCategory),
    [catalog, createCategory]
  );

  const availableEncounters = useMemo(() => eligibleEncounters, [eligibleEncounters]);

  useEffect(() => {
    setSelectedServiceId((current) => {
      if (availableServices.some((item) => item.serviceId === current)) {
        return current;
      }

      return availableServices[0]?.serviceId ?? "";
    });
  }, [availableServices]);

  const resetCreateForm = () => {
    setSelectedEncounterId("");
    setCreateCategory("Lab");
    setSelectedServiceId("");
    setPriorityCode("Routine");
  };

  const resetLabResultForm = () => {
    setSpecimenCode("");
    setLabResultItems([EMPTY_LAB_RESULT_ITEM]);
  };

  const resetImagingReportForm = () => {
    setFindings("");
    setImpression("");
    setReportUri("");
  };

  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endItem = totalCount === 0 ? 0 : Math.min(pageNumber * pageSize, totalCount);

  const openDetail = async (clinicalOrderId: string) => {
    try {
      const detail = await hospitalClinicalOrderService.getById(clinicalOrderId);
      setSelectedDetail(detail);
      setIsDetailModalOpen(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải chi tiết chỉ định."));
    }
  };

  const handleCreateOrder = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!selectedEncounterId) {
      toast.error("Cần chọn hồ sơ khám để tạo chỉ định.");
      return;
    }

    if (!selectedServiceId) {
      toast.error("Cần chọn dịch vụ cận lâm sàng.");
      return;
    }

    setIsSubmitting(true);

    try {
      await hospitalClinicalOrderService.create(
        buildCreatePayload(selectedEncounterId, createCategory, selectedServiceId, priorityCode)
      );

      toast.success("Đã tạo chỉ định cận lâm sàng.");
      setIsCreateModalOpen(false);
      resetCreateForm();
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tạo chỉ định."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const openLabResultModal = (order: HospitalClinicalOrderSummary) => {
    setLabResultTarget(order);
    resetLabResultForm();
    setIsLabResultModalOpen(true);
  };

  const handleAddLabResultRow = () => {
    setLabResultItems((current) => [...current, { ...EMPTY_LAB_RESULT_ITEM }]);
  };

  const handleRemoveLabResultRow = (index: number) => {
    setLabResultItems((current) =>
      current.length === 1 ? current : current.filter((_, itemIndex) => itemIndex !== index)
    );
  };

  const handleUpdateLabResultRow = (
    index: number,
    field: keyof RecordHospitalLabResultItemPayload,
    value: string
  ) => {
    setLabResultItems((current) =>
      current.map((item, itemIndex) =>
        itemIndex === index ? { ...item, [field]: value } : item
      )
    );
  };

  const handleRecordLabResult = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!labResultTarget) {
      toast.error("Không xác định được chỉ định xét nghiệm.");
      return;
    }

    const resultItems = buildLabResultItems(labResultItems);
    if (resultItems.length === 0) {
      toast.error("Cần có ít nhất một dòng kết quả xét nghiệm hợp lệ.");
      return;
    }

    setIsSubmitting(true);

    try {
      const updated = await hospitalClinicalOrderService.recordLabResult(
        labResultTarget.clinicalOrderId,
        {
          specimenCode: specimenCode.trim() || undefined,
          resultItems,
        }
      );

      toast.success("Đã ghi nhận kết quả xét nghiệm.");
      setIsLabResultModalOpen(false);
      setLabResultTarget(null);
      resetLabResultForm();
      if (selectedDetail?.clinicalOrderId === updated.clinicalOrderId) {
        setSelectedDetail(updated);
      }
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể ghi nhận kết quả xét nghiệm."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const openImagingReportModal = (order: HospitalClinicalOrderSummary) => {
    setImagingReportTarget(order);
    resetImagingReportForm();
    setIsImagingReportModalOpen(true);
  };

  const handleRecordImagingReport = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!imagingReportTarget) {
      toast.error("Không xác định được chỉ định chẩn đoán hình ảnh.");
      return;
    }

    if (!findings.trim() && !impression.trim()) {
      toast.error("Cần có mô tả hoặc kết luận để lưu báo cáo.");
      return;
    }

    setIsSubmitting(true);

    try {
      const updated = await hospitalClinicalOrderService.recordImagingReport(
        imagingReportTarget.clinicalOrderId,
        {
          findings: findings.trim() || undefined,
          impression: impression.trim() || undefined,
          reportUri: reportUri.trim() || undefined,
        }
      );

      toast.success("Đã ghi nhận báo cáo chẩn đoán hình ảnh.");
      setIsImagingReportModalOpen(false);
      setImagingReportTarget(null);
      resetImagingReportForm();
      if (selectedDetail?.clinicalOrderId === updated.clinicalOrderId) {
        setSelectedDetail(updated);
      }
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể ghi nhận báo cáo."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <div className="rounded-[2rem] border border-violet-100 bg-gradient-to-br from-violet-50 via-white to-cyan-50 p-6 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.26em] text-violet-700">
              Cận lâm sàng
            </p>
            <h1 className="mt-3 text-3xl font-bold text-slate-950">
              Chỉ định cận lâm sàng
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-7 text-slate-600">
              Quản lý danh sách xét nghiệm và chẩn đoán hình ảnh theo hospital database
              mới, bám theo hồ sơ khám và phiếu chỉ định.
            </p>
          </div>

          <div className="flex flex-col gap-3 md:flex-row md:items-center">
            <input
              type="text"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Tìm theo mã chỉ định, bệnh nhân, dịch vụ..."
              className="min-w-[260px] rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
            />

            <select
              value={categoryFilter}
              onChange={(event) => {
                setCategoryFilter(event.target.value as HospitalClinicalOrderCategory | "All");
                setPageNumber(1);
              }}
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
            >
              {CATEGORY_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>

            <select
              value={statusFilter}
              onChange={(event) => {
                setStatusFilter(event.target.value as HospitalClinicalOrderStatus | "All");
                setPageNumber(1);
              }}
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
            >
              {STATUS_OPTIONS.map((option) => (
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
            <Button onClick={() => setIsCreateModalOpen(true)}>Tạo chỉ định</Button>
          </div>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <MetricCard label="Tổng chỉ định" value={metrics.total} tone="slate" />
        <MetricCard label="Xét nghiệm" value={metrics.Lab} tone="cyan" />
        <MetricCard label="Chẩn đoán hình ảnh" value={metrics.Imaging} tone="violet" />
        <MetricCard label="Đang chờ kết quả" value={metrics.Requested} tone="amber" />
        <MetricCard label="Đã hoàn thành" value={metrics.Completed} tone="emerald" />
      </div>

      <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
        <div className="flex flex-col gap-3 border-b border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-bold text-slate-900">Danh sách chỉ định</h2>
            <p className="mt-1 text-sm text-slate-500">
              Hiển thị {startItem}-{endItem} / {totalCount} chỉ định.
            </p>
          </div>

          <select
            value={pageSize}
            onChange={(event) => {
              setPageSize(Number(event.target.value));
              setPageNumber(1);
            }}
            className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
          >
            <option value={10}>10 dòng / trang</option>
            <option value={20}>20 dòng / trang</option>
            <option value={50}>50 dòng / trang</option>
          </select>
        </div>

        {isLoading ? (
          <LoadingState title="Đang tải danh sách chỉ định..." tone="violet" />
        ) : listError ? (
          <ErrorState
            title="Không thể tải danh sách chỉ định"
            description={listError}
            onAction={() => void fetchData(true)}
          />
        ) : orders.length === 0 ? (
          <EmptyState
            title="Chưa có chỉ định nào khớp bộ lọc hiện tại."
            description="Thử đổi phân loại, trạng thái hoặc từ khóa để mở rộng danh sách chỉ định."
            tone="violet"
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-[1280px] w-full border-collapse text-left">
              <thead>
                <tr className="bg-slate-50">
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Thời gian
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Bệnh nhân
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Dịch vụ
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Phân loại
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Tóm tắt
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Thao tác
                  </th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr
                    key={order.clinicalOrderId}
                    className="border-t border-slate-100 align-top transition-colors hover:bg-violet-50/20"
                  >
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">
                        {formatDateTime(order.requestedAtLocal)}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">{order.orderNumber}</div>
                      <div className="mt-1 text-xs text-slate-400">
                        {order.orderedByUsername || "Không rõ người chỉ định"}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{order.patientName}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        {order.medicalRecordNumber}
                      </div>
                      <div className="mt-1 text-xs text-slate-400">{order.encounterNumber}</div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{order.serviceName}</div>
                      <div className="mt-1 text-sm text-slate-500">{order.serviceCode}</div>
                      <div className="mt-1 text-xs text-slate-400">
                        {order.doctorName} / {order.clinicName}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-col gap-2">
                        <span
                          className={`inline-flex w-fit rounded-full px-3 py-1 text-xs font-bold ${getCategoryClass(
                            order.category
                          )}`}
                        >
                          {getCategoryLabel(order.category)}
                        </span>
                        <span
                          className={`inline-flex w-fit rounded-full px-3 py-1 text-xs font-bold ${getStatusClass(
                            order.status
                          )}`}
                        >
                          {getStatusLabel(order.status)}
                        </span>
                        {order.priorityCode && (
                          <span className="text-xs text-slate-500">
                            Ưu tiên: {order.priorityCode}
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="max-w-[320px] text-sm leading-6 text-slate-600">
                        {order.summaryText || "Chưa có nội dung kết quả."}
                      </div>
                      <div className="mt-2 text-xs text-slate-400">
                        Số dòng kết quả: {order.resultItemCount}
                      </div>
                      <div className="mt-1 text-xs text-slate-400">
                        Hoàn thành: {formatDateTime(order.completedAtLocal)}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-col gap-2">
                        <Button
                          variant="secondary"
                          className="justify-start"
                          onClick={() => void openDetail(order.clinicalOrderId)}
                        >
                          Xem chi tiết
                        </Button>
                        {order.status !== "Completed" && order.category === "Lab" && (
                          <Button
                            className="justify-start"
                            onClick={() => openLabResultModal(order)}
                          >
                            Nhập kết quả
                          </Button>
                        )}
                        {order.status !== "Completed" && order.category === "Imaging" && (
                          <Button
                            className="justify-start"
                            onClick={() => openImagingReportModal(order)}
                          >
                            Nhập báo cáo
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

        <div className="flex flex-col gap-4 border-t border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
          <div className="text-sm text-slate-500">
            Trang {pageNumber} / {totalPages}
          </div>

          <div className="flex gap-3">
            <Button
              variant="secondary"
              disabled={pageNumber <= 1}
              onClick={() => setPageNumber((current) => Math.max(1, current - 1))}
            >
              Trang trước
            </Button>
            <Button
              variant="secondary"
              disabled={pageNumber >= totalPages}
              onClick={() => setPageNumber((current) => Math.min(totalPages, current + 1))}
            >
              Trang sau
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
        title="Tạo chỉ định cận lâm sàng"
      >
        <form className="space-y-4" onSubmit={handleCreateOrder}>
          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">Hồ sơ khám</label>
            <select
              value={selectedEncounterId}
              onChange={(event) => setSelectedEncounterId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
            >
              <option value="">Chọn hồ sơ khám</option>
              {availableEncounters.map((encounter) => (
                <option key={encounter.encounterId} value={encounter.encounterId}>
                  {encounter.encounterNumber} - {encounter.patientName} -{" "}
                  {encounter.primaryDiagnosisName || encounter.specialtyName}
                </option>
              ))}
            </select>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-semibold text-slate-700">
                Loại chỉ định
              </label>
              <select
                value={createCategory}
                onChange={(event) =>
                  setCreateCategory(event.target.value as HospitalClinicalOrderCategory)
                }
                className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
              >
                <option value="Lab">Xét nghiệm</option>
                <option value="Imaging">Chẩn đoán hình ảnh</option>
              </select>
            </div>

            <div>
              <label className="mb-2 block text-sm font-semibold text-slate-700">
                Mức ưu tiên
              </label>
              <input
                type="text"
                value={priorityCode}
                onChange={(event) => setPriorityCode(event.target.value)}
                disabled={createCategory !== "Lab"}
                placeholder="Routine"
                className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100 disabled:bg-slate-100"
              />
            </div>
          </div>

          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">Dịch vụ</label>
            <select
              value={selectedServiceId}
              onChange={(event) => setSelectedServiceId(event.target.value)}
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
            >
              <option value="">Chọn dịch vụ</option>
              {availableServices.map((service) => (
                <option key={service.serviceId} value={service.serviceId}>
                  {service.serviceCode} - {service.serviceName}
                  {service.extraLabel ? ` (${service.extraLabel})` : ""}
                </option>
              ))}
            </select>
          </div>

          <div className="flex justify-end gap-3 pt-2">
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
              {isSubmitting ? "Đang tạo..." : "Lưu chỉ định"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        isOpen={isDetailModalOpen}
        onClose={() => {
          setIsDetailModalOpen(false);
          setSelectedDetail(null);
        }}
        title="Chi tiết chỉ định"
      >
        {!selectedDetail ? (
          <div className="py-8 text-sm text-slate-500">Đang tải chi tiết...</div>
        ) : (
          <div className="space-y-4">
            <div className="grid gap-3 md:grid-cols-2">
              <InfoTile label="Mã chỉ định" value={selectedDetail.orderNumber} />
              <InfoTile label="Loại" value={getCategoryLabel(selectedDetail.category)} />
              <InfoTile label="Bệnh nhân" value={selectedDetail.patientName} />
              <InfoTile label="Mã bệnh án" value={selectedDetail.medicalRecordNumber} />
              <InfoTile label="Hồ sơ khám" value={selectedDetail.encounterNumber} />
              <InfoTile label="Dịch vụ" value={selectedDetail.serviceName} />
              <InfoTile label="Trạng thái" value={getStatusLabel(selectedDetail.status)} />
              <InfoTile
                label="Hoàn thành"
                value={formatDateTime(selectedDetail.completedAtLocal)}
              />
            </div>

            {selectedDetail.category === "Lab" ? (
              <div className="space-y-3">
                <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                  <div className="text-sm font-semibold text-slate-800">Thông tin mẫu</div>
                  <div className="mt-2 grid gap-2 text-sm text-slate-600 md:grid-cols-2">
                    <div>Mã mẫu: {selectedDetail.specimenCode || "--"}</div>
                    <div>Trạng thái: {selectedDetail.specimenStatus || "--"}</div>
                    <div>Lấy mẫu: {formatDateTime(selectedDetail.collectedAtLocal)}</div>
                    <div>Tiếp nhận: {formatDateTime(selectedDetail.receivedAtLocal)}</div>
                  </div>
                </div>

                <div>
                  <div className="mb-2 text-sm font-semibold text-slate-800">
                    Kết quả xét nghiệm
                  </div>
                  {selectedDetail.resultItems.length === 0 ? (
                    <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-6 text-sm text-slate-500">
                      Chưa có dòng kết quả nào.
                    </div>
                  ) : (
                    <div className="space-y-2">
                      {selectedDetail.resultItems.map((item) => (
                        <div
                          key={item.resultItemId}
                          className="rounded-2xl border border-slate-100 px-4 py-3"
                        >
                          <div className="font-semibold text-slate-900">
                            {item.analyteName}
                          </div>
                          <div className="mt-1 text-sm text-slate-600">
                            {item.resultValue || "--"} {item.unit || ""}
                          </div>
                          <div className="mt-1 text-xs text-slate-400">
                            Ref: {item.referenceRange || "--"} | Bất thường:{" "}
                            {item.abnormalFlag || "--"}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            ) : (
              <div className="space-y-3">
                <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                  <div className="text-sm font-semibold text-slate-800">Mô tả</div>
                  <div className="mt-2 whitespace-pre-line text-sm leading-6 text-slate-600">
                    {selectedDetail.findings || "--"}
                  </div>
                </div>
                <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
                  <div className="text-sm font-semibold text-slate-800">Kết luận</div>
                  <div className="mt-2 whitespace-pre-line text-sm leading-6 text-slate-600">
                    {selectedDetail.impression || "--"}
                  </div>
                </div>
                <div className="grid gap-3 md:grid-cols-2">
                  <InfoTile label="Người ký" value={selectedDetail.signedByUsername || "--"} />
                  <InfoTile
                    label="Thời điểm ký"
                    value={formatDateTime(selectedDetail.signedAtLocal)}
                  />
                  <InfoTile label="Liên kết báo cáo" value={selectedDetail.reportUri || "--"} />
                  <InfoTile label="Tóm tắt" value={selectedDetail.summaryText || "--"} />
                </div>
              </div>
            )}
          </div>
        )}
      </Modal>

      <Modal
        isOpen={isLabResultModalOpen}
        onClose={() => {
          setIsLabResultModalOpen(false);
          setLabResultTarget(null);
          resetLabResultForm();
        }}
        title="Nhập kết quả xét nghiệm"
      >
        <form className="space-y-4" onSubmit={handleRecordLabResult}>
          {labResultTarget && (
            <div className="rounded-2xl border border-cyan-100 bg-cyan-50 px-4 py-3 text-sm text-cyan-800">
              {labResultTarget.orderNumber} - {labResultTarget.patientName} -{" "}
              {labResultTarget.serviceName}
            </div>
          )}

          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">Mã mẫu</label>
            <input
              type="text"
              value={specimenCode}
              onChange={(event) => setSpecimenCode(event.target.value)}
              placeholder="Để trống nếu hệ thống tự sinh"
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            />
          </div>

          <div className="space-y-3">
            {labResultItems.map((item, index) => (
              <div
                key={`${index}-${item.analyteCode ?? ""}`}
                className="rounded-2xl border border-slate-100 p-4"
              >
                <div className="mb-3 flex items-center justify-between">
                  <div className="text-sm font-semibold text-slate-800">
                    Dòng kết quả {index + 1}
                  </div>
                  <button
                    type="button"
                    onClick={() => handleRemoveLabResultRow(index)}
                    className="text-sm font-medium text-rose-600"
                  >
                    Xóa dòng
                  </button>
                </div>

                <div className="grid gap-3 md:grid-cols-2">
                  <input
                    type="text"
                    value={item.analyteName}
                    onChange={(event) =>
                      handleUpdateLabResultRow(index, "analyteName", event.target.value)
                    }
                    placeholder="Tên chỉ số"
                    className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                  />
                  <input
                    type="text"
                    value={item.analyteCode ?? ""}
                    onChange={(event) =>
                      handleUpdateLabResultRow(index, "analyteCode", event.target.value)
                    }
                    placeholder="Mã chỉ số"
                    className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                  />
                  <input
                    type="text"
                    value={item.resultValue ?? ""}
                    onChange={(event) =>
                      handleUpdateLabResultRow(index, "resultValue", event.target.value)
                    }
                    placeholder="Giá trị"
                    className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                  />
                  <input
                    type="text"
                    value={item.unit ?? ""}
                    onChange={(event) =>
                      handleUpdateLabResultRow(index, "unit", event.target.value)
                    }
                    placeholder="Đơn vị"
                    className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                  />
                  <input
                    type="text"
                    value={item.referenceRange ?? ""}
                    onChange={(event) =>
                      handleUpdateLabResultRow(index, "referenceRange", event.target.value)
                    }
                    placeholder="Khoảng tham chiếu"
                    className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                  />
                  <input
                    type="text"
                    value={item.abnormalFlag ?? ""}
                    onChange={(event) =>
                      handleUpdateLabResultRow(index, "abnormalFlag", event.target.value)
                    }
                    placeholder="Cảnh báo bất thường"
                    className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                  />
                </div>
              </div>
            ))}
          </div>

          <Button type="button" variant="secondary" onClick={handleAddLabResultRow}>
            Thêm dòng kết quả
          </Button>

          <div className="flex justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setIsLabResultModalOpen(false);
                setLabResultTarget(null);
                resetLabResultForm();
              }}
            >
              Đóng
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Đang lưu..." : "Lưu kết quả"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        isOpen={isImagingReportModalOpen}
        onClose={() => {
          setIsImagingReportModalOpen(false);
          setImagingReportTarget(null);
          resetImagingReportForm();
        }}
        title="Nhập báo cáo chẩn đoán hình ảnh"
      >
        <form className="space-y-4" onSubmit={handleRecordImagingReport}>
          {imagingReportTarget && (
            <div className="rounded-2xl border border-violet-100 bg-violet-50 px-4 py-3 text-sm text-violet-800">
              {imagingReportTarget.orderNumber} - {imagingReportTarget.patientName} -{" "}
              {imagingReportTarget.serviceName}
            </div>
          )}

          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">Mô tả</label>
            <textarea
              value={findings}
              onChange={(event) => setFindings(event.target.value)}
              rows={5}
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
              placeholder="Mô tả chi tiết kết quả chẩn đoán hình ảnh..."
            />
          </div>

          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">
              Kết luận
            </label>
            <textarea
              value={impression}
              onChange={(event) => setImpression(event.target.value)}
              rows={4}
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
              placeholder="Kết luận chẩn đoán..."
            />
          </div>

          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">
              Liên kết báo cáo
            </label>
            <input
              type="text"
              value={reportUri}
              onChange={(event) => setReportUri(event.target.value)}
              placeholder="https://... hoặc đường dẫn nội bộ"
              className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
            />
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setIsImagingReportModalOpen(false);
                setImagingReportTarget(null);
                resetImagingReportForm();
              }}
            >
              Đóng
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Đang lưu..." : "Lưu báo cáo"}
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
  tone: "slate" | "cyan" | "violet" | "amber" | "emerald";
}) {
  const toneClass = {
    slate: "border-slate-200 bg-slate-50 text-slate-700",
    cyan: "border-cyan-200 bg-cyan-50 text-cyan-700",
    violet: "border-violet-200 bg-violet-50 text-violet-700",
    amber: "border-amber-200 bg-amber-50 text-amber-700",
    emerald: "border-emerald-200 bg-emerald-50 text-emerald-700",
  }[tone];

  return (
    <Card className={`border p-5 shadow-sm ${toneClass}`}>
      <p className="text-xs font-bold uppercase tracking-[0.24em]">{label}</p>
      <p className="mt-3 text-3xl font-bold">{value}</p>
    </Card>
  );
}

function InfoTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
      <div className="text-xs font-bold uppercase tracking-[0.18em] text-slate-400">
        {label}
      </div>
      <div className="mt-2 text-sm font-medium text-slate-800">{value}</div>
    </div>
  );
}
