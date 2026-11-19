"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { HospitalPaymentIntent } from "../types";
import { formatCurrency } from "../utils/billingFormatters";

interface BillingGatewayCallbackModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (e: React.FormEvent) => void;
  isSubmitting: boolean;
  gatewayIntent: HospitalPaymentIntent | null;
  gatewayStatus: string;
  onGatewayStatusChange: (val: string) => void;
}

export function BillingGatewayCallbackModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
  gatewayIntent,
  gatewayStatus,
  onGatewayStatusChange,
}: BillingGatewayCallbackModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Xác nhận giao dịch"
    >
      <form className="space-y-4" onSubmit={onSubmit}>
        <div className="rounded-2xl border border-slate-100 bg-slate-50 p-4 text-sm text-slate-700">
          <div className="font-medium text-slate-900">
            {gatewayIntent?.paymentReference || "--"}
          </div>
          <div className="mt-1">
            Gateway: {gatewayIntent?.gatewayProvider || "--"} / {gatewayIntent?.callbackMode || "--"}
          </div>
          <div className="mt-1">
            External Tx: {gatewayIntent?.externalTransactionId || "--"}
          </div>
          <div className="mt-1">
            Số tiền: {gatewayIntent ? formatCurrency(gatewayIntent.amount) : "--"}
          </div>
        </div>
        <select
          value={gatewayStatus}
          onChange={(event) => onGatewayStatusChange(event.target.value)}
          aria-label="Trạng thái callback gateway"
          className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
        >
          <option value="Captured">Captured / Thành công</option>
          <option value="Failed">Failed / Thất bại</option>
        </select>
        <div className="rounded-2xl border border-amber-100 bg-amber-50/80 px-4 py-4 text-sm text-slate-700">
          Xác nhận trạng thái thanh toán từ cổng giao dịch.
        </div>
        <div className="flex justify-end gap-3">
          <Button
            type="button"
            variant="secondary"
            onClick={onClose}
          >
            Đóng
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Đang xử lý..." : "Xác nhận"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
