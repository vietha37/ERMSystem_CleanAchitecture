"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import {
  HospitalClinicalOrderSummary,
  RecordHospitalLabResultItemPayload,
} from "../types";

interface ClinicalOrderLabModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (e: React.FormEvent) => void;
  isSubmitting: boolean;
  labResultTarget: HospitalClinicalOrderSummary | null;
  specimenCode: string;
  onSpecimenCodeChange: (val: string) => void;
  labResultItems: RecordHospitalLabResultItemPayload[];
  onAddRow: () => void;
  onRemoveRow: (index: number) => void;
  onUpdateRow: (
    index: number,
    field: keyof RecordHospitalLabResultItemPayload,
    value: string
  ) => void;
}

export function ClinicalOrderLabModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
  labResultTarget,
  specimenCode,
  onSpecimenCodeChange,
  labResultItems,
  onAddRow,
  onRemoveRow,
  onUpdateRow,
}: ClinicalOrderLabModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Nhập kết quả xét nghiệm">
      <form className="space-y-4" onSubmit={onSubmit}>
        {labResultTarget && (
          <div className="rounded-2xl border border-cyan-100 bg-cyan-50 px-4 py-3 text-sm text-cyan-800">
            {labResultTarget.orderNumber} - {labResultTarget.patientName} -{" "}
            {labResultTarget.serviceName}
          </div>
        )}

        <div>
          <label className="mb-2 block text-sm font-semibold text-slate-700">
            Mã mẫu
          </label>
          <input
            type="text"
            value={specimenCode}
            onChange={(event) => onSpecimenCodeChange(event.target.value)}
            placeholder="Để trống nếu hệ thống tự sinh"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
          />
        </div>

        <div className="space-y-3">
          {labResultItems.map((item, index) => (
            <div
              key={`${index}-${item.analyteCode ?? ""}`}
              className="rounded-2xl border border-slate-100 p-4"
            >
              <div className="mb-3 flex items-center justify-between">
                <div className="text-sm font-semibold text-slate-800">
                  Dòng kết quả {index + 1}
                </div>
                <button
                  type="button"
                  onClick={() => onRemoveRow(index)}
                  className="text-sm font-medium text-rose-600"
                >
                  Xóa dòng
                </button>
              </div>

              <div className="grid gap-3 md:grid-cols-2">
                <input
                  type="text"
                  value={item.analyteName}
                  onChange={(event) =>
                    onUpdateRow(index, "analyteName", event.target.value)
                  }
                  placeholder="Tên chỉ số"
                  className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                />
                <input
                  type="text"
                  value={item.analyteCode ?? ""}
                  onChange={(event) =>
                    onUpdateRow(index, "analyteCode", event.target.value)
                  }
                  placeholder="Mã chỉ số"
                  className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                />
                <input
                  type="text"
                  value={item.resultValue ?? ""}
                  onChange={(event) =>
                    onUpdateRow(index, "resultValue", event.target.value)
                  }
                  placeholder="Giá trị"
                  className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                />
                <input
                  type="text"
                  value={item.unit ?? ""}
                  onChange={(event) =>
                    onUpdateRow(index, "unit", event.target.value)
                  }
                  placeholder="Đơn vị"
                  className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                />
                <input
                  type="text"
                  value={item.referenceRange ?? ""}
                  onChange={(event) =>
                    onUpdateRow(index, "referenceRange", event.target.value)
                  }
                  placeholder="Khoảng tham chiếu"
                  className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                />
                <input
                  type="text"
                  value={item.abnormalFlag ?? ""}
                  onChange={(event) =>
                    onUpdateRow(index, "abnormalFlag", event.target.value)
                  }
                  placeholder="Cảnh báo bất thường"
                  className="rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
                />
              </div>
            </div>
          ))}
        </div>

        <Button type="button" variant="secondary" onClick={onAddRow}>
          Thêm dòng kết quả
        </Button>

        <div className="flex justify-end gap-3 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Đóng
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Đang lưu..." : "Lưu kết quả"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
