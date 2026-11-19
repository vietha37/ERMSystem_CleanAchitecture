"use client";

import { Button } from "@/components/ui/Button";
import { HospitalPrescriptionSummary } from "../types";
import {
  formatDateTime,
  getStatusClass,
  getStatusLabel,
} from "../utils/prescriptionFormatters";

interface PrescriptionTableRowProps {
  prescription: HospitalPrescriptionSummary;
  role?: string | null;
  onOpenDetail: (id: string) => void;
  onDelete: (id: string) => void;
}

export function PrescriptionTableRow({
  prescription,
  role,
  onOpenDetail,
  onDelete,
}: PrescriptionTableRowProps) {
  return (
    <tr className="border-t border-slate-100 align-top transition-colors hover:bg-cyan-50/30">
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">
          {prescription.prescriptionNumber}
        </div>
        <div className="mt-1 text-sm text-slate-500">
          {prescription.encounterNumber}
        </div>
        <div className="mt-1 text-xs text-slate-400">
          {prescription.totalItems} thuốc
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">
          {prescription.patientName}
        </div>
        <div className="mt-1 text-sm text-slate-500">
          {prescription.medicalRecordNumber}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">
          {prescription.doctorName}
        </div>
        <div className="mt-1 text-sm text-slate-500">
          {prescription.specialtyName}
        </div>
        <div className="mt-1 text-sm text-slate-500">{prescription.clinicName}</div>
      </td>
      <td className="px-6 py-4">
        <div className="font-semibold text-cyan-700">
          {prescription.primaryDiagnosisName || "--"}
        </div>
        <div className="mt-1 text-sm text-slate-500">
          {prescription.notes || "Không có ghi chú thêm."}
        </div>
      </td>
      <td className="px-6 py-4">
        <span
          className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getStatusClass(
            prescription.status
          )}`}
        >
          {getStatusLabel(prescription.status)}
        </span>
      </td>
      <td className="px-6 py-4 text-sm text-slate-600">
        {formatDateTime(prescription.createdAtLocal)}
      </td>
      <td className="px-6 py-4">
        <div className="flex flex-wrap gap-2">
          <Button
            variant="secondary"
            className="border-cyan-200 text-cyan-700 hover:bg-cyan-50"
            onClick={() => onOpenDetail(prescription.prescriptionId)}
          >
            Xem
          </Button>
          {role !== "Doctor" && (
            <Button
              variant="secondary"
              className="border-rose-200 text-rose-700 hover:bg-rose-50"
              onClick={() => onDelete(prescription.prescriptionId)}
            >
              Xóa
            </Button>
          )}
        </div>
      </td>
    </tr>
  );
}
