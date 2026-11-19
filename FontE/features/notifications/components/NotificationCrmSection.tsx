"use client";

import { Card } from "@/components/ui/Card";
import { formatDateTimeValue } from "@/lib/dateFormatting";
import { HospitalCrmEngagementSummary } from "../types";

interface NotificationCrmSectionProps {
  engagementSummary: HospitalCrmEngagementSummary | null;
}

export function NotificationCrmSection({ engagementSummary }: NotificationCrmSectionProps) {
  return (
    <Card className="overflow-hidden border border-slate-200 bg-white p-0 shadow-xs">
      <div className="border-b border-slate-200 bg-slate-50/60 px-6 py-4">
        <div className="flex flex-col gap-2 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.2em] text-cyan-700">
              Chăm sóc bệnh nhân
            </p>
            <h2 className="mt-1 text-lg font-bold text-slate-900">
              Theo dõi sau khám
            </h2>
            <p className="text-xs text-slate-500">
              Nhắc tái khám, khảo sát hài lòng và hoạt động chăm sóc khách hàng.
            </p>
          </div>

          <p className="text-xs text-slate-500">
            Cập nhật: {formatDateTimeValue(engagementSummary?.generatedAtUtc)}
          </p>
        </div>
      </div>

      <div className="grid gap-4 px-6 py-5 md:grid-cols-2 xl:grid-cols-4">
        <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-4">
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-cyan-700">
            Tổng chiến dịch
          </p>
          <p className="mt-2 text-2xl font-bold text-slate-950">
            {engagementSummary?.totalCampaignMessages ?? 0}
          </p>
          <p className="mt-1 text-xs text-slate-500">Tổng số đã tạo.</p>
        </div>

        <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-4">
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-emerald-700">
            Đã gửi thành công
          </p>
          <p className="mt-2 text-2xl font-bold text-slate-950">
            {engagementSummary?.deliveredDeliveries ?? 0}
          </p>
          <p className="mt-1 text-xs text-slate-500">Đã gửi thành công.</p>
        </div>

        <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-4">
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-amber-700">
            Đang chờ / cần xử lý
          </p>
          <p className="mt-2 text-2xl font-bold text-slate-950">
            {(engagementSummary?.queuedDeliveries ?? 0) +
              (engagementSummary?.actionRequiredDeliveries ?? 0)}
          </p>
          <p className="mt-1 text-xs text-slate-500">Đang chờ hoặc cần xử lý.</p>
        </div>

        <div className="rounded-xl border border-slate-200 bg-slate-50/40 p-4">
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-violet-700">
            Tổng người nhận
          </p>
          <p className="mt-2 text-2xl font-bold text-slate-950">
            {engagementSummary?.totalRecipients ?? 0}
          </p>
          <p className="mt-1 text-xs text-slate-500">Tổng Email/SMS đã tạo.</p>
        </div>
      </div>

      <div className="grid gap-4 border-t border-slate-200 px-6 py-5 lg:grid-cols-[1.1fr,1.4fr]">
        <div className="space-y-4">
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="rounded-xl border border-sky-100 bg-sky-50/50 p-3">
              <p className="text-xs font-bold uppercase tracking-[0.14em] text-sky-700">
                Nhắc tái khám
              </p>
              <p className="mt-1.5 text-xl font-bold text-slate-900">
                {engagementSummary?.revisitReminderMessages ?? 0}
              </p>
            </div>
            <div className="rounded-xl border border-teal-100 bg-teal-50/50 p-3">
              <p className="text-xs font-bold uppercase tracking-[0.14em] text-teal-700">
                Khảo sát
              </p>
              <p className="mt-1.5 text-xl font-bold text-slate-900">
                {engagementSummary?.satisfactionSurveyMessages ?? 0}
              </p>
            </div>
            <div className="rounded-xl border border-indigo-100 bg-indigo-50/50 p-3">
              <p className="text-xs font-bold uppercase tracking-[0.14em] text-indigo-700">
                CSKH follow-up
              </p>
              <p className="mt-1.5 text-xl font-bold text-slate-900">
                {engagementSummary?.customerCareFollowUpMessages ?? 0}
              </p>
            </div>
          </div>

          <div className="rounded-xl border border-slate-200 bg-white p-4">
            <h3 className="text-xs font-bold uppercase tracking-[0.15em] text-slate-700">
              Xu hướng 14 ngày gần nhất
            </h3>
            <div className="mt-3 space-y-2.5">
              {engagementSummary?.trendPoints?.slice(-7).map((point) => {
                const total =
                  point.revisitReminderCount +
                  point.satisfactionSurveyCount +
                  point.customerCareFollowUpCount;

                return (
                  <div key={point.date} className="space-y-1.5">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-medium text-slate-700">{point.label}</span>
                      <span className="text-slate-500">{total} chiến dịch</span>
                    </div>
                    <div className="flex h-2 overflow-hidden rounded-full bg-slate-100">
                      <div
                        className="bg-sky-500"
                        style={{ width: `${Math.min(100, point.revisitReminderCount * 12)}%` }}
                      />
                      <div
                        className="bg-teal-500"
                        style={{ width: `${Math.min(100, point.satisfactionSurveyCount * 12)}%` }}
                      />
                      <div
                        className="bg-indigo-500"
                        style={{ width: `${Math.min(100, point.customerCareFollowUpCount * 12)}%` }}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-4">
          <h3 className="text-xs font-bold uppercase tracking-[0.15em] text-slate-700">
            Hoạt động CRM gần đây
          </h3>
          <div className="mt-3 space-y-2.5 max-h-72 overflow-y-auto pr-1">
            {engagementSummary?.recentActivities?.length ? (
              engagementSummary.recentActivities.map((activity) => (
                <div
                  key={activity.outboxMessageId}
                  className="rounded-lg border border-slate-100 bg-slate-50/70 p-3"
                >
                  <div className="flex flex-col gap-1 lg:flex-row lg:items-start lg:justify-between">
                    <div>
                      <p className="text-xs font-bold text-slate-900">
                        {activity.eventLabel} · {activity.patientName}
                      </p>
                      <p className="mt-0.5 text-xs text-slate-500">
                        MRN: {activity.medicalRecordNumber || "--"} ·{" "}
                        {activity.clinicName || "Chưa rõ phòng khám"} ·{" "}
                        {activity.doctorName || "Chưa rõ bác sĩ"}
                      </p>
                    </div>
                    <p className="text-xs font-medium text-slate-400">
                      {formatDateTimeValue(activity.availableAtUtc)}
                    </p>
                  </div>

                  <div className="mt-2 flex flex-wrap gap-1.5 text-xs font-semibold">
                    <span className="rounded-full bg-sky-100 px-2 py-0.5 text-sky-700">
                      Người nhận: {activity.recipientCount}
                    </span>
                    <span className="rounded-full bg-amber-100 px-2 py-0.5 text-amber-700">
                      Chờ: {activity.queuedCount}
                    </span>
                    <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-emerald-700">
                      Gửi: {activity.deliveredCount}
                    </span>
                    <span className="rounded-full bg-rose-100 px-2 py-0.5 text-rose-700">
                      Lỗi: {activity.failedCount + activity.skippedCount}
                    </span>
                  </div>
                </div>
              ))
            ) : (
              <div className="rounded-lg border border-dashed border-slate-200 bg-slate-50 p-4 text-center text-xs text-slate-500">
                Chưa có hoạt động CRM nào được ghi nhận.
              </div>
            )}
          </div>
        </div>
      </div>
    </Card>
  );
}
