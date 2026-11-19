"use client";

import { Card } from "@/components/ui/Card";
import { NotificationDeliveryStatus } from "../types";

interface NotificationMetricsProps {
  metrics: Record<NotificationDeliveryStatus, number>;
}

export function NotificationMetrics({ metrics }: NotificationMetricsProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
      <Card className="border border-slate-200 bg-white p-5 shadow-xs">
        <p className="text-xs font-bold uppercase tracking-[0.2em] text-amber-700">
          Đang chờ gửi
        </p>
        <p className="mt-2 text-3xl font-bold text-slate-900">
          {metrics.Queued}
        </p>
        <p className="mt-1 text-xs text-slate-500">
          Thông báo trong hàng đợi xử lý.
        </p>
      </Card>

      <Card className="border border-slate-200 bg-white p-5 shadow-xs">
        <p className="text-xs font-bold uppercase tracking-[0.2em] text-emerald-700">
          Đã gửi
        </p>
        <p className="mt-2 text-3xl font-bold text-slate-900">
          {metrics.Delivered}
        </p>
        <p className="mt-1 text-xs text-slate-500">
          Đã gửi tới người nhận thành công.
        </p>
      </Card>

      <Card className="border border-slate-200 bg-white p-5 shadow-xs">
        <p className="text-xs font-bold uppercase tracking-[0.2em] text-rose-700">
          Thất bại
        </p>
        <p className="mt-2 text-3xl font-bold text-slate-900">
          {metrics.Failed}
        </p>
        <p className="mt-1 text-xs text-slate-500">
          Gửi lỗi, có thể gửi lại thủ công.
        </p>
      </Card>

      <Card className="border border-slate-200 bg-white p-5 shadow-xs">
        <p className="text-xs font-bold uppercase tracking-[0.2em] text-slate-600">
          Bỏ qua
        </p>
        <p className="mt-2 text-3xl font-bold text-slate-900">
          {metrics.Skipped}
        </p>
        <p className="mt-1 text-xs text-slate-500">
          Bỏ qua do thông tin người nhận trống.
        </p>
      </Card>
    </div>
  );
}
