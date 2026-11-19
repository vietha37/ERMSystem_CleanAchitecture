"use client";

import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { TablePagination } from "@/shared/ui/Table/TablePagination";
import { StaffRoleFilter, StaffUser } from "../types";

interface StaffTableProps {
  items: StaffUser[];
  isLoading: boolean;
  listError: string | null;
  search: string;
  onSearchChange: (val: string) => void;
  roleFilter: StaffRoleFilter;
  onRoleFilterChange: (val: StaffRoleFilter) => void;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  onRetry: () => void;
  onEdit: (user: StaffUser) => void;
  onDelete: (user: StaffUser) => void;
  t: (key: string, options?: Record<string, string | number>) => string;
}

function getRoleBadgeClass(role: string): string {
  switch (role) {
    case "Doctor":
      return "bg-sky-50 text-sky-700 border border-sky-100";
    case "Cashier":
      return "bg-amber-50 text-amber-700 border border-amber-100";
    default:
      return "bg-emerald-50 text-emerald-700 border border-emerald-100";
  }
}

export function StaffTable({
  items,
  isLoading,
  listError,
  search,
  onSearchChange,
  roleFilter,
  onRoleFilterChange,
  pageNumber,
  pageSize,
  totalPages,
  totalCount,
  onPageChange,
  onPageSizeChange,
  onRetry,
  onEdit,
  onDelete,
  t,
}: StaffTableProps) {
  return (
    <Card padding="none">
      <div className="p-4 sm:p-5 border-b border-slate-100 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative w-full sm:max-w-xs">
          <input
            type="text"
            placeholder={t("staff.searchPlaceholder")}
            className="w-full rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-900 shadow-2xs outline-none transition focus:border-slate-500 focus:ring-1 focus:ring-slate-500"
            value={search}
            onChange={(event) => onSearchChange(event.target.value)}
          />
        </div>

        <div className="flex items-center gap-2">
          <select
            className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-700 shadow-2xs outline-none transition focus:border-slate-500"
            value={roleFilter}
            onChange={(event) => onRoleFilterChange(event.target.value as StaffRoleFilter)}
          >
            <option value="">{t("common.status.all")}</option>
            <option value="Doctor">{t("auth.roles.Doctor")}</option>
            <option value="Cashier">{t("auth.roles.Cashier")}</option>
            <option value="Patient">{t("auth.roles.Patient")}</option>
          </select>
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full border-collapse text-left text-xs">
          <thead className="border-b border-slate-200 bg-slate-50 text-slate-600 font-semibold">
            <tr>
              <th className="px-5 py-3">{t("staff.fields.username")}</th>
              <th className="px-5 py-3">{t("staff.fields.name")}</th>
              <th className="px-5 py-3">{t("staff.fields.role")}</th>
              <th className="px-5 py-3 text-right">{t("common.labels.actions")}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 text-slate-800">
            {isLoading ? (
              <tr>
                <td colSpan={4}>
                  <LoadingState title={t("common.actions.loading")} />
                </td>
              </tr>
            ) : listError ? (
              <tr>
                <td colSpan={4}>
                  <ErrorState
                    title={t("common.messages.error")}
                    description={listError}
                    onAction={onRetry}
                  />
                </td>
              </tr>
            ) : items.length === 0 ? (
              <tr>
                <td colSpan={4}>
                  <EmptyState
                    title={t("common.labels.empty")}
                    description={t("common.labels.noResults")}
                  />
                </td>
              </tr>
            ) : (
              items.map((user) => (
                <tr key={user.id} className="transition-colors hover:bg-slate-50/70">
                  <td className="px-5 py-3.5 font-semibold text-slate-900 font-mono">
                    {user.username}
                  </td>
                  <td className="px-5 py-3.5 font-medium">{user.name}</td>
                  <td className="px-5 py-3.5">
                    <span
                      className={`inline-flex items-center rounded-md px-2 py-0.5 text-[11px] font-semibold ${getRoleBadgeClass(
                        user.role
                      )}`}
                    >
                      {t(`auth.roles.${user.role}`) || user.role}
                    </span>
                  </td>
                  <td className="px-5 py-3.5 text-right space-x-1.5">
                    {user.role !== "Patient" && (
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => onEdit(user)}
                      >
                        {t("common.actions.edit")}
                      </Button>
                    )}
                    <Button
                      variant="danger"
                      size="sm"
                      onClick={() => onDelete(user)}
                    >
                      {t("common.actions.delete")}
                    </Button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {!isLoading && totalCount > 0 && (
        <div className="border-t border-slate-100 px-5 py-2">
          <TablePagination
            currentPage={pageNumber}
            totalPages={totalPages}
            totalItems={totalCount}
            pageSize={pageSize}
            onPageChange={onPageChange}
            onPageSizeChange={onPageSizeChange}
            pageSizeOptions={[10, 20, 50]}
            itemName="nhân viên"
          />
        </div>
      )}
    </Card>
  );
}
