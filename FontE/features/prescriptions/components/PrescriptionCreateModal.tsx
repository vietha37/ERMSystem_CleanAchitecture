"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import {
  HospitalMedicineCatalog,
  HospitalPrescriptionEligibleEncounter,
  PrescriptionItemForm,
} from "../types";

interface PrescriptionCreateModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (e: React.FormEvent) => void;
  isSubmitting: boolean;
  selectedEncounterId: string;
  onEncounterChange: (val: string) => void;
  availableEncounters: HospitalPrescriptionEligibleEncounter[];
  notes: string;
  onNotesChange: (val: string) => void;
  prescriptionItems: PrescriptionItemForm[];
  medicines: HospitalMedicineCatalog[];
  onAddRow: () => void;
  onRemoveRow: (index: number) => void;
  onUpdateRow: (
    index: number,
    field: keyof PrescriptionItemForm,
    value: string
  ) => void;
}

function InputField({
  label,
  value,
  onChange,
  placeholder,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  type?: "text" | "number";
}) {
  return (
    <div>
      <label className="mb-2 block text-sm font-medium text-slate-700">{label}</label>
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
      />
    </div>
  );
}

export function PrescriptionCreateModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
  selectedEncounterId,
  onEncounterChange,
  availableEncounters,
  notes,
  onNotesChange,
  prescriptionItems,
  medicines,
  onAddRow,
  onRemoveRow,
  onUpdateRow,
}: PrescriptionCreateModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Phát hành đơn thuốc">
      <form onSubmit={onSubmit} className="space-y-5">
        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Chọn hồ sơ khám
          </label>
          <select
            value={selectedEncounterId}
            onChange={(event) => onEncounterChange(event.target.value)}
            className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            required
          >
            <option value="">-- Chọn hồ sơ khám --</option>
            {availableEncounters.map((encounter) => (
              <option key={encounter.encounterId} value={encounter.encounterId}>
                {encounter.encounterNumber} - {encounter.patientName} -{" "}
                {encounter.primaryDiagnosisName || "Chưa có chẩn đoán"}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">Ghi chú</label>
          <textarea
            rows={3}
            value={notes}
            onChange={(event) => onNotesChange(event.target.value)}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            placeholder="Lưu ý sử dụng thuốc, hướng dẫn bổ sung..."
          />
        </div>

        <div className="space-y-4 border-t border-slate-100 pt-5">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold text-slate-800">Danh sách thuốc</h3>
            <Button type="button" variant="secondary" onClick={onAddRow}>
              Thêm dòng
            </Button>
          </div>

          {prescriptionItems.map((item, index) => (
            <div
              key={index}
              className="rounded-2xl border border-dashed border-slate-200 bg-slate-50/70 p-4"
            >
              <div className="grid gap-4 md:grid-cols-2">
                <div className="md:col-span-2">
                  <label className="mb-2 block text-sm font-medium text-slate-700">
                    Thuốc
                  </label>
                  <select
                    value={item.medicineId}
                    onChange={(event) =>
                      onUpdateRow(index, "medicineId", event.target.value)
                    }
                    className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                    required
                  >
                    <option value="">-- Chọn thuốc --</option>
                    {medicines.map((medicine) => (
                      <option key={medicine.medicineId} value={medicine.medicineId}>
                        {medicine.name} ({medicine.drugCode}){" "}
                        {medicine.strength ? `- ${medicine.strength}` : ""}
                      </option>
                    ))}
                  </select>
                </div>

                <InputField
                  label="Liều dùng"
                  value={item.doseInstruction}
                  onChange={(value) => onUpdateRow(index, "doseInstruction", value)}
                  placeholder="1 viên/lần"
                />
                <InputField
                  label="Đường dùng"
                  value={item.route}
                  onChange={(value) => onUpdateRow(index, "route", value)}
                  placeholder="Uống"
                />
                <InputField
                  label="Tần suất"
                  value={item.frequency}
                  onChange={(value) => onUpdateRow(index, "frequency", value)}
                  placeholder="Ngày 2 lần"
                />
                <InputField
                  label="Số ngày"
                  value={item.durationDays}
                  onChange={(value) => onUpdateRow(index, "durationDays", value)}
                  placeholder="5"
                  type="number"
                />
                <InputField
                  label="Số lượng"
                  value={item.quantity}
                  onChange={(value) => onUpdateRow(index, "quantity", value)}
                  placeholder="10"
                  type="number"
                />
              </div>

              <div className="mt-4 flex justify-end">
                <Button
                  type="button"
                  variant="secondary"
                  className="border-rose-200 text-rose-700 hover:bg-rose-50"
                  onClick={() => onRemoveRow(index)}
                  disabled={prescriptionItems.length === 1}
                >
                  Xóa dòng
                </Button>
              </div>
            </div>
          ))}
        </div>

        <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
          <Button type="button" variant="secondary" onClick={onClose}>
            Đóng
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Đang phát hành..." : "Xác nhận đơn thuốc"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
