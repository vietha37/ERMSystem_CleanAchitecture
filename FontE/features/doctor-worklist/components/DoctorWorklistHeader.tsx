"use client";

import { Button } from "@/components/ui/Button";
import { hospitalDoctorService } from "@/services/hospitalDoctorService";
import { HospitalDoctorWorklistResponse } from "@/services/types";

type DoctorOption = Awaited<ReturnType<typeof hospitalDoctorService.getAll>>[number];

interface DoctorWorklistHeaderProps {
  workDate: string;
  onWorkDateChange: (val: string) => void;
  canSelectDoctor: boolean;
  doctorFilter: string;
  onDoctorFilterChange: (val: string) => void;
  doctors: DoctorOption[];
  isRefreshing: boolean;
  onRefresh: () => void;
  worklist: HospitalDoctorWorklistResponse | null;
  t: (key: string) => string;
}

export function DoctorWorklistHeader({
  workDate,
  onWorkDateChange,
  canSelectDoctor,
  doctorFilter,
  onDoctorFilterChange,
  doctors,
  isRefreshing,
  onRefresh,
  worklist,
  t,
}: DoctorWorklistHeaderProps) {
  return (
    <div className="space-y-4">
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
            onChange={(e) => onWorkDateChange(e.target.value)}
            className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
          />

          {canSelectDoctor && (
            <select
              value={doctorFilter}
              onChange={(e) => onDoctorFilterChange(e.target.value)}
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
            onClick={onRefresh}
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
    </div>
  );
}
