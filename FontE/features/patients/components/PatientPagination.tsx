import React from "react";

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
    <div className="mt-5 flex items-center justify-between text-sm">
      <div className="font-medium text-gray-500">
        Hiển thị{" "}
        <span className="font-bold text-gray-900">
          {totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1}
        </span>{" "}
        đến{" "}
        <span className="font-bold text-gray-900">
          {Math.min(currentPage * pageSize, totalItems)}
        </span>{" "}
        trong tổng số <span className="font-bold text-blue-600">{totalItems}</span> bệnh nhân
      </div>

      <div className="flex items-center gap-1 rounded-xl border border-gray-200 bg-gray-50 p-1 shadow-sm">
        <button
          disabled={currentPage === 1}
          onClick={() => onPageChange(1)}
          className="rounded-lg px-3 py-1.5 font-bold text-gray-600 transition-all hover:bg-white hover:shadow-sm disabled:opacity-40"
        >
          «
        </button>
        <button
          disabled={currentPage === 1}
          onClick={() => onPageChange(currentPage - 1)}
          className="rounded-lg px-3 py-1.5 font-bold text-gray-600 transition-all hover:bg-white hover:shadow-sm disabled:opacity-40"
        >
          Trước
        </button>
        <span className="rounded-lg bg-blue-100/50 px-4 py-1.5 font-bold text-blue-700 shadow-inner">
          {currentPage} / {totalPages}
        </span>
        <button
          disabled={currentPage === totalPages}
          onClick={() => onPageChange(currentPage + 1)}
          className="rounded-lg px-3 py-1.5 font-bold text-gray-600 transition-all hover:bg-white hover:shadow-sm disabled:opacity-40"
        >
          Tiếp
        </button>
      </div>
    </div>
  );
}
