"use client";

import { Card } from "@/components/ui/Card";
import {
  HospitalClinicalOrderCategory,
  HospitalClinicalOrderStatus,
} from "../types";

interface ClinicalOrderMetricsProps {
  metrics: Record<"total" | HospitalClinicalOrderCategory | HospitalClinicalOrderStatus, number>;
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

export function ClinicalOrderMetrics({ metrics }: ClinicalOrderMetricsProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
      <MetricCard label="Tổng chỉ định" value={metrics.total} tone="slate" />
      <MetricCard label="Xét nghiệm" value={metrics.Lab} tone="cyan" />
      <MetricCard label="Chẩn đoán hình ảnh" value={metrics.Imaging} tone="violet" />
      <MetricCard label="Đang chờ kết quả" value={metrics.Requested} tone="amber" />
      <MetricCard label="Đã hoàn thành" value={metrics.Completed} tone="emerald" />
    </div>
  );
}
