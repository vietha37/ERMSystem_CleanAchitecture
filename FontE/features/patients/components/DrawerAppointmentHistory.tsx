import React from "react";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { HospitalAppointmentWorklistItem } from "@/services/types";

interface DrawerAppointmentHistoryProps {
  appointments: HospitalAppointmentWorklistItem[];
  isLoading: boolean;
}

export function DrawerAppointmentHistory({
  appointments,
  isLoading,
}: DrawerAppointmentHistoryProps) {
  return (
    <div>
      <h4 className="mb-3 px-1 text-sm font-bold uppercase tracking-wide text-gray-800">
        Lịch hẹn gần đây
      </h4>
      <div className="overflow-hidden rounded-2xl border border-gray-100 bg-white shadow-sm">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-gray-100 bg-gray-50">
            <tr>
              <th className="px-4 py-3 font-semibold text-gray-500">Ngày</th>
              <th className="px-4 py-3 font-semibold text-gray-500">Bác sĩ</th>
              <th className="px-4 py-3 font-semibold text-gray-500">Trạng thái</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-50">
            {isLoading ? (
              <tr>
                <td colSpan={3} className="px-4 py-6 text-center text-gray-400">
                  Đang tải lịch sử...
                </td>
              </tr>
            ) : appointments.length === 0 ? (
              <tr>
                <td colSpan={3} className="px-4 py-6 text-center text-gray-400">
                  Chưa có lịch hẹn nào.
                </td>
              </tr>
            ) : (
              appointments.slice(0, 5).map((appointment) => (
                <tr
                  key={appointment.appointmentId}
                  className="transition-colors hover:bg-blue-50/30"
                >
                  <td className="px-4 py-3 text-gray-600">
                    {formatDateTimeValue(appointment.appointmentStartLocal)}
                  </td>
                  <td className="px-4 py-3 font-semibold text-gray-800">
                    {appointment.doctorName}
                  </td>
                  <td className="px-4 py-3">
                    <span className="rounded-md bg-blue-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider text-blue-700">
                      {appointment.status}
                    </span>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
