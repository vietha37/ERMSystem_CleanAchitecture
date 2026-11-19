"use client";

import {
  EncounterFormState,
  HospitalEncounterEligibleAppointment,
  HospitalEncounterStatus,
} from "../types";
import { formatDateTime } from "../utils/medicalRecordFormatters";

interface EncounterBasicFieldsProps {
  form: EncounterFormState;
  onChange: (field: keyof EncounterFormState, value: string) => void;
  availableAppointments: HospitalEncounterEligibleAppointment[];
  isEditing: boolean;
  disabled: boolean;
}

export function EncounterBasicFields({
  form,
  onChange,
  availableAppointments,
  isEditing,
  disabled,
}: EncounterBasicFieldsProps) {
  return (
    <>
      {!isEditing && (
        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Lịch hẹn đã check-in / đã hoàn thành
          </label>
          <select
            value={form.appointmentId}
            onChange={(event) => onChange("appointmentId", event.target.value)}
            className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            required
            disabled={disabled}
          >
            <option value="">-- Chọn lịch hẹn --</option>
            {availableAppointments.map((appointment) => (
              <option key={appointment.appointmentId} value={appointment.appointmentId}>
                {appointment.appointmentNumber} - {appointment.patientName} -{" "}
                {formatDateTime(appointment.appointmentStartLocal)}
              </option>
            ))}
          </select>
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Chẩn đoán
          </label>
          <input
            type="text"
            value={form.diagnosisName}
            onChange={(event) => onChange("diagnosisName", event.target.value)}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            placeholder="Ví dụ: Tăng huyết áp"
            required
            disabled={disabled}
          />
        </div>

        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Mã chẩn đoán
          </label>
          <input
            type="text"
            value={form.diagnosisCode}
            onChange={(event) => onChange("diagnosisCode", event.target.value)}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            placeholder="ICD-10 nếu có"
            disabled={disabled}
          />
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Loại chẩn đoán
          </label>
          <input
            type="text"
            value={form.diagnosisType}
            onChange={(event) => onChange("diagnosisType", event.target.value)}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            placeholder="Working / Final"
            disabled={disabled}
          />
        </div>

        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Trạng thái hồ sơ khám
          </label>
          <select
            value={form.encounterStatus}
            onChange={(event) =>
              onChange("encounterStatus", event.target.value as HospitalEncounterStatus)
            }
            className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
            disabled={disabled}
          >
            <option value="InProgress">Đang khám</option>
            <option value="Finalized">Đã chốt hồ sơ</option>
          </select>
          {isEditing ? (
            <p className="mt-2 text-xs text-slate-500">
              Duyệt hồ sơ là bước riêng sau khi đã chốt hồ sơ khám.
            </p>
          ) : null}
        </div>
      </div>

      <div>
        <label className="mb-2 block text-sm font-medium text-slate-700">
          Tóm tắt hồ sơ
        </label>
        <textarea
          rows={3}
          value={form.summary}
          onChange={(event) => onChange("summary", event.target.value)}
          className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          placeholder="Tóm tắt diễn biến và kết luận chung"
          disabled={disabled}
        />
      </div>
    </>
  );
}
