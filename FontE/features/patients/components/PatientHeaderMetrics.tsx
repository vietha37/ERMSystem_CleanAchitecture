import React from "react";
import { Button } from "@/components/ui/Button";
import { PatientMetrics } from "../types";

interface PatientHeaderMetricsProps {
  isDoctor: boolean;
  metrics: PatientMetrics;
  onOpenCreateModal: () => void;
}

export function PatientHeaderMetrics({
  isDoctor,
  metrics,
  onOpenCreateModal,
}: PatientHeaderMetricsProps) {
  return (
    <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-gray-900">
            {isDoctor ? "Bệnh nhân liên quan" : "Bệnh nhân"}
          </h1>
          <p className="mt-1 text-sm text-gray-500">
            {isDoctor ? "" : "Danh sách bệnh nhân và hồ sơ khám."}
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          <div className="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-800">
            <div className="text-xs font-semibold uppercase tracking-wide text-blue-500">
              Hồ sơ đang theo dõi
            </div>
            <div className="mt-1 text-xl font-bold">{metrics.activePatients}</div>
          </div>
          <div className="rounded-xl border border-emerald-100 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
            <div className="text-xs font-semibold uppercase tracking-wide text-emerald-500">
              Có lần khám gần đây
            </div>
            <div className="mt-1 text-xl font-bold">{metrics.withRecentVisit}</div>
          </div>
          {!isDoctor && (
            <Button
              onClick={onOpenCreateModal}
              className="shadow-md transition-all hover:-translate-y-0.5 hover:shadow-lg"
            >
              + Thêm bệnh nhân
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
