"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { useAuth } from "@/hooks/useAuth";
import { UserRole } from "@/services/types";

type MenuItem = { name: string; path: string; icon: string };

const menuItemsByRole: Record<UserRole, MenuItem[]> = {
  Admin: [
    { name: "Tổng quan", path: "/dashboard", icon: "📊" },
    { name: "Công việc bác sĩ", path: "/doctor-worklist", icon: "🩺" },
    { name: "Nhân sự", path: "/staff", icon: "👥" },
    { name: "Bảo mật", path: "/security", icon: "🔐" },
    { name: "Bệnh nhân", path: "/patients", icon: "🏥" },
    { name: "Lịch hẹn", path: "/appointments", icon: "📅" },
    { name: "Hồ sơ bệnh án", path: "/medical-records", icon: "📋" },
    { name: "Đơn thuốc", path: "/prescriptions", icon: "💊" },
    { name: "Chỉ định cận lâm sàng", path: "/clinical-orders", icon: "🔬" },
    { name: "Hóa đơn", path: "/billing", icon: "💳" },
    { name: "Thông báo", path: "/notifications", icon: "🔔" },
  ],
  Doctor: [
    { name: "Tổng quan", path: "/dashboard", icon: "📊" },
    { name: "Công việc bác sĩ", path: "/doctor-worklist", icon: "🩺" },
    { name: "Bảo mật", path: "/security", icon: "🔐" },
    { name: "Bệnh nhân", path: "/patients", icon: "🏥" },
    { name: "Lịch hẹn", path: "/appointments", icon: "📅" },
    { name: "Hồ sơ bệnh án", path: "/medical-records", icon: "📋" },
    { name: "Đơn thuốc", path: "/prescriptions", icon: "💊" },
    { name: "Chỉ định cận lâm sàng", path: "/clinical-orders", icon: "🔬" },
  ],
  Cashier: [
    { name: "Tổng quan", path: "/dashboard", icon: "📊" },
    { name: "Bảo mật", path: "/security", icon: "🔐" },
    { name: "Bệnh nhân", path: "/patients", icon: "🏥" },
    { name: "Lịch hẹn", path: "/appointments", icon: "📅" },
    { name: "Hóa đơn", path: "/billing", icon: "💳" },
    { name: "Thông báo", path: "/notifications", icon: "🔔" },
  ],
  Patient: [{ name: "Cổng thông tin bệnh nhân", path: "/portal", icon: "🏠" }],
};

function SidebarInner({
  menuItems,
  pathname,
  onLogout,
  onNavClick,
}: {
  menuItems: MenuItem[];
  pathname: string;
  onLogout: () => void;
  onNavClick?: () => void;
}) {
  return (
    <div className="flex h-full flex-col border-r border-gray-200 bg-white shadow-sm">
      <div className="border-b border-gray-100 p-6">
        <h1 className="text-2xl font-bold text-blue-600">ERM Hospital</h1>
      </div>

      <nav className="flex-1 space-y-1 overflow-y-auto p-4">
        {menuItems.map((item) => {
          const isActive = pathname === item.path || pathname.startsWith(`${item.path}/`);
          return (
            <Link
              key={item.path}
              href={item.path}
              onClick={onNavClick}
              className={`flex items-center gap-3 rounded-xl px-4 py-3 transition-colors ${
                isActive
                  ? "bg-blue-50 font-semibold text-blue-600"
                  : "text-gray-700 hover:bg-blue-50 hover:text-blue-600"
              }`}
            >
              <span className="text-base leading-none">{item.icon}</span>
              <span className="font-medium">{item.name}</span>
            </Link>
          );
        })}
      </nav>

      <div className="border-t border-gray-100 p-4">
        <button
          onClick={onLogout}
          className="w-full rounded-xl px-4 py-3 text-left font-medium text-red-500 transition-colors hover:bg-red-50"
        >
          Đăng xuất
        </button>
      </div>
    </div>
  );
}

export function Sidebar() {
  const { logout, role } = useAuth();
  const pathname = usePathname();
  const [mobileOpen, setMobileOpen] = useState(false);
  const sidebarRef = useRef<HTMLDivElement>(null);

  // Đóng khi click bên ngoài drawer
  useEffect(() => {
    if (!mobileOpen) return;

    const onPointerDown = (event: MouseEvent) => {
      if (sidebarRef.current && !sidebarRef.current.contains(event.target as Node)) {
        setMobileOpen(false);
      }
    };

    document.addEventListener("mousedown", onPointerDown);
    return () => document.removeEventListener("mousedown", onPointerDown);
  }, [mobileOpen]);

  const menuItems = role ? menuItemsByRole[role] : [];
  const closeMobile = () => setMobileOpen(false);

  return (
    <>
      {/* Desktop sidebar */}
      <aside className="fixed left-0 top-0 z-20 hidden h-screen w-64 lg:block">
        <SidebarInner menuItems={menuItems} pathname={pathname} onLogout={logout} />
      </aside>

      {/* Mobile hamburger button */}
      <button
        aria-label="Mở menu"
        onClick={() => setMobileOpen(true)}
        className="fixed left-4 top-4 z-30 flex h-10 w-10 items-center justify-center rounded-xl border border-gray-200 bg-white shadow-sm lg:hidden"
      >
        <span className="text-lg">☰</span>
      </button>

      {/* Mobile drawer overlay */}
      {mobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-slate-900/40 lg:hidden"
          aria-hidden="true"
          onClick={closeMobile}
        />
      )}

      {/* Mobile drawer */}
      <aside
        ref={sidebarRef}
        className={`fixed left-0 top-0 z-50 h-screen w-72 transition-transform duration-300 lg:hidden ${
          mobileOpen ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div className="relative h-full">
          <button
            aria-label="Đóng menu"
            onClick={closeMobile}
            className="absolute right-3 top-3 z-10 flex h-8 w-8 items-center justify-center rounded-full bg-gray-100 text-gray-500 hover:bg-gray-200"
          >
            ✕
          </button>
          <SidebarInner
            menuItems={menuItems}
            pathname={pathname}
            onLogout={logout}
            onNavClick={closeMobile}
          />
        </div>
      </aside>
    </>
  );
}
