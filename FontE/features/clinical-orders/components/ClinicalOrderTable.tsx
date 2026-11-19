"use client";

import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { TablePagination } from "@/shared/ui/Table/TablePagination";
import { HospitalClinicalOrderSummary } from "../types";
import { ClinicalOrderTableRow } from "./ClinicalOrderTableRow";

interface ClinicalOrderTableProps {
  orders: HospitalClinicalOrderSummary[];
  isLoading: boolean;
  listError: string | null;
  startItem?: number;
  endItem?: number;
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  onPageSizeChange: (size: number) => void;
  onPageChange: (page: number) => void;
  onRetry: () => void;
  onOpenDetail: (id: string) => void;
  onOpenLabModal: (order: HospitalClinicalOrderSummary) => void;
  onOpenImagingModal: (order: HospitalClinicalOrderSummary) => void;
}

export function ClinicalOrderTable({
  orders,
  isLoading,
  listError,
  totalCount,
  pageNumber,
  pageSize,
  totalPages,
  onPageSizeChange,
  onPageChange,
  onRetry,
  onOpenDetail,
  onOpenLabModal,
  onOpenImagingModal,
}: ClinicalOrderTableProps) {
  return (
    <Card className="overflow-hidden border border-slate-200 bg-white p-0 shadow-xs">
      <div className="flex flex-col gap-3 border-b border-slate-200 px-6 py-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-base font-bold text-slate-900">Danh sách chỉ định</h2>
          <p className="text-xs text-slate-500">
            Tổng cộng {totalCount} chỉ định theo bộ lọc hiện tại.
          </p>
        </div>

        <div className="rounded-md bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
          Trang {pageNumber} / {Math.max(1, totalPages)}
        </div>
      </div>

      {isLoading ? (
        <LoadingState title="Đang tải danh sách chỉ định..." tone="violet" />
      ) : listError ? (
        <ErrorState
          title="Không thể tải danh sách chỉ định"
          description={listError}
          onAction={onRetry}
        />
      ) : orders.length === 0 ? (
        <EmptyState
          title="Chưa có chỉ định nào khớp bộ lọc hiện tại."
          description="Thay đổi bộ lọc để xem chỉ định khác."
          tone="violet"
        />
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-[1280px] w-full border-collapse text-left">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50/75">
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Thời gian
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Bệnh nhân
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Dịch vụ
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Phân loại
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Tóm tắt
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Thao tác
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {orders.map((order) => (
                <ClinicalOrderTableRow
                  key={order.clinicalOrderId}
                  order={order}
                  onOpenDetail={onOpenDetail}
                  onOpenLabModal={onOpenLabModal}
                  onOpenImagingModal={onOpenImagingModal}
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
          itemName="chỉ định"
        />
      </div>
    </Card>
  );
}
