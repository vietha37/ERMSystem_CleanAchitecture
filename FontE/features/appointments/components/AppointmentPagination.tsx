"use client";

import React from "react";
import { TablePagination } from "@/shared/ui/Table/TablePagination";

interface AppointmentPaginationProps {
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  pageSize?: number;
  onPageChange: (page: number) => void;
}

export function AppointmentPagination({
  pageNumber,
  totalPages,
  totalCount,
  pageSize = 10,
  onPageChange,
}: AppointmentPaginationProps) {
  if (totalPages <= 0) return null;

  return (
    <div className="border-t border-slate-200 px-5 py-2">
      <TablePagination
        currentPage={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalItems={totalCount}
        onPageChange={onPageChange}
        itemName="lịch hẹn"
      />
    </div>
  );
}
