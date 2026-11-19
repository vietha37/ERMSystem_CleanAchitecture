"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import {
  HospitalMedicineCatalog,
  HospitalPrescriptionEligibleEncounter,
  PrescriptionItemForm,
} from "../types";
import { PrescriptionItemRow } from "./PrescriptionItemRow";

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
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Kê đơn thuốc mới"
      maxWidth="2xl"
    >
      <form onSubmit={onSubmit} className="space-y-6">
        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Hồ sơ khám bệnh
          </label>
          <select
            value={selectedEncounterId}
            onChange={(event) => onEncounterChange(event.target.value)}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100"
            required
          >
            <option value="">-- Chọn hồ sơ khám hoàn tất --</option>
            {availableEncounters.map((encounter) => (
              <option key={encounter.encounterId} value={encounter.encounterId}>
                {encounter.patientName} ({encounter.medicalRecordNumber}) -{" "}
                {encounter.primaryDiagnosisName || encounter.encounterNumber}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Ghi chú đơn thuốc
          </label>
          <textarea
            value={notes}
            onChange={(event) => onNotesChange(event.target.value)}
            rows={3}
            placeholder="Lời dặn của bác sĩ, cách dùng..."
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none transition focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100"
          />
        </div>

        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold text-slate-800">Danh sách thuốc</h3>
            <Button type="button" variant="secondary" onClick={onAddRow}>
              Thêm dòng
            </Button>
          </div>

          {prescriptionItems.map((item, index) => (
            <PrescriptionItemRow
              key={index}
              item={item}
              index={index}
              medicines={medicines}
              canRemove={prescriptionItems.length > 1}
              onUpdateRow={onUpdateRow}
              onRemoveRow={onRemoveRow}
            />
          ))}
        </div>

        <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
          <Button
            type="button"
            variant="secondary"
            onClick={onClose}
          >
            Hủy
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Đang lưu..." : "Lưu đơn thuốc"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
