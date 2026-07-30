"use client";

import { useAuthContext } from "@/contexts/AuthContext";

/**
 * Gate component: chặn render children cho đến khi AuthContext đã xác minh session.
 * Dùng trong layout để ngăn các page component fetch data trước khi token sẵn sàng.
 */
export function AuthReadyGate({ children }: { children: React.ReactNode }) {
  const { isReady } = useAuthContext();

  if (!isReady) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="h-10 w-10 animate-spin rounded-full border-4 border-blue-200 border-t-blue-600" />
          <p className="text-sm font-medium tracking-wide text-gray-400">
            Đang tải dữ liệu...
          </p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
