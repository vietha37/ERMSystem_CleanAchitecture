"use client";

import { Modal } from "@/components/ui/Modal";
import { HospitalInvoiceDetail } from "../types";
import {
  formatCurrency,
  formatDateTime,
  getStatusLabel,
} from "../utils/billingFormatters";

interface BillingDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  selectedInvoice: HospitalInvoiceDetail | null;
  detailBreakdown: {
    medItems: HospitalInvoiceDetail["items"];
    otherItems: HospitalInvoiceDetail["items"];
    medTotal: number;
    otherTotal: number;
  };
}

function InfoLine({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="text-xs text-slate-400">{label}</div>
      <div className="font-medium text-slate-800">{value}</div>
    </div>
  );
}

export function BillingDetailModal({
  isOpen,
  onClose,
  selectedInvoice,
  detailBreakdown,
}: BillingDetailModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Chi tiết hóa đơn">
      {!selectedInvoice ? (
        <div className="py-8 text-sm text-slate-500">Đang tải chi tiết...</div>
      ) : (
        <div className="space-y-5">
          <div className="grid gap-3 md:grid-cols-3">
            <InfoLine label="Mã Hóa đơn" value={selectedInvoice.invoiceNumber} />
            <InfoLine label="Bệnh nhân" value={selectedInvoice.patientName} />
            <InfoLine label="Mã bệnh án" value={selectedInvoice.medicalRecordNumber} />
            <InfoLine label="Hồ sơ khám" value={selectedInvoice.encounterNumber || "--"} />
            <InfoLine label="Bác sĩ chỉ định" value={selectedInvoice.doctorName || "--"} />
            <InfoLine label="Trạng thái" value={getStatusLabel(selectedInvoice.invoiceStatus)} />
          </div>

          {/* BẢNG TỔNG HỢP CÔNG THỨC CHI PHÍ */}
          <div className="rounded-2xl border border-emerald-100 bg-gradient-to-r from-emerald-50 via-teal-50 to-cyan-50 p-4">
            <div className="mb-2 text-xs font-bold uppercase tracking-wider text-emerald-800">
              Tổng hợp chi phí
            </div>
            <div className="grid gap-2 text-sm sm:grid-cols-2">
              <div className="flex justify-between rounded-xl bg-white/90 px-3 py-2 shadow-xs">
                <span className="text-slate-600">Tiền thuốc:</span>
                <span className="font-bold text-slate-900">{formatCurrency(detailBreakdown.medTotal)}</span>
              </div>
              <div className="flex justify-between rounded-xl bg-white/90 px-3 py-2 shadow-xs">
                <span className="text-slate-600">Tiền khám & dịch vụ:</span>
                <span className="font-bold text-slate-900">{formatCurrency(detailBreakdown.otherTotal)}</span>
              </div>
              <div className="flex justify-between rounded-xl bg-emerald-100/90 px-3 py-2 shadow-xs sm:col-span-2 font-bold text-emerald-950">
                <span>Tổng cộng:</span>
                <span className="text-emerald-700 text-base">{formatCurrency(selectedInvoice.totalAmount)}</span>
              </div>
              <div className="flex justify-between rounded-xl bg-white/90 px-3 py-2 text-emerald-700">
                <span>Đã thanh toán:</span>
                <span className="font-bold">{formatCurrency(selectedInvoice.paidAmount)}</span>
              </div>
              <div className="flex justify-between rounded-xl bg-white/90 px-3 py-2 text-rose-600">
                <span>Còn nợ:</span>
                <span className="font-bold">{formatCurrency(selectedInvoice.balanceAmount)}</span>
              </div>
            </div>
          </div>

          {/* DANH SÁCH CHI TIẾT THUỐC */}
          {detailBreakdown.medItems.length > 0 && (
            <div>
              <p className="mb-2 text-xs font-bold uppercase tracking-wider text-slate-700">
                Thuốc ({detailBreakdown.medItems.length} mục)
              </p>
              <div className="space-y-2">
                {detailBreakdown.medItems.map((item) => (
                  <div key={item.invoiceItemId} className="flex items-center justify-between rounded-2xl border border-emerald-100 bg-emerald-50/30 px-4 py-3">
                    <div>
                      <div className="font-semibold text-slate-900">{item.description}</div>
                      <div className="mt-0.5 text-xs text-slate-500">
                        {item.quantity} x {formatCurrency(item.unitPrice)}
                      </div>
                    </div>
                    <div className="font-bold text-emerald-700">
                      {formatCurrency(item.lineAmount)}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* DANH SÁCH KHÁM VÀ DỊCH VỤ KHÁC */}
          {detailBreakdown.otherItems.length > 0 && (
            <div>
              <p className="mb-2 text-xs font-bold uppercase tracking-wider text-slate-700">
                Khám & dịch vụ khác ({detailBreakdown.otherItems.length} mục)
              </p>
              <div className="space-y-2">
                {detailBreakdown.otherItems.map((item) => (
                  <div key={item.invoiceItemId} className="flex items-center justify-between rounded-2xl border border-slate-100 bg-slate-50/60 px-4 py-3">
                    <div>
                      <div className="font-semibold text-slate-900">{item.description}</div>
                      <div className="mt-0.5 text-xs text-slate-500">
                        Loại: {item.itemType} | {item.quantity} x {formatCurrency(item.unitPrice)}
                      </div>
                    </div>
                    <div className="font-bold text-slate-900">
                      {formatCurrency(item.lineAmount)}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* LỊCH SỬ THANH TOÁN */}
          <div>
            <p className="mb-2 text-xs font-bold uppercase tracking-wider text-slate-700">Lịch sử thanh toán</p>
            {selectedInvoice.payments.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-slate-200 px-4 py-4 text-center text-sm text-slate-500">
                Chưa có thanh toán.
              </div>
            ) : (
              <div className="space-y-2">
                {selectedInvoice.payments.map((payment) => (
                  <div key={payment.paymentId} className="rounded-2xl border border-slate-100 px-4 py-3">
                    <div className="flex justify-between font-medium text-slate-900">
                      <span>{payment.paymentReference} ({payment.paymentMethod})</span>
                      <span className="font-bold text-emerald-600">{formatCurrency(payment.amount)}</span>
                    </div>
                    <div className="mt-1 text-xs text-slate-500">
                      Thời gian: {formatDateTime(payment.paidAtLocal)} | Trạng thái: {payment.paymentStatus}
                      {payment.externalTransactionId ? ` | TxID: ${payment.externalTransactionId}` : ""}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}
    </Modal>
  );
}
