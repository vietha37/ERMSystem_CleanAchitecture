"use client";

import React from "react";

export interface TablePaginationProps {
  currentPage: number;
  totalPages: number;
  totalItems: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  pageSizeOptions?: number[];
  itemName?: string;
  className?: string;
}

export function TablePagination({
  currentPage,
  totalPages,
  totalItems,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [5, 10, 20, 50],
  itemName = "bản ghi",
  className = "",
}: TablePaginationProps) {
  if (totalItems === 0 && totalPages <= 1) {
    return null;
  }

  const startItem = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalItems);

  return (
    <div
      className={`flex flex-col gap-3 py-3 sm:flex-row sm:items-center sm:justify-between text-sm ${className}`}
    >
      <div className="flex items-center gap-3 text-slate-500">
        <span>
          Hiển thị{" "}
          <span className="font-semibold text-slate-800">{startItem}</span> –{" "}
          <span className="font-semibold text-slate-800">{endItem}</span> trong tổng số{" "}
          <span className="font-semibold text-slate-900">{totalItems}</span> {itemName}
        </span>

        {onPageSizeChange && (
          <div className="flex items-center gap-1.5 pl-3 border-l border-slate-200">
            <span className="text-xs text-slate-500">Cỡ trang:</span>
            <select
              aria-label="Số dòng trên mỗi trang"
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="rounded-md border border-slate-300 bg-white px-2 py-1 text-xs font-medium text-slate-700 shadow-2xs focus:border-sky-600 focus:outline-none"
            >
              {pageSizeOptions.map((opt) => (
                <option key={opt} value={opt}>
                  {opt} / trang
                </option>
              ))}
            </select>
          </div>
        )}
      </div>

      <nav aria-label="Phân trang danh sách" className="flex items-center gap-1">
        <button
          type="button"
          disabled={currentPage <= 1}
          onClick={() => onPageChange(1)}
          aria-label="Đến trang đầu tiên"
          className="rounded-md border border-slate-300 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          «
        </button>

        <button
          type="button"
          disabled={currentPage <= 1}
          onClick={() => onPageChange(currentPage - 1)}
          aria-label="Trang trước"
          className="rounded-md border border-slate-300 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          Trước
        </button>

        <span className="rounded-md bg-slate-100 px-3 py-1 text-xs font-bold text-slate-800">
          {currentPage} / {Math.max(1, totalPages)}
        </span>

        <button
          type="button"
          disabled={currentPage >= totalPages}
          onClick={() => onPageChange(currentPage + 1)}
          aria-label="Trang tiếp theo"
          className="rounded-md border border-slate-300 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          Tiếp
        </button>

        <button
          type="button"
          disabled={currentPage >= totalPages}
          onClick={() => onPageChange(totalPages)}
          aria-label="Đến trang cuối cùng"
          className="rounded-md border border-slate-300 bg-white px-2.5 py-1 text-xs font-semibold text-slate-700 transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          »
        </button>
      </nav>
    </div>
  );
}
