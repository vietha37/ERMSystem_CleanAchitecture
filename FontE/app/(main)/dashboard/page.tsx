"use client";

import React, { useEffect, useMemo, useState } from "react";
import { Card } from "@/components/ui/Card";
import { ErrorState } from "@/components/ui/DataState";
import { dashboardService } from "@/services/dashboardService";
import { DashboardStats, DashboardTrendPoint, DashboardTrends } from "@/services/types";
import { getApiErrorMessage } from "@/services/error";
import toast from "react-hot-toast";

type TrendPeriod = "daily" | "monthly";

function toDateInputValue(date: Date): string {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

function getDefaultRange(period: TrendPeriod): { from: string; to: string } {
  const today = new Date();

  if (period === "monthly") {
    const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    const from = new Date(monthStart);
    from.setMonth(from.getMonth() - 11);
    return { from: toDateInputValue(from), to: toDateInputValue(today) };
  }

  const from = new Date(today);
  from.setDate(from.getDate() - 29);
  return { from: toDateInputValue(from), to: toDateInputValue(today) };
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat("vi-VN").format(value);
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value);
}

function chartPoints(values: number[], width: number, height: number, padding: number): string {
  if (values.length === 0) return "";
  const max = Math.max(...values, 1);
  const innerWidth = width - padding * 2;
  const innerHeight = height - padding * 2;

  return values
    .map((value, idx) => {
      const x = padding + (idx * innerWidth) / Math.max(values.length - 1, 1);
      const y = padding + innerHeight - (value / max) * innerHeight;
      return `${x},${y}`;
    })
    .join(" ");
}

function areaPath(points: string, width: number, height: number, padding: number): string {
  if (!points) return "";
  const first = points.split(" ")[0];
  const last = points.split(" ").at(-1);
  if (!first || !last) return "";
  const firstX = first.split(",")[0];
  const lastX = last.split(",")[0];
  const bottomY = height - padding;
  return `M ${firstX},${bottomY} L ${points.replaceAll(" ", " L ")} L ${lastX},${bottomY} Z`;
}

function ProgressRing({
  value,
  colorClass,
}: {
  value: number;
  colorClass: string;
}) {
  const size = 64;
  const stroke = 7;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const progress = (clamp(value, 0, 100) / 100) * circumference;

  return (
    <div className="relative h-16 w-16">
      <svg width={size} height={size} className="-rotate-90">
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke="#dbeafe"
          strokeWidth={stroke}
          fill="transparent"
        />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke="currentColor"
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={circumference - progress}
          className={colorClass}
          fill="transparent"
        />
      </svg>
      <span className="absolute inset-0 flex items-center justify-center text-xs font-semibold text-slate-600">
        {Math.round(value)}%
      </span>
    </div>
  );
}

function KpiCard({
  title,
  value,
  percent,
  colorClass,
  note,
}: {
  title: string;
  value: number;
  percent: number;
  colorClass: string;
  note: string;
}) {
  return (
    <Card className="rounded-3xl border border-slate-100 bg-white/90 p-5 shadow-sm">
      <div className="mb-3 flex items-start justify-between">
        <div>
          <p className="text-sm font-semibold text-slate-700">{title}</p>
          <p className="mt-2 text-2xl font-bold text-slate-900">{formatNumber(value)}</p>
          <p className="mt-1 text-xs text-slate-500">{note}</p>
        </div>
        <ProgressRing value={percent} colorClass={colorClass} />
      </div>
    </Card>
  );
}

