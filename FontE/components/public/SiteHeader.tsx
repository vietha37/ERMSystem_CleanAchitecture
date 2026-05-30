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

  useEffect(() => {
    const syncSession = async () => {
      const isValid = await authService.ensureValidSession();
      setRole(isValid ? authService.getRole() : null);
      setUsername(isValid ? authService.getUsername() : null);
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
    <header className="sticky top-0 z-50 border-b border-slate-900/8 bg-[rgba(2,6,23,0.78)] text-white shadow-[0_18px_45px_rgba(15,23,42,0.16)] backdrop-blur-xl">
      <div className="border-b border-white/10">
        <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-3 px-4 py-3 text-sm md:px-6">
          <div className="flex flex-wrap items-center gap-3 text-slate-300">
            <span className="rounded-full border border-cyan-400/30 bg-cyan-400/10 px-3 py-1 text-cyan-100">
              Tổng đài ưu tiên 1900 565 656
            </span>
            <span>Phục vụ 24/7 cho đặt lịch, xét nghiệm tại nhà và hỗ trợ sau khám.</span>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            {isPatient && (
              <span className="rounded-full border border-emerald-300/30 bg-emerald-400/10 px-3 py-1 text-xs font-semibold text-emerald-100">
                Đang đăng nhập: {username ?? "Bệnh nhân"}
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
            <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-br from-cyan-400 to-emerald-400 text-lg font-bold text-slate-950 shadow-[0_18px_45px_rgba(6,182,212,0.28)]">
              EH
            </div>
            <div>
              <p className="font-serif text-3xl leading-none tracking-tight text-white">Bệnh viện tư ERM</p>
              <p className="mt-1 text-sm uppercase tracking-[0.24em] text-cyan-200">
                Chăm sóc dự phòng, chẩn đoán và y học gia đình
              </p>
            </div>
          </Link>

          <nav className="flex flex-wrap gap-2 text-sm font-medium text-slate-200">
            {navItems.map((item) => {
              const active = isActivePath(pathname, item.href);

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  aria-current={active ? "page" : undefined}
                  className={`rounded-full px-4 py-2 transition ${
                    active
                      ? "bg-white text-slate-950 shadow-sm"
                      : "hover:bg-white/8 hover:text-white"
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
            className="rounded-full border border-cyan-300/40 px-4 py-2 font-semibold text-cyan-100 transition hover:border-cyan-200 hover:bg-white/10"
          >
            Đặt lịch nhanh
          </Link>
          <Link
            href={isPatient ? "/portal" : "/login"}
            className="rounded-full bg-white px-5 py-2 font-semibold text-slate-950 transition hover:bg-cyan-100"
          >
            {isPatient ? "Mở hồ sơ bệnh nhân" : "Vào cổng bệnh nhân"}
          </Link>
        </div>
      </div>
    </header>
  );
}
