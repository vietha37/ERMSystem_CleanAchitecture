"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuthContext } from "@/contexts/AuthContext";
import { UserRole } from "@/services/types";
import { authService } from "@/services/authService";

const allowedRoutesByRole: Record<UserRole, string[]> = {
  Admin: [
    "/dashboard",
    "/doctor-worklist",
    "/staff",
    "/security",
    "/patients",
    "/appointments",
    "/medical-records",
    "/prescriptions",
    "/clinical-orders",
    "/billing",
    "/notifications",
  ],
  Doctor: [
    "/dashboard",
    "/doctor-worklist",
    "/security",
    "/patients",
    "/appointments",
    "/medical-records",
    "/prescriptions",
    "/clinical-orders",
  ],
  Cashier: ["/dashboard", "/security", "/patients", "/appointments", "/billing", "/notifications"],
  Patient: ["/portal"],
};

export default function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { isReady, isAuthenticated, role } = useAuthContext();

  useEffect(() => {
    if (!isReady) return; // Chờ AuthContext verify xong

    if (!isAuthenticated || !role) {
      void authService.logout().catch(() => {});
      router.push("/login");
      return;
    }

    const allowedRoutes = allowedRoutesByRole[role] ?? [];
    if (allowedRoutes.length === 0) {
      void authService.logout().catch(() => {});
      router.push("/login");
      return;
    }

    const isAllowed = allowedRoutes.some(
      (route) => pathname === route || pathname.startsWith(`${route}/`)
    );

    if (!isAllowed) {
      router.push(allowedRoutes[0] ?? "/login");
    }
  }, [isReady, isAuthenticated, role, pathname, router]);

  // Hiển thị loading spinner cho đến khi auth đã được verify
  if (!isReady) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-blue-50/50">
        <div className="flex flex-col items-center">
          <div className="h-12 w-12 animate-spin rounded-full border-4 border-blue-200 border-t-blue-600" />
          <p className="mt-4 font-medium tracking-wide text-gray-500">
            Đang xác thực phiên đăng nhập...
          </p>
        </div>
      </div>
    );
  }

  // Nếu đã ready nhưng không authenticated → đang redirect, giữ spinner
  if (!isAuthenticated || !role) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-blue-50/50">
        <div className="h-12 w-12 animate-spin rounded-full border-4 border-blue-200 border-t-blue-600" />
      </div>
    );
  }

  return <>{children}</>;
}
