"use client";

import { Card } from "@/components/ui/Card";
import { HospitalDoctorWorklistResponse } from "@/services/types";

interface DoctorWorklistMetricsProps {
  worklist: HospitalDoctorWorklistResponse | null;
}

export function DoctorWorklistMetrics({ worklist }: DoctorWorklistMetricsProps) {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-5">
      <Card className="flex flex-col justify-between">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Tổng lịch hẹn
        </p>
        <p className="mt-1 text-2xl font-bold tracking-tight text-slate-900">
          {worklist?.totalAppointments ?? 0}
        </p>
      </Card>
      <Card className="flex flex-col justify-between">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Đã check-in
        </p>
        <p className="mt-1 text-2xl font-bold tracking-tight text-amber-700">
          {worklist?.checkedInAppointments ?? 0}
        </p>
      </Card>
      <Card className="flex flex-col justify-between">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Đang khám
        </p>
        <p className="mt-1 text-2xl font-bold tracking-tight text-sky-700">
          {worklist?.inProgressEncounters ?? 0}
        </p>
      </Card>
      <Card className="flex flex-col justify-between">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Đã kê đơn
        </p>
        <p className="mt-1 text-2xl font-bold tracking-tight text-purple-700">
          {worklist?.issuedPrescriptions ?? 0}
        </p>
      </Card>
      <Card className="flex flex-col justify-between">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
          Hoàn thành
        </p>
        <p className="mt-1 text-2xl font-bold tracking-tight text-emerald-700">
          {worklist?.finalizedEncounters ?? 0}
        </p>
      </Card>
    </div>
  );
}
