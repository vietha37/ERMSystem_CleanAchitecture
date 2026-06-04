"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { Modal } from "@/components/ui/Modal";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalBillingService } from "@/services/hospitalBillingService";
import {
  HospitalBillingEligibleEncounter,
  HospitalInvoiceDetail,
  HospitalInvoiceStatus,
  HospitalInvoiceSummary,
  HospitalPaymentIntent,
  HospitalPaymentReconciliationSummary,
} from "@/services/types";
import toast from "react-hot-toast";

const STATUS_OPTIONS: Array<{ value: HospitalInvoiceStatus | "All"; label: string }> = [
  { value: "All", label: "Táº¥t cáº£" },
  { value: "Issued", label: "ÄÃ£ phÃ¡t hÃ nh" },
  { value: "PartiallyPaid", label: "Thanh toÃ¡n má»™t pháº§n" },
  { value: "Paid", label: "ÄÃ£ thanh toÃ¡n" },
  { value: "Cancelled", label: "ÄÃ£ há»§y" },
];

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(value);
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
      return "ÄÃ£ phÃ¡t hÃ nh";
    case "PartiallyPaid":
      return "Thanh toÃ¡n má»™t pháº§n";
    case "Paid":
      return "ÄÃ£ thanh toÃ¡n";
    case "Cancelled":
      return "ÄÃ£ há»§y";
    default:
      return status;
  }
}

