"use client";

import { FormEvent, useState } from "react";
import { useAuth } from "@/hooks/useAuth";

type LoginMode = "staff" | "patient";
type PatientMode = "login" | "register";

const patientInitialState = {
  username: "",
  password: "",
  fullName: "",
  dateOfBirth: "",
  gender: "Female",
  phone: "",
  address: "",
};

export default function LoginPage() {
  const { login, registerPatient, verifyMfaLogin } = useAuth();
  const [loginMode, setLoginMode] = useState<LoginMode>("staff");
  const [patientMode, setPatientMode] = useState<PatientMode>("login");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [remember, setRemember] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [patientForm, setPatientForm] = useState(patientInitialState);
  const [mfaCode, setMfaCode] = useState("");
  const [staffMfaChallenge, setStaffMfaChallenge] = useState<{
    token: string;
    expiresAtUtc: string | null;
  } | null>(null);

  const handleStaffLogin = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    const result = await login(username, password, remember);
    if (result.success && result.requiresTwoFactor && result.challengeToken) {
      setStaffMfaChallenge({
        token: result.challengeToken,
        expiresAtUtc: result.challengeExpiresAtUtc ?? null,
      });
      setMfaCode("");
    }
    setIsSubmitting(false);
  };

  const handleStaffMfaLogin = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!staffMfaChallenge) {
      return;
    }

    setIsSubmitting(true);
    const result = await verifyMfaLogin(staffMfaChallenge.token, mfaCode, remember);
    if (result.success) {
      setStaffMfaChallenge(null);
      setMfaCode("");
    }
    setIsSubmitting(false);
  };

  const handlePatientLogin = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    await login(patientForm.username, patientForm.password, true);
    setIsSubmitting(false);
  };

  const handlePatientRegister = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    await registerPatient({
      username: patientForm.username,
      password: patientForm.password,
      fullName: patientForm.fullName,
      dateOfBirth: patientForm.dateOfBirth,
      gender: patientForm.gender,
      phone: patientForm.phone,
      address: patientForm.address,
    });
    setIsSubmitting(false);
  };

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,_rgba(34,211,238,0.2),_transparent_30%),linear-gradient(135deg,_#082f49_0%,_#0f172a_38%,_#ecfeff_38%,_#f8fafc_100%)]">
      <div className="mx-auto grid min-h-screen max-w-7xl gap-10 px-4 py-10 md:px-6 lg:grid-cols-[0.95fr_1.05fr] lg:items-center">
        <section className="rounded-[2.5rem] border border-white/10 bg-slate-950/72 p-8 text-white shadow-[0_35px_90px_rgba(2,6,23,0.42)] backdrop-blur md:p-10">
          <p className="text-sm font-semibold uppercase tracking-[0.32em] text-cyan-200">ERM Private Hospital</p>
          <h1 className="mt-5 font-serif text-5xl leading-none tracking-tight md:text-6xl">
            Cổng truy cập tách riêng cho vận hành nội bộ và bệnh nhân.
          </h1>
          <p className="mt-6 max-w-xl text-base leading-8 text-slate-300">
            Bệnh viện tư thực tế không thể chỉ có một form đăng nhập chung cho nhân sự. Hệ thống cần phân luồng rõ:
            khối nội bộ cho nhân sự, còn bệnh nhân có cổng riêng để theo dõi hồ sơ, lịch hẹn và dịch vụ chăm sóc.
          </p>

          <div className="mt-10 grid gap-4">
            {[
              "Nhân sự nội bộ tiếp tục đi vào dashboard vận hành.",
              "Bệnh nhân có thể tự tạo tài khoản và đi vào cổng riêng.",
              "JWT role-based routing tách biệt giữa nội bộ và bệnh nhân.",
            ].map((item) => (
              <div key={item} className="rounded-[1.5rem] border border-white/10 bg-white/6 px-5 py-4 text-sm leading-7 text-slate-200">
                {item}
              </div>
            ))}
          </div>
        </section>

        <section className="rounded-[2.5rem] border border-slate-200 bg-white/94 p-6 shadow-[0_30px_90px_rgba(15,23,42,0.12)] backdrop-blur md:p-8">
          <div className="rounded-full bg-slate-100 p-1">
            <div className="grid grid-cols-2 gap-1">
              <button
                type="button"
                onClick={() => setLoginMode("staff")}
                className={`rounded-full px-4 py-3 text-sm font-semibold transition ${
                  loginMode === "staff" ? "bg-slate-950 text-white" : "text-slate-600"
                }`}
              >
                Nhân sự nội bộ
              </button>
              <button
                type="button"
                onClick={() => setLoginMode("patient")}
                className={`rounded-full px-4 py-3 text-sm font-semibold transition ${
                  loginMode === "patient" ? "bg-cyan-700 text-white" : "text-slate-600"
                }`}
              >
                Bệnh nhân
              </button>
            </div>
          </div>

          {loginMode === "staff" ? (
            staffMfaChallenge ? (
              <form onSubmit={handleStaffMfaLogin} className="mt-8 space-y-5 animate-fade-in">
                <HeaderBlock
                  title="Xác thực hai bước"
                  description="Nhập mã 6 số từ ứng dụng xác thực để hoàn tất đăng nhập nội bộ."
                />

                <Field
                  label="Mã xác thực"
                  value={mfaCode}
                  onChange={setMfaCode}
                  placeholder="123456"
                />

                <div className="rounded-2xl border border-cyan-100 bg-cyan-50 px-4 py-3 text-sm text-cyan-900">
                  {staffMfaChallenge.expiresAtUtc
                    ? `Phiên xác thực này sẽ hết hạn lúc ${new Date(staffMfaChallenge.expiresAtUtc).toLocaleTimeString("vi-VN")}.`
                    : "Phiên xác thực này sẽ hết hạn sau ít phút."}
                </div>

                <div className="flex items-center justify-between gap-3">
                  <button
                    type="button"
                    onClick={() => {
                      setStaffMfaChallenge(null);
                      setMfaCode("");
                    }}
                    className="inline-flex h-12 items-center justify-center rounded-full border border-slate-200 px-6 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:bg-slate-50"
                  >
                    Quay lại
                  </button>
                  <div className="flex-1">
                    <SubmitButton submitting={isSubmitting} idleText="Xác nhận mã MFA" loadingText="Đang xác thực..." />
                  </div>
                </div>
              </form>
            ) : (
              <form onSubmit={handleStaffLogin} className="mt-8 space-y-5 animate-fade-in">
                <HeaderBlock
                  title="Đăng nhập nội bộ"
                  description="Dành cho Admin, Doctor và Receptionist vận hành hệ thống bệnh viện."
                />

                <Field label="Tên đăng nhập" value={username} onChange={setUsername} placeholder="staff.username" />
                <PasswordField
                  value={password}
                  onChange={setPassword}
                  showPassword={showPassword}
                  setShowPassword={setShowPassword}
                />

                <div className="flex items-center justify-between">
                  <label className="flex items-center gap-2 text-sm text-slate-600">
                    <input
                      type="checkbox"
                      checked={remember}
                      onChange={(event) => setRemember(event.target.checked)}
                      className="h-4 w-4 rounded border-slate-300 text-cyan-700"
                    />
                    Ghi nhớ đăng nhập
                  </label>
                  <span className="text-sm font-medium text-slate-400">Hỗ trợ bởi bộ phận IT nội bộ</span>
                </div>

                <SubmitButton submitting={isSubmitting} idleText="Đăng nhập dashboard nội bộ" loadingText="Đang xác thực..." />
              </form>
            )
          ) : (
            <div className="mt-8 animate-fade-in">
              <HeaderBlock
                title="Cổng bệnh nhân"
                description="Bệnh nhân có thể đăng nhập cổng riêng hoặc tự tạo tài khoản mới để theo dõi hồ sơ chăm sóc."
              />

              <div className="mt-6 rounded-full bg-cyan-50 p-1">
                <div className="grid grid-cols-2 gap-1">
                  <button
                    type="button"
                    onClick={() => setPatientMode("login")}
                    className={`rounded-full px-4 py-3 text-sm font-semibold transition ${
                      patientMode === "login" ? "bg-cyan-700 text-white" : "text-slate-600"
                    }`}
                  >
                    Đăng nhập
                  </button>
                  <button
                    type="button"
                    onClick={() => setPatientMode("register")}
                    className={`rounded-full px-4 py-3 text-sm font-semibold transition ${
                      patientMode === "register" ? "bg-cyan-700 text-white" : "text-slate-600"
                    }`}
                  >
                    Tạo tài khoản
                  </button>
                </div>
              </div>

              {patientMode === "login" ? (
                <form onSubmit={handlePatientLogin} className="mt-6 space-y-5">
                  <Field
                    label="Tên đăng nhập bệnh nhân"
                    value={patientForm.username}
                    onChange={(value) => setPatientForm((current) => ({ ...current, username: value }))}
                    placeholder="benhnhan.nguyenvana"
                  />
                  <PasswordField
                    value={patientForm.password}
                    onChange={(value) => setPatientForm((current) => ({ ...current, password: value }))}
                    showPassword={showPassword}
                    setShowPassword={setShowPassword}
                  />
                  <SubmitButton submitting={isSubmitting} idleText="Vào cổng bệnh nhân" loadingText="Đang đăng nhập..." />
                </form>
              ) : (
                <form onSubmit={handlePatientRegister} className="mt-6 grid gap-4 md:grid-cols-2">
                  <Field
                    label="Họ và tên"
                    value={patientForm.fullName}
                    onChange={(value) => setPatientForm((current) => ({ ...current, fullName: value }))}
                    placeholder="Nguyễn Văn A"
                  />
                  <Field
                    label="Số điện thoại"
                    value={patientForm.phone}
                    onChange={(value) => setPatientForm((current) => ({ ...current, phone: value }))}
                    placeholder="09xx xxx xxx"
                  />
                  <Field
                    label="Tên đăng nhập"
                    value={patientForm.username}
                    onChange={(value) => setPatientForm((current) => ({ ...current, username: value }))}
                    placeholder="nguyenvana"
                  />
                  <PasswordField
                    value={patientForm.password}
                    onChange={(value) => setPatientForm((current) => ({ ...current, password: value }))}
                    showPassword={showPassword}
                    setShowPassword={setShowPassword}
                  />
                  <Field
                    label="Ngày sinh"
                    type="date"
                    value={patientForm.dateOfBirth}
                    onChange={(value) => setPatientForm((current) => ({ ...current, dateOfBirth: value }))}
                  />
                  <label className="grid gap-2 text-sm font-medium text-slate-700">
                    Giới tính
                    <select
                      value={patientForm.gender}
                      onChange={(event) => setPatientForm((current) => ({ ...current, gender: event.target.value }))}
                      className="h-12 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white"
                    >
                      <option value="Female">Nữ</option>
                      <option value="Male">Nam</option>
                      <option value="Other">Khác</option>
                    </select>
                  </label>
                  <label className="md:col-span-2 grid gap-2 text-sm font-medium text-slate-700">
                    Địa chỉ
                    <textarea
                      rows={3}
                      value={patientForm.address}
                      onChange={(event) => setPatientForm((current) => ({ ...current, address: event.target.value }))}
                      placeholder="Số nhà, phường/xã, quận/huyện, tỉnh/thành"
                      className="rounded-3xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white"
                    />
                  </label>
                  <div className="md:col-span-2">
                    <SubmitButton submitting={isSubmitting} idleText="Tạo tài khoản bệnh nhân" loadingText="Đang tạo tài khoản..." />
                  </div>
                </form>
              )}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

function HeaderBlock({ title, description }: { title: string; description: string }) {
  return (
    <div>
      <h1 className="text-3xl font-semibold tracking-tight text-slate-950">{title}</h1>
      <p className="mt-2 text-sm leading-7 text-slate-500">{description}</p>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  placeholder,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  type?: string;
}) {
  return (
    <label className="grid gap-2 text-sm font-medium text-slate-700">
      {label}
      <input
        type={type}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        className="h-12 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white"
        required
      />
    </label>
  );
}

function PasswordField({
  value,
  onChange,
  showPassword,
  setShowPassword,
}: {
  value: string;
  onChange: (value: string) => void;
  showPassword: boolean;
  setShowPassword: (value: boolean | ((value: boolean) => boolean)) => void;
}) {
  return (
    <label className="grid gap-2 text-sm font-medium text-slate-700">
      Mật khẩu
      <div className="relative">
        <input
          type={showPassword ? "text" : "password"}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder="••••••••"
          className="h-12 w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 pr-14 text-sm text-slate-900 outline-none transition focus:border-cyan-500 focus:bg-white"
          required
        />
        <button
          type="button"
          onClick={() => setShowPassword((current) => !current)}
          className="absolute inset-y-0 right-0 px-4 text-xs font-semibold text-slate-500"
        >
          {showPassword ? "Ẩn" : "Hiện"}
        </button>
      </div>
    </label>
  );
}

function SubmitButton({
  submitting,
  idleText,
  loadingText,
}: {
  submitting: boolean;
  idleText: string;
  loadingText: string;
}) {
  return (
    <button
      type="submit"
      disabled={submitting}
      className="inline-flex h-12 w-full items-center justify-center rounded-full bg-slate-950 px-6 text-sm font-semibold text-white transition hover:bg-cyan-700 disabled:cursor-not-allowed disabled:opacity-70"
    >
      {submitting ? loadingText : idleText}
    </button>
  );
}
