"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { HospitalPrescriptionSummary } from "../types";
import { PrescriptionTableRow } from "./PrescriptionTableRow";

interface PrescriptionTableProps {
  prescriptions: HospitalPrescriptionSummary[];
  isLoading: boolean;
  listError: string | null;
  role?: string | null;
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
  onDelete: (id: string) => void;
}

export function PrescriptionTable({
  prescriptions,
  isLoading,
  listError,
  role,
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
  onDelete,
}: PrescriptionTableProps) {
  return (
    <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
      <div className="flex flex-col gap-3 border-b border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-lg font-bold text-slate-900">Danh sách đơn thuốc</h2>
          <p className="mt-1 text-sm text-slate-500">
            Hiển thị {startItem}-{endItem} / {totalCount} đơn thuốc.
          </p>
        </div>

        <select
          value={pageSize}
          onChange={(event) => onPageSizeChange(Number(event.target.value))}
          className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
        >
          <option value={10}>10 dòng / trang</option>
          <option value={20}>20 dòng / trang</option>
          <option value={50}>50 dòng / trang</option>
        </select>
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
              <tr className="bg-slate-50">
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Đơn thuốc
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Bệnh nhân
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Bác sĩ
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Chẩn đoán
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Trạng thái
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Ngày tạo
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Tác vụ
                </th>
              </tr>
            </thead>
            <tbody>
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

      <div className="flex flex-col gap-3 border-t border-slate-100 px-6 py-4 md:flex-row md:items-center md:justify-between">
        <p className="text-sm text-slate-500">
          Trang {pageNumber}/{totalPages}
        </p>

        <div className="flex items-center gap-2">
          <Button
            variant="secondary"
            onClick={() => onPageChange(1)}
            disabled={pageNumber === 1}
          >
            Đầu
          </Button>
          <Button
            variant="secondary"
            onClick={() => onPageChange(pageNumber - 1)}
            disabled={pageNumber === 1}
          >
            Trước
          </Button>
          <Button
            variant="secondary"
            onClick={() => onPageChange(pageNumber + 1)}
            disabled={pageNumber >= totalPages}
          >
            Sau
          </Button>
        </div>
      </div>
    </Card>
  );
}
