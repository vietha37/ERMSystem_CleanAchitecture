import React from "react";
import toast from "react-hot-toast";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { DrawerData, PatientViewModel } from "../types";
import { getPatientInitials } from "../utils/patientFormatters";
import { DrawerAppointmentHistory } from "./DrawerAppointmentHistory";
import { DrawerEncounterHistory } from "./DrawerEncounterHistory";

interface PatientDetailDrawerProps {
  isOpen: boolean;
  patient: PatientViewModel | null;
  drawerData: DrawerData;
  latestDiagnosis: string;
  isDoctor: boolean;
  onClose: () => void;
}

export function PatientDetailDrawer({
  isOpen,
  patient,
  drawerData,
  latestDiagnosis,
  isDoctor,
  onClose,
}: PatientDetailDrawerProps) {
  if (!isOpen || !patient) return null;

  return (
    <>
      <div
        className="fixed inset-0 z-40 bg-gray-900/30 backdrop-blur-sm transition-opacity duration-300"
        onClick={onClose}
      />

      <div className="fixed right-0 top-0 z-50 flex h-full w-full max-w-md transform flex-col bg-white shadow-2xl transition-transform duration-300 ease-in-out translate-x-0">
        <div className="sticky top-0 z-10 flex items-center justify-between border-b border-gray-100 bg-blue-50/30 px-6 py-5 backdrop-blur-sm">
          <h2 className="text-xl font-bold text-gray-800">Chi tiết bệnh nhân</h2>
          <button
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-full bg-gray-100 font-bold text-gray-500 transition-colors hover:bg-red-100 hover:text-red-500"
          >
            ×
          </button>
        </div>

        <div className="flex-1 space-y-8 overflow-y-auto p-6">
          <div className="flex flex-col items-center text-center">
            <div className="mb-4 flex h-24 w-24 items-center justify-center rounded-full border-4 border-blue-50 bg-gradient-to-br from-blue-500 to-blue-600 text-3xl font-bold text-white shadow-lg">
              {getPatientInitials(patient.fullName)}
            </div>
            <h3 className="text-2xl font-bold text-gray-900">{patient.fullName}</h3>
            <div className="mt-2 flex flex-wrap items-center justify-center gap-2">
              <span className="rounded-full bg-green-100 px-3 py-1 text-xs font-bold uppercase tracking-wide text-green-700">
                {patient.status || "Đang theo dõi"}
              </span>
              <span className="rounded-full bg-gray-100 px-3 py-1 text-xs font-bold text-gray-600">
                Mã: {patient.medicalRecordNumber || "Chưa có"}
              </span>
            </div>
          </div>

          <div className="space-y-3 rounded-2xl border border-gray-100 bg-gray-50 p-4 shadow-inner">
            <div className="flex items-start gap-3">
              <div className="mt-0.5 text-blue-500">DT</div>
              <div>
                <div className="text-xs font-bold uppercase text-gray-400">Số điện thoại</div>
                <div className="font-semibold text-gray-800">
                  {patient.phone || "Chưa có số điện thoại"}
                </div>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <div className="mt-0.5 text-blue-500">BS</div>
              <div>
                <div className="text-xs font-bold uppercase text-gray-400">
                  Bác sĩ phụ trách gần nhất
                </div>
                <div className="font-semibold text-gray-800">
                  {patient.doctorName || "Chưa phân công"}
                </div>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <div className="mt-0.5 text-blue-500">DC</div>
              <div>
                <div className="text-xs font-bold uppercase text-gray-400">Địa chỉ</div>
                <div className="font-semibold text-gray-800">
                  {patient.address || "Chưa có địa chỉ"}
                </div>
              </div>
            </div>
          </div>

          <div className="relative overflow-hidden rounded-2xl border border-blue-100 bg-gradient-to-br from-blue-50 to-blue-100/50 p-5 shadow-sm">
            <h4 className="mb-4 flex items-center gap-2 text-sm font-bold uppercase tracking-wide text-blue-800">
              <span className="h-2 w-2 rounded-full bg-blue-500" />
              Tóm tắt hồ sơ
            </h4>

            {drawerData.isLoading ? (
              <div className="space-y-3 animate-pulse">
                <div className="h-4 w-full rounded bg-blue-200/50" />
                <div className="h-4 w-2/3 rounded bg-blue-200/50" />
                <div className="h-4 w-3/4 rounded bg-blue-200/50" />
              </div>
            ) : (
              <div className="space-y-4">
                <div className="flex items-center justify-between rounded-xl border border-white bg-white/60 p-3">
                  <span className="text-sm font-medium text-gray-600">Tổng lịch hẹn</span>
                  <span className="text-lg font-bold text-blue-700">
                    {drawerData.appointments.length}
                  </span>
                </div>
                <div className="flex items-center justify-between rounded-xl border border-white bg-white/60 p-3">
                  <span className="text-sm font-medium text-gray-600">Lần khám gần nhất</span>
                  <span className="text-sm font-bold text-gray-800">
                    {drawerData.appointments[0]
                      ? formatDateTimeValue(drawerData.appointments[0].appointmentStartLocal, "Chưa từng")
                      : "Chưa từng"}
                  </span>
                </div>
                <div className="flex items-center justify-between rounded-xl border border-white bg-white/60 p-3">
                  <span className="text-sm font-medium text-gray-600">Chẩn đoán gần nhất</span>
                  <span className="text-sm font-bold text-blue-700">{latestDiagnosis}</span>
                </div>
              </div>
            )}
          </div>

          <DrawerAppointmentHistory
            appointments={drawerData.appointments}
            isLoading={drawerData.isLoading}
          />

          <DrawerEncounterHistory
            encounters={drawerData.encounters}
            isLoading={drawerData.isLoading}
          />
        </div>

        <div className="shrink-0 border-t border-gray-100 bg-gray-50 p-6">
          {isDoctor ? (
            <div className="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-800">
              Bác sĩ chỉ được xem bệnh nhân thuộc lịch khám và hồ sơ khám liên quan.
            </div>
          ) : (
            <div className="flex flex-col gap-3">
              <button
                onClick={() => {
                  toast("Chuyển sang màn tạo lịch hẹn...");
                  onClose();
                }}
                className="w-full rounded-xl bg-blue-600 py-3.5 font-bold text-white shadow-md transition-all hover:bg-blue-700 hover:shadow-lg"
              >
                + Tạo lịch hẹn
              </button>
              <button
                onClick={() => {
                  toast("Chuyển sang màn hồ sơ khám...");
                  onClose();
                }}
                className="w-full rounded-xl border border-blue-200 bg-white py-3.5 font-bold text-blue-700 shadow-sm transition-all hover:bg-gray-50"
              >
                Xem toàn bộ hồ sơ khám
              </button>
            </div>
          )}
        </div>
      </div>
    </>
  );
}
