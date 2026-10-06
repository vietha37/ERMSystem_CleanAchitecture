"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { LanguageToggle } from "@/components/layout/LanguageToggle";
import { useTranslation } from "@/hooks/useTranslation";
import { authService } from "@/services/authService";
import { UserRole } from "@/services/types";

function isActivePath(pathname: string, href: string): boolean {
  if (href === "/") {
    return pathname === "/";
  }
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function SiteHeader() {
  const pathname = usePathname();
  const { t } = useTranslation();
  const [mounted, setMounted] = useState(false);
  const [role, setRole] = useState<UserRole | null>(null);
  const [username, setUsername] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState<string | null>(null);

  const navItems = [
    { label: t("nav.home"), href: "/" },
    { label: t("nav.services"), href: "/services" },
    { label: t("nav.doctors"), href: "/doctors" },
  ];

  useEffect(() => {
    let isAlive = true;

    const syncSession = async () => {
      const isValid = await authService.ensureValidSession();
      if (!isAlive) return;

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
    ? t("nav.dashboard")
    : isPatient
      ? t("nav.portal")
      : t("public.portalButton");

  return (
    <header className="sticky top-0 z-50 border-b border-slate-200 bg-white/95 text-slate-900 shadow-2xs backdrop-blur-md">
      {/* Top Notification Bar */}
      <div className="bg-slate-900 text-slate-200">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-1.5 text-xs md:px-6">
          <div className="flex min-w-0 items-center gap-2">
            <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-emerald-400" />
            <span className="truncate">{t("public.emergencyPhone")}</span>
          </div>

          <div className="flex items-center gap-4">
            <span className="hidden md:inline text-slate-400">{t("public.address")}</span>
            {isPatient && (
              <span className="rounded bg-slate-800 px-2 py-0.5 text-[11px] font-medium text-emerald-300">
                {displayName ?? username ?? "Bệnh nhân"}
              </span>
            )}
            <LanguageToggle className="border-slate-700 bg-slate-800 text-slate-300" />
          </div>
        </div>
      </div>

      {/* Main Navigation Bar */}
      <div className="mx-auto max-w-7xl px-4 md:px-6">
        <div className="flex min-h-16 items-center justify-between gap-4 py-2">
          {/* Brand */}
          <Link href="/" className="flex min-w-0 items-center gap-3">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-slate-900 text-sm font-bold text-white shadow-2xs">
              +
            </div>
            <div className="min-w-0">
              <p className="truncate text-lg font-bold leading-none tracking-tight text-slate-900">
                ERM Hospital
              </p>
              <p className="mt-0.5 hidden text-[10px] font-semibold uppercase tracking-wider text-slate-500 sm:block">
                {t("public.tagline")}
              </p>
            </div>
          </Link>

          {/* Desktop Navigation Links */}
          <nav className="hidden items-center gap-1 text-sm font-medium text-slate-600 md:flex">
            {navItems.map((item) => {
              const active = isActivePath(activePathname, item.href);

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  aria-current={active ? "page" : undefined}
                  className={`rounded-lg px-3 py-1.5 transition-colors ${
                    active
                      ? "bg-slate-100 font-semibold text-slate-900"
                      : "hover:bg-slate-50 hover:text-slate-900"
                  }`}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>

          {/* Action Buttons */}
          <div className="flex items-center gap-2">
            <Link
              href="/booking"
              className="inline-flex h-9 items-center justify-center rounded-lg bg-slate-900 px-4 text-xs font-semibold text-white shadow-2xs transition-colors hover:bg-slate-800"
            >
              {t("public.bookAppointment")}
            </Link>
            <Link
              href={portalHref}
              className="inline-flex h-9 items-center justify-center rounded-lg border border-slate-300 bg-white px-3.5 text-xs font-semibold text-slate-700 shadow-2xs transition-colors hover:bg-slate-50 hover:text-slate-900"
            >
              {portalLabel}
            </Link>
          </div>
        </div>
      </div>
    </header>
  );
}
