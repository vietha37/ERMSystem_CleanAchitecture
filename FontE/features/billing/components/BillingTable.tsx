"use client";

import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { TablePagination } from "@/shared/ui/Table/TablePagination";
import { HospitalInvoiceSummary } from "../types";
import { BillingTableRow } from "./BillingTableRow";

interface BillingTableProps {
  invoices: HospitalInvoiceSummary[];
  isLoading: boolean;
  listError: string | null;
  pageNumber: number;
  pageSize?: number;
  totalCount?: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
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
  pageSize = 10,
  totalCount = 0,
  totalPages,
  onPageChange,
  onPageSizeChange,
  onRetry,
  onOpenDetail,
  onOpenPayment,
  onOpenGateway,
}: BillingTableProps) {
  return (
    <Card className="overflow-hidden border border-slate-200 bg-white p-0 shadow-xs">
      <div className="flex flex-col gap-3 border-b border-slate-200 px-6 py-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-base font-bold text-slate-900">Danh sách hóa đơn</h2>
          <p className="text-xs text-slate-500">
            Tổng cộng {totalCount} hóa đơn theo bộ lọc hiện tại.
          </p>
        </div>

        <div className="rounded-md bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
          Trang {pageNumber} / {Math.max(1, totalPages)}
        </div>
      </div>

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
              <tr className="border-b border-slate-200 bg-slate-50/75">
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Hóa đơn
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Bệnh nhân
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Số tiền
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Trạng thái
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Thao tác
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
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

      <div className="border-t border-slate-200 px-6 py-3">
        <TablePagination
          currentPage={pageNumber}
          totalPages={totalPages}
          totalItems={totalCount}
          pageSize={pageSize}
          onPageChange={onPageChange}
          onPageSizeChange={onPageSizeChange}
          pageSizeOptions={[10, 20, 50]}
          itemName="hóa đơn"
        />
      </div>
    </Card>
  );
}
