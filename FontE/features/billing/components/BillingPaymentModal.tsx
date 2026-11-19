"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { HospitalInvoiceSummary } from "../types";
import { formatCurrency } from "../utils/billingFormatters";

interface BillingPaymentModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (e: React.FormEvent) => void;
  isSubmitting: boolean;
  paymentInvoice: HospitalInvoiceSummary | null;
  paymentMethod: string;
  onPaymentMethodChange: (val: string) => void;
  paymentAmount: string;
  onPaymentAmountChange: (val: string) => void;
  paymentReference: string;
  onPaymentReferenceChange: (val: string) => void;
}

export function BillingPaymentModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
  paymentInvoice,
  paymentMethod,
  onPaymentMethodChange,
  paymentAmount,
  onPaymentAmountChange,
  paymentReference,
  onPaymentReferenceChange,
}: BillingPaymentModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Thu tiền hóa đơn">
      <form className="space-y-4" onSubmit={onSubmit}>
        <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4 text-sm text-slate-700">
          <div className="font-semibold text-slate-900">{paymentInvoice?.invoiceNumber}</div>
          <div className="mt-1">Bệnh nhân: {paymentInvoice?.patientName}</div>
          <div className="mt-1 text-emerald-700 font-bold">
            Số tiền còn nợ: {paymentInvoice ? formatCurrency(paymentInvoice.balanceAmount) : "--"}
          </div>
        </div>

        <div>
          <label className="mb-1 block text-xs font-semibold text-slate-600">
            Phương thức thanh toán
          </label>
          <select
            value={paymentMethod}
            onChange={(event) => onPaymentMethodChange(event.target.value)}
            aria-label="Phương thức thu tiền"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          >
            <option value="Cash">Tiền mặt</option>
            <option value="Transfer">Chuyển khoản bank</option>
            <option value="Card">Thẻ POS</option>
          </select>
        </div>

        <div>
          <label className="mb-1 block text-xs font-semibold text-slate-600">
            Số tiền thu (VNĐ)
          </label>
          <input
            type="number"
            value={paymentAmount}
            onChange={(event) => onPaymentAmountChange(event.target.value)}
            placeholder="Số tiền"
            aria-label="Số tiền thu"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          />
        </div>

        <div>
          <label className="mb-1 block text-xs font-semibold text-slate-600">
            Mã tham chiếu / Ghi chú
          </label>
          <input
            type="text"
            value={paymentReference}
            onChange={(event) => onPaymentReferenceChange(event.target.value)}
            placeholder="VD: CKS-123456 hoặc Phiếu thu #01"
            aria-label="Mã tham chiếu thanh toán"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
          />
        </div>

        <div className="flex justify-end gap-3 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Đóng
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Đang xử lý..." : "Thu tiền"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
