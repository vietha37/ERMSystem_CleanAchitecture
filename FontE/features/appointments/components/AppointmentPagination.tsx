import React from "react";
import { Button } from "@/components/ui/Button";
import { useTranslation } from "@/hooks/useTranslation";

interface AppointmentPaginationProps {
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

export function AppointmentPagination({
  pageNumber,
  totalPages,
  totalCount,
  onPageChange,
}: AppointmentPaginationProps) {
  const { t } = useTranslation();

  if (totalPages <= 0) return null;

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between border-t border-slate-100 px-5 py-3.5 text-xs text-slate-600">
      <div>{t("common.labels.totalRecords", { count: totalCount })}</div>
      <div className="flex items-center gap-1.5">
        <Button
          variant="secondary"
          size="sm"
          disabled={pageNumber <= 1}
          onClick={() => onPageChange(Math.max(1, pageNumber - 1))}
        >
          {t("common.labels.previous")}
        </Button>
        <span className="px-2 font-medium">
          {pageNumber} / {totalPages}
        </span>
        <Button
          variant="secondary"
          size="sm"
          disabled={pageNumber >= totalPages}
          onClick={() => onPageChange(Math.min(totalPages, pageNumber + 1))}
        >
          {t("common.labels.next")}
        </Button>
      </div>
    </div>
  );
}
