"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { HospitalInvoiceSummary } from "../types";
import { BillingTableRow } from "./BillingTableRow";

interface BillingTableProps {
  invoices: HospitalInvoiceSummary[];
  isLoading: boolean;
  listError: string | null;
  pageNumber: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onRetry: () => void;
  onOpenDetail: (id: string) => void;
  onOpenPayment: (invoice: HospitalInvoiceSummary) => void;
  onOpenGateway: (invoice: HospitalInvoiceSummary) => void;
}

export function BillingTable({
  invoices,
  isLoading,
  listError,
  pageNumber,
  totalPages,
  onPageChange,
  onRetry,
  onOpenDetail,
  onOpenPayment,
  onOpenGateway,
}: BillingTableProps) {
  return (
    <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
      {isLoading ? (
        <LoadingState title="Đang tải danh sách hóa đơn..." tone="emerald" />
      ) : listError ? (
        <ErrorState
          title="Không thể tải danh sách hóa đơn"
          description={listError}
          onAction={onRetry}
        />
      ) : invoices.length === 0 ? (
        <EmptyState
          title="Không tìm thấy hóa đơn."
          description="Thay đổi bộ lọc hoặc lập hóa đơn mới."
          tone="emerald"
        />
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-[1160px] w-full border-collapse text-left">
            <thead>
              <tr className="bg-slate-50">
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Hóa đơn
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Bệnh nhân
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Số tiền
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Trạng thái
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Thao tác
                </th>
              </tr>
            </thead>
            <tbody>
              {invoices.map((invoice) => (
                <BillingTableRow
                  key={invoice.invoiceId}
                  invoice={invoice}
                  onOpenDetail={onOpenDetail}
                  onOpenPayment={onOpenPayment}
                  onOpenGateway={onOpenGateway}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="flex items-center justify-between border-t border-slate-100 px-6 py-5">
        <div className="text-sm text-slate-500">
          Trang {pageNumber} / {Math.max(1, totalPages)}
        </div>
        <div className="flex gap-3">
          <Button
            variant="secondary"
            disabled={pageNumber <= 1}
            onClick={() => onPageChange(Math.max(1, pageNumber - 1))}
          >
            Trang trước
          </Button>
          <Button
            variant="secondary"
            disabled={pageNumber >= totalPages}
            onClick={() => onPageChange(Math.min(totalPages, pageNumber + 1))}
          >
            Trang sau
          </Button>
        </div>
      </div>
    </Card>
  );
}
