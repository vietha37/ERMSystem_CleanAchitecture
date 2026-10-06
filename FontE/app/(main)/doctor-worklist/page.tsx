"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { useAuth } from "@/hooks/useAuth";
import { useTranslation } from "@/hooks/useTranslation";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalDoctorService } from "@/services/hospitalDoctorService";
import { hospitalDoctorWorklistService } from "@/services/hospitalDoctorWorklistService";
import {
  HospitalDoctorWorklistItem,
  HospitalDoctorWorklistResponse,
} from "@/services/types";

function toDateInputValue(date: Date): string {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

function formatDateTime(value?: string | null): string {
  return formatDateTimeValue(value);
}

function getStatusClass(stage: string): string {
  switch (stage) {
    case "Cho tiep don":
      return "border border-slate-200 bg-slate-100 text-slate-700";
    case "Cho mo ho so":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "Dang kham":
      return "border border-sky-200 bg-sky-50 text-sky-700";
    case "Cho ke don":
      return "border border-violet-200 bg-violet-50 text-violet-700";
    case "Da ke don":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Da hoan thanh":
      return "border border-blue-200 bg-blue-50 text-blue-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}

export default function DoctorWorklistPage() {
  const { role } = useAuth();
  const { t } = useTranslation();
  const [workDate, setWorkDate] = useState("");
  const [doctorFilter, setDoctorFilter] = useState("");
  const [doctors, setDoctors] = useState<
    Awaited<ReturnType<typeof hospitalDoctorService.getAll>>
  >([]);
  const [worklist, setWorklist] = useState<HospitalDoctorWorklistResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);

  const canSelectDoctor = role === "Admin";

  useEffect(() => {
    setWorkDate(toDateInputValue(new Date()));
  }, []);

  const fetchPageData = useCallback(
    async (showRefreshState = false) => {
      if (!workDate) return;

      if (showRefreshState) {
        setIsRefreshing(true);
      } else {
        setIsLoading(true);
      }

      try {
        const [doctorData, worklistData] = await Promise.all([
          canSelectDoctor ? hospitalDoctorService.getAll() : Promise.resolve([]),
          hospitalDoctorWorklistService.get(
            workDate,
            canSelectDoctor ? doctorFilter || undefined : undefined
          ),
        ]);

        setDoctors(doctorData);
        setWorklist(worklistData);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, t("common.messages.error"));
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [canSelectDoctor, doctorFilter, workDate, t]
  );

  useEffect(() => {
    if (!workDate) return;
    void fetchPageData();
  }, [fetchPageData, workDate]);

  const items = useMemo(() => worklist?.items ?? [], [worklist]);

  const groupedSummary = useMemo(() => {
    return items.reduce<Record<string, number>>((acc, item) => {
      acc[item.workflowStage] = (acc[item.workflowStage] ?? 0) + 1;
      return acc;
    }, {});
  }, [items]);

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border-b border-slate-200 pb-5">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-slate-900">
            {t("doctorWorklist.title")}
          </h1>
          <p className="mt-1 text-xs text-slate-500">
            {t("doctorWorklist.subtitle")}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <input
            type="date"
            value={workDate}
            onChange={(e) => setWorkDate(e.target.value)}
            className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
          />

          {canSelectDoctor && (
            <select
              value={doctorFilter}
              onChange={(e) => setDoctorFilter(e.target.value)}
              className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-700 shadow-2xs outline-none focus:border-slate-500"
            >
              <option value="">Tất cả bác sĩ</option>
              {doctors.map((doctor) => (
                <option key={doctor.doctorProfileId} value={doctor.doctorProfileId}>
                  {doctor.fullName} - {doctor.specialtyName}
                </option>
              ))}
            </select>
          )}

          <Button
            variant="secondary"
            size="sm"
            onClick={() => void fetchPageData(true)}
            disabled={isRefreshing}
          >
            {isRefreshing ? t("common.actions.processing") : t("common.actions.refresh")}
          </Button>
        </div>
      </div>

      {worklist && !worklist.isDoctorResolved && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-xs text-amber-800">
          {worklist.resolutionMessage || "Chưa xác định được hồ sơ bác sĩ hiện tại."}
        </div>
      )}

      {/* Metric Cards */}
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

      {/* Main Table Card */}
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
            onAction={() => void fetchPageData(true)}
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
                {items.map((item: HospitalDoctorWorklistItem) => (
                  <tr
                    key={item.appointmentId}
                    className="transition-colors hover:bg-slate-50/70"
                  >
                    <td className="px-5 py-3.5">
                      <div className="font-semibold text-slate-900 font-mono">
                        {formatDateTime(item.appointmentStartLocal)}
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
    </div>
  );
}
