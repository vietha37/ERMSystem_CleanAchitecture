"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { Modal } from "@/components/ui/Modal";
import { useTranslation } from "@/hooks/useTranslation";
import { getApiErrorMessage } from "@/services/error";
import { staffUserService } from "@/services/staffUserService";
import { StaffUser, UpdateStaffUserPayload } from "@/services/types";

type FormState = {
  username: string;
  name: string;
  password: string;
  role: "Doctor" | "Cashier";
};

const initialForm: FormState = {
  username: "",
  name: "",
  password: "",
  role: "Doctor",
};

export default function StaffPage() {
  const { t } = useTranslation();
  const [items, setItems] = useState<StaffUser[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSyncing, setIsSyncing] = useState(false);

  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const [roleFilter, setRoleFilter] = useState<"" | "Doctor" | "Cashier" | "Patient">("");
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [mode, setMode] = useState<"create" | "edit">("create");
  const [selected, setSelected] = useState<StaffUser | null>(null);
  const [form, setForm] = useState<FormState>(initialForm);
  const [showPassword, setShowPassword] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPageNumber(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [search]);

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await staffUserService.getAll(pageNumber, pageSize, roleFilter, debouncedSearch);
      setItems(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages || 1);
      setListError(null);
    } catch (error) {
      const message = getApiErrorMessage(error, t("common.messages.error"));
      setListError(message);
      setItems([]);
      setTotalCount(0);
      setTotalPages(1);
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  }, [pageNumber, pageSize, roleFilter, debouncedSearch, t]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const openCreateModal = () => {
    setMode("create");
    setSelected(null);
    setForm(initialForm);
    setShowPassword(false);
    setIsModalOpen(true);
  };

  const openEditModal = (user: StaffUser) => {
    if (user.role === "Patient") return;

    setMode("edit");
    setSelected(user);
    setForm({
      username: user.username,
      name: user.name,
      password: "",
      role: user.role,
    });
    setShowPassword(false);
    setIsModalOpen(true);
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setIsSubmitting(true);
    try {
      if (mode === "create") {
        await staffUserService.create({
          username: form.username.trim(),
          name: form.name.trim(),
          password: form.password,
          role: form.role,
        });
        toast.success(t("common.messages.createSuccess"));
      } else if (selected) {
        const payload: UpdateStaffUserPayload = {
          username: form.username.trim(),
          name: form.name.trim(),
          role: form.role,
        };
        if (form.password.trim()) {
          payload.password = form.password.trim();
        }
        await staffUserService.update(selected.id, payload);
        toast.success(t("common.messages.updateSuccess"));
      }

      setIsModalOpen(false);
      fetchData();
    } catch (error) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (user: StaffUser) => {
    const ok = window.confirm(
      `${t("common.confirmations.deleteMessage")} (${user.username})`
    );
    if (!ok) return;

    try {
      await staffUserService.delete(user.id);
      toast.success(t("common.messages.deleteSuccess"));
      if (items.length === 1 && pageNumber > 1) {
        setPageNumber((current) => current - 1);
      } else {
        fetchData();
      }
    } catch (error) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    }
  };

  const handleSyncHospitalIdentity = async () => {
    setIsSyncing(true);
    try {
      const res = await staffUserService.syncHospitalIdentity();
      toast.success(
        t("common.messages.syncSuccess", {
          synced: res.syncedUsers,
          total: res.totalUsers,
        })
      );
      fetchData();
    } catch (error) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setIsSyncing(false);
    }
  };

  const titleByMode = useMemo(
    () => (mode === "create" ? t("staff.createStaff") : t("common.actions.edit")),
    [mode, t]
  );

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border-b border-slate-200 pb-5">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-slate-900">
            {t("staff.title")}
          </h1>
          <p className="mt-1 text-xs text-slate-500">
            {t("staff.subtitle")}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2.5">
          <Button
            variant="secondary"
            size="sm"
            isLoading={isSyncing}
            onClick={handleSyncHospitalIdentity}
          >
            {t("common.actions.syncHospitalIdentity")}
          </Button>
          <Button variant="primary" size="sm" onClick={openCreateModal}>
            + {t("staff.createStaff")}
          </Button>
        </div>
      </div>

      {/* Main Table Card */}
      <Card padding="none">
        <div className="p-4 sm:p-5 border-b border-slate-100 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          {/* Search Input */}
          <div className="relative w-full sm:max-w-xs">
            <input
              type="text"
              placeholder={t("staff.searchPlaceholder")}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-900 shadow-2xs outline-none transition focus:border-slate-500 focus:ring-1 focus:ring-slate-500"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </div>

          {/* Filters */}
          <div className="flex items-center gap-2">
            <select
              className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-700 shadow-2xs outline-none transition focus:border-slate-500"
              value={roleFilter}
              onChange={(event) => {
                setRoleFilter(event.target.value as "" | "Doctor" | "Cashier" | "Patient");
                setPageNumber(1);
              }}
            >
              <option value="">{t("common.status.all")}</option>
              <option value="Doctor">{t("auth.roles.Doctor")}</option>
              <option value="Cashier">{t("auth.roles.Cashier")}</option>
              <option value="Patient">{t("auth.roles.Patient")}</option>
            </select>

            <select
              className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs text-slate-700 shadow-2xs outline-none transition focus:border-slate-500"
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value));
                setPageNumber(1);
              }}
            >
              <option value={10}>10 / trang</option>
              <option value={20}>20 / trang</option>
              <option value={50}>50 / trang</option>
            </select>
          </div>
        </div>

        {/* Table Body */}
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
                      onAction={() => void fetchData()}
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
                        className={`inline-flex items-center rounded-md px-2 py-0.5 text-[11px] font-semibold ${
                          user.role === "Doctor"
                            ? "bg-sky-50 text-sky-700 border border-sky-100"
                            : user.role === "Cashier"
                            ? "bg-amber-50 text-amber-700 border border-amber-100"
                            : "bg-emerald-50 text-emerald-700 border border-emerald-100"
                        }`}
                      >
                        {t(`auth.roles.${user.role}`) || user.role}
                      </span>
                    </td>
                    <td className="px-5 py-3.5 text-right space-x-1.5">
                      {user.role !== "Patient" && (
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => openEditModal(user)}
                        >
                          {t("common.actions.edit")}
                        </Button>
                      )}
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => handleDelete(user)}
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

        {/* Pagination Footer */}
        {!isLoading && totalPages > 0 && (
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between border-t border-slate-100 px-5 py-3.5 text-xs text-slate-600">
            <div>
              {t("common.labels.totalRecords", { count: totalCount })}
            </div>
            <div className="flex items-center gap-1.5">
              <Button
                variant="secondary"
                size="sm"
                disabled={pageNumber <= 1}
                onClick={() => setPageNumber((c) => Math.max(1, c - 1))}
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
                onClick={() => setPageNumber((c) => Math.min(totalPages, c + 1))}
              >
                {t("common.labels.next")}
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Create / Edit Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={titleByMode}
        badge={t("nav.staff")}
      >
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              {t("staff.fields.username")} <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
              value={form.username}
              onChange={(event) =>
                setForm((current) => ({ ...current, username: event.target.value }))
              }
              required
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              {t("staff.fields.name")} <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
              value={form.name}
              onChange={(event) =>
                setForm((current) => ({ ...current, name: event.target.value }))
              }
              required
              minLength={3}
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              {t("staff.fields.role")} <span className="text-rose-500">*</span>
            </label>
            <select
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
              value={form.role}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  role: event.target.value as "Doctor" | "Cashier",
                }))
              }
              required
            >
              <option value="Doctor">{t("auth.roles.Doctor")}</option>
              <option value="Cashier">{t("auth.roles.Cashier")}</option>
            </select>
          </div>

          <div>
            <label className="mb-1 block text-xs font-semibold text-slate-700">
              {mode === "create" ? (
                <>
                  {t("staff.fields.password")}{" "}
                  <span className="text-rose-500">*</span>
                </>
              ) : (
                `${t("staff.fields.password")} (${t("common.labels.optional")})`
              )}
            </label>
            <div className="relative">
              <input
                type={showPassword ? "text" : "password"}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 pr-14 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
                value={form.password}
                onChange={(event) =>
                  setForm((current) => ({ ...current, password: event.target.value }))
                }
                required={mode === "create"}
                minLength={6}
              />
              <button
                type="button"
                onClick={() => setShowPassword((v) => !v)}
                className="absolute inset-y-0 right-0 px-3 text-[11px] font-semibold text-slate-500 hover:text-slate-800"
              >
                {showPassword ? "Ẩn" : "Hiện"}
              </button>
            </div>
          </div>

          <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setIsModalOpen(false)}
            >
              {t("common.actions.cancel")}
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="sm"
              isLoading={isSubmitting}
            >
              {mode === "create"
                ? t("common.actions.create")
                : t("common.actions.save")}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
