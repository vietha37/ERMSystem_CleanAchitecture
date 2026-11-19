"use client";

import { Button } from "@/components/ui/Button";
import { HospitalMedicineCatalog, PrescriptionItemForm } from "../types";

interface PrescriptionItemRowProps {
  item: PrescriptionItemForm;
  index: number;
  medicines: HospitalMedicineCatalog[];
  canRemove: boolean;
  onUpdateRow: (
    index: number,
    field: keyof PrescriptionItemForm,
    value: string
  ) => void;
  onRemoveRow: (index: number) => void;
}

export function PrescriptionItemRow({
  item,
  index,
  medicines,
  canRemove,
  onUpdateRow,
  onRemoveRow,
}: PrescriptionItemRowProps) {
  return (
    <div className="rounded-xl border border-slate-200 bg-slate-50/70 p-4">
      <div className="grid gap-3 md:grid-cols-2">
        <div className="md:col-span-2">
          <label className="mb-1 block text-xs font-semibold text-slate-700">
            Thuốc
          </label>
          <select
            value={item.medicineId}
            onChange={(event) =>
              onUpdateRow(index, "medicineId", event.target.value)
            }
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100"
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

        <div>
          <label className="mb-1 block text-xs font-semibold text-slate-700">
            Liều dùng
          </label>
          <input
            type="text"
            value={item.doseInstruction}
            onChange={(event) =>
              onUpdateRow(index, "doseInstruction", event.target.value)
            }
            placeholder="1 viên/lần"
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-cyan-600"
          />
        </div>

        <div>
          <label className="mb-1 block text-xs font-semibold text-slate-700">
            Đường dùng
          </label>
          <input
            type="text"
            value={item.route}
            onChange={(event) => onUpdateRow(index, "route", event.target.value)}
            placeholder="Uống"
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-cyan-600"
          />
        </div>

        <div>
          <label className="mb-1 block text-xs font-semibold text-slate-700">
            Tần suất
          </label>
          <input
            type="text"
            value={item.frequency}
            onChange={(event) =>
              onUpdateRow(index, "frequency", event.target.value)
            }
            placeholder="Ngày 2 lần"
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-cyan-600"
          />
        </div>

        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              Số ngày
            </label>
            <input
              type="number"
              value={item.durationDays}
              onChange={(event) =>
                onUpdateRow(index, "durationDays", event.target.value)
              }
              placeholder="5"
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-cyan-600"
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              Số lượng
            </label>
            <input
              type="number"
              value={item.quantity}
              onChange={(event) =>
                onUpdateRow(index, "quantity", event.target.value)
              }
              placeholder="10"
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none transition focus:border-cyan-600"
            />
          </div>
        </div>
      </div>

      <div className="mt-3 flex justify-end">
        <Button
          type="button"
          variant="secondary"
          className="border-rose-200 text-xs text-rose-700 hover:bg-rose-50"
          onClick={() => onRemoveRow(index)}
          disabled={!canRemove}
        >
          Xóa dòng này
        </Button>
      </div>
    </div>
  );
}
