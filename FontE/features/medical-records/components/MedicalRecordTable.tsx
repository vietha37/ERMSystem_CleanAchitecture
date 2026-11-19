"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { HospitalEncounterSummary } from "../types";
import { MedicalRecordTableRow } from "./MedicalRecordTableRow";

interface MedicalRecordTableProps {
  encounters: HospitalEncounterSummary[];
  isLoading: boolean;
  listError: string | null;
  approvingEncounterId: string | null;
  startItem: number;
  endItem: number;
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
  startItem,
  endItem,
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
    <Card className="overflow-hidden border border-slate-100 p-0 shadow-sm">
      <div className="flex flex-col gap-3 border-b border-slate-100 px-6 py-5 md:flex-row md:items-center md:justify-between">
        <div>
          <h2 className="text-lg font-bold text-slate-900">Danh sách hồ sơ khám</h2>
          <p className="mt-1 text-sm text-slate-500">
            Hiển thị {startItem}-{endItem} / {totalCount} hồ sơ.
          </p>
        </div>

        <select
          value={pageSize}
          onChange={(event) => onPageSizeChange(Number(event.target.value))}
          className="rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
        >
          <option value={10}>10 dòng / trang</option>
          <option value={20}>20 dòng / trang</option>
          <option value={50}>50 dòng / trang</option>
        </select>
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
              <tr className="bg-slate-50">
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Hồ sơ khám
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
                  Cập nhật
                </th>
                <th className="px-6 py-4 text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
                  Tác vụ
                </th>
              </tr>
            </thead>
            <tbody>
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
