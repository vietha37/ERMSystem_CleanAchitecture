"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import {
  HospitalBillingEligibleEncounter,
  HospitalBillingEncounterPreview,
} from "../types";
import { formatCurrency } from "../utils/billingFormatters";

interface BillingCreateInvoiceModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (e: React.FormEvent) => void;
  isSubmitting: boolean;
  selectedEncounterId: string;
  onEncounterChange: (val: string) => void;
  availableEncounters: HospitalBillingEligibleEncounter[];
  isLoadingPreview: boolean;
  encounterPreview: HospitalBillingEncounterPreview | null;
  previewBreakdown: { medTotal: number; otherTotal: number; total: number };
  discountAmount: string;
  onDiscountAmountChange: (val: string) => void;
}

export function BillingCreateInvoiceModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
  selectedEncounterId,
  onEncounterChange,
  availableEncounters,
  isLoadingPreview,
  encounterPreview,
  previewBreakdown,
  discountAmount,
  onDiscountAmountChange,
}: BillingCreateInvoiceModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Lập hóa đơn">
      <form className="space-y-4" onSubmit={onSubmit}>
        <div>
          <label className="mb-1 block text-xs font-semibold text-slate-600">
            Chọn hồ sơ khám
          </label>
          <select
            value={selectedEncounterId}
            onChange={(event) => onEncounterChange(event.target.value)}
            aria-label="Chọn hồ sơ khám để lập hóa đơn"
            className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          >
            <option value="">-- Chọn hồ sơ khám --</option>
            {availableEncounters.map((item) => (
              <option key={item.encounterId} value={item.encounterId}>
                {item.encounterNumber} - {item.patientName} (Khoa: {item.specialtyName})
              </option>
            ))}
          </select>
        </div>

        {isLoadingPreview && (
          <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4 text-center text-sm text-slate-500">
            Đang tải chi phí...
          </div>
        )}

        {encounterPreview && !isLoadingPreview && (
          <div className="space-y-3 rounded-2xl border border-emerald-100 bg-gradient-to-br from-emerald-50/70 to-teal-50/70 p-4">
            <div className="text-xs font-bold uppercase tracking-wider text-emerald-800">
              Chi tiết ({encounterPreview.patientName} - MRN: {encounterPreview.medicalRecordNumber})
            </div>

            <div className="grid gap-2 text-sm">
              <div className="flex justify-between rounded-xl bg-white/80 px-3 py-2">
                <span className="text-slate-600">Tiền thuốc:</span>
                <span className="font-semibold text-slate-900">
                  {formatCurrency(previewBreakdown.medTotal)}
                </span>
              </div>
              <div className="flex justify-between rounded-xl bg-white/80 px-3 py-2">
                <span className="text-slate-600">Tiền khám & dịch vụ:</span>
                <span className="font-semibold text-slate-900">
                  {formatCurrency(previewBreakdown.otherTotal)}
                </span>
              </div>
              <div className="flex justify-between border-t border-emerald-200/60 pt-2 font-bold text-slate-900 px-3">
                <span>Tạm tính:</span>
                <span className="text-emerald-700">
                  {formatCurrency(previewBreakdown.total)}
                </span>
              </div>
            </div>

            {encounterPreview.billableLines.length > 0 && (
              <div className="mt-2 text-xs text-slate-500">
                Chi tiết: {encounterPreview.billableLines.map((l) => l.description).join(", ")}
              </div>
            )}
          </div>
        )}

        <div>
          <label className="mb-1 block text-xs font-semibold text-slate-600">
            Giảm giá (nếu có)
          </label>
          <input
            type="number"
            value={discountAmount}
            onChange={(event) => onDiscountAmountChange(event.target.value)}
            placeholder="0"
            aria-label="Giảm giá"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          />
        </div>

        {encounterPreview && (
          <div className="flex items-center justify-between rounded-2xl border border-emerald-200 bg-emerald-100/60 px-4 py-3 font-bold text-emerald-950">
            <span>Tổng thanh toán:</span>
            <span className="text-lg text-emerald-700">
              {formatCurrency(Math.max(0, previewBreakdown.total - Number(discountAmount || 0)))}
            </span>
          </div>
        )}

        <div className="flex justify-end gap-3 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Hủy
          </Button>
          <Button type="submit" disabled={isSubmitting || !selectedEncounterId}>
            {isSubmitting ? "Đang lập..." : "Tạo hóa đơn"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
