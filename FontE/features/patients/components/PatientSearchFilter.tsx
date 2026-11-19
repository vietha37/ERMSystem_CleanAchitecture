import React from "react";

interface PatientSearchFilterProps {
  searchQuery: string;
  onSearchChange: (value: string) => void;
  pageSize: number;
  onPageSizeChange: (value: number) => void;
}

export function PatientSearchFilter({
  searchQuery,
  onSearchChange,
  pageSize,
  onPageSizeChange,
}: PatientSearchFilterProps) {
  return (
    <div className="mb-6 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
      <div className="relative w-full md:w-[420px]">
        <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-gray-400">
          Tim
        </span>
        <input
          type="text"
          placeholder="Tìm theo tên, số điện thoại, mã bệnh nhân..."
          className="w-full rounded-xl border border-gray-200 py-2.5 pl-10 pr-4 text-sm shadow-sm outline-none transition-all focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
          value={searchQuery}
          onChange={(event) => onSearchChange(event.target.value)}
        />
      </div>

      <div className="flex items-center gap-3 rounded-xl border border-gray-100 bg-gray-50 px-4 py-2">
        <span className="text-sm font-medium text-gray-500">Hiển thị</span>
        <select
          className="cursor-pointer bg-transparent text-sm font-bold text-gray-700 outline-none"
          value={pageSize}
          onChange={(event) => onPageSizeChange(Number(event.target.value))}
        >
          <option value={5}>5 / trang</option>
          <option value={10}>10 / trang</option>
          <option value={20}>20 / trang</option>
        </select>
      </div>
    </div>
  );
}
