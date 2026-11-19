"use client";

import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import {
  HospitalDoctorWorklistItem,
  HospitalDoctorWorklistResponse,
} from "@/services/types";
import { getStatusClass } from "../utils/doctorWorklistFormatters";

interface DoctorWorklistTableProps {
  worklist: HospitalDoctorWorklistResponse | null;
  workDate: string;
  groupedSummary: Record<string, number>;
  items: HospitalDoctorWorklistItem[];
  isLoading: boolean;
  listError: string | null;
  onRetry: () => void;
  t: (key: string) => string;
}

export function DoctorWorklistTable({
  worklist,
  workDate,
  groupedSummary,
  items,
  isLoading,
  listError,
  onRetry,
  t,
}: DoctorWorklistTableProps) {
  return (
    <Card padding="none">
      <div className="p-4 sm:p-5 border-b border-slate-100 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">
            {worklist?.doctorName || "Hồ sơ công việc"}
          </h2>
          <p className="text-xs text-slate-500 mt-0.5">
            {worklist?.specialtyName || "Tất cả chuyên khoa"} &middot; {workDate}
          </p>
        </div>
        <div className="flex flex-wrap gap-1.5">
          {Object.entries(groupedSummary).map(([stage, count]) => (
            <span
              key={stage}
              className={`inline-flex items-center rounded-md px-2 py-0.5 text-[11px] font-semibold ${getStatusClass(
                stage
              )}`}
            >
              {stage}: {count}
            </span>
          ))}
        </div>
      </div>

      {isLoading ? (
        <LoadingState title={t("common.actions.loading")} />
      ) : listError ? (
        <ErrorState
          title={t("common.messages.error")}
          description={listError}
          onAction={onRetry}
        />
      ) : items.length === 0 ? (
        <EmptyState
          title={t("common.labels.empty")}
          description={t("common.labels.noResults")}
        />
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-left text-xs">
            <thead className="border-b border-slate-200 bg-slate-50 text-slate-600 font-semibold">
              <tr>
                <th className="px-5 py-3">Thời gian</th>
                <th className="px-5 py-3">Bệnh nhân</th>
                <th className="px-5 py-3">Bác sĩ / Phòng</th>
                <th className="px-5 py-3">Hồ sơ khám</th>
                <th className="px-5 py-3">Đơn thuốc</th>
                <th className="px-5 py-3">Trạng thái luồng</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 text-slate-800">
              {items.map((item) => (
                <tr
                  key={item.appointmentId}
                  className="transition-colors hover:bg-slate-50/70"
                >
                  <td className="px-5 py-3.5">
                    <div className="font-semibold text-slate-900 font-mono">
                      {formatDateTimeValue(item.appointmentStartLocal)}
                    </div>
                    <div className="mt-0.5 text-[11px] text-slate-500">
                      {item.appointmentNumber}
                    </div>
                  </td>
                  <td className="px-5 py-3.5">
                    <div className="font-semibold text-slate-900">
                      {item.patientName}
                    </div>
                    <div className="mt-0.5 text-[11px] text-slate-500 font-mono">
                      {item.medicalRecordNumber}
                    </div>
                  </td>
                  <td className="px-5 py-3.5">
                    <div className="font-medium text-slate-900">{item.doctorName}</div>
                    <div className="mt-0.5 text-[11px] text-slate-500">
                      {item.clinicName || item.specialtyName}
                    </div>
                  </td>
                  <td className="px-5 py-3.5">
                    <div className="font-medium text-slate-900">
                      {item.encounterNumber || "--"}
                    </div>
                    <div className="text-[11px] text-slate-500">
                      {item.encounterStatus || "Chưa mở hồ sơ"}
                    </div>
                  </td>
                  <td className="px-5 py-3.5 font-mono text-slate-700">
                    {item.prescriptionNumber || "--"}
                  </td>
                  <td className="px-5 py-3.5">
                    <span
                      className={`inline-flex items-center rounded-md px-2 py-0.5 text-[11px] font-semibold ${getStatusClass(
                        item.workflowStage
                      )}`}
                    >
                      {item.workflowStage}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Card>
  );
}
