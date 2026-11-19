"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";

interface BillingGatewayModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (e: React.FormEvent) => void;
  isSubmitting: boolean;
  gatewayMethod: string;
  onGatewayMethodChange: (val: string) => void;
  gatewayAmount: string;
  onGatewayAmountChange: (val: string) => void;
}

export function BillingGatewayModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
  gatewayMethod,
  onGatewayMethodChange,
  gatewayAmount,
  onGatewayAmountChange,
}: BillingGatewayModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Tạo giao dịch">
      <form className="space-y-4" onSubmit={onSubmit}>
        <select
          value={gatewayMethod}
          onChange={(event) => onGatewayMethodChange(event.target.value)}
          aria-label="Phương thức gateway"
          className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
        >
          <option value="Transfer">Chuyển khoản QR Gateway</option>
          <option value="Card">Thẻ quốc tế / Cổng Online</option>
          <option value="EWallet">Ví điện tử (MoMo/ZaloPay)</option>
        </select>
        <input
          type="number"
          value={gatewayAmount}
          onChange={(event) => onGatewayAmountChange(event.target.value)}
          placeholder="Số tiền giao dịch"
          aria-label="Số tiền giao dịch gateway"
          className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
        />
        <div className="rounded-2xl border border-violet-100 bg-violet-50/70 px-4 py-4 text-sm text-slate-700">
          Giao dịch sẽ ở trạng thái chờ xác nhận.
        </div>
        <div className="flex justify-end gap-3">
          <Button type="button" variant="secondary" onClick={onClose}>
            Đóng
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Đang tạo..." : "Tạo giao dịch"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