function MainTrendChart({ points }: { points: DashboardTrendPoint[] }) {
  const width = 980;
  const height = 310;
  const padding = 28;
  const patientValues = points.map((point) => point.patientsCount);
  const appointmentValues = points.map((point) => point.appointmentsCount);
  const prescriptionValues = points.map((point) => point.prescriptionsCount);
  const maxValue = Math.max(...patientValues, ...appointmentValues, ...prescriptionValues, 1);
  const patientLine = chartPoints(patientValues, width, height, padding);
  const appointmentLine = chartPoints(appointmentValues, width, height, padding);
  const prescriptionLine = chartPoints(prescriptionValues, width, height, padding);
import { DashboardStats, DashboardTrendPoint, DashboardTrends } from "@/services/types";
import { getApiErrorMessage } from "@/services/error";
import toast from "react-hot-toast";

type TrendPeriod = "daily" | "monthly";

function toDateInputValue(date: Date): string {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

function getDefaultRange(period: TrendPeriod): { from: string; to: string } {
  const today = new Date();

  if (period === "monthly") {
    const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    const from = new Date(monthStart);
    from.setMonth(from.getMonth() - 11);
    return { from: toDateInputValue(from), to: toDateInputValue(today) };
  }

  const from = new Date(today);
  from.setDate(from.getDate() - 29);
  return { from: toDateInputValue(from), to: toDateInputValue(today) };
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat("vi-VN").format(value);
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value);
}

function chartPoints(values: number[], width: number, height: number, padding: number): string {
  if (values.length === 0) return "";
  const max = Math.max(...values, 1);
  const innerWidth = width - padding * 2;
  const innerHeight = height - padding * 2;

  return values
    .map((value, idx) => {
      const x = padding + (idx * innerWidth) / Math.max(values.length - 1, 1);
      const y = padding + innerHeight - (value / max) * innerHeight;
      return `${x},${y}`;
    })
    .join(" ");
}

function areaPath(points: string, width: number, height: number, padding: number): string {
  if (!points) return "";
  const first = points.split(" ")[0];
  const last = points.split(" ").at(-1);
  if (!first || !last) return "";
  const firstX = first.split(",")[0];
  const lastX = last.split(",")[0];
  const bottomY = height - padding;
  return `M ${firstX},${bottomY} L ${points.replaceAll(" ", " L ")} L ${lastX},${bottomY} Z`;
}

function ProgressRing({
  value,
  colorClass,
}: {
  value: number;
  colorClass: string;
}) {
  const size = 64;
  const stroke = 7;
  const radius = (size - stroke) / 2;
  const circumference = 2 * Math.PI * radius;
  const progress = (clamp(value, 0, 100) / 100) * circumference;

  return (
    <div className="relative h-16 w-16">
      <svg width={size} height={size} className="-rotate-90">
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke="#dbeafe"
          strokeWidth={stroke}
          fill="transparent"
        />
        <circle
          cx={size / 2}
          cy={size / 2}
          r={radius}
          stroke="currentColor"
          strokeWidth={stroke}
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={circumference - progress}
          className={colorClass}
          fill="transparent"
        />
      </svg>
      <span className="absolute inset-0 flex items-center justify-center text-xs font-semibold text-slate-600">
        {Math.round(value)}%
      </span>
    </div>
  );
}

function KpiCard({
  title,
  value,
  percent,
  colorClass,
  note,
}: {
  title: string;
  value: number;
  percent: number;
  colorClass: string;
  note: string;
}) {
  return (
    <Card className="rounded-3xl border border-slate-100 bg-white/90 p-5 shadow-sm">
      <div className="mb-3 flex items-start justify-between">
        <div>
          <p className="text-sm font-semibold text-slate-700">{title}</p>
          <p className="mt-2 text-2xl font-bold text-slate-900">{formatNumber(value)}</p>
          <p className="mt-1 text-xs text-slate-500">{note}</p>
        </div>
        <ProgressRing value={percent} colorClass={colorClass} />
      </div>
    </Card>
  );
}

function MainTrendChart({ points }: { points: DashboardTrendPoint[] }) {
  const width = 980;
  const height = 310;
  const padding = 28;
  const patientValues = points.map((point) => point.patientsCount);
  const appointmentValues = points.map((point) => point.appointmentsCount);
  const prescriptionValues = points.map((point) => point.prescriptionsCount);
  const maxValue = Math.max(...patientValues, ...appointmentValues, ...prescriptionValues, 1);
  const patientLine = chartPoints(patientValues, width, height, padding);
  const appointmentLine = chartPoints(appointmentValues, width, height, padding);
  const prescriptionLine = chartPoints(prescriptionValues, width, height, padding);
  const patientArea = areaPath(patientLine, width, height, padding);
  const labelStep = Math.max(1, Math.floor(points.length / 7));

  return (
    <div className="rounded-3xl border border-slate-100 bg-white p-4 shadow-sm">
      <div className="mb-4 flex items-center justify-between">
        <div>
          <p className="text-sm font-semibold text-slate-800">Biểu đồ</p>
          <p className="text-xs text-slate-500">Bệnh nhân, lịch hẹn và đơn thuốc theo thời gian</p>
        </div>
        <div className="flex items-center gap-4 text-xs">
          <span className="flex items-center gap-2 text-slate-600">
            <span className="h-2.5 w-2.5 rounded-full bg-blue-500" />
            Bệnh nhân
          </span>
          <span className="flex items-center gap-2 text-slate-600">
            <span className="h-2.5 w-2.5 rounded-full bg-amber-500" />
            Lịch hẹn
          </span>
          <span className="flex items-center gap-2 text-slate-600">
            <span className="h-2.5 w-2.5 rounded-full bg-emerald-500" />
            Đơn thuốc
          </span>
        </div>
      </div>

      <div className="overflow-x-auto">
        <svg viewBox={`0 0 ${width} ${height}`} className="h-72 w-full min-w-[700px]">
          <defs>
            <linearGradient id="patientsFill" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#3b82f6" stopOpacity="0.35" />
              <stop offset="100%" stopColor="#3b82f6" stopOpacity="0.02" />
            </linearGradient>
          </defs>

          {[0, 0.25, 0.5, 0.75, 1].map((tick) => {
            const y = padding + (1 - tick) * (height - padding * 2);
            const value = Math.round(maxValue * tick);
            return (
              <g key={tick}>
                <line x1={padding} y1={y} x2={width - padding} y2={y} stroke="#e2e8f0" strokeWidth="1" />
                <text x={6} y={y + 4} fontSize="11" fill="#94a3b8">
                  {value}
                </text>
              </g>
            );
          })}

          <line x1={padding} y1={height - padding} x2={width - padding} y2={height - padding} stroke="#cbd5e1" />
          <line x1={padding} y1={padding} x2={padding} y2={height - padding} stroke="#cbd5e1" />

          {patientArea ? <path d={patientArea} fill="url(#patientsFill)" /> : null}
          <polyline fill="none" stroke="#2563eb" strokeWidth="3" points={patientLine} />
          <polyline fill="none" stroke="#f59e0b" strokeWidth="2.5" points={appointmentLine} />
          <polyline fill="none" stroke="#10b981" strokeWidth="2.5" points={prescriptionLine} />

          {points.map((point, idx) => {
            if (idx % labelStep !== 0 && idx !== points.length - 1) return null;
            const x = padding + (idx * (width - padding * 2)) / Math.max(points.length - 1, 1);
            return (
              <text
                key={`${point.label}-${idx}`}
                x={x}
                y={height - 8}
                textAnchor="middle"
                fontSize="11"
                fill="#64748b"
              >
                {point.label}
              </text>
            );
          })}
        </svg>
      </div>
    </div>
  );
}

function DailySnapshotDonut({
  items,
}: {
  items: Array<{ label: string; value: number; color: string; light: string }>;
}) {
  const total = items.reduce((sum, item) => sum + item.value, 0);

  if (total <= 0) {
    return <p className="text-sm text-slate-500">Chưa có dữ liệu.</p>;
  }

  const stops = items.reduce<{ next: number; parts: string[] }>(
    (acc, item) => {
      const start = (acc.next / total) * 100;
      const endValue = acc.next + item.value;
      const end = (endValue / total) * 100;
      acc.parts.push(`${item.color} ${start.toFixed(2)}% ${end.toFixed(2)}%`);
      return { next: endValue, parts: acc.parts };
    },
    { next: 0, parts: [] }
  ).parts;

  const gradient = `conic-gradient(${stops.join(", ")})`;

  return (
    <div className="space-y-4">
      <div className="mx-auto flex h-44 w-44 items-center justify-center rounded-full" style={{ background: gradient }}>
        <div className="flex h-28 w-28 flex-col items-center justify-center rounded-full bg-white shadow-inner">
          <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">Tổng</p>
          <p className="text-xl font-bold text-slate-900">{formatNumber(total)}</p>
        </div>
      </div>

      <div className="space-y-2">
        {items.map((item) => (
          <div key={item.label} className="flex items-center justify-between rounded-xl border border-slate-100 px-3 py-2">
            <span className="flex items-center gap-2 text-sm font-medium text-slate-700">
              <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: item.color }} />
              {item.label}
            </span>
            <span className="text-xs font-semibold text-slate-600">{formatNumber(item.value)}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [trends, setTrends] = useState<DashboardTrends | null>(null);
  const [period, setPeriod] = useState<TrendPeriod>("daily");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isTrendsLoading, setIsTrendsLoading] = useState(true);
  const [statsError, setStatsError] = useState<string | null>(null);
  const [trendsError, setTrendsError] = useState<string | null>(null);

  useEffect(() => {
    const range = getDefaultRange("daily");
    setFromDate(range.from);
    setToDate(range.to);
  }, []);

  useEffect(() => {
    const fetchStats = async () => {
      setIsLoading(true);
      try {
        const result = await dashboardService.getStats();
        setStats(result);
        setStatsError(null);
      } catch (error: unknown) {
        setStatsError(getApiErrorMessage(error, "Không thể tải thống kê tổng quan."));
        toast.error(getApiErrorMessage(error, "Không thể tải thống kê tổng quan."));
        setStats(null);
      } finally {
        setIsLoading(false);
      }
    };

    fetchStats();
  }, []);

  useEffect(() => {
    const range = getDefaultRange(period);
    setFromDate(range.from);
    setToDate(range.to);
  }, [period]);

  useEffect(() => {
    const fetchTrends = async () => {
      if (!fromDate || !toDate || fromDate > toDate) {
        setTrends(null);
        setIsTrendsLoading(false);
        return;
      }

      setIsTrendsLoading(true);
      try {
        const result = await dashboardService.getTrends({
          period,
          fromDate,
          toDate,
        });
        setTrends(result);
        setTrendsError(null);
      } catch (error: unknown) {
        setTrendsError(getApiErrorMessage(error, "Không thể tải dữ liệu biểu đồ."));
        toast.error(getApiErrorMessage(error, "Không thể tải dữ liệu biểu đồ."));
        setTrends(null);
      } finally {
        setIsTrendsLoading(false);
      }
    };

    fetchTrends();
  }, [period, fromDate, toDate]);

  const trendPoints = useMemo(() => trends?.points ?? [], [trends]);

  const topDiagnoses = useMemo(() => {
    return Object.entries(stats?.topDiagnoses || {})
      .map(([name, count]) => ({ name, count: Number(count) }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 5);
  }, [stats]);

  const trendSummary = useMemo(() => {
    if (!trends && trendPoints.length === 0) {
      return {
        totalPatients: 0,
        totalAppointments: 0,
        totalPrescriptions: 0,
        previousPatients: 0,
        previousAppointments: 0,
        previousPrescriptions: 0,
      };
    }

    const totalPatients = trends?.currentPatientsTotal
      ?? trendPoints.reduce((sum, item) => sum + item.patientsCount, 0);
    const totalAppointments = trends?.currentAppointmentsTotal
      ?? trendPoints.reduce((sum, item) => sum + item.appointmentsCount, 0);
    const totalPrescriptions = trends?.currentPrescriptionsTotal
      ?? trendPoints.reduce((sum, item) => sum + item.prescriptionsCount, 0);
    const previousPatients = trends?.previousPatientsTotal ?? 0;
    const previousAppointments = trends?.previousAppointmentsTotal ?? 0;
    const previousPrescriptions = trends?.previousPrescriptionsTotal ?? 0;

    return {
      totalPatients,
      totalAppointments,
      totalPrescriptions,
      previousPatients,
      previousAppointments,
      previousPrescriptions,
    };
  }, [trendPoints, trends]);

  const compareDelta = useMemo(() => {
    return {
      patients: trendSummary.totalPatients - trendSummary.previousPatients,
      appointments: trendSummary.totalAppointments - trendSummary.previousAppointments,
      prescriptions: trendSummary.totalPrescriptions - trendSummary.previousPrescriptions,
    };
  }, [trendSummary]);

  const snapshotPieData = useMemo(() => {
    const palette = [
      { color: "#3b82f6", light: "#dbeafe" },
      { color: "#10b981", light: "#d1fae5" },
      { color: "#f59e0b", light: "#fef3c7" },
      { color: "#ef4444", light: "#fee2e2" },
      { color: "#8b5cf6", light: "#ede9fe" },
    ];

    if (topDiagnoses.length > 0) {
      return topDiagnoses.map((item, index) => ({
        label: item.name,
        value: Math.max(item.count, 0),
        color: palette[index % palette.length].color,
        light: palette[index % palette.length].light,
      }));
    }

    return [
      {
        label: "Bệnh nhân",
        value: Math.max(stats?.totalPatients ?? 0, 0),
        color: palette[0].color,
        light: palette[0].light,
      },
      {
        label: "Lịch hẹn",
        value: Math.max(stats?.appointmentsToday ?? 0, 0),
        color: palette[1].color,
        light: palette[1].light,
      },
      {
        label: "Hoàn thành",
        value: Math.max(stats?.completedAppointments ?? 0, 0),
        color: palette[2].color,
        light: palette[2].light,
      },
      {
        label: "Đang chờ",
        value: Math.max(stats?.pendingAppointments ?? 0, 0),
        color: palette[3].color,
        light: palette[3].light,
      },
      {
        label: "Đã hủy",
        value: Math.max(stats?.cancelledAppointments ?? 0, 0),
        color: palette[4].color,
        light: palette[4].light,
      },
    ].filter((x) => x.value > 0);
  }, [topDiagnoses, stats]);

  if (isLoading) {
    return (
      <div className="flex min-h-[60vh] flex-col items-center justify-center">
        <div className="h-12 w-12 animate-spin rounded-full border-4 border-blue-200 border-t-blue-600" />
        <p className="mt-4 font-medium tracking-wide text-slate-500">Đang tải bảng điều hành...</p>
      </div>
    );
  }

  if (statsError) {
    return (
      <div className="mx-auto max-w-3xl pt-12">
        <ErrorState
          title="Không thể tải bảng điều hành"
          description={statsError}
          onAction={() => window.location.reload()}
        />
      </div>
    );
  }

  const patients = stats?.totalPatients ?? 0;
  const appointmentsToday = stats?.appointmentsToday ?? 0;
  const pending = stats?.pendingAppointments ?? 0;
  const completed = stats?.completedAppointments ?? 0;
  const cancelled = stats?.cancelledAppointments ?? 0;
  const revisitAppointmentsToday = stats?.revisitAppointmentsToday ?? 0;
  const totalInvoices = stats?.totalInvoices ?? 0;
  const paidInvoices = stats?.paidInvoices ?? 0;
  const issuedAmountThisMonth = stats?.issuedAmountThisMonth ?? 0;
  const collectedAmountThisMonth = stats?.collectedAmountThisMonth ?? 0;
  const outstandingBalanceAmount = stats?.outstandingBalanceAmount ?? 0;

  const patientsPercent = clamp((patients / 1000) * 100, 0, 100);
  const todayPercent = clamp((appointmentsToday / 50) * 100, 0, 100);
  const completedPercent = clamp(stats?.completionRatePercent ?? 0, 0, 100);
  const cancelledPercent = clamp(stats?.cancellationRatePercent ?? 0, 0, 100);
  const revisitPercent = clamp(stats?.revisitRatePercent ?? 0, 0, 100);
  const collectionPercent = clamp(stats?.collectionRatePercent ?? 0, 0, 100);

  return (
    <div className="animate-fade-in space-y-6">
      <div className="rounded-[28px] border border-slate-200/70 bg-gradient-to-br from-slate-100 to-blue-50 p-4 shadow-inner md:p-6">
        <div className="grid gap-4 xl:grid-cols-12">
          <div className="space-y-4 xl:col-span-9">
            <div className="flex flex-wrap items-center justify-between gap-3 rounded-3xl border border-slate-100 bg-white px-5 py-4 shadow-sm">
              <div>
                <h1 className="text-2xl font-bold text-slate-900">Bảng điều hành</h1>
                <p className="text-xs text-slate-500">Tổng quan</p>
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <input
                  type="date"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                  className="rounded-lg border border-slate-300 px-2 py-1.5 text-sm text-slate-700 outline-none focus:border-blue-400"
                />
                <span className="text-sm text-slate-400">-</span>
                <input
                  type="date"
                  value={toDate}
                  onChange={(e) => setToDate(e.target.value)}
                  className="rounded-lg border border-slate-300 px-2 py-1.5 text-sm text-slate-700 outline-none focus:border-blue-400"
                />
                <div className="inline-flex rounded-xl border border-slate-200 bg-slate-50 p-1">
                  <button
                    onClick={() => setPeriod("daily")}
                    className={`rounded-lg px-3 py-1.5 text-sm font-medium transition ${
                      period === "daily" ? "bg-white text-blue-700 shadow-sm" : "text-slate-600"
                    }`}
                  >
                    Ngày
                  </button>
                  <button
                    onClick={() => setPeriod("monthly")}
                    className={`rounded-lg px-3 py-1.5 text-sm font-medium transition ${
                      period === "monthly" ? "bg-white text-blue-700 shadow-sm" : "text-slate-600"
                    }`}
                  >
                    Tháng
                  </button>
                </div>
              </div>
            </div>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
              <KpiCard
                title="Tổng bệnh nhân"
                value={patients}
                percent={patientsPercent}
                colorClass="text-blue-500"
                note="Tổng số bệnh nhân đã đăng ký"
              />
              <KpiCard
                title="Lịch hẹn hôm nay"
                value={appointmentsToday}
                percent={todayPercent}
                colorClass="text-sky-500"
                note="Gồm lịch mới và tái khám"
              />
              <KpiCard
                title="Hoàn thành hôm nay"
                value={completed}
                percent={completedPercent}
                colorClass="text-emerald-500"
                note="Số lượt khám đã kết thúc"
              />
            </div>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <Card className="rounded-3xl border border-slate-100 bg-white p-5 shadow-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-semibold text-slate-800">Lịch hẹn hôm nay</p>
                    <p className="text-xs text-slate-500">Theo trạng thái</p>
                  </div>
                  <span className="rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold text-amber-700">
                    Đang chờ {formatNumber(pending)}
                  </span>
                </div>
                <div className="mt-4 grid grid-cols-2 gap-3">
                  <div className="rounded-2xl bg-emerald-50 px-4 py-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">Tỷ lệ hoàn thành</p>
                    <p className="mt-1 text-2xl font-bold text-emerald-900">{completedPercent.toFixed(0)}%</p>
                    <p className="mt-1 text-xs text-emerald-700">{formatNumber(completed)} lượt đã hoàn thành hôm nay</p>
                  </div>
                  <div className="rounded-2xl bg-rose-50 px-4 py-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-rose-700">Tỷ lệ hủy</p>
                    <p className="mt-1 text-2xl font-bold text-rose-900">{cancelledPercent.toFixed(0)}%</p>
                    <p className="mt-1 text-xs text-rose-700">{formatNumber(cancelled)} lịch đã hủy hôm nay</p>
                  </div>
                  <div className="col-span-2 rounded-2xl bg-violet-50 px-4 py-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-violet-700">Tỷ lệ tái khám</p>
                    <p className="mt-1 text-2xl font-bold text-violet-900">{revisitPercent.toFixed(0)}%</p>
                    <p className="mt-1 text-xs text-violet-700">{formatNumber(revisitAppointmentsToday)} lịch tái khám hôm nay</p>
                  </div>
                </div>
              </Card>

              <Card className="rounded-3xl border border-slate-100 bg-white p-5 shadow-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-semibold text-slate-800">Tài chính tháng này</p>
                    <p className="text-xs text-slate-500">Hóa đơn phát hành và đã thu</p>
                  </div>
                  <span className="rounded-full bg-sky-50 px-3 py-1 text-xs font-semibold text-sky-700">
                    {collectionPercent.toFixed(0)}% đã thu
                  </span>
                </div>
                <div className="mt-4 grid grid-cols-1 gap-3">
                  <div className="rounded-2xl bg-cyan-50 px-4 py-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-cyan-700">Phát hành trong tháng</p>
                    <p className="mt-1 text-xl font-bold text-cyan-900">{formatCurrency(issuedAmountThisMonth)}</p>
                    <p className="mt-1 text-xs text-cyan-700">{formatNumber(totalInvoices)} hóa đơn trong hệ thống</p>
                  </div>
                  <div className="rounded-2xl bg-emerald-50 px-4 py-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-emerald-700">Đã thu trong tháng</p>
                    <p className="mt-1 text-xl font-bold text-emerald-900">{formatCurrency(collectedAmountThisMonth)}</p>
                    <p className="mt-1 text-xs text-emerald-700">{formatNumber(paidInvoices)} hóa đơn đã thanh toán đủ</p>
                  </div>
                  <div className="rounded-2xl bg-rose-50 px-4 py-3">
                    <p className="text-xs font-semibold uppercase tracking-wide text-rose-700">Công nợ còn lại</p>
                    <p className="mt-1 text-xl font-bold text-rose-900">{formatCurrency(outstandingBalanceAmount)}</p>
                    <p className="mt-1 text-xs text-rose-700">Tổng số dư chưa thanh toán hiện tại</p>
                  </div>
                </div>
              </Card>
            </div>

            <Card className="rounded-3xl border border-slate-100 bg-white p-5 shadow-sm">
              <p className="text-sm font-semibold text-slate-800">Hoạt động hôm nay</p>
              <div className="mt-4">
                <DailySnapshotDonut items={snapshotPieData} />
              </div>
            </Card>

            {isTrendsLoading ? (
              <Card className="flex h-[390px] items-center justify-center rounded-3xl border border-slate-100 bg-white text-slate-500">
                Đang tải biểu đồ...
              </Card>
            ) : trendsError ? (
              <Card className="h-[390px] rounded-3xl border border-slate-100 bg-white">
                <ErrorState
                  title="Không thể tải biểu đồ"
                  description={trendsError}
                  onAction={() => window.location.reload()}
                />
              </Card>
            ) : trendPoints.length > 0 ? (
              <MainTrendChart points={trendPoints} />
            ) : (
              <Card className="flex h-[390px] items-center justify-center rounded-3xl border border-slate-100 bg-white text-slate-500">
                Chưa có dữ liệu.
              </Card>
            )}

            <div className="grid grid-cols-1 gap-4">
              <Card className="rounded-3xl border border-slate-100 bg-white p-5 shadow-sm">
                <p className="text-sm font-semibold text-slate-800">Top 5 chẩn đoán</p>
                <div className="mt-4 space-y-3">
                  {topDiagnoses.length ? (
                    topDiagnoses.map((item, index) => (
                      <div key={item.name} className="flex items-center justify-between rounded-xl bg-slate-50 px-3 py-2">
                        <span className="text-sm font-medium text-slate-700">
                          {index + 1}. {item.name}
                        </span>
                        <span className="rounded-full bg-blue-100 px-2 py-0.5 text-xs font-semibold text-blue-700">
                          {item.count}
                        </span>
                      </div>
                    ))
                  ) : (
                    <p className="text-sm text-slate-500">Chưa có dữ liệu chẩn đoán.</p>
                  )}
                </div>
              </Card>

            </div>
          </div>

          <div className="space-y-4 xl:col-span-3">
            <Card className="rounded-3xl border border-slate-100 bg-white p-5 shadow-sm">
              <p className="text-sm font-semibold text-slate-800">Tổng hợp</p>
              <div className="mt-4 space-y-3">
                <div className="rounded-2xl bg-gradient-to-r from-blue-500 to-sky-500 px-4 py-3 text-white">
                  <p className="text-xs opacity-90">Bệnh nhân</p>
                  <p className="mt-1 text-2xl font-bold">{formatNumber(trendSummary.totalPatients)}</p>
                </div>
                <div className="rounded-2xl bg-gradient-to-r from-amber-500 to-orange-500 px-4 py-3 text-white">
                  <p className="text-xs opacity-90">Lịch hẹn</p>
                  <p className="mt-1 text-2xl font-bold">{formatNumber(trendSummary.totalAppointments)}</p>
                </div>
                <div className="rounded-2xl bg-gradient-to-r from-emerald-500 to-teal-500 px-4 py-3 text-white">
                  <p className="text-xs opacity-90">Đơn thuốc</p>
                  <p className="mt-1 text-2xl font-bold">{formatNumber(trendSummary.totalPrescriptions)}</p>
                </div>
              </div>
            </Card>

            <Card className="rounded-3xl border border-slate-100 bg-white p-5 shadow-sm">
              <p className="text-sm font-semibold text-slate-800">So với kỳ trước</p>
              <div className="mt-4 space-y-3">
                <div className="flex items-center justify-between rounded-xl bg-slate-50 px-3 py-2">
                  <span className="text-sm text-slate-600">Bệnh nhân</span>
                  <span
                    className={`text-sm font-semibold ${
                      compareDelta.patients >= 0 ? "text-emerald-600" : "text-rose-600"
                    }`}
                  >
                    {compareDelta.patients >= 0 ? "+" : ""}
                    {compareDelta.patients}
                  </span>
                </div>
                <div className="flex items-center justify-between rounded-xl bg-slate-50 px-3 py-2">
                  <span className="text-sm text-slate-600">Lịch hẹn</span>
                  <span
                    className={`text-sm font-semibold ${
                      compareDelta.appointments >= 0 ? "text-emerald-600" : "text-rose-600"
                    }`}
                  >
                    {compareDelta.appointments >= 0 ? "+" : ""}
                    {compareDelta.appointments}
                  </span>
                </div>
                <div className="flex items-center justify-between rounded-xl bg-slate-50 px-3 py-2">
                  <span className="text-sm text-slate-600">Đơn thuốc</span>
                  <span
                    className={`text-sm font-semibold ${
                      compareDelta.prescriptions >= 0 ? "text-emerald-600" : "text-rose-600"
                    }`}
                  >
                    {compareDelta.prescriptions >= 0 ? "+" : ""}
                    {compareDelta.prescriptions}
                  </span>
                </div>
              </div>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
