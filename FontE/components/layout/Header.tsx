"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
import { useAuth } from "@/hooks/useAuth";
import { formatTimeValue } from "@/lib/dateFormatting";
import { notificationService } from "@/services/notificationService";
import { AppointmentNotification } from "@/services/types";

function formatTime(value: string): string {
  return formatTimeValue(value);
}

function computeUnreadCount(
  items: AppointmentNotification[],
  fallbackUnread: number,
  viewedAt: number
): number {
  if (viewedAt <= 0) return fallbackUnread;

  const unseenByTime = items.filter((item) => {
    const t = new Date(item.appointmentDate).getTime();
    return Number.isFinite(t) && t > viewedAt;
  }).length;

  return unseenByTime;
}

export function Header() {
  const { logout, role, username, displayName, isAuthenticated } = useAuth();
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

  const title = useMemo(() => {
    if (role === "Admin") return "Tất cả lịch khám hôm nay";
    if (role === "Doctor") return "Lịch khám của bạn hôm nay";
    return "Thông báo hôm nay";
  }, [role]);

  return (
    <header className="relative z-20 flex h-20 items-center justify-between border-b border-gray-100 bg-white/90 px-8 shadow-sm backdrop-blur-md transition-all duration-300">
      <div>
        <h2 className="text-xl font-bold tracking-tight text-gray-800">
          Trung tâm điều hành
        </h2>
        <p className="text-sm font-medium text-gray-500">
          {displayName ?? role ?? "Người dùng"}
          {username ? ` - ${username}` : ""}
        </p>
      </div>

      <div className="flex items-center gap-5">
        <button
          onClick={logout}
          className="rounded-xl bg-red-50 px-4 py-2 text-sm font-bold text-red-500 shadow-sm transition-colors hover:bg-red-100 hover:text-red-600"
        >
          Đăng xuất
        </button>

        <div ref={notificationRef} className="relative z-30">
          <button
            onClick={handleToggleNotifications}
            className="relative rounded-full bg-gray-50 p-2.5 text-gray-500 shadow-sm transition-colors hover:bg-gray-100"
            aria-label="Thông báo"
          >
            <span className="text-xl">🔔</span>
            {unreadCount > 0 && (
              <span className="absolute -right-1 -top-1 flex h-5 min-w-5 items-center justify-center rounded-full border-2 border-white bg-blue-600 px-1 text-[10px] font-bold text-white">
                {unreadCount > 99 ? "99+" : unreadCount}
              </span>
            )}
          </button>

          {isOpen && (
            <div className="absolute right-0 z-40 mt-2 max-h-[420px] w-[380px] overflow-auto rounded-2xl border border-gray-200 bg-white shadow-xl">
              <div className="sticky top-0 border-b border-gray-100 bg-white px-4 py-3">
                <p className="text-sm font-bold text-gray-800">{title}</p>
                <p className="text-xs text-gray-500">{unreadCount} thông báo mới</p>
              </div>

              {isLoadingNotifications ? (
                <div className="p-4 text-sm text-gray-500">Đang tải thông báo...</div>
              ) : notifications.length === 0 ? (
                <div className="p-4 text-sm text-gray-500">
                  Không có lịch khám nào hôm nay.
                </div>
              ) : (
                <ul className="divide-y divide-gray-100">
                  {notifications.map((item) => (
                    <li
                      key={item.appointmentId}
                      className="px-4 py-3 transition-colors hover:bg-blue-50/60"
                    >
                      <div className="flex items-center justify-between gap-2">
                        <p className="truncate text-sm font-semibold text-gray-800">
                          {item.patientName}
                        </p>
                        <span className="text-xs font-bold text-blue-700">
                          {formatTime(item.appointmentDate)}
                        </span>
                      </div>
                      <p className="mt-1 text-xs text-gray-600">
                        Bác sĩ: {item.doctorName}
                      </p>
                      <p className="mt-1 truncate text-xs text-gray-500">{item.message}</p>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>

        <div className="flex cursor-pointer items-center gap-3 transition-opacity hover:opacity-80">
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-blue-500 to-blue-600 font-bold text-white shadow-md ring-2 ring-blue-50">
            {(displayName ?? role ?? "U").charAt(0)}
          </div>
          <div className="hidden text-left md:block">
            <p className="text-sm font-bold leading-tight text-gray-800">
              {displayName ?? role ?? "Người dùng"}
            </p>
            <p className="text-[11px] font-bold uppercase tracking-wider text-blue-600">
              Đang hoạt động
            </p>
          </div>
        </div>
      </div>
    </header>
  );
}
