"use client";

import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { TablePagination } from "@/shared/ui/Table/TablePagination";
import { HospitalPrescriptionSummary } from "../types";
import { PrescriptionTableRow } from "./PrescriptionTableRow";

interface PrescriptionTableProps {
  prescriptions: HospitalPrescriptionSummary[];
  isLoading: boolean;
  listError: string | null;
  role?: string | null;
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
  onDelete: (id: string) => void;
}

export function PrescriptionTable({
  prescriptions,
  isLoading,
  listError,
  role,
  totalCount,
  pageNumber,
  pageSize,
  totalPages,
  onPageSizeChange,
  onPageChange,
  onRetry,
  onOpenDetail,
  onDelete,
}: PrescriptionTableProps) {
  return (
    <Card className="overflow-hidden border border-slate-200 bg-white p-0 shadow-xs">
      <div className="flex flex-col gap-3 border-b border-slate-200 px-6 py-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-base font-bold text-slate-900">Danh sách đơn thuốc</h2>
          <p className="text-xs text-slate-500">
            Tổng cộng {totalCount} đơn thuốc theo bộ lọc hiện tại.
          </p>
        </div>

        <div className="rounded-md bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
          Trang {pageNumber} / {Math.max(1, totalPages)}
        </div>
      </div>

      {isLoading ? (
        <LoadingState title="Đang tải đơn thuốc..." tone="cyan" />
      ) : listError ? (
        <ErrorState
          title="Không thể tải dữ liệu đơn thuốc"
          description={listError}
          onAction={onRetry}
        />
      ) : prescriptions.length === 0 ? (
        <EmptyState
          title="Chưa có đơn thuốc nào khớp bộ lọc hiện tại."
          description="Thay đổi bộ lọc hoặc tạo đơn thuốc mới."
          tone="cyan"
        />
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-[1260px] w-full border-collapse text-left">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50/75">
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Đơn thuốc
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Bệnh nhân
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Bác sĩ
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Chẩn đoán
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Trạng thái
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Ngày tạo
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Tác vụ
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {prescriptions.map((prescription) => (
                <PrescriptionTableRow
                  key={prescription.prescriptionId}
                  prescription={prescription}
                  role={role}
                  onOpenDetail={onOpenDetail}
                  onDelete={onDelete}
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
          itemName="đơn thuốc"
        />
      </div>
    </Card>
  );
}
