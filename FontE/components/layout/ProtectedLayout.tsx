"use client";

import { useEffect, useState } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { authService } from '@/services/authService';
import { UserRole } from '@/services/types';

export default function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [authorized, setAuthorized] = useState(false);

  useEffect(() => {
    const checkAuth = async () => {
      const isAuth = await authService.ensureValidSession();
      const role = authService.getRole();

      if (!isAuth || !role) {
        await authService.logout();
        router.push('/login');
        return;
      }

      const allowedRoutesByRole: Record<UserRole, string[]> = {
        Admin: ['/dashboard', '/doctor-worklist', '/staff', '/security', '/patients', '/appointments', '/medical-records', '/prescriptions', '/clinical-orders', '/billing', '/notifications'],
        Doctor: ['/dashboard', '/doctor-worklist', '/security', '/patients', '/appointments', '/medical-records', '/prescriptions', '/clinical-orders'],
        Cashier: ['/dashboard', '/security', '/patients', '/appointments', '/billing', '/notifications'],
        Patient: ['/portal'],
      };

      const allowedRoutes = allowedRoutesByRole[role] ?? [];
      if (allowedRoutes.length === 0) {
        await authService.logout();
        router.push('/login');
        return;
      }

      const isAllowed = allowedRoutes.some(
        (route) => pathname === route || pathname.startsWith(`${route}/`)
      );

      if (!isAllowed) {
        router.push(allowedRoutes[0] ?? '/login');
        return;
      }

      setAuthorized(true);
    };

    void checkAuth();
  }, [pathname, router]);

  if (!authorized) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-blue-50/50">
        <div className="flex flex-col items-center">
          <div className="w-12 h-12 border-4 border-blue-200 border-t-blue-600 rounded-full animate-spin"></div>
          <p className="mt-4 text-gray-500 font-medium tracking-wide">Đang xác thực phiên đăng nhập...</p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
