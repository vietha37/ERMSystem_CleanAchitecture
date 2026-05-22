"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Modal } from "@/components/ui/Modal";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalBillingService } from "@/services/hospitalBillingService";
import {
  HospitalBillingEligibleEncounter,
  HospitalInvoiceDetail,
  HospitalInvoiceStatus,
  HospitalInvoiceSummary,
} from "@/services/types";
import toast from "react-hot-toast";

const STATUS_OPTIONS: Array<{ value: HospitalInvoiceStatus | "All"; label: string }> = [
  { value: "All", label: "Tất cả" },
  { value: "Issued", label: "Đã phát hành" },
  { value: "PartiallyPaid", label: "Thanh toán một phần" },
  { value: "Paid", label: "Đã thanh toán" },
  { value: "Cancelled", label: "Đã hủy" },
];

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(value);
}

function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

function getStatusClass(status: HospitalInvoiceStatus): string {
  switch (status) {
    case "Issued":
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
    case "PartiallyPaid":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "Paid":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Cancelled":
      return "border border-rose-200 bg-rose-50 text-rose-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

function getStatusLabel(status: HospitalInvoiceStatus): string {
  switch (status) {
    case "Issued":
      return "Đã phát hành";
    case "PartiallyPaid":
      return "Thanh toán một phần";
    case "Paid":
      return "Đã thanh toán";
    case "Cancelled":
      return "Đã hủy";
    default:
      return status;
  }
}

export default function BillingPage() {
  const [invoices, setInvoices] = useState<HospitalInvoiceSummary[]>([]);
  const [eligibleEncounters, setEligibleEncounters] = useState<HospitalBillingEligibleEncounter[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [statusFilter, setStatusFilter] = useState<HospitalInvoiceStatus | "All">("All");
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;
  const [totalCount, setTotalCount] = useState(0);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [isPaymentModalOpen, setIsPaymentModalOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [selectedEncounterId, setSelectedEncounterId] = useState("");
  const [discountAmount, setDiscountAmount] = useState("0");
  const [insuranceAmount, setInsuranceAmount] = useState("0");
  const [selectedInvoice, setSelectedInvoice] = useState<HospitalInvoiceDetail | null>(null);
  const [paymentInvoice, setPaymentInvoice] = useState<HospitalInvoiceSummary | null>(null);
  const [paymentMethod, setPaymentMethod] = useState("Cash");
  const [paymentAmount, setPaymentAmount] = useState("");
  const [paymentReference, setPaymentReference] = useState("");

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(searchQuery.trim());
      setPageNumber(1);
    }, 400);

    return () => window.clearTimeout(timer);
  }, [searchQuery]);

  const fetchData = useCallback(async (showRefreshState = false) => {
    if (showRefreshState) {
      setIsRefreshing(true);
    } else {
      setIsLoading(true);
    }

    try {
      const [worklist, encounters] = await Promise.all([
        hospitalBillingService.getAll({
          pageNumber,
          pageSize,
          invoiceStatus: statusFilter,
          textSearch: debouncedSearch || undefined,
        }),
        hospitalBillingService.getEligibleEncounters(),
      ]);

      setInvoices(worklist.items);
      setTotalCount(worklist.totalCount);
      setEligibleEncounters(encounters);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải dữ liệu hóa đơn."));
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, [debouncedSearch, pageNumber, pageSize, statusFilter]);

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const availableEncounters = eligibleEncounters.filter((item) => !item.existingInvoiceId);

  const metrics = useMemo(() => invoices.reduce((acc, item) => {
    acc[item.invoiceStatus] += 1;
    return acc;
  }, {
    Issued: 0,
    PartiallyPaid: 0,
    Paid: 0,
    Cancelled: 0,
  } as Record<HospitalInvoiceStatus, number>), [invoices]);

  const openDetail = async (invoiceId: string) => {
    try {
      const detail = await hospitalBillingService.getById(invoiceId);
      setSelectedInvoice(detail);
      setIsDetailModalOpen(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải chi tiết hóa đơn."));
    }
  };

  const handleCreateInvoice = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedEncounterId) {
      toast.error("Cần chọn hồ sơ khám để lập hóa đơn.");
      return;
    }

    setIsSubmitting(true);
    try {
      await hospitalBillingService.createInvoice({
        encounterId: selectedEncounterId,
        discountAmount: Number(discountAmount || 0),
        insuranceAmount: Number(insuranceAmount || 0),
      });

      toast.success("Đã tạo hóa đơn.");
      setIsCreateModalOpen(false);
      setSelectedEncounterId("");
      setDiscountAmount("0");
      setInsuranceAmount("0");
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tạo hóa đơn."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleReceivePayment = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!paymentInvoice) {
      return;
    }

    setIsSubmitting(true);
    try {
      const updated = await hospitalBillingService.receivePayment(paymentInvoice.invoiceId, {
        paymentMethod,
        paymentReference: paymentReference.trim() || undefined,
        amount: Number(paymentAmount),
      });

      toast.success("Đã ghi nhận thanh toán.");
      setIsPaymentModalOpen(false);
      setPaymentInvoice(null);
      setPaymentAmount("");
      setPaymentReference("");
      setSelectedInvoice(updated);
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể ghi nhận thanh toán."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <div className="rounded-[2rem] border border-emerald-100 bg-gradient-to-br from-emerald-50 via-white to-cyan-50 p-6 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.26em] text-emerald-700">Tài chính</p>
            <h1 className="mt-3 text-3xl font-bold text-slate-950">Hóa đơn và thanh toán</h1>
            <p className="mt-2 max-w-3xl text-sm leading-7 text-slate-600">
              Lập hóa đơn từ hồ sơ khám mới, gom phí khám và các dịch vụ cận lâm sàng đã hoàn thành.
            </p>
          </div>

          <div className="flex flex-col gap-3 md:flex-row md:items-center">
            <input
              type="text"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Tìm theo mã hóa đơn, bệnh nhân, hồ sơ khám..."
              className="min-w-[260px] rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            />
            <select
              value={statusFilter}
              onChange={(event) => {
                setStatusFilter(event.target.value as HospitalInvoiceStatus | "All");
                setPageNumber(1);
              }}
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            >
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
            <Button variant="secondary" onClick={() => void fetchData(true)} disabled={isRefreshing}>
              {isRefreshing ? "Đang làm mới..." : "Làm mới"}
            </Button>
            <Button onClick={() => setIsCreateModalOpen(true)}>Lập hóa đơn</Button>
          </div>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-4">
        <MetricCard label="Đã phát hành" value={metrics.Issued} tone="cyan" />
        <MetricCard label="Thanh toán một phần" value={metrics.PartiallyPaid} tone="amber" />
        <MetricCard label="Đã thanh toán" value={metrics.Paid} tone="emerald" />
        <MetricCard label="Hồ sơ chờ lập" value={availableEncounters.length} tone="slate" />
      </div>

      <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
        {isLoading ? (
          <div className="p-16 text-center text-sm text-slate-500">Đang tải danh sách hóa đơn...</div>
        ) : invoices.length === 0 ? (
          <div className="p-16 text-center text-sm text-slate-500">Chưa có hóa đơn nào khớp bộ lọc hiện tại.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-[1160px] w-full border-collapse text-left">
              <thead>
                <tr className="bg-slate-50">
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Hóa đơn</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Bệnh nhân</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Số tiền</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Trạng thái</th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {invoices.map((invoice) => (
                  <tr key={invoice.invoiceId} className="border-t border-slate-100 align-top">
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{invoice.invoiceNumber}</div>
                      <div className="mt-1 text-sm text-slate-500">{invoice.encounterNumber || "--"}</div>
                      <div className="mt-1 text-xs text-slate-400">{formatDateTime(invoice.issuedAtLocal)}</div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{invoice.patientName}</div>
                      <div className="mt-1 text-sm text-slate-500">{invoice.medicalRecordNumber}</div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="text-sm text-slate-700">Tổng: {formatCurrency(invoice.totalAmount)}</div>
                      <div className="mt-1 text-sm text-slate-500">Đã thu: {formatCurrency(invoice.paidAmount)}</div>
                      <div className="mt-1 text-sm font-semibold text-rose-600">Còn lại: {formatCurrency(invoice.balanceAmount)}</div>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getStatusClass(invoice.invoiceStatus)}`}>
                        {getStatusLabel(invoice.invoiceStatus)}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-col gap-2">
                        <Button variant="secondary" onClick={() => void openDetail(invoice.invoiceId)}>
                          Xem chi tiết
                        </Button>
                        {invoice.balanceAmount > 0 && invoice.invoiceStatus !== "Cancelled" && (
                          <Button onClick={() => {
                            setPaymentInvoice(invoice);
                            setPaymentAmount(invoice.balanceAmount.toString());
                            setPaymentReference("");
                            setPaymentMethod("Cash");
                            setIsPaymentModalOpen(true);
                          }}>
                            Thu tiền
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

        <div className="flex items-center justify-between border-t border-slate-100 px-6 py-5">
          <div className="text-sm text-slate-500">Trang {pageNumber} / {Math.max(1, totalPages)}</div>
          <div className="flex gap-3">
            <Button variant="secondary" disabled={pageNumber <= 1} onClick={() => setPageNumber((x) => Math.max(1, x - 1))}>
              Trang trước
            </Button>
            <Button variant="secondary" disabled={pageNumber >= totalPages} onClick={() => setPageNumber((x) => Math.min(totalPages, x + 1))}>
              Trang sau
            </Button>
          </div>
        </div>
      </Card>

      <Modal isOpen={isCreateModalOpen} onClose={() => setIsCreateModalOpen(false)} title="Lập hóa đơn">
        <form className="space-y-4" onSubmit={handleCreateInvoice}>
          <select
            value={selectedEncounterId}
            onChange={(event) => setSelectedEncounterId(event.target.value)}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          >
            <option value="">Chọn hồ sơ khám</option>
            {availableEncounters.map((item) => (
              <option key={item.encounterId} value={item.encounterId}>
                {item.encounterNumber} - {item.patientName} - {item.completedLabOrders} xét nghiệm - {item.completedImagingOrders} CĐHA
              </option>
            ))}
          </select>
          <div className="grid gap-4 md:grid-cols-2">
            <input
              type="number"
              value={discountAmount}
              onChange={(event) => setDiscountAmount(event.target.value)}
              placeholder="Giảm giá"
              className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            />
            <input
              type="number"
              value={insuranceAmount}
              onChange={(event) => setInsuranceAmount(event.target.value)}
              placeholder="Bảo hiểm"
              className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            />
          </div>
          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => setIsCreateModalOpen(false)}>Đóng</Button>
            <Button type="submit" disabled={isSubmitting}>{isSubmitting ? "Đang lập..." : "Tạo hóa đơn"}</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={isPaymentModalOpen} onClose={() => setIsPaymentModalOpen(false)} title="Thu tiền">
        <form className="space-y-4" onSubmit={handleReceivePayment}>
          <select
            value={paymentMethod}
            onChange={(event) => setPaymentMethod(event.target.value)}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          >
            <option value="Cash">Tiền mặt</option>
            <option value="Transfer">Chuyển khoản</option>
            <option value="Card">Thẻ</option>
          </select>
          <input
            type="number"
            value={paymentAmount}
            onChange={(event) => setPaymentAmount(event.target.value)}
            placeholder="Số tiền"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          />
          <input
            type="text"
            value={paymentReference}
            onChange={(event) => setPaymentReference(event.target.value)}
            placeholder="Mã tham chiếu"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          />
          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => setIsPaymentModalOpen(false)}>Đóng</Button>
            <Button type="submit" disabled={isSubmitting}>{isSubmitting ? "Đang ghi nhận..." : "Xác nhận thu tiền"}</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={isDetailModalOpen} onClose={() => setIsDetailModalOpen(false)} title="Chi tiết hóa đơn">
        {!selectedInvoice ? (
          <div className="py-8 text-sm text-slate-500">Đang tải chi tiết...</div>
        ) : (
          <div className="space-y-4">
            <div className="grid gap-3 md:grid-cols-2">
              <InfoLine label="Hóa đơn" value={selectedInvoice.invoiceNumber} />
              <InfoLine label="Bệnh nhân" value={selectedInvoice.patientName} />
              <InfoLine label="Hồ sơ khám" value={selectedInvoice.encounterNumber || "--"} />
              <InfoLine label="Bác sĩ" value={selectedInvoice.doctorName || "--"} />
              <InfoLine label="Trạng thái" value={getStatusLabel(selectedInvoice.invoiceStatus)} />
              <InfoLine label="Còn lại" value={formatCurrency(selectedInvoice.balanceAmount)} />
            </div>

            <div>
              <p className="mb-2 text-sm font-semibold text-slate-800">Dòng hóa đơn</p>
              <div className="space-y-2">
                {selectedInvoice.items.map((item) => (
                  <div key={item.invoiceItemId} className="rounded-2xl border border-slate-100 px-4 py-3">
                    <div className="font-medium text-slate-900">{item.description}</div>
                    <div className="mt-1 text-sm text-slate-600">
                      {item.quantity} x {formatCurrency(item.unitPrice)} = {formatCurrency(item.lineAmount)}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div>
              <p className="mb-2 text-sm font-semibold text-slate-800">Lịch sử thanh toán</p>
              {selectedInvoice.payments.length === 0 ? (
                <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-6 text-sm text-slate-500">
                  Chưa có thanh toán nào.
                </div>
              ) : (
                <div className="space-y-2">
                  {selectedInvoice.payments.map((payment) => (
                    <div key={payment.paymentId} className="rounded-2xl border border-slate-100 px-4 py-3">
                      <div className="font-medium text-slate-900">{payment.paymentReference}</div>
                      <div className="mt-1 text-sm text-slate-600">
                        {payment.paymentMethod} - {formatCurrency(payment.amount)} - {formatDateTime(payment.paidAtLocal)}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}

function MetricCard({ label, value, tone }: { label: string; value: number; tone: "cyan" | "amber" | "emerald" | "slate"; }) {
  const toneClass = {
    cyan: "border-cyan-200 bg-cyan-50 text-cyan-700",
    amber: "border-amber-200 bg-amber-50 text-amber-700",
    emerald: "border-emerald-200 bg-emerald-50 text-emerald-700",
    slate: "border-slate-200 bg-slate-50 text-slate-700",
  }[tone];

  return (
    <Card className={`border p-5 shadow-sm ${toneClass}`}>
      <p className="text-xs font-bold uppercase tracking-[0.24em]">{label}</p>
      <p className="mt-3 text-3xl font-bold">{value}</p>
    </Card>
  );
}

function InfoLine({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4">
      <div className="text-xs font-bold uppercase tracking-[0.18em] text-slate-400">{label}</div>
      <div className="mt-2 text-sm font-medium text-slate-800">{value}</div>
    </div>
  );
}
