"use client";

import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { TablePagination } from "@/shared/ui/Table/TablePagination";
import { HospitalEncounterSummary } from "../types";
import { MedicalRecordTableRow } from "./MedicalRecordTableRow";

interface MedicalRecordTableProps {
  encounters: HospitalEncounterSummary[];
  isLoading: boolean;
  listError: string | null;
  approvingEncounterId: string | null;
  startItem?: number;
  endItem?: number;
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  onPageSizeChange: (size: number) => void;
  onPageChange: (page: number) => void;
  onRetry: () => void;
  onOpenEdit: (id: string) => void;
  onApprove: (id: string) => void;
}

export function MedicalRecordTable({
  encounters,
  isLoading,
  listError,
  approvingEncounterId,
  totalCount,
  pageNumber,
  pageSize,
  totalPages,
  onPageSizeChange,
  onPageChange,
  onRetry,
  onOpenEdit,
  onApprove,
}: MedicalRecordTableProps) {
  return (
    <Card className="overflow-hidden border border-slate-200 bg-white p-0 shadow-xs">
      <div className="flex flex-col gap-3 border-b border-slate-200 px-6 py-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-base font-bold text-slate-900">Danh sách hồ sơ khám</h2>
          <p className="text-xs text-slate-500">
            Tổng cộng {totalCount} hồ sơ khám theo bộ lọc hiện tại.
          </p>
        </div>

        <div className="rounded-md bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600">
          Trang {pageNumber} / {Math.max(1, totalPages)}
        </div>
      </div>

      {isLoading ? (
        <LoadingState title="Đang tải hồ sơ khám..." tone="emerald" />
      ) : listError ? (
        <ErrorState
          title="Không thể tải hồ sơ khám"
          description={listError}
          onAction={onRetry}
        />
      ) : encounters.length === 0 ? (
        <EmptyState
          title="Chưa có hồ sơ khám nào khớp bộ lọc hiện tại."
          description="Thử đổi ngày, trạng thái hoặc từ khóa để tìm thêm hồ sơ khám."
          tone="emerald"
        />
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-[1260px] w-full border-collapse text-left">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50/75">
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Hồ sơ khám
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
                  Cập nhật
                </th>
                <th className="px-6 py-3.5 text-xs font-semibold uppercase tracking-wider text-slate-500">
                  Tác vụ
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {encounters.map((encounter) => (
                <MedicalRecordTableRow
                  key={encounter.encounterId}
                  encounter={encounter}
                  approvingEncounterId={approvingEncounterId}
                  onOpenEdit={onOpenEdit}
                  onApprove={onApprove}
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
          itemName="hồ sơ"
        />
      </div>
    </Card>
  );
}
