"use client";

import { EncounterFormState } from "../types";

interface EncounterVitalSignsFieldsProps {
  form: EncounterFormState;
  onChange: (field: keyof EncounterFormState, value: string) => void;
  disabled?: boolean;
}

function NumberField({
  label,
  value,
  onChange,
  disabled = false,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  return (
    <div>
      <label className="mb-2 block text-sm font-medium text-slate-700">{label}</label>
      <input
        type="number"
        step="0.1"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
        className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100 disabled:bg-slate-100"
      />
    </div>
  );
}

export function EncounterVitalSignsFields({
  form,
  onChange,
  disabled = false,
}: EncounterVitalSignsFieldsProps) {
  return (
    <div className="grid gap-4 md:grid-cols-4">
      <NumberField
        label="Chiều cao (cm)"
        value={form.heightCm}
        onChange={(value) => onChange("heightCm", value)}
        disabled={disabled}
      />
      <NumberField
        label="Cân nặng (kg)"
        value={form.weightKg}
        onChange={(value) => onChange("weightKg", value)}
        disabled={disabled}
      />
      <NumberField
        label="Nhiệt độ (C)"
        value={form.temperatureC}
        onChange={(value) => onChange("temperatureC", value)}
        disabled={disabled}
      />
      <NumberField
        label="Mạch"
        value={form.pulseRate}
        onChange={(value) => onChange("pulseRate", value)}
        disabled={disabled}
      />
      <NumberField
        label="Nhịp thở"
        value={form.respiratoryRate}
        onChange={(value) => onChange("respiratoryRate", value)}
        disabled={disabled}
      />
      <NumberField
        label="HA tâm thu"
        value={form.systolicBp}
        onChange={(value) => onChange("systolicBp", value)}
        disabled={disabled}
      />
      <NumberField
        label="HA tâm trương"
        value={form.diastolicBp}
        onChange={(value) => onChange("diastolicBp", value)}
        disabled={disabled}
      />
      <NumberField
        label="SpO2 (%)"
        value={form.oxygenSaturation}
        onChange={(value) => onChange("oxygenSaturation", value)}
        disabled={disabled}
      />
    </div>
  );
}
