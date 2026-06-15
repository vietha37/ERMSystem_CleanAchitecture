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
  const [mounted, setMounted] = useState(false);
  const [role, setRole] = useState<UserRole | null>(null);
  const [username, setUsername] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState<string | null>(null);

  useEffect(() => {
    let isAlive = true;

    const syncSession = async () => {
      const isValid = await authService.ensureValidSession();
      if (!isAlive) {
        return;
      }

      setRole(isValid ? authService.getRole() : null);
      setUsername(isValid ? authService.getUsername() : null);
      setDisplayName(isValid ? authService.getDisplayName() : null);
      setMounted(true);
    };

    void syncSession();

    const handleFocus = () => {
      void syncSession();
    };

    window.addEventListener("focus", handleFocus);
    return () => {
      isAlive = false;
      window.removeEventListener("focus", handleFocus);
    };
  }, [pathname]);

  const activePathname = mounted ? pathname : "";
  const isPatient = mounted && role === "Patient";
  const isInternalUser = mounted && role != null && role !== "Patient";
  const portalHref = isInternalUser ? "/dashboard" : isPatient ? "/portal" : "/login";
  const portalLabel = isInternalUser
    ? "Dashboard nội bộ"
    : isPatient
      ? "Hồ sơ bệnh nhân"
      : "Cổng bệnh nhân";

  return (
    <header className="sticky top-0 z-50 border-b border-slate-200 bg-white/95 text-slate-950 shadow-sm backdrop-blur-xl">
      <div className="bg-slate-950 text-white">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-2 text-xs md:px-6">
          <div className="flex min-w-0 items-center gap-3 text-slate-200">
            <span className="hidden h-2 w-2 shrink-0 rounded-full bg-cyan-300 md:block" />
            <span className="truncate">Tổng đài 1900 565 656 - Hỗ trợ đặt lịch và hướng dẫn đi khám</span>
          </div>

          <div className="hidden items-center gap-4 text-slate-300 md:flex">
            <span>42 Nguyễn Văn Huyên, Cầu Giấy</span>
            {isPatient && (
              <span className="rounded-full bg-emerald-400/10 px-3 py-1 font-semibold text-emerald-100">
                {displayName ?? username ?? "Bệnh nhân"}
              </span>
            )}
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-7xl px-4 md:px-6">
        <div className="flex min-h-20 items-center justify-between gap-4 py-3">
          <Link href="/" className="flex min-w-0 items-center gap-3">
            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-cyan-600 text-sm font-bold text-white shadow-sm">
              EH
            </div>
            <div className="min-w-0">
              <p className="truncate text-xl font-bold leading-none tracking-tight text-slate-950 md:text-2xl">
                ERM Hospital
              </p>
              <p className="mt-1 hidden text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-500 sm:block">
                Bệnh viện đa chuyên khoa
              </p>
            </div>
          </Link>

          <nav className="hidden items-center justify-center gap-1 rounded-full border border-slate-200 bg-slate-50 p-1 text-sm font-semibold text-slate-600 lg:flex">
            {navItems.map((item) => {
              const active = isActivePath(activePathname, item.href);

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  aria-current={active ? "page" : undefined}
                  className={`rounded-full px-4 py-2 transition ${
                    active
                      ? "bg-white text-cyan-700 shadow-sm"
                      : "hover:bg-white hover:text-slate-950"
                  }`}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>

          <div className="hidden shrink-0 items-center gap-2 lg:flex">
            <Link
              href="/booking"
              className="inline-flex h-10 items-center justify-center rounded-full bg-cyan-600 px-5 text-sm font-semibold text-white transition hover:bg-cyan-700"
            >
              Đặt lịch
            </Link>
            <Link
              href={portalHref}
              className="inline-flex h-10 items-center justify-center rounded-full border border-slate-300 px-5 text-sm font-semibold text-slate-800 transition hover:border-cyan-300 hover:text-cyan-700"
            >
              {portalLabel}
            </Link>
          </div>

          <div className="flex shrink-0 items-center gap-2 lg:hidden">
            <Link
              href="/booking"
              className="inline-flex h-10 items-center justify-center rounded-full bg-cyan-600 px-4 text-sm font-semibold text-white"
            >
              Đặt lịch
            </Link>
            <Link
              href={portalHref}
              className="inline-flex h-10 items-center justify-center rounded-full border border-slate-300 px-4 text-sm font-semibold text-slate-800"
            >
              Hồ sơ
            </Link>
          </div>
        </div>

        <nav className="-mx-4 flex gap-2 overflow-x-auto border-t border-slate-100 px-4 py-3 text-sm font-semibold text-slate-600 lg:hidden">
          {navItems.map((item) => {
            const active = isActivePath(activePathname, item.href);

            return (
              <Link
                key={item.href}
                href={item.href}
                aria-current={active ? "page" : undefined}
                className={`shrink-0 rounded-full px-4 py-2 transition ${
                  active ? "bg-cyan-50 text-cyan-700" : "bg-slate-50 hover:bg-slate-100"
                }`}
              >
                {item.label}
              </Link>
            );
          })}
        </nav>
      </div>
    </header>
  );
}
