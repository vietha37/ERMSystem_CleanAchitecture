"use client";

import React, { useEffect, useRef } from "react";
import { Modal } from "@/components/ui/Modal";
import { PatientFormState } from "../types";

interface PatientFormModalProps {
  isOpen: boolean;
  modalMode: "create" | "edit";
  fullName: string;
  onFullNameChange: (value: string) => void;
  formData: PatientFormState;
  onFormDataChange: (updater: (prev: PatientFormState) => PatientFormState) => void;
  isSubmitting: boolean;
  onSubmit: (event: React.FormEvent) => void;
  onClose: () => void;
}

export function PatientFormModal({
  isOpen,
  modalMode,
  fullName,
  onFullNameChange,
  formData,
  onFormDataChange,
  isSubmitting,
  onSubmit,
  onClose,
}: PatientFormModalProps) {
  const firstInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isOpen && firstInputRef.current) {
      const timer = setTimeout(() => firstInputRef.current?.focus(), 100);
      return () => clearTimeout(timer);
    }
  }, [isOpen]);

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={modalMode === "create" ? "Thêm bệnh nhân mới" : "Cập nhật hồ sơ bệnh nhân"}
    >
      <form onSubmit={onSubmit} className="mt-2 space-y-4 px-1 pb-2">
        <div>
          <label className="mb-1.5 block text-sm font-bold text-gray-700">
            Họ và tên <span className="text-red-500">*</span>
          </label>
          <input
            ref={firstInputRef}
            type="text"
            className="w-full rounded-xl border border-gray-300 px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all placeholder:text-gray-400 focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
            placeholder="Ví dụ: Nguyễn Văn A"
            value={fullName}
            onChange={(event) => onFullNameChange(event.target.value)}
            required
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="mb-1.5 block text-sm font-bold text-gray-700">
              Ngày sinh <span className="text-red-500">*</span>
            </label>
            <input
              type="date"
              className="w-full rounded-xl border border-gray-300 px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
              value={formData.dateOfBirth}
              onChange={(event) =>
                onFormDataChange((prev) => ({ ...prev, dateOfBirth: event.target.value }))
              }
              required
            />
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-bold text-gray-700">Giới tính</label>
            <select
              className="w-full cursor-pointer rounded-xl border border-gray-300 bg-white px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
              value={formData.gender}
              onChange={(event) =>
                onFormDataChange((prev) => ({ ...prev, gender: event.target.value }))
              }
            >
              <option value="Male">Nam</option>
              <option value="Female">Nữ</option>
              <option value="Other">Khác</option>
            </select>
          </div>
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-bold text-gray-700">Số điện thoại</label>
          <input
            type="tel"
            className="w-full rounded-xl border border-gray-300 px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all placeholder:text-gray-400 focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
            placeholder="0901234567"
            value={formData.phone}
            onChange={(event) =>
              onFormDataChange((prev) => ({ ...prev, phone: event.target.value }))
            }
          />
        </div>

        <div>
          <label className="mb-1.5 block text-sm font-bold text-gray-700">Địa chỉ thường trú</label>
          <textarea
            rows={3}
            className="w-full resize-y rounded-xl border border-gray-300 px-4 py-2.5 font-medium text-gray-800 shadow-sm outline-none transition-all placeholder:text-gray-400 focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20"
            placeholder="Nhập địa chỉ..."
            value={formData.address}
            onChange={(event) =>
              onFormDataChange((prev) => ({ ...prev, address: event.target.value }))
            }
          />
        </div>

        <div className="flex justify-end gap-3 border-t border-gray-100 pt-6">
          <button
            type="button"
            onClick={onClose}
            className="rounded-xl bg-gray-100 px-5 py-2.5 font-bold text-gray-600 transition-colors hover:bg-gray-200"
          >
            Hủy
          </button>
          <button
            type="submit"
            disabled={isSubmitting}
            className="flex items-center gap-2 rounded-xl bg-blue-600 px-6 py-2.5 font-bold text-white shadow-md transition-colors hover:bg-blue-700 hover:shadow-lg disabled:opacity-50"
          >
            {isSubmitting ? (
              <>
                <div className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
                {modalMode === "create" ? "Đang tạo..." : "Đang cập nhật..."}
              </>
            ) : modalMode === "create" ? (
              "Lưu bệnh nhân"
            ) : (
              "Cập nhật thay đổi"
            )}
          </button>
        </div>
      </form>
    </Modal>
  );
}
