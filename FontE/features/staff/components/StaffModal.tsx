"use client";

import { FormEvent } from "react";
import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { StaffFormState } from "../types";

interface StaffModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  form: StaffFormState;
  onFormChange: (field: keyof StaffFormState, value: string) => void;
  mode: "create" | "edit";
  showPassword: boolean;
  onToggleShowPassword: () => void;
  onSubmit: (e: FormEvent) => void;
  isSubmitting: boolean;
  t: (key: string) => string;
}

export function StaffModal({
  isOpen,
  onClose,
  title,
  form,
  onFormChange,
  mode,
  showPassword,
  onToggleShowPassword,
  onSubmit,
  isSubmitting,
  t,
}: StaffModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={title}
      badge={t("nav.staff")}
    >
      <form onSubmit={onSubmit} className="space-y-4">
        <div>
          <label className="mb-1 block text-xs font-semibold text-slate-700">
            {t("staff.fields.username")} <span className="text-rose-500">*</span>
          </label>
          <input
            type="text"
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs text-slate-900 shadow-2xs outline-none focus:border-slate-500"
            value={form.username}
            onChange={(event) => onFormChange("username", event.target.value)}
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
            onChange={(event) => onFormChange("name", event.target.value)}
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
              onFormChange("role", event.target.value)
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
              onChange={(event) => onFormChange("password", event.target.value)}
              required={mode === "create"}
              minLength={6}
            />
            <button
              type="button"
              onClick={onToggleShowPassword}
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
            onClick={onClose}
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
  );
}
