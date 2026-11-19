"use client";

import { Card } from "@/components/ui/Card";
import { HospitalEncounterStatus } from "../types";

interface MedicalRecordMetricsProps {
  metrics: Record<HospitalEncounterStatus, number>;
  availableAppointmentsCount: number;
}

function MetricCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone: "amber" | "emerald" | "cyan" | "slate";
}) {
  const toneClasses = {
    amber: "border-amber-100 bg-amber-50/70 text-amber-700",
    emerald: "border-emerald-100 bg-emerald-50/70 text-emerald-700",
    cyan: "border-cyan-100 bg-cyan-50/70 text-cyan-700",
    slate: "border-slate-200 bg-slate-50/80 text-slate-700",
  };

  return (
    <Card className={`border p-5 shadow-sm hover:shadow-sm ${toneClasses[tone]}`}>
      <p className="text-xs font-bold uppercase tracking-[0.2em]">{label}</p>
      <p className="mt-3 text-3xl font-bold text-slate-950">{value}</p>
    </Card>
  );
}

export function MedicalRecordMetrics({
  metrics,
  availableAppointmentsCount,
}: MedicalRecordMetricsProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <MetricCard label="Đang khám" value={metrics.InProgress} tone="amber" />
      <MetricCard label="Đã chốt hồ sơ" value={metrics.Finalized} tone="emerald" />
      <MetricCard label="Đã duyệt hồ sơ" value={metrics.Approved} tone="cyan" />
      <MetricCard
        label="Lịch chờ mở hồ sơ"
        value={availableAppointmentsCount}
        tone="slate"
      />
    </div>
  );
}
