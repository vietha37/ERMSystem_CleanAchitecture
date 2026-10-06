"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import toast from "react-hot-toast";
import { Button } from "@/components/ui/Button";
import { Card, CardHeader } from "@/components/ui/Card";
import { ErrorState, LoadingState } from "@/components/ui/DataState";
import { useTranslation } from "@/hooks/useTranslation";
import { authService } from "@/services/authService";
import { getApiErrorMessage } from "@/services/error";
import { MfaSetupResponse, MfaStatus } from "@/services/types";

const emptyStatus: MfaStatus = {
  isEnabled: false,
  isSetupPending: false,
  enabledAtUtc: null,
};

export default function SecurityPage() {
  const { t } = useTranslation();
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
      const message = getApiErrorMessage(error, t("common.messages.error"));
      setStatusError(message);
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  }, [t]);

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
      toast.success(t("security.scanQr"));
    } catch (error) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleEnable = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    try {
      await authService.enableMfa({ code: enableCode.trim() });
      toast.success(t("common.messages.success"));
      setSetup(null);
      setEnableCode("");
      await loadStatus();
    } catch (error) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDisable = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    try {
      await authService.disableMfa({ code: disableCode.trim() });
      toast.success(t("common.messages.success"));
      setDisableCode("");
      setSetup(null);
      await loadStatus();
    } catch (error) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return <LoadingState title={t("common.actions.loading")} />;
  }

  if (statusError) {
    return (
      <ErrorState
        title={t("common.messages.error")}
        description={statusError}
        onAction={() => void loadStatus()}
      />
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="border-b border-slate-200 pb-5">
        <h1 className="text-xl font-bold tracking-tight text-slate-900">
          {t("security.title")}
        </h1>
        <p className="mt-1 text-xs text-slate-500">
          {t("security.subtitle")}
        </p>
      </div>

      {/* 2FA Status Grid */}
      <div className="grid gap-5 md:grid-cols-2">
        <Card>
          <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
            {t("security.mfaStatus")}
          </p>
          <div className="mt-3 flex items-center gap-3">
            <span
              className={`inline-flex items-center rounded-md px-2.5 py-1 text-xs font-semibold ${
                status.isEnabled
                  ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
                  : "bg-amber-50 text-amber-700 border border-amber-200"
              }`}
            >
              {status.isEnabled
                ? t("security.mfaEnabled")
                : t("security.mfaDisabled")}
            </span>
          </div>
          <p className="mt-3 text-xs leading-relaxed text-slate-600">
            {status.isEnabled
              ? status.enabledAtUtc
                ? `Kích hoạt từ: ${new Date(status.enabledAtUtc).toLocaleString("vi-VN")}`
                : "Phiên đăng nhập được bảo vệ bởi xác thực hai bước."
              : "Hiện tại tài khoản đang sử dụng mật khẩu đơn. Khuyến nghị bật 2FA để bảo vệ dữ liệu y tế."}
          </p>
        </Card>

        <Card>
          <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
            Tiêu chuẩn an ninh
          </p>
          <ul className="mt-3 space-y-2 text-xs leading-relaxed text-slate-600">
            <li className="flex items-start gap-2">
              <span className="text-slate-400 font-bold">&bull;</span>
              <span>Tương thích Google Authenticator, Microsoft Authenticator hoặc TOTP tương đương.</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-slate-400 font-bold">&bull;</span>
              <span>Chỉ kích hoạt trên thiết bị công vụ hoặc thiết bị cá nhân có cài đặt khóa sinh trắc.</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-slate-400 font-bold">&bull;</span>
              <span>Khi đổi hoặc hủy 2FA, các phiên đăng nhập khác sẽ tự động bị thu hồi.</span>
            </li>
          </ul>
        </Card>
      </div>

      {/* Setup Section if Disabled */}
      {!status.isEnabled && (
        <Card>
          <CardHeader
            title={t("security.enableMfa")}
            subtitle="Tạo khóa bảo mật và liên kết với ứng dụng xác thực của bạn."
            action={
              <Button
                variant="primary"
                size="sm"
                disabled={isSubmitting}
                onClick={handleStartSetup}
                isLoading={isSubmitting && !setup}
              >
                Khởi tạo phiên thiết lập
              </Button>
            }
          />

          {setup && (
            <div className="mt-5 space-y-4">
              <div className="rounded-lg border border-sky-100 bg-sky-50/70 p-4">
                <p className="text-xs font-semibold text-sky-900">Mã bí mật (Nhập thủ công nếu không quét được QR)</p>
                <p className="mt-1 font-mono text-xs font-semibold text-sky-950 select-all">
                  {setup.manualEntryKey}
                </p>
                <p className="mt-2 text-[11px] text-sky-700">
                  Hết hạn lúc {new Date(setup.expiresAtUtc).toLocaleTimeString("vi-VN")}.
                </p>
              </div>

              <div>
                <label className="mb-1 block text-xs font-semibold text-slate-700">
                  Liên kết cấu hình OTP Auth URI
                </label>
                <textarea
                  rows={2}
                  readOnly
                  value={setup.otpAuthUri}
                  className="w-full rounded-lg border border-slate-200 bg-slate-50 p-2.5 font-mono text-xs text-slate-800 outline-none select-all"
                />
              </div>

              <form onSubmit={handleEnable} className="flex flex-col gap-3 sm:flex-row sm:items-end">
                <div className="flex-1">
                  <label className="mb-1 block text-xs font-semibold text-slate-700">
                    {t("auth.mfaPrompt")} <span className="text-rose-500">*</span>
                  </label>
                  <input
                    type="text"
                    value={enableCode}
                    onChange={(event) => setEnableCode(event.target.value)}
                    placeholder="000000"
                    maxLength={6}
                    className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs font-mono tracking-widest text-slate-900 shadow-2xs outline-none focus:border-slate-500"
                    required
                  />
                </div>
                <Button
                  type="submit"
                  variant="primary"
                  size="sm"
                  isLoading={isSubmitting}
                >
                  {t("auth.verifyMfa")}
                </Button>
              </form>
            </div>
          )}
        </Card>
      )}

      {/* Disable Section if Enabled */}
      {status.isEnabled && (
        <Card className="border-rose-200">
          <CardHeader
            title={t("security.disableMfa")}
            subtitle="Nhập mã xác thực 6 số hiện tại để xác nhận việc gỡ bỏ xác thực hai lớp."
          />

          <form onSubmit={handleDisable} className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-end">
            <div className="flex-1">
              <label className="mb-1 block text-xs font-semibold text-slate-700">
                Mã xác thực 6 số <span className="text-rose-500">*</span>
              </label>
              <input
                type="text"
                value={disableCode}
                onChange={(event) => setDisableCode(event.target.value)}
                placeholder="000000"
                maxLength={6}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs font-mono tracking-widest text-slate-900 shadow-2xs outline-none focus:border-rose-400"
                required
              />
            </div>
            <Button
              type="submit"
              variant="danger"
              size="sm"
              isLoading={isSubmitting}
            >
              Xác nhận tắt 2FA
            </Button>
          </form>
        </Card>
      )}
    </div>
  );
}
