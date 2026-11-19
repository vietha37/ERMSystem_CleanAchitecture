"use client";

import { Card } from "@/components/ui/Card";
import {
  HospitalInvoiceStatus,
  HospitalPaymentReconciliationSummary,
} from "../types";
import { formatDateTime } from "../utils/billingFormatters";

interface BillingMetricsProps {
  metrics: Record<HospitalInvoiceStatus, number>;
  waitingEncountersCount: number;
  reconciliationSummary: HospitalPaymentReconciliationSummary | null;
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
  const toneClasses = {
    cyan: "border-cyan-100 bg-cyan-50/70 text-cyan-900",
    amber: "border-amber-100 bg-amber-50/70 text-amber-900",
    emerald: "border-emerald-100 bg-emerald-50/70 text-emerald-900",
    slate: "border-slate-200 bg-slate-50 text-slate-900",
  };

  return (
    <Card className={`border p-5 ${toneClasses[tone]}`}>
      <p className="text-xs font-semibold uppercase tracking-[0.2em] opacity-75">{label}</p>
      <p className="mt-3 text-3xl font-extrabold">{value}</p>
    </Card>
  );
}

function MiniMetric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-violet-200/60 bg-white/90 px-3 py-2 text-center">
      <p className="text-[11px] font-semibold text-slate-500">{label}</p>
      <p className="mt-1 text-base font-bold text-slate-900">{value}</p>
    </div>
  );
}

export function BillingMetrics({
  metrics,
  waitingEncountersCount,
  reconciliationSummary,
}: BillingMetricsProps) {
  return (
    <div className="space-y-4">
      <section className="grid gap-4 md:grid-cols-4">
        <MetricCard label="Đã phát hành" value={metrics.Issued} tone="cyan" />
        <MetricCard label="Thanh toán một phần" value={metrics.PartiallyPaid} tone="amber" />
        <MetricCard label="Đã thanh toán" value={metrics.Paid} tone="emerald" />
        <MetricCard label="Hồ sơ chờ lập" value={waitingEncountersCount} tone="slate" />
      </section>

      {reconciliationSummary && (
        <Card className="border border-violet-100 bg-violet-50/70 p-5 shadow-sm">
          <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
            <div>
              <p className="text-xs font-bold uppercase tracking-[0.24em] text-violet-700">
                Đối soát thanh toán
              </p>
              <h2 className="mt-2 text-lg font-bold text-slate-950">
                Trạng thái giao dịch
              </h2>
              <p className="mt-1 text-sm text-slate-600">
                Cập nhật {formatDateTime(reconciliationSummary.generatedAtLocal)}
              </p>
            </div>
            <div className="grid gap-3 md:grid-cols-5">
              <MiniMetric label="Pending" value={`${reconciliationSummary.pendingPayments}`} />
              <MiniMetric label="Captured" value={`${reconciliationSummary.capturedPayments}`} />
              <MiniMetric label="Failed" value={`${reconciliationSummary.failedPayments}`} />
              <MiniMetric label="Refunded" value={`${reconciliationSummary.refundedPayments}`} />
              <MiniMetric
                label="Thiếu external ID"
                value={`${reconciliationSummary.missingExternalTransactionCount}`}
              />
            </div>
          </div>
        </Card>
      )}
    </div>
  );
}
