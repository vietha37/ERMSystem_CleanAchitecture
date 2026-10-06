"use client";

import { usePathname } from "next/navigation";
import React, { useEffect, useMemo, useRef, useState } from "react";
import { useAuth } from "@/hooks/useAuth";
import { useTranslation } from "@/hooks/useTranslation";
import { formatTimeValue } from "@/lib/dateFormatting";
import { normalizeVietnameseText } from "@/lib/textEncoding";
import { notificationService } from "@/services/notificationService";
import { AppointmentNotification } from "@/services/types";
import { LanguageToggle } from "./LanguageToggle";
import { resolvePageTitle } from "./navigation";

function formatTime(value: string): string {
  return formatTimeValue(value);
}

function computeUnreadCount(
  items: AppointmentNotification[],
  fallbackUnread: number,
  viewedAt: number
): number {
  if (viewedAt <= 0) return fallbackUnread;

  return items.filter((item) => {
    const t = new Date(item.appointmentDate).getTime();
    return Number.isFinite(t) && t > viewedAt;
  }).length;
}

export function Header() {
  const { logout, role, username, displayName, isAuthenticated } = useAuth();
  const { t } = useTranslation();
  const pathname = usePathname();

  const [notifications, setNotifications] = useState<AppointmentNotification[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoadingNotifications, setIsLoadingNotifications] = useState(false);
  const [lastViewedAt, setLastViewedAt] = useState<number>(0);
  const notificationRef = useRef<HTMLDivElement>(null);

  const storageKey = useMemo(
    () => `emr_notifications_seen_at_${username ?? "anonymous"}`,
    [username]
  );

  const pageMeta = useMemo(() => resolvePageTitle(pathname), [pathname]);

  const accountName = useMemo(() => {
    const raw = displayName ? normalizeVietnameseText(displayName) : null;
    if (raw) return raw;
    if (role) return t(`auth.roles.${role}`) || role;
    return "User";
  }, [displayName, role, t]);

  const accountInitial = accountName.charAt(0).toUpperCase() || "U";

  useEffect(() => {
    if (!isAuthenticated) {
      setLastViewedAt(0);
      return;
    }

    const raw =
      typeof window !== "undefined" ? window.localStorage.getItem(storageKey) : null;
    const parsed = raw ? Number(raw) : 0;
    setLastViewedAt(Number.isFinite(parsed) ? parsed : 0);
  }, [isAuthenticated, storageKey]);

  useEffect(() => {
    if (!isAuthenticated) {
      setNotifications([]);
      setUnreadCount(0);
      return;
    }

    let isCancelled = false;

    const fetchNotifications = async () => {
      setIsLoadingNotifications(true);
      try {
        const data = await notificationService.getToday();
        if (!isCancelled) {
          const nextNotifications = data.notifications ?? [];
          setNotifications(nextNotifications);
          setUnreadCount(
            computeUnreadCount(nextNotifications, data.unreadCount ?? 0, lastViewedAt)
          );
        }
      } catch {
        if (!isCancelled) {
          setNotifications([]);
          setUnreadCount(0);
        }
      } finally {
        if (!isCancelled) {
          setIsLoadingNotifications(false);
        }
      }
    };

    void fetchNotifications();
    const timer = setInterval(() => {
      void fetchNotifications();
    }, 30000);

    return () => {
      isCancelled = true;
      clearInterval(timer);
    };
  }, [isAuthenticated, role, lastViewedAt]);

  useEffect(() => {
    if (!isOpen) return;

    const onPointerDown = (event: MouseEvent) => {
      const target = event.target as Node;
      if (notificationRef.current && !notificationRef.current.contains(target)) {
        setIsOpen(false);
      }
    };

    const onEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onEscape);

    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onEscape);
    };
  }, [isOpen]);

  const handleToggleNotifications = () => {
    setIsOpen((current) => {
      const next = !current;
      if (next) {
        const now = Date.now();
        setLastViewedAt(now);
        setUnreadCount(0);
        if (typeof window !== "undefined") {
          window.localStorage.setItem(storageKey, now.toString());
        }
      }
      return next;
    });
  };

  return (
    <header className="relative z-20 flex h-18 items-center justify-between border-b border-slate-200 bg-white px-6 transition-all duration-200">
      {/* Current Page Title / Hierarchy */}
      <div className="min-w-0">
        <h2 className="truncate text-base font-semibold tracking-tight text-slate-900">
          {pageMeta.title}
        </h2>
        <p className="hidden truncate text-xs text-slate-500 sm:block">
          {pageMeta.subtitle}
        </p>
      </div>

      {/* Header Actions */}
      <div className="flex items-center gap-3">
        {/* Language Switcher */}
        <LanguageToggle />

        {/* Notifications Popover */}
        <div ref={notificationRef} className="relative">
          <button
            type="button"
            onClick={handleToggleNotifications}
            className="relative flex h-9 w-9 items-center justify-center rounded-lg border border-slate-200 bg-white text-slate-600 transition-colors hover:bg-slate-50 hover:text-slate-900 focus-visible:ring-2 focus-visible:ring-slate-400"
            aria-label="Thông báo"
          >
            <svg
              className="h-4.5 w-4.5"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={1.75}
              aria-hidden="true"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0"
              />
            </svg>
            {unreadCount > 0 && (
              <span
                className="absolute -right-1 -top-1 flex h-4.5 min-w-4.5 items-center justify-center rounded-full bg-rose-600 px-1 text-[10px] font-bold text-white shadow-xs"
                suppressHydrationWarning
              >
                {unreadCount > 99 ? "99+" : unreadCount}
              </span>
            )}
          </button>

          {isOpen && (
            <div className="absolute right-0 z-40 mt-2 max-h-[420px] w-80 sm:w-96 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xl">
              <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50/80 px-4 py-3">
                <p className="text-xs font-bold uppercase tracking-wider text-slate-700">
                  {t("nav.notifications")}
                </p>
                <span className="text-xs text-slate-500" suppressHydrationWarning>
                  {unreadCount > 0 ? `${unreadCount} mới` : ""}
                </span>
              </div>

              <div className="max-h-[340px] overflow-y-auto">
                {isLoadingNotifications ? (
                  <div className="p-6 text-center text-xs text-slate-500">
                    {t("common.actions.loading")}
                  </div>
                ) : notifications.length === 0 ? (
                  <div className="p-6 text-center text-xs text-slate-500">
                    {t("common.labels.empty")}
                  </div>
                ) : (
                  <ul className="divide-y divide-slate-100">
                    {notifications.map((item) => (
                      <li
                        key={item.appointmentId}
                        className="px-4 py-3 transition-colors hover:bg-slate-50"
                      >
                        <div className="flex items-center justify-between gap-2">
                          <p className="truncate text-xs font-semibold text-slate-900">
                            {item.patientName}
                          </p>
                          <span className="text-[11px] font-semibold text-sky-700">
                            {formatTime(item.appointmentDate)}
                          </span>
                        </div>
                        <p className="mt-0.5 text-xs text-slate-600">
                          {item.doctorName}
                        </p>
                        <p className="mt-0.5 truncate text-[11px] text-slate-500">
                          {item.message}
                        </p>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          )}
        </div>

        {/* User Profile Badge */}
        <div className="flex items-center gap-2.5 border-l border-slate-200 pl-3">
          <div
            className="flex h-8 w-8 items-center justify-center rounded-lg bg-slate-900 text-xs font-bold text-white shadow-2xs"
            suppressHydrationWarning
          >
            {accountInitial}
          </div>
          <div className="hidden text-left md:block">
            <p className="truncate text-xs font-semibold leading-tight text-slate-900" suppressHydrationWarning>
              {accountName}
            </p>
            <p className="text-[10px] font-medium text-emerald-600">
              {t("common.status.active")}
            </p>
          </div>
        </div>

        {/* Logout Quick Button */}
        <button
          type="button"
          onClick={logout}
          aria-label={t("common.actions.logout")}
          className="flex h-9 w-9 items-center justify-center rounded-lg border border-slate-200 text-slate-500 transition-colors hover:border-rose-200 hover:bg-rose-50 hover:text-rose-600 focus-visible:ring-2 focus-visible:ring-rose-400"
          title={t("common.actions.logout")}
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.75}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
          </svg>
        </button>
      </div>
    </header>
  );
}
