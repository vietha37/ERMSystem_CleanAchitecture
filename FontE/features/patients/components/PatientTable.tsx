import React from "react";
import { ErrorState } from "@/components/ui/DataState";
import { PatientViewModel } from "../types";
import { PatientTableRow } from "./PatientTableRow";

interface PatientTableProps {
  patients: PatientViewModel[];
  isLoading: boolean;
  listError: string | null;
  debouncedSearch: string;
  isDoctor: boolean;
  onRetry: () => void;
  onSelectPatient: (patient: PatientViewModel) => void;
  onEditPatient: (patient: PatientViewModel, event: React.MouseEvent) => void;
  onDeletePatient: (id: string, event: React.MouseEvent) => void;
}

export function PatientTable({
  patients,
  isLoading,
  listError,
  debouncedSearch,
  isDoctor,
  onRetry,
  onSelectPatient,
  onEditPatient,
  onDeletePatient,
}: PatientTableProps) {
  return (
    <div className="relative h-[600px] overflow-hidden rounded-2xl border border-gray-100 bg-white shadow-sm">
      <div className="h-full overflow-y-auto">
        <table className="min-w-max w-full border-collapse text-left">
          <thead className="sticky top-0 z-10 border-b border-gray-200 bg-white/95 shadow-sm backdrop-blur-md">
            <tr>
              <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                <span className="sr-only">Ảnh</span>
              </th>
              <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                Họ và tên
              </th>
              <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                Mã bệnh nhân
              </th>
              <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                Điện thoại
              </th>
              <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                Bác sĩ phụ trách
              </th>
              <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                Lần khám gần nhất
              </th>
              <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">
                Trạng thái
              </th>
              <th className="p-4 text-right text-xs font-bold uppercase tracking-wider text-gray-500">
                Thao tác
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-50">
            {isLoading ? (
              Array.from({ length: 5 }).map((_, index) => (
                <tr key={index} className="animate-pulse border-b border-gray-50">
                  <td className="p-4">
                    <div className="h-10 w-10 rounded-full bg-gray-200" />
                  </td>
                  <td className="p-4">
                    <div className="h-4 w-40 rounded bg-gray-200" />
                  </td>
                  <td className="p-4">
                    <div className="h-4 w-24 rounded bg-gray-200" />
                  </td>
                  <td className="p-4">
                    <div className="h-4 w-28 rounded bg-gray-200" />
                  </td>
                  <td className="p-4">
                    <div className="h-4 w-32 rounded bg-gray-200" />
                  </td>
                  <td className="p-4">
                    <div className="h-4 w-28 rounded bg-gray-200" />
                  </td>
                  <td className="p-4">
                    <div className="h-6 w-24 rounded-full bg-gray-200" />
                  </td>
                  <td className="p-4">
                    <div className="ml-auto h-8 w-20 rounded-lg bg-gray-200" />
                  </td>
                </tr>
              ))
            ) : listError ? (
              <tr>
                <td colSpan={8}>
                  <ErrorState
                    title="Không thể tải danh sách bệnh nhân"
                    description={listError}
                    onAction={onRetry}
                  />
                </td>
              </tr>
            ) : patients.length === 0 ? (
              <tr>
                <td colSpan={8} className="p-16 text-center text-gray-500">
                  <div className="mb-3 font-semibold text-gray-400">Hồ sơ trống</div>
                  <p className="font-medium">
                    {debouncedSearch
                      ? "Không có bệnh nhân nào khớp điều kiện tìm kiếm."
                      : "Chưa có hồ sơ bệnh nhân phù hợp."}
                  </p>
                </td>
              </tr>
            ) : (
              patients.map((patient) => (
                <PatientTableRow
                  key={patient.id}
                  patient={patient}
                  isDoctor={isDoctor}
                  onSelectPatient={onSelectPatient}
                  onEditPatient={onEditPatient}
                  onDeletePatient={onDeletePatient}
                />
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