export default function BillingPage() {
  const [invoices, setInvoices] = useState<HospitalInvoiceSummary[]>([]);
  const [eligibleEncounters, setEligibleEncounters] = useState<HospitalBillingEligibleEncounter[]>([]);
  const [reconciliationSummary, setReconciliationSummary] =
    useState<HospitalPaymentReconciliationSummary | null>(null);
  const [selectedInvoice, setSelectedInvoice] = useState<HospitalInvoiceDetail | null>(null);
  const [paymentInvoice, setPaymentInvoice] = useState<HospitalInvoiceSummary | null>(null);
  const [gatewayInvoice, setGatewayInvoice] = useState<HospitalInvoiceSummary | null>(null);
  const [gatewayIntent, setGatewayIntent] = useState<HospitalPaymentIntent | null>(null);

  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<HospitalInvoiceStatus | "All">("All");
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;
  const [totalCount, setTotalCount] = useState(0);

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isPaymentModalOpen, setIsPaymentModalOpen] = useState(false);
  const [isGatewayModalOpen, setIsGatewayModalOpen] = useState(false);
  const [isGatewayCallbackModalOpen, setIsGatewayCallbackModalOpen] = useState(false);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);

  const [selectedEncounterId, setSelectedEncounterId] = useState("");
  const [discountAmount, setDiscountAmount] = useState("0");
  const [insuranceAmount, setInsuranceAmount] = useState("0");

  const [paymentMethod, setPaymentMethod] = useState("Cash");
  const [paymentAmount, setPaymentAmount] = useState("");
  const [paymentReference, setPaymentReference] = useState("");

  const [gatewayMethod, setGatewayMethod] = useState("Transfer");
  const [gatewayAmount, setGatewayAmount] = useState("");
  const [gatewayStatus, setGatewayStatus] = useState("Captured");

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
        const [worklist, encounters, reconciliation] = await Promise.all([
          hospitalBillingService.getAll({
            pageNumber,
            pageSize,
            invoiceStatus: statusFilter,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalBillingService.getEligibleEncounters(),
          hospitalBillingService.getReconciliationSummary(),
        ]);

        setInvoices(worklist.items);
        setTotalCount(worklist.totalCount);
        setEligibleEncounters(encounters);
        setReconciliationSummary(reconciliation);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, "Không thể tải dữ liệu hóa đơn.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [debouncedSearch, pageNumber, statusFilter]
  );

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const availableEncounters = eligibleEncounters.filter((item) => !item.existingInvoiceId);

  const metrics = useMemo(
    () =>
      invoices.reduce(
        (acc, item) => {
          acc[item.invoiceStatus] += 1;
          return acc;
        },
        {
          Issued: 0,
          PartiallyPaid: 0,
          Paid: 0,
          Cancelled: 0,
        } as Record<HospitalInvoiceStatus, number>
      ),
    [invoices]
  );

  const openDetail = async (invoiceId: string) => {
    try {
      const detail = await hospitalBillingService.getById(invoiceId);
      setSelectedInvoice(detail);
      setIsDetailModalOpen(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "KhÃ´ng thá»ƒ táº£i chi tiáº¿t hÃ³a Ä‘Æ¡n."));
    }
  };

  const handleCreateInvoice = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedEncounterId) {
      toast.error("Cáº§n chá»n há»“ sÆ¡ khÃ¡m Ä‘á»ƒ láº­p hÃ³a Ä‘Æ¡n.");
      return;
    }

    setIsSubmitting(true);
    try {
      await hospitalBillingService.createInvoice({
        encounterId: selectedEncounterId,
        discountAmount: Number(discountAmount || 0),
        insuranceAmount: Number(insuranceAmount || 0),
      });

      toast.success("ÄÃ£ táº¡o hÃ³a Ä‘Æ¡n.");
      setIsCreateModalOpen(false);
      setSelectedEncounterId("");
      setDiscountAmount("0");
      setInsuranceAmount("0");
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "KhÃ´ng thá»ƒ táº¡o hÃ³a Ä‘Æ¡n."));
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

      toast.success("ÄÃ£ ghi nháº­n thanh toÃ¡n.");
      setSelectedInvoice(updated);
      setPaymentInvoice(null);
      setPaymentAmount("");
      setPaymentReference("");
      setIsPaymentModalOpen(false);
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "KhÃ´ng thá»ƒ ghi nháº­n thanh toÃ¡n."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCreateGatewayIntent = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!gatewayInvoice) {
      return;
    }

    setIsSubmitting(true);
    try {
      const intent = await hospitalBillingService.createPaymentIntent(gatewayInvoice.invoiceId, {
        gatewayProvider: "MockGateway",
        paymentMethod: gatewayMethod,
        amount: Number(gatewayAmount),
      });

      setGatewayIntent(intent);
      setGatewayStatus("Captured");
      setIsGatewayModalOpen(false);
      setIsGatewayCallbackModalOpen(true);
      toast.success("ÄÃ£ táº¡o giao dá»‹ch chá» xÃ¡c nháº­n.");
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "KhÃ´ng thá»ƒ táº¡o giao dá»‹ch thanh toÃ¡n."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleConfirmGatewayCallback = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!gatewayIntent) {
      return;
    }

    setIsSubmitting(true);
    try {
      const updated = await hospitalBillingService.simulatePaymentCallback({
        invoiceId: gatewayIntent.invoiceId,
        gatewayProvider: gatewayIntent.gatewayProvider,
        paymentReference: gatewayIntent.paymentReference,
        externalTransactionId: gatewayIntent.externalTransactionId || undefined,
        gatewayStatus,
        amount: gatewayIntent.amount,
      });

      setSelectedInvoice(updated);
      setGatewayIntent(null);
      setIsGatewayCallbackModalOpen(false);
      toast.success(
        gatewayStatus === "Captured"
          ? "ÄÃ£ xÃ¡c nháº­n callback thanh toÃ¡n thÃ nh cÃ´ng."
          : "ÄÃ£ ghi nháº­n callback tháº¥t báº¡i."
      );
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "KhÃ´ng thá»ƒ xá»­ lÃ½ callback thanh toÃ¡n."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <section className="rounded-[2rem] border border-emerald-100 bg-gradient-to-br from-emerald-50 via-white to-cyan-50 p-6 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.26em] text-emerald-700">
              TÃ i chÃ­nh
            </p>
            <h1 className="mt-3 text-3xl font-bold text-slate-950">
              HÃ³a Ä‘Æ¡n vÃ  thanh toÃ¡n
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-7 text-slate-600">
              Láº­p hÃ³a Ä‘Æ¡n tá»« há»“ sÆ¡ khÃ¡m má»›i, gom phÃ­ khÃ¡m vÃ  cÃ¡c dá»‹ch vá»¥ cáº­n lÃ¢m sÃ ng Ä‘Ã£
              hoÃ n thÃ nh.
            </p>
          </div>

          <div className="flex flex-col gap-3 md:flex-row md:items-center">
            <input
              type="text"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="TÃ¬m theo mÃ£ hÃ³a Ä‘Æ¡n, bá»‡nh nhÃ¢n, há»“ sÆ¡ khÃ¡m..."
              aria-label="TÃ¬m kiáº¿m hÃ³a Ä‘Æ¡n"
              className="min-w-[260px] rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            />
            <select
              value={statusFilter}
              onChange={(event) => {
                setStatusFilter(event.target.value as HospitalInvoiceStatus | "All");
                setPageNumber(1);
              }}
              aria-label="Lá»c tráº¡ng thÃ¡i hÃ³a Ä‘Æ¡n"
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            >
              {STATUS_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            <Button variant="secondary" onClick={() => void fetchData(true)} disabled={isRefreshing}>
              {isRefreshing ? "Äang lÃ m má»›i..." : "LÃ m má»›i"}
            </Button>
            <Button onClick={() => setIsCreateModalOpen(true)}>Láº­p hÃ³a Ä‘Æ¡n</Button>
          </div>
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-4">
        <MetricCard label="ÄÃ£ phÃ¡t hÃ nh" value={metrics.Issued} tone="cyan" />
        <MetricCard label="Thanh toÃ¡n má»™t pháº§n" value={metrics.PartiallyPaid} tone="amber" />
        <MetricCard label="ÄÃ£ thanh toÃ¡n" value={metrics.Paid} tone="emerald" />
        <MetricCard label="Há»“ sÆ¡ chá» láº­p" value={availableEncounters.length} tone="slate" />
      </section>

      {reconciliationSummary && (
        <Card className="border border-violet-100 bg-violet-50/70 p-5 shadow-sm">
          <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.24em] text-violet-700">
                Äá»‘i soÃ¡t thanh toÃ¡n
              </p>
              <h2 className="mt-2 text-lg font-bold text-slate-950">
                Queue gateway vÃ  áº£nh chá»¥p giao dá»‹ch
              </h2>
              <p className="mt-1 text-sm text-slate-600">
                Cáº­p nháº­t {formatDateTime(reconciliationSummary.generatedAtLocal)}
              </p>
            </div>
            <div className="grid gap-3 md:grid-cols-5">
              <MiniMetric label="Pending" value={`${reconciliationSummary.pendingPayments}`} />
              <MiniMetric label="Captured" value={`${reconciliationSummary.capturedPayments}`} />
              <MiniMetric label="Failed" value={`${reconciliationSummary.failedPayments}`} />
              <MiniMetric label="Refunded" value={`${reconciliationSummary.refundedPayments}`} />
              <MiniMetric
                label="Thiáº¿u external ID"
                value={`${reconciliationSummary.missingExternalTransactionCount}`}
              />
            </div>
          </div>
        </Card>
      )}

      <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
        {isLoading ? (
          <LoadingState title="Đang tải danh sách hóa đơn..." tone="emerald" />
        ) : listError ? (
          <ErrorState
            title="Không thể tải danh sách hóa đơn"
            description={listError}
            onAction={() => void fetchData(true)}
          />
        ) : invoices.length === 0 ? (
          <EmptyState
            title="Chưa có hóa đơn nào khớp bộ lọc hiện tại."
            description="Thử đổi trạng thái, từ khóa hoặc lập hóa đơn từ hồ sơ khám đủ điều kiện."
            tone="emerald"
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-[1160px] w-full border-collapse text-left">
              <thead>
                <tr className="bg-slate-50">
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    HÃ³a Ä‘Æ¡n
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Bá»‡nh nhÃ¢n
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Sá»‘ tiá»n
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Tráº¡ng thÃ¡i
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Thao tÃ¡c
                  </th>
                </tr>
              </thead>
              <tbody>
                {invoices.map((invoice) => (
                  <tr key={invoice.invoiceId} className="border-t border-slate-100 align-top">
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{invoice.invoiceNumber}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        {invoice.encounterNumber || "--"}
                      </div>
                      <div className="mt-1 text-xs text-slate-400">
                        {formatDateTime(invoice.issuedAtLocal)}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{invoice.patientName}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        {invoice.medicalRecordNumber}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="text-sm text-slate-700">
                        Tá»•ng: {formatCurrency(invoice.totalAmount)}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        ÄÃ£ thu: {formatCurrency(invoice.paidAmount)}
                      </div>
                      <div className="mt-1 text-sm font-semibold text-rose-600">
                        CÃ²n láº¡i: {formatCurrency(invoice.balanceAmount)}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getStatusClass(invoice.invoiceStatus)}`}
                      >
                        {getStatusLabel(invoice.invoiceStatus)}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-col gap-2">
                        <Button
                          variant="secondary"
                          onClick={() => void openDetail(invoice.invoiceId)}
                        >
                          Xem chi tiáº¿t
                        </Button>
                        {invoice.balanceAmount > 0 && invoice.invoiceStatus !== "Cancelled" && (
                          <Button
                            onClick={() => {
                              setPaymentInvoice(invoice);
                              setPaymentAmount(invoice.balanceAmount.toString());
                              setPaymentReference("");
                              setPaymentMethod("Cash");
                              setIsPaymentModalOpen(true);
                            }}
                          >
                            Thu tiá»n
                          </Button>
                        )}
                        {invoice.balanceAmount > 0 && invoice.invoiceStatus !== "Cancelled" && (
                          <Button
                            variant="secondary"
                            onClick={() => {
                              setGatewayInvoice(invoice);
                              setGatewayMethod("Transfer");
                              setGatewayAmount(invoice.balanceAmount.toString());
                              setGatewayIntent(null);
                              setIsGatewayModalOpen(true);
                            }}
                          >
                            Táº¡o giao dá»‹ch
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
          <div className="text-sm text-slate-500">
            Trang {pageNumber} / {Math.max(1, totalPages)}
          </div>
          <div className="flex gap-3">
            <Button
              variant="secondary"
              disabled={pageNumber <= 1}
              onClick={() => setPageNumber((current) => Math.max(1, current - 1))}
            >
              Trang trÆ°á»›c
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

      <Modal isOpen={isCreateModalOpen} onClose={() => setIsCreateModalOpen(false)} title="Láº­p hÃ³a Ä‘Æ¡n">
        <form className="space-y-4" onSubmit={handleCreateInvoice}>
          <select
            value={selectedEncounterId}
            onChange={(event) => setSelectedEncounterId(event.target.value)}
            aria-label="Chá»n há»“ sÆ¡ khÃ¡m Ä‘á»ƒ láº­p hÃ³a Ä‘Æ¡n"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          >
            <option value="">Chá»n há»“ sÆ¡ khÃ¡m</option>
            {availableEncounters.map((item) => (
              <option key={item.encounterId} value={item.encounterId}>
                {item.encounterNumber} - {item.patientName} - {item.completedLabOrders} xÃ©t nghiá»‡m -{" "}
                {item.completedImagingOrders} CÄHA
              </option>
            ))}
          </select>
          <div className="grid gap-4 md:grid-cols-2">
            <input
              type="number"
              value={discountAmount}
              onChange={(event) => setDiscountAmount(event.target.value)}
              placeholder="Giáº£m giÃ¡"
              aria-label="Giáº£m giÃ¡"
              className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            />
            <input
              type="number"
              value={insuranceAmount}
              onChange={(event) => setInsuranceAmount(event.target.value)}
              placeholder="Báº£o hiá»ƒm"
              aria-label="GiÃ¡ trá»‹ báº£o hiá»ƒm"
              className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            />
          </div>
          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => setIsCreateModalOpen(false)}>
              ÄÃ³ng
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Äang láº­p..." : "Táº¡o hÃ³a Ä‘Æ¡n"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={isPaymentModalOpen} onClose={() => setIsPaymentModalOpen(false)} title="Thu tiá»n">
        <form className="space-y-4" onSubmit={handleReceivePayment}>
          <select
            value={paymentMethod}
            onChange={(event) => setPaymentMethod(event.target.value)}
            aria-label="PhÆ°Æ¡ng thá»©c thu tiá»n"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          >
            <option value="Cash">Tiá»n máº·t</option>
            <option value="Transfer">Chuyá»ƒn khoáº£n</option>
            <option value="Card">Tháº»</option>
          </select>
          <input
            type="number"
            value={paymentAmount}
            onChange={(event) => setPaymentAmount(event.target.value)}
            placeholder="Sá»‘ tiá»n"
            aria-label="Sá»‘ tiá»n thu"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          />
          <input
            type="text"
            value={paymentReference}
            onChange={(event) => setPaymentReference(event.target.value)}
            placeholder="MÃ£ tham chiáº¿u"
            aria-label="MÃ£ tham chiáº¿u thanh toÃ¡n"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          />
          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => setIsPaymentModalOpen(false)}>
              ÄÃ³ng
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Äang ghi nháº­n..." : "XÃ¡c nháº­n thu tiá»n"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        isOpen={isGatewayModalOpen}
        onClose={() => setIsGatewayModalOpen(false)}
        title="Táº¡o giao dá»‹ch gateway"
      >
        <form className="space-y-4" onSubmit={handleCreateGatewayIntent}>
          <select
            value={gatewayMethod}
            onChange={(event) => setGatewayMethod(event.target.value)}
            aria-label="PhÆ°Æ¡ng thá»©c gateway"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
          >
            <option value="Transfer">Chuyá»ƒn khoáº£n gateway</option>
            <option value="Card">Tháº» / cá»•ng thanh toÃ¡n</option>
            <option value="EWallet">VÃ­ Ä‘iá»‡n tá»­</option>
          </select>
          <input
            type="number"
            value={gatewayAmount}
            onChange={(event) => setGatewayAmount(event.target.value)}
            placeholder="Sá»‘ tiá»n giao dá»‹ch"
            aria-label="Sá»‘ tiá»n giao dá»‹ch gateway"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
          />
          <div className="rounded-2xl border border-violet-100 bg-violet-50/70 px-4 py-4 text-sm text-slate-700">
            Táº¡o giao dá»‹ch `Pending` Ä‘á»ƒ mÃ´ phá»ng luá»“ng gateway callback vÃ o API.
          </div>
          <div className="flex justify-end gap-3">
            <Button type="button" variant="secondary" onClick={() => setIsGatewayModalOpen(false)}>
              ÄÃ³ng
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Äang táº¡o..." : "Táº¡o giao dá»‹ch"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        isOpen={isGatewayCallbackModalOpen}
        onClose={() => setIsGatewayCallbackModalOpen(false)}
        title="MÃ´ phá»ng callback thanh toÃ¡n"
      >
        <form className="space-y-4" onSubmit={handleConfirmGatewayCallback}>
          <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4 text-sm text-slate-700">
            <div className="font-medium text-slate-900">
              {gatewayIntent?.paymentReference || "--"}
            </div>
            <div className="mt-1">
              Gateway: {gatewayIntent?.gatewayProvider || "--"} / {gatewayIntent?.callbackMode || "--"}
            </div>
            <div className="mt-1">
              External Tx: {gatewayIntent?.externalTransactionId || "--"}
            </div>
            <div className="mt-1">
              Sá»‘ tiá»n: {gatewayIntent ? formatCurrency(gatewayIntent.amount) : "--"}
            </div>
          </div>
          <select
            value={gatewayStatus}
            onChange={(event) => setGatewayStatus(event.target.value)}
            aria-label="Tráº¡ng thÃ¡i callback gateway"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
          >
            <option value="Captured">Captured / Success</option>
            <option value="Failed">Failed</option>
          </select>
          <div className="rounded-2xl border border-amber-100 bg-amber-50/80 px-4 py-4 text-sm text-slate-700">
            Luá»“ng nÃ y gá»i endpoint mÃ´ phá»ng ná»™i bá»™. Webhook public cá»§a gateway hiá»‡n Ä‘Ã£ tÃ¡ch riÃªng
            vÃ  yÃªu cáº§u chá»¯ kÃ½ há»£p lá»‡.
          </div>
          <div className="flex justify-end gap-3">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setIsGatewayCallbackModalOpen(false)}
            >
              ÄÃ³ng
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Äang xá»­ lÃ½..." : "Gá»­i callback mÃ´ phá»ng"}
            </Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={isDetailModalOpen} onClose={() => setIsDetailModalOpen(false)} title="Chi tiáº¿t hÃ³a Ä‘Æ¡n">
        {!selectedInvoice ? (
          <div className="py-8 text-sm text-slate-500">Äang táº£i chi tiáº¿t...</div>
        ) : (
          <div className="space-y-4">
            <div className="grid gap-3 md:grid-cols-2">
              <InfoLine label="HÃ³a Ä‘Æ¡n" value={selectedInvoice.invoiceNumber} />
              <InfoLine label="Bá»‡nh nhÃ¢n" value={selectedInvoice.patientName} />
              <InfoLine label="Há»“ sÆ¡ khÃ¡m" value={selectedInvoice.encounterNumber || "--"} />
              <InfoLine label="BÃ¡c sÄ©" value={selectedInvoice.doctorName || "--"} />
              <InfoLine label="Tráº¡ng thÃ¡i" value={getStatusLabel(selectedInvoice.invoiceStatus)} />
              <InfoLine label="CÃ²n láº¡i" value={formatCurrency(selectedInvoice.balanceAmount)} />
            </div>

            <div>
              <p className="mb-2 text-sm font-semibold text-slate-800">DÃ²ng hÃ³a Ä‘Æ¡n</p>
              <div className="space-y-2">
                {selectedInvoice.items.map((item) => (
                  <div key={item.invoiceItemId} className="rounded-2xl border border-slate-100 px-4 py-3">
                    <div className="font-medium text-slate-900">{item.description}</div>
                    <div className="mt-1 text-sm text-slate-600">
                      {item.quantity} x {formatCurrency(item.unitPrice)} ={" "}
                      {formatCurrency(item.lineAmount)}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div>
              <p className="mb-2 text-sm font-semibold text-slate-800">Lá»‹ch sá»­ thanh toÃ¡n</p>
              {selectedInvoice.payments.length === 0 ? (
                <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-6 text-sm text-slate-500">
                  ChÆ°a cÃ³ thanh toÃ¡n nÃ o.
                </div>
              ) : (
                <div className="space-y-2">
                  {selectedInvoice.payments.map((payment) => (
                    <div key={payment.paymentId} className="rounded-2xl border border-slate-100 px-4 py-3">
                      <div className="font-medium text-slate-900">{payment.paymentReference}</div>
                      <div className="mt-1 text-sm text-slate-600">
                        {payment.paymentMethod} - {formatCurrency(payment.amount)} -{" "}
                        {formatDateTime(payment.paidAtLocal)}
                      </div>
                      <div className="mt-1 text-xs text-slate-500">
                        {payment.paymentStatus}
                        {payment.externalTransactionId ? ` / ${payment.externalTransactionId}` : ""}
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

function MetricCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone: "cyan" | "amber" | "emerald" | "slate";
}) {
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

function MiniMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/80 bg-white/80 px-4 py-4">
      <div className="text-[11px] font-bold uppercase tracking-[0.18em] text-slate-500">
        {label}
      </div>
      <div className="mt-2 text-sm font-semibold text-slate-900">{value}</div>
    </div>
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
