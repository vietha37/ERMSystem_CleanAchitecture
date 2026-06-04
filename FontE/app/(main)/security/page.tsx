"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import toast from "react-hot-toast";
import { ErrorState } from "@/components/ui/DataState";
import { authService } from "@/services/authService";
import { getApiErrorMessage } from "@/services/error";
import { MfaSetupResponse, MfaStatus } from "@/services/types";

const emptyStatus: MfaStatus = {
  isEnabled: false,
  isSetupPending: false,
  enabledAtUtc: null,
};

export default function SecurityPage() {
  const [status, setStatus] = useState<MfaStatus>(emptyStatus);
  const [setup, setSetup] = useState<MfaSetupResponse | null>(null);
  const [enableCode, setEnableCode] = useState("");
  const [disableCode, setDisableCode] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [statusError, setStatusError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadStatus = useCallback(async () => {
    setIsLoading(true);
    try {
      const nextStatus = await authService.getMfaStatus();
      setStatus(nextStatus);
      setStatusError(null);
    } catch (error) {
      setStatusError(getApiErrorMessage(error, "Không thể tải trạng thái bảo mật."));
      toast.error(getApiErrorMessage(error, "Không thể tải trạng thái bảo mật."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadStatus();
  }, [loadStatus]);

  const handleStartSetup = async () => {
    setIsSubmitting(true);
    try {
      const response = await authService.setupMfa();
      setSetup(response);
      setEnableCode("");
      setStatus((current) => ({ ...current, isSetupPending: true }));
      toast.success("Đã tạo phiên thiết lập MFA.");
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể bắt đầu thiết lập MFA."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleEnable = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    try {
      const nextStatus = await authService.enableMfa({ code: enableCode });
      setStatus(nextStatus);
      setSetup(null);
      setEnableCode("");
      toast.success("Đã bật xác thực hai bước.");
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể bật MFA."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDisable = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    try {
      const nextStatus = await authService.disableMfa({ code: disableCode });
      setStatus(nextStatus);
      setSetup(null);
      setDisableCode("");
      toast.success("Đã tắt xác thực hai bước.");
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể tắt MFA."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <section className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
        <h1 className="text-3xl font-bold tracking-tight text-gray-800">Bảo mật đăng nhập</h1>
        <p className="mt-2 text-sm leading-7 text-gray-500">
          Quản lý xác thực hai bước cho tài khoản nội bộ. Khi bật MFA, hệ thống sẽ yêu cầu thêm mã
          6 số từ ứng dụng xác thực sau bước nhập mật khẩu.
        </p>
      </section>

      <section className="grid gap-6 md:grid-cols-2">
        <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
          <p className="text-sm font-semibold uppercase tracking-[0.2em] text-gray-400">Trạng thái</p>
          {isLoading ? (
            <p className="mt-4 text-sm text-gray-500">Đang tải trạng thái...</p>
          ) : statusError ? (
            <ErrorState
              title="Không thể tải trạng thái bảo mật"
              description={statusError}
              actionLabel="Tải lại"
              onAction={() => void loadStatus()}
            />
          ) : (
            <>
              <div className="mt-4 inline-flex rounded-full border border-cyan-100 bg-cyan-50 px-4 py-2 text-sm font-semibold text-cyan-700">
                {status.isEnabled ? "MFA đang bật" : "MFA chưa bật"}
              </div>
              <p className="mt-4 text-sm text-gray-600">
                {status.isEnabled && status.enabledAtUtc
                  ? `Đã kích hoạt lúc ${new Date(status.enabledAtUtc).toLocaleString("vi-VN")}.`
                  : "Hiện tại bạn vẫn có thể đăng nhập chỉ bằng mật khẩu."}
              </p>
            </>
          )}
        </div>

        <div className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
          <p className="text-sm font-semibold uppercase tracking-[0.2em] text-gray-400">Khuyến nghị</p>
          <ul className="mt-4 space-y-3 text-sm leading-7 text-gray-600">
            <li>Dùng Google Authenticator, Microsoft Authenticator hoặc ứng dụng TOTP tương đương.</li>
            <li>Chỉ bật MFA trên thiết bị cá nhân hoặc thiết bị công vụ được kiểm soát.</li>
            <li>Sau khi tắt MFA, các refresh token hiện tại sẽ bị thu hồi.</li>
          </ul>
        </div>
      </section>

      {!status.isEnabled && (
        <section className="rounded-2xl border border-gray-100 bg-white p-6 shadow-sm">
          <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
            <div>
              <h2 className="text-xl font-semibold text-gray-800">Thiết lập xác thực hai bước</h2>
              <p className="mt-2 text-sm text-gray-500">
                Tạo khóa mới, thêm vào ứng dụng xác thực, rồi nhập mã 6 số để kích hoạt.
              </p>
            </div>
            <button
              type="button"
              disabled={isSubmitting}
              onClick={handleStartSetup}
              className="inline-flex h-11 items-center justify-center rounded-full bg-slate-950 px-5 text-sm font-semibold text-white transition hover:bg-cyan-700 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {isSubmitting ? "Đang chuẩn bị..." : "Tạo phiên thiết lập"}
            </button>
          </div>

          {setup && (
            <div className="mt-6 grid gap-4">
              <div className="rounded-2xl border border-cyan-100 bg-cyan-50 p-4">
                <p className="text-sm font-semibold text-cyan-900">Khóa nhập tay</p>
                <p className="mt-2 break-all font-mono text-sm text-cyan-950">{setup.manualEntryKey}</p>
                <p className="mt-3 text-xs text-cyan-800">
                  Hết hạn lúc {new Date(setup.expiresAtUtc).toLocaleString("vi-VN")}.
                </p>
              </div>

              <label className="grid gap-2 text-sm font-medium text-slate-700">
                Liên kết cấu hình OTP
                <textarea
                  rows={3}
                  readOnly
                  value={setup.otpAuthUri}
                  className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 font-mono text-xs text-slate-800 outline-none"
                />
              </label>

              <form onSubmit={handleEnable} className="grid gap-4 md:grid-cols-[1fr_auto]">
                <label className="grid gap-2 text-sm font-medium text-slate-700">
                  Mã xác thực 6 số
                  <input
                    type="text"
                    value={enableCode}
                    onChange={(event) => setEnableCode(event.target.value)}
                    placeholder="123456"
                    className="h-12 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white"
                    required
                  />
                </label>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="inline-flex h-12 items-center justify-center self-end rounded-full bg-cyan-700 px-6 text-sm font-semibold text-white transition hover:bg-cyan-800 disabled:cursor-not-allowed disabled:opacity-70"
                >
                  {isSubmitting ? "Đang kích hoạt..." : "Bật MFA"}
                </button>
              </form>
            </div>
          )}
        </section>
      )}

      {status.isEnabled && (
        <section className="rounded-2xl border border-red-100 bg-white p-6 shadow-sm">
          <h2 className="text-xl font-semibold text-gray-800">Tắt xác thực hai bước</h2>
          <p className="mt-2 text-sm text-gray-500">
            Nhập mã hiện tại từ ứng dụng xác thực để xác nhận thao tác. Sau khi tắt, các phiên đăng
            nhập đang hoạt động sẽ bị thu hồi.
          </p>

          <form onSubmit={handleDisable} className="mt-5 grid gap-4 md:grid-cols-[1fr_auto]">
            <label className="grid gap-2 text-sm font-medium text-slate-700">
              Mã xác thực hiện tại
              <input
                type="text"
                value={disableCode}
                onChange={(event) => setDisableCode(event.target.value)}
                placeholder="123456"
                className="h-12 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 outline-none transition focus:border-red-400 focus:bg-white"
                required
              />
            </label>
            <button
              type="submit"
              disabled={isSubmitting}
              className="inline-flex h-12 items-center justify-center self-end rounded-full bg-red-600 px-6 text-sm font-semibold text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {isSubmitting ? "Đang xử lý..." : "Tắt MFA"}
            </button>
          </form>
        </section>
      )}
    </div>
  );
}
