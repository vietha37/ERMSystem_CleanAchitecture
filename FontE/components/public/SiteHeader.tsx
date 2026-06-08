"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { authService } from "@/services/authService";
import { UserRole } from "@/services/types";

const navItems = [
  { label: "Trang chủ", href: "/" },
  { label: "Dịch vụ", href: "/services" },
  { label: "Chuyên khoa", href: "/specialties" },
  { label: "Bác sĩ", href: "/doctors" },
  { label: "Đặt lịch", href: "/booking" },
  { label: "Tin tức", href: "/news" },
];

function isActivePath(pathname: string, href: string): boolean {
  if (href === "/") {
    return pathname === "/";
  }

  return pathname === href || pathname.startsWith(`${href}/`);
}

export function SiteHeader() {
  const pathname = usePathname();
  const [role, setRole] = useState<UserRole | null>(null);
  const [username, setUsername] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState<string | null>(null);

  useEffect(() => {
    const syncSession = async () => {
      const isValid = await authService.ensureValidSession();
      setRole(isValid ? authService.getRole() : null);
      setUsername(isValid ? authService.getUsername() : null);
      setDisplayName(isValid ? authService.getDisplayName() : null);
    };

    void syncSession();

    const handleFocus = () => {
      void syncSession();
    };

    window.addEventListener("focus", handleFocus);
    return () => window.removeEventListener("focus", handleFocus);
  }, [pathname]);

  const isPatient = role === "Patient";
  const isInternalUser = role != null && role !== "Patient";

  return (
    <header className="sticky top-0 z-50 border-b border-slate-200 bg-white/95 text-slate-950 shadow-sm backdrop-blur-xl">
      <div className="border-b border-slate-800 bg-slate-950 text-white">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-3 px-4 py-3 text-sm md:px-6">
          <div className="flex flex-wrap items-center gap-3 text-slate-200">
            <span className="rounded-full border border-cyan-300/30 bg-cyan-300/10 px-3 py-1 text-cyan-100">
              Tổng đài 1900 565 656
            </span>
            <span>Hỗ trợ đặt lịch, hướng dẫn đi khám và chăm sóc sau khám.</span>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            {isPatient && (
              <span className="rounded-full border border-emerald-300/30 bg-emerald-400/10 px-3 py-1 text-xs font-semibold text-emerald-100">
                Đang đăng nhập: {displayName ?? username ?? "Bệnh nhân"}
              </span>
            )}
            {isInternalUser ? (
              <Link
                href="/dashboard"
                className="rounded-full border border-white/15 px-4 py-2 font-medium text-white transition hover:border-cyan-300 hover:text-cyan-200"
              >
                Vào dashboard nội bộ
              </Link>
            ) : (
              <Link
                href={isPatient ? "/portal" : "/login"}
                className="rounded-full border border-white/15 px-4 py-2 font-medium text-white transition hover:border-cyan-300 hover:text-cyan-200"
              >
                {isPatient ? "Vào cổng bệnh nhân" : "Đăng nhập / đăng ký"}
              </Link>
            )}
          </div>
        </div>
      </div>

      <div className="mx-auto flex max-w-7xl flex-col gap-5 px-4 py-4 md:px-6 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:gap-8">
          <Link href="/" className="flex items-center gap-4">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-cyan-600 text-base font-bold text-white shadow-sm">
              EH
            </div>
            <div>
              <p className="text-2xl font-bold leading-none tracking-tight text-slate-950">ERM Hospital</p>
              <p className="mt-1 text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">
                Khám bệnh, cận lâm sàng và hồ sơ số
              </p>
            </div>
          </Link>

          <nav className="flex flex-wrap gap-2 text-sm font-medium text-slate-600">
            {navItems.map((item) => {
              const active = isActivePath(pathname, item.href);

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  aria-current={active ? "page" : undefined}
                  className={`rounded-full px-4 py-2 transition ${
                    active
                      ? "bg-cyan-50 text-cyan-700"
                      : "hover:bg-slate-100 hover:text-slate-950"
                  }`}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </div>

        <div className="flex flex-wrap items-center gap-2 text-sm">
          <Link
            href="/booking"
            className="rounded-full border border-cyan-200 px-4 py-2 font-semibold text-cyan-700 transition hover:border-cyan-500 hover:bg-cyan-50"
          >
            Đặt lịch nhanh
          </Link>
          <Link
            href={isPatient ? "/portal" : "/login"}
            className="rounded-full bg-slate-950 px-5 py-2 font-semibold text-white transition hover:bg-cyan-700"
          >
            {isPatient ? "Mở hồ sơ bệnh nhân" : "Vào cổng bệnh nhân"}
          </Link>
        </div>
      </div>
    </header>
  );
}
