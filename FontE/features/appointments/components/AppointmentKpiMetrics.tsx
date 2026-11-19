import React from "react";
import { Card } from "@/components/ui/Card";
import { AppointmentMetrics } from "../types";

interface AppointmentKpiMetricsProps {
  metrics: AppointmentMetrics;
}

export function AppointmentKpiMetrics({ metrics }: AppointmentKpiMetricsProps) {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      <Card className="flex flex-col justify-between">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Đã xếp lịch
        </p>
        <p className="mt-1 text-2xl font-bold tracking-tight text-sky-700">
          {metrics.Scheduled}
        </p>
      </Card>
      <Card className="flex flex-col justify-between">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Đã check-in
        </p>
        <p className="mt-1 text-2xl font-bold tracking-tight text-amber-700">
          {metrics.CheckedIn}
        </p>
      </Card>
      <Card className="flex flex-col justify-between">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Đã hoàn thành
        </p>
        <p className="mt-1 text-2xl font-bold tracking-tight text-emerald-700">
          {metrics.Completed}
        </p>
      </Card>
      <Card className="flex flex-col justify-between">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Đã hủy
        </p>
        <p className="mt-1 text-2xl font-bold tracking-tight text-rose-700">
          {metrics.Cancelled}
        </p>
      </Card>
    </div>
  );
}
