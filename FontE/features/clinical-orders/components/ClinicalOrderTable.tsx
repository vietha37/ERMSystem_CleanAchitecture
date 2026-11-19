"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { HospitalClinicalOrderSummary } from "../types";
import { ClinicalOrderTableRow } from "./ClinicalOrderTableRow";

interface ClinicalOrderTableProps {
  orders: HospitalClinicalOrderSummary[];
  isLoading: boolean;
  listError: string | null;
  startItem: number;
  endItem: number;
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
  startItem,
  endItem,
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
    <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
      <div className="flex flex-col gap-3 border-b border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-lg font-bold text-slate-900">Danh sách chỉ định</h2>
          <p className="mt-1 text-sm text-slate-500">
            Hiển thị {startItem}-{endItem} / {totalCount} chỉ định.
          </p>
        </div>

        <select
          value={pageSize}
          onChange={(event) => onPageSizeChange(Number(event.target.value))}
          className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
        >
          <option value={10}>10 dòng / trang</option>
          <option value={20}>20 dòng / trang</option>
          <option value={50}>50 dòng / trang</option>
        </select>
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
              <tr className="bg-slate-50">
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Thời gian
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Bệnh nhân
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Dịch vụ
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Phân loại
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Tóm tắt
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Thao tác
                </th>
              </tr>
            </thead>
            <tbody>
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

      <div className="flex flex-col gap-4 border-t border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
        <div className="text-sm text-slate-500">
          Trang {pageNumber} / {totalPages}
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
