import React from "react";
import { formatDateTimeValue, formatDateValue } from "@/lib/dateFormatting";
import { PatientViewModel } from "../types";
import { getPatientInitials } from "../utils/patientFormatters";

interface PatientTableRowProps {
  patient: PatientViewModel;
  isDoctor: boolean;
  onSelectPatient: (patient: PatientViewModel) => void;
  onEditPatient: (patient: PatientViewModel, event: React.MouseEvent) => void;
  onDeletePatient: (id: string, event: React.MouseEvent) => void;
}

export function PatientTableRow({
  patient,
  isDoctor,
  onSelectPatient,
  onEditPatient,
  onDeletePatient,
}: PatientTableRowProps) {
  return (
    <tr
      onClick={() => onSelectPatient(patient)}
      className="cursor-pointer bg-white transition-all duration-200 hover:-translate-y-0.5 hover:bg-blue-50/40 hover:shadow-md"
    >
      <td className="p-4">
        <div className="flex h-10 w-10 items-center justify-center rounded-full border border-blue-200 bg-gradient-to-br from-blue-100 to-blue-200 text-sm font-bold text-blue-700 shadow-sm">
          {getPatientInitials(patient.fullName)}
        </div>
      </td>
      <td className="p-4">
        <div className="font-semibold text-gray-800">{patient.fullName}</div>
        <div className="mt-0.5 text-xs text-gray-500">
          {patient.dateOfBirth
            ? formatDateValue(patient.dateOfBirth, "Chưa có ngày sinh")
            : "Chưa có ngày sinh"}
        </div>
      </td>
      <td className="p-4 text-sm font-semibold text-blue-700">
        {patient.medicalRecordNumber || "Chưa có"}
      </td>
      <td className="p-4 text-sm font-medium text-gray-600">
        {patient.phone || "Chưa có"}
      </td>
      <td className="p-4 text-sm text-gray-600">
        {patient.doctorName || "Chưa phân công"}
      </td>
      <td className="p-4 text-sm text-gray-600">
        {patient.latestVisitDate
          ? formatDateTimeValue(patient.latestVisitDate, "Chưa từng khám")
          : "Chưa từng khám"}
      </td>
      <td className="p-4">
        <span className="inline-flex w-max items-center gap-1.5 rounded-full border border-green-100 bg-green-50 px-2.5 py-1 text-xs font-bold text-green-700">
          <span className="h-2 w-2 animate-pulse rounded-full bg-green-500" />
          {patient.status || "Đang theo dõi"}
        </span>
      </td>
      <td className="p-4">
        <div className="flex justify-end gap-2">
          {!isDoctor && (
            <button
              onClick={(event) => onEditPatient(patient, event)}
              className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-sm font-semibold text-gray-600 shadow-sm transition-colors hover:border-blue-300 hover:bg-blue-50 hover:text-blue-600"
              title="Sửa hồ sơ bệnh nhân"
            >
              Sửa
            </button>
          )}
          {!isDoctor && (
            <button
              onClick={(event) => onDeletePatient(patient.id, event)}
              className="rounded-lg border border-red-100 bg-white px-3 py-1.5 text-sm font-semibold text-red-500 shadow-sm transition-colors hover:border-red-600 hover:bg-red-500 hover:text-white"
              title="Xóa hồ sơ bệnh nhân"
            >
              Xóa
            </button>
          )}
          {isDoctor && (
            <span className="rounded-lg border border-blue-100 bg-blue-50 px-3 py-1.5 text-sm font-semibold text-blue-700">
              Chỉ xem
            </span>
          )}
        </div>
      </td>
    </tr>
  );
}
