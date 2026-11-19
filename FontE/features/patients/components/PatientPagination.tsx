"use client";

import React from "react";
import { TablePagination } from "@/shared/ui/Table/TablePagination";

interface PatientPaginationProps {
  currentPage: number;
  pageSize: number;
  totalPages: number;
  totalItems: number;
  onPageChange: (page: number) => void;
}

export function PatientPagination({
  currentPage,
  pageSize,
  totalPages,
  totalItems,
  onPageChange,
}: PatientPaginationProps) {
  if (totalPages <= 0) return null;

  return (
    <div className="mt-4 border-t border-slate-200 pt-3">
      <TablePagination
        currentPage={currentPage}
        pageSize={pageSize}
        totalPages={totalPages}
        totalItems={totalItems}
        onPageChange={onPageChange}
        itemName="bệnh nhân"
      />
    </div>
  );
}
