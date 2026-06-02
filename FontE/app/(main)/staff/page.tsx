"use client";

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Modal } from "@/components/ui/Modal";
import { getApiErrorMessage } from "@/services/error";
import { staffUserService } from "@/services/staffUserService";
import { StaffUser, UpdateStaffUserPayload } from "@/services/types";

type FormState = {
  username: string;
  name: string;
  password: string;
  role: "Doctor" | "Receptionist";
};

const initialForm: FormState = {
  username: "",
  name: "",
  password: "",
  role: "Doctor",
};

export default function StaffPage() {
  const [items, setItems] = useState<StaffUser[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const [roleFilter, setRoleFilter] = useState<"" | "Doctor" | "Receptionist">("");
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
    } catch (error) {
      setItems([]);
      setTotalCount(0);
      setTotalPages(1);
      toast.error(getApiErrorMessage(error, "Không thể tải danh sách tài khoản."));
    } finally {
      setIsLoading(false);
    }
  }, [pageNumber, pageSize, roleFilter, debouncedSearch]);

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
        toast.success("Đã tạo tài khoản nhân sự.");
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
        toast.success("Đã cập nhật tài khoản nhân sự.");
      }

      setIsModalOpen(false);
      fetchData();
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể lưu tài khoản nhân sự."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (user: StaffUser) => {
    const ok = window.confirm(`Xóa tài khoản ${user.username} (${user.role})?`);
    if (!ok) {
      return;
    }

    try {
      await staffUserService.delete(user.id);
      toast.success("Đã xóa tài khoản nhân sự.");
      if (items.length === 1 && pageNumber > 1) {
        setPageNumber((current) => current - 1);
      } else {
        fetchData();
      }
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể xóa tài khoản nhân sự."));
    }
  };

  const titleByMode = useMemo(
    () => (mode === "create" ? "Tạo tài khoản nhân sự" : "Cập nhật tài khoản nhân sự"),
    [mode]
  );

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <div className="flex items-center justify-between rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
        <div>
          <h1 className="text-3xl font-bold tracking-tight text-gray-800">Nhân sự</h1>
          <p className="mt-1 text-sm text-gray-500">
            Quản trị viên có thể quản lý tài khoản bác sĩ và lễ tân.
          </p>
        </div>
        <Button onClick={openCreateModal}>+ Thêm nhân sự</Button>
      </div>

      <Card className="rounded-2xl border-none bg-white p-6 shadow-sm">
        <div className="mb-6 flex flex-col justify-between gap-3 md:flex-row">
          <input
            type="text"
            placeholder="Tìm theo họ tên hoặc tên đăng nhập..."
            className="w-full rounded-xl border border-gray-200 px-4 py-2.5 text-sm shadow-sm outline-none transition-all focus:border-blue-500 focus:ring-4 focus:ring-blue-500/20 md:w-[360px]"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />

          <div className="flex items-center gap-3">
            <select
              className="rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm"
              value={roleFilter}
              onChange={(event) => {
                setRoleFilter(event.target.value as "" | "Doctor" | "Receptionist");
                setPageNumber(1);
              }}
            >
              <option value="">Tất cả vai trò</option>
              <option value="Doctor">Bác sĩ</option>
              <option value="Receptionist">Lễ tân</option>
            </select>

            <select
              className="rounded-xl border border-gray-200 bg-white px-3 py-2 text-sm"
              value={pageSize}
              onChange={(event) => {
                setPageSize(Number(event.target.value));
                setPageNumber(1);
              }}
            >
              <option value={5}>5 / trang</option>
              <option value={10}>10 / trang</option>
              <option value={20}>20 / trang</option>
            </select>
          </div>
        </div>

        <div className="overflow-hidden rounded-2xl border border-gray-100 bg-white shadow-sm">
          <table className="w-full border-collapse text-left">
            <thead className="border-b border-gray-200 bg-gray-50">
              <tr>
                <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">Tên đăng nhập</th>
                <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">Họ và tên</th>
                <th className="p-4 text-xs font-bold uppercase tracking-wider text-gray-500">Vai trò</th>
                <th className="p-4 text-right text-xs font-bold uppercase tracking-wider text-gray-500">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {isLoading ? (
                <tr>
                  <td colSpan={4} className="p-8 text-center text-gray-500">
                    Đang tải...
                  </td>
                </tr>
              ) : items.length === 0 ? (
                <tr>
                  <td colSpan={4} className="p-8 text-center text-gray-500">
                    Không tìm thấy tài khoản nào.
                  </td>
                </tr>
              ) : (
                items.map((user) => (
                  <tr key={user.id} className="transition-colors hover:bg-blue-50/30">
                    <td className="p-4 font-semibold text-gray-800">{user.username}</td>
                    <td className="p-4 text-gray-700">{user.name}</td>
                    <td className="p-4">
                      <span
                        className={`rounded-lg px-2.5 py-1 text-xs font-bold ${
                          user.role === "Doctor"
                            ? "border border-cyan-100 bg-cyan-50 text-cyan-700"
                            : "border border-amber-100 bg-amber-50 text-amber-700"
                        }`}
                      >
                        {user.role === "Doctor" ? "Bác sĩ" : "Lễ tân"}
                      </span>
                    </td>
                    <td className="space-x-2 p-4 text-right">
                      <button
                        onClick={() => openEditModal(user)}
                        className="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-sm font-semibold text-gray-600 shadow-sm transition-colors hover:border-blue-300 hover:bg-blue-50 hover:text-blue-600"
                      >
                        Sửa
                      </button>
                      <button
                        onClick={() => handleDelete(user)}
                        className="rounded-lg border border-red-100 bg-white px-3 py-1.5 text-sm font-semibold text-red-500 shadow-sm transition-colors hover:border-red-600 hover:bg-red-500 hover:text-white"
                      >
                        Xóa
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {!isLoading && totalPages > 0 && (
          <div className="mt-5 flex items-center justify-between text-sm">
            <div className="font-medium text-gray-500">
              Hiển thị{" "}
              <span className="font-bold text-gray-900">
                {totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1}
              </span>{" "}
              đến{" "}
              <span className="font-bold text-gray-900">{Math.min(pageNumber * pageSize, totalCount)}</span>{" "}
              trong tổng số <span className="font-bold text-blue-600">{totalCount}</span> tài khoản
            </div>
            <div className="flex items-center gap-1 rounded-xl border border-gray-200 bg-gray-50 p-1">
              <button
                disabled={pageNumber === 1}
                onClick={() => setPageNumber(1)}
                className="rounded-lg px-3 py-1.5 font-bold text-gray-600 disabled:opacity-40 hover:bg-white"
              >
                «
              </button>
              <button
                disabled={pageNumber === 1}
                onClick={() => setPageNumber((current) => current - 1)}
                className="rounded-lg px-3 py-1.5 font-bold text-gray-600 disabled:opacity-40 hover:bg-white"
              >
                Trước
              </button>
              <span className="rounded-lg bg-blue-100/50 px-4 py-1.5 font-bold text-blue-700">
                {pageNumber} / {totalPages}
              </span>
              <button
                disabled={pageNumber === totalPages}
                onClick={() => setPageNumber((current) => current + 1)}
                className="rounded-lg px-3 py-1.5 font-bold text-gray-600 disabled:opacity-40 hover:bg-white"
              >
                Tiếp
              </button>
            </div>
          </div>
        )}
      </Card>

      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title={titleByMode}>
        <form onSubmit={handleSubmit} className="mt-2 space-y-4 px-1 pb-2">
          <div>
            <label className="mb-1.5 block text-sm font-bold text-gray-700">
              Tên đăng nhập <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              className="w-full rounded-xl border border-gray-300 px-4 py-2.5 text-gray-800 outline-none"
              value={form.username}
              onChange={(event) => setForm((current) => ({ ...current, username: event.target.value }))}
              required
            />
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-bold text-gray-700">
              Họ và tên <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              className="w-full rounded-xl border border-gray-300 px-4 py-2.5 text-gray-800 outline-none"
              value={form.name}
              onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
              required
              minLength={3}
            />
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-bold text-gray-700">
              Vai trò <span className="text-red-500">*</span>
            </label>
            <select
              className="w-full rounded-xl border border-gray-300 bg-white px-4 py-2.5 text-gray-800 outline-none"
              value={form.role}
              onChange={(event) =>
                setForm((current) => ({
                  ...current,
                  role: event.target.value as "Doctor" | "Receptionist",
                }))
              }
              required
            >
              <option value="Doctor">Bác sĩ</option>
              <option value="Receptionist">Lễ tân</option>
            </select>
          </div>

          <div>
            <label className="mb-1.5 block text-sm font-bold text-gray-700">
              {mode === "create" ? (
                <>
                  Mật khẩu <span className="text-red-500">*</span>
                </>
              ) : (
                "Mật khẩu mới (không bắt buộc)"
              )}
            </label>
            <div className="relative">
              <input
                type={showPassword ? "text" : "password"}
                className="w-full rounded-xl border border-gray-300 px-4 py-2.5 pr-16 text-gray-800 outline-none"
                value={form.password}
                onChange={(event) => setForm((current) => ({ ...current, password: event.target.value }))}
                required={mode === "create"}
                minLength={6}
              />
              <button
                type="button"
                onClick={() => setShowPassword((value) => !value)}
                className="absolute inset-y-0 right-0 px-3 text-xs font-semibold text-gray-500 transition-colors hover:text-blue-600"
                aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                title={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
              >
                {showPassword ? "Ẩn" : "Hiện"}
              </button>
            </div>
          </div>

          <div className="flex justify-end gap-3 border-t border-gray-100 pt-6">
            <button
              type="button"
              onClick={() => setIsModalOpen(false)}
              className="rounded-xl bg-gray-100 px-5 py-2.5 font-bold text-gray-600 hover:bg-gray-200"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="rounded-xl bg-blue-600 px-6 py-2.5 font-bold text-white disabled:opacity-50 hover:bg-blue-700"
            >
              {isSubmitting ? "Đang lưu..." : mode === "create" ? "Tạo tài khoản" : "Lưu thay đổi"}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
