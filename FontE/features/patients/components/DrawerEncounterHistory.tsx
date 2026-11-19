import React from "react";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { HospitalEncounterSummary } from "@/services/types";

interface DrawerEncounterHistoryProps {
  encounters: HospitalEncounterSummary[];
  isLoading: boolean;
}

export function DrawerEncounterHistory({
  encounters,
  isLoading,
}: DrawerEncounterHistoryProps) {
  return (
    <div>
      <h4 className="mb-3 px-1 text-sm font-bold uppercase tracking-wide text-gray-800">
        Hồ sơ khám gần đây
      </h4>
      <div className="space-y-3">
        {isLoading ? (
          <div className="rounded-2xl border border-gray-100 bg-white p-4 text-sm text-gray-400">
            Đang tải hồ sơ khám...
          </div>
        ) : encounters.length === 0 ? (
          <div className="rounded-2xl border border-gray-100 bg-white p-4 text-sm text-gray-400">
            Chưa có hồ sơ khám nào.
          </div>
        ) : (
          encounters.slice(0, 4).map((encounter) => (
            <div
              key={encounter.encounterId}
              className="rounded-2xl border border-gray-100 bg-white p-4 shadow-sm"
            >
              <div className="flex items-start justify-between gap-3">
                <div>
                  <div className="text-sm font-bold text-gray-900">
                    {encounter.encounterNumber}
                  </div>
                  <div className="mt-1 text-xs text-gray-500">
                    {formatDateTimeValue(encounter.startedAtLocal)}
                  </div>
                </div>
                <span className="rounded-full bg-gray-100 px-2.5 py-1 text-xs font-semibold text-gray-700">
                  {encounter.encounterStatus}
                </span>
              </div>
              <div className="mt-3 text-sm text-gray-700">
                <div>
                  <span className="font-semibold">Chẩn đoán:</span>{" "}
                  {encounter.primaryDiagnosisName || "Chưa có"}
                </div>
                <div className="mt-1">
                  <span className="font-semibold">Tóm tắt:</span>{" "}
                  {encounter.summary || "Chưa có ghi chú tóm tắt"}
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
