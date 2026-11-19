"use client";

import { EncounterFormState } from "../types";

interface EncounterSoapFieldsProps {
  form: EncounterFormState;
  onChange: (field: keyof EncounterFormState, value: string) => void;
  disabled?: boolean;
}

function TextAreaField({
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
      <textarea
        rows={4}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
        className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100 disabled:bg-slate-100"
      />
    </div>
  );
}

export function EncounterSoapFields({
  form,
  onChange,
  disabled = false,
}: EncounterSoapFieldsProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2">
      <TextAreaField
        label="Triệu chứng / Subjective"
        value={form.subjective}
        onChange={(value) => onChange("subjective", value)}
        disabled={disabled}
      />
      <TextAreaField
        label="Khám thực thể / Objective"
        value={form.objective}
        onChange={(value) => onChange("objective", value)}
        disabled={disabled}
      />
      <TextAreaField
        label="Đánh giá / Assessment"
        value={form.assessment}
        onChange={(value) => onChange("assessment", value)}
        disabled={disabled}
      />
      <TextAreaField
        label="Hướng điều trị / Care plan"
        value={form.carePlan}
        onChange={(value) => onChange("carePlan", value)}
        disabled={disabled}
      />
    </div>
  );
}
