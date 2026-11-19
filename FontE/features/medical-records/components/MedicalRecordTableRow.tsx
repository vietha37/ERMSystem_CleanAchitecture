"use client";

import { Button } from "@/components/ui/Button";
import { HospitalEncounterSummary } from "../types";
import {
  formatDateTime,
  getEncounterStatusClass,
  getEncounterStatusLabel,
} from "../utils/medicalRecordFormatters";

interface MedicalRecordTableRowProps {
  encounter: HospitalEncounterSummary;
  approvingEncounterId: string | null;
  onOpenEdit: (id: string) => void;
  onApprove: (id: string) => void;
}

export function MedicalRecordTableRow({
  encounter,
  approvingEncounterId,
  onOpenEdit,
  onApprove,
}: MedicalRecordTableRowProps) {
  return (
    <tr className="border-t border-slate-100 align-top transition-colors hover:bg-emerald-50/30">
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">
          {encounter.encounterNumber}
        </div>
        <div className="mt-1 text-sm text-slate-500">
          {encounter.appointmentNumber || "--"}
        </div>
        <div className="mt-1 text-xs text-slate-400">
          Bắt đầu: {formatDateTime(encounter.startedAtLocal)}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">{encounter.patientName}</div>
        <div className="mt-1 text-sm text-slate-500">
          {encounter.medicalRecordNumber}
        </div>
        <div className="mt-1 text-xs text-slate-400">
          Lịch khám: {formatDateTime(encounter.appointmentStartLocal)}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">{encounter.doctorName}</div>
        <div className="mt-1 text-sm text-slate-500">{encounter.specialtyName}</div>
        <div className="mt-1 text-sm text-slate-500">{encounter.clinicName}</div>
      </td>
      <td className="px-6 py-4">
        <div className="font-semibold text-emerald-700">
          {encounter.primaryDiagnosisName || "--"}
        </div>
        <div className="mt-1 max-w-[280px] text-sm text-slate-500">
          {encounter.summary || "Chưa có tóm tắt bệnh án."}
        </div>
      </td>
      <td className="px-6 py-4">
        <span
          className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getEncounterStatusClass(
            encounter.encounterStatus
          )}`}
        >
          {getEncounterStatusLabel(encounter.encounterStatus)}
        </span>
      </td>
      <td className="px-6 py-4 text-sm text-slate-600">
        <div>{formatDateTime(encounter.updatedAtLocal)}</div>
        <div className="mt-1 text-xs text-slate-400">
          Kết thúc: {formatDateTime(encounter.endedAtLocal)}
        </div>
        {encounter.encounterStatus === "Approved" ? (
          <div className="mt-1 text-xs font-medium text-cyan-700">
            Hồ sơ đã duyệt.
          </div>
        ) : null}
      </td>
      <td className="px-6 py-4">
        <div className="flex flex-wrap gap-2">
          <Button
            variant="secondary"
            className="border-emerald-200 text-emerald-700 hover:bg-emerald-50"
            onClick={() => onOpenEdit(encounter.encounterId)}
          >
            {encounter.encounterStatus === "Approved" ? "Xem" : "Cập nhật"}
          </Button>
          {encounter.encounterStatus === "Finalized" ? (
            <Button
              variant="secondary"
              className="border-cyan-200 text-cyan-700 hover:bg-cyan-50"
              onClick={() => onApprove(encounter.encounterId)}
              disabled={approvingEncounterId === encounter.encounterId}
            >
              {approvingEncounterId === encounter.encounterId
                ? "Đang duyệt..."
                : "Duyệt hồ sơ"}
            </Button>
          ) : null}
        </div>
      </td>
    </tr>
  );
}
