"use client";

import { Card } from "@/components/ui/Card";
import { HospitalPrescriptionStatus } from "../types";

interface PrescriptionMetricsProps {
  metrics: Record<HospitalPrescriptionStatus, number>;
  availableEncountersCount: number;
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

export function PrescriptionMetrics({
  metrics,
  availableEncountersCount,
}: PrescriptionMetricsProps) {
  return (
    <div className="grid gap-4 md:grid-cols-4">
      <MetricCard label="Đã phát hành" value={metrics.Issued} tone="cyan" />
      <MetricCard label="Đã cấp thuốc" value={metrics.Dispensed} tone="emerald" />
      <MetricCard label="Đã hủy" value={metrics.Cancelled} tone="rose" />
      <MetricCard
        label="Hồ sơ chờ kê đơn"
        value={availableEncountersCount}
        tone="amber"
      />
    </div>
  );
}
