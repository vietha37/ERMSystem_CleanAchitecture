"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { UserRole } from "@/services/types";

export function Sidebar() {
  const { logout, role } = useAuth();
  const pathname = usePathname();

  const menuItemsByRole: Record<UserRole, Array<{ name: string; path: string }>> = {
    Admin: [
      { name: "Tổng quan", path: "/dashboard" },
      { name: "Công việc bác sĩ", path: "/doctor-worklist" },
      { name: "Nhân sự", path: "/staff" },
      { name: "Bảo mật", path: "/security" },
      { name: "Bệnh nhân", path: "/patients" },
      { name: "Lịch hẹn", path: "/appointments" },
      { name: "Hồ sơ bệnh án", path: "/medical-records" },
      { name: "Đơn thuốc", path: "/prescriptions" },
      { name: "Chỉ định cận lâm sàng", path: "/clinical-orders" },
      { name: "Hóa đơn", path: "/billing" },
      { name: "Thông báo", path: "/notifications" },
    ],
    Doctor: [
      { name: "Tổng quan", path: "/dashboard" },
      { name: "Công việc bác sĩ", path: "/doctor-worklist" },
      { name: "Bảo mật", path: "/security" },
      { name: "Bệnh nhân", path: "/patients" },
      { name: "Lịch hẹn", path: "/appointments" },
      { name: "Hồ sơ bệnh án", path: "/medical-records" },
      { name: "Đơn thuốc", path: "/prescriptions" },
      { name: "Chỉ định cận lâm sàng", path: "/clinical-orders" },
    ],
    Receptionist: [
      { name: "Tổng quan", path: "/dashboard" },
      { name: "Công việc bác sĩ", path: "/doctor-worklist" },
      { name: "Bảo mật", path: "/security" },
      { name: "Bệnh nhân", path: "/patients" },
      { name: "Lịch hẹn", path: "/appointments" },
      { name: "Chỉ định cận lâm sàng", path: "/clinical-orders" },
      { name: "Hóa đơn", path: "/billing" },
      { name: "Thông báo", path: "/notifications" },
    ],
    Patient: [{ name: "Cổng thông tin bệnh nhân", path: "/portal" }],
  };

  const menuItems = role ? menuItemsByRole[role] : [];

  return (
    <aside className="fixed left-0 top-0 z-20 flex h-screen w-64 flex-col border-r border-gray-200 bg-white shadow-sm">
      <div className="border-b border-gray-100 p-6">
        <h1 className="text-2xl font-bold text-blue-600">ERM Hospital</h1>
        <p className="mt-1 text-xs font-medium uppercase tracking-wide text-gray-400">
          {role ?? "Không xác định"}
        </p>
      </div>

      <nav className="flex-1 space-y-2 overflow-y-auto p-4">
        {menuItems.map((item) => {
          const isActive = pathname === item.path || pathname.startsWith(`${item.path}/`);
          return (
            <Link
              key={item.path}
              href={item.path}
              className={`flex items-center rounded-xl px-4 py-3 transition-colors ${
                isActive
                  ? "bg-blue-50 font-semibold text-blue-600"
                  : "text-gray-700 hover:bg-blue-50 hover:text-blue-600"
              }`}
            >
              <span className="font-medium">{item.name}</span>
            </Link>
          );
        })}
      </nav>

      <div className="border-t border-gray-100 p-4">
        <button
          onClick={logout}
          className="w-full rounded-xl px-4 py-3 text-left font-medium text-red-500 transition-colors hover:bg-red-50"
        >
          Đăng xuất
        </button>
      </div>
    </aside>
  );
}
