"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { useAuth } from "@/hooks/useAuth";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { getApiErrorMessage } from "@/services/error";
import { hospitalDoctorService } from "@/services/hospitalDoctorService";
import { hospitalDoctorWorklistService } from "@/services/hospitalDoctorWorklistService";
import {
  HospitalDoctorWorklistItem,
  HospitalDoctorWorklistResponse,
} from "@/services/types";
import toast from "react-hot-toast";

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
      return "border border-cyan-200 bg-cyan-50 text-cyan-700";
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
  const [workDate, setWorkDate] = useState("");
  const [doctorFilter, setDoctorFilter] = useState("");
  const [doctors, setDoctors] = useState<
    Awaited<ReturnType<typeof hospitalDoctorService.getAll>>
  >([]);
  const [worklist, setWorklist] = useState<HospitalDoctorWorklistResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);

  const canSelectDoctor = role === "Admin" || role === "Receptionist";

  useEffect(() => {
    setWorkDate(toDateInputValue(new Date()));
  }, []);

  const fetchPageData = useCallback(
    async (showRefreshState = false) => {
      if (!workDate) {
        return;
      }

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
      } catch (error: unknown) {
        toast.error(getApiErrorMessage(error, "Không thể tải danh sách công việc bác sĩ."));
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [canSelectDoctor, doctorFilter, workDate]
  );

  useEffect(() => {
    if (!workDate) {
      return;
    }

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
    <div className="mx-auto max-w-7xl space-y-6">
      <div className="rounded-[2rem] border border-blue-100 bg-gradient-to-br from-blue-50 via-white to-cyan-50 p-6 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.26em] text-blue-700">
              Vận hành lâm sàng
            </p>
            <h1 className="mt-3 text-3xl font-bold text-slate-950">
              Công việc bác sĩ trong ngày
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-7 text-slate-600">
              Theo dõi luồng khám theo từng bác sĩ: lịch hẹn, hồ sơ khám và đơn thuốc.
            </p>
          </div>

          <div className="flex flex-col gap-3 md:flex-row md:items-center">
            <input
              type="date"
              value={workDate}
              onChange={(event) => setWorkDate(event.target.value)}
              className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
            />

            {canSelectDoctor && (
              <select
                value={doctorFilter}
                onChange={(event) => setDoctorFilter(event.target.value)}
                className="min-w-[280px] rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
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
              onClick={() => void fetchPageData(true)}
              disabled={isRefreshing}
            >
              {isRefreshing ? "Đang làm mới..." : "Làm mới"}
            </Button>
          </div>
        </div>
      </div>

      {worklist && !worklist.isDoctorResolved && (
        <Card className="border border-amber-200 bg-amber-50 p-5 text-sm text-amber-800 shadow-sm">
          {worklist.resolutionMessage || "Chưa xác định được hồ sơ bác sĩ hiện tại."}
        </Card>
      )}

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
        <MetricCard label="Tổng lịch hẹn" value={worklist?.totalAppointments ?? 0} tone="slate" />
        <MetricCard label="Đã check-in" value={worklist?.checkedInAppointments ?? 0} tone="amber" />
        <MetricCard
          label="Đang khám"
          value={worklist?.inProgressEncounters ?? 0}
          tone="cyan"
        />
        <MetricCard
          label="Đã chốt hồ sơ"
          value={worklist?.finalizedEncounters ?? 0}
          tone="violet"
        />
        <MetricCard
          label="Đã kê đơn"
          value={worklist?.issuedPrescriptions ?? 0}
          tone="emerald"
        />
      </div>

      <Card className="border border-slate-100 p-5 shadow-sm">
        <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 className="text-lg font-bold text-slate-900">
              {worklist?.doctorName
                ? `Công việc của ${worklist.doctorName}`
                : "Công việc theo ngày"}
            </h2>
            <p className="text-sm text-slate-500">
              {worklist?.specialtyName || "Tất cả chuyên khoa"} / {workDate}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            {Object.entries(groupedSummary).map(([stage, count]) => (
              <span
                key={stage}
                className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getStatusClass(stage)}`}
              >
                {stage}: {count}
              </span>
            ))}
          </div>
        </div>
      </Card>

      <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
        {isLoading ? (
          <div className="flex flex-col items-center justify-center p-16">
            <div className="mb-4 h-10 w-10 animate-spin rounded-full border-4 border-blue-100 border-t-blue-600" />
            <p className="text-sm font-medium text-slate-500">
              Đang tải công việc bác sĩ...
            </p>
          </div>
        ) : items.length === 0 ? (
          <div className="p-16 text-center text-sm text-slate-500">
            Không có ca khám nào cho bộ lọc hiện tại.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-[1240px] w-full border-collapse text-left">
              <thead>
                <tr className="bg-slate-50">
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Thời gian
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Bệnh nhân
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Bác sĩ / phòng
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Hồ sơ khám
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Đơn thuốc
                  </th>
                  <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                    Trạng thái luồng
                  </th>
                </tr>
              </thead>
              <tbody>
                {items.map((item: HospitalDoctorWorklistItem) => (
                  <tr
                    key={item.appointmentId}
                    className="border-t border-slate-100 align-top transition-colors hover:bg-blue-50/30"
                  >
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">
                        {formatDateTime(item.appointmentStartLocal)}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {item.appointmentNumber}
                      </div>
                      <div className="mt-1 text-xs text-slate-400">
                        {item.appointmentStatus}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{item.patientName}</div>
                      <div className="mt-1 text-sm text-slate-500">
                        {item.medicalRecordNumber}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{item.doctorName}</div>
                      <div className="mt-1 text-sm text-slate-500">{item.specialtyName}</div>
                      <div className="mt-1 text-sm text-slate-500">{item.clinicName}</div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">
                        {item.encounterNumber || "--"}
                      </div>
                      <div className="mt-1 text-sm text-slate-500">
                        {item.encounterStatus || "Chưa mở hồ sơ"}
                      </div>
                      <div className="mt-1 text-xs text-slate-400">
                        {item.primaryDiagnosisName || "--"}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">
                        {item.prescriptionNumber || "--"}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getStatusClass(
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

function MetricCard({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone: "slate" | "amber" | "cyan" | "violet" | "emerald";
}) {
  const toneClasses = {
    slate: "border-slate-100 bg-slate-50/70 text-slate-700",
    amber: "border-amber-100 bg-amber-50/70 text-amber-700",
    cyan: "border-cyan-100 bg-cyan-50/70 text-cyan-700",
    violet: "border-violet-100 bg-violet-50/70 text-violet-700",
    emerald: "border-emerald-100 bg-emerald-50/70 text-emerald-700",
  };

  return (
    <Card className={`border p-5 shadow-sm hover:shadow-sm ${toneClasses[tone]}`}>
      <p className="text-xs font-bold uppercase tracking-[0.2em]">{label}</p>
      <p className="mt-3 text-3xl font-bold text-slate-950">{value}</p>
    </Card>
  );
}
