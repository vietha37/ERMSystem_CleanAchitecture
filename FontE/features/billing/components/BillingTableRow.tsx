"use client";

import { Button } from "@/components/ui/Button";
import { HospitalInvoiceSummary } from "../types";
import {
  formatCurrency,
  formatDateTime,
  getStatusClass,
  getStatusLabel,
} from "../utils/billingFormatters";

interface BillingTableRowProps {
  invoice: HospitalInvoiceSummary;
  onOpenDetail: (id: string) => void;
  onOpenPayment: (invoice: HospitalInvoiceSummary) => void;
  onOpenGateway: (invoice: HospitalInvoiceSummary) => void;
}

export function BillingTableRow({
  invoice,
  onOpenDetail,
  onOpenPayment,
  onOpenGateway,
}: BillingTableRowProps) {
  return (
    <tr className="border-t border-slate-100 align-top">
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">{invoice.invoiceNumber}</div>
        <div className="mt-1 text-sm text-slate-500">
          {invoice.encounterNumber || "--"}
        </div>
        <div className="mt-1 text-xs text-slate-400">
          {formatDateTime(invoice.issuedAtLocal)}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">{invoice.patientName}</div>
        <div className="mt-1 text-sm text-slate-500">
          {invoice.medicalRecordNumber}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="text-sm font-semibold text-slate-900">
          Tổng: {formatCurrency(invoice.totalAmount)}
        </div>
        <div className="mt-1 text-sm text-emerald-600">
          Đã thu: {formatCurrency(invoice.paidAmount)}
        </div>
        <div className="mt-1 text-sm font-bold text-rose-600">
          Còn nợ: {formatCurrency(invoice.balanceAmount)}
        </div>
      </td>
      <td className="px-6 py-4">
        <span
          className={`inline-flex rounded-full px-3 py-1 text-xs font-bold ${getStatusClass(
            invoice.invoiceStatus
          )}`}
        >
          {getStatusLabel(invoice.invoiceStatus)}
        </span>
      </td>
      <td className="px-6 py-4">
        <div className="flex flex-col gap-2">
          <Button
            variant="secondary"
            onClick={() => onOpenDetail(invoice.invoiceId)}
          >
            Xem chi tiết
          </Button>
          {invoice.balanceAmount > 0 && invoice.invoiceStatus !== "Cancelled" && (
            <Button onClick={() => onOpenPayment(invoice)}>
              Thu tiền
            </Button>
          )}
          {invoice.balanceAmount > 0 && invoice.invoiceStatus !== "Cancelled" && (
            <Button
              variant="secondary"
              onClick={() => onOpenGateway(invoice)}
            >
              Tạo giao dịch
            </Button>
          )}
        </div>
      </td>
    </tr>
  );
}
