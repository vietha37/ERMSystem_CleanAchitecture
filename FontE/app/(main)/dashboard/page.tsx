"use client";

import React, { useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { Card, CardHeader } from "@/components/ui/Card";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/DataState";
import { useTranslation } from "@/hooks/useTranslation";
import { dashboardService } from "@/services/dashboardService";
import { getApiErrorMessage } from "@/services/error";
import { DashboardStats, DashboardTrendPoint, DashboardTrends } from "@/services/types";

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

function TrendChart({
  points,
  title,
  period,
}: {
  points: DashboardTrendPoint[];
  title: string;
  period: TrendPeriod;
}) {
  const { t } = useTranslation();
  if (points.length === 0) {
    return (
      <EmptyState
        title={t("common.labels.empty")}
        description={t("common.labels.noResults")}
      />
    );
  }

  const width = 840;
  const height = 260;
  const padding = 42;

  const patientValues = points.map((p) => p.patientsCount);
  const appointmentValues = points.map((p) => p.appointmentsCount);
  const prescriptionValues = points.map((p) => p.prescriptionsCount);

  const maxVal = Math.max(
    ...patientValues,
    ...appointmentValues,
    ...prescriptionValues,
    5
  );

  const patientLine = chartPoints(patientValues, width, height, padding);
  const patientArea = areaPath(patientLine, width, height, padding);
  const appointmentLine = chartPoints(appointmentValues, width, height, padding);
  const prescriptionLine = chartPoints(prescriptionValues, width, height, padding);

  const yTicks = [0, Math.round(maxVal / 2), maxVal];
  const labelStep = Math.max(Math.floor(points.length / (period === "daily" ? 7 : 6)), 1);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-3">
        <h4 className="text-xs font-semibold uppercase tracking-wider text-slate-700">
          {title}
        </h4>
        <div className="flex items-center gap-4 text-xs font-medium text-slate-600">
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-xs bg-slate-900" />
            {t("nav.patients")}
          </span>
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-xs bg-sky-600" />
            {t("nav.appointments")}
          </span>
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-xs bg-emerald-600" />
            {t("nav.prescriptions")}
          </span>
        </div>
      </div>

      <div className="w-full overflow-x-auto">
        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="h-64 w-full min-w-[560px]"
          preserveAspectRatio="none"
          role="img"
          aria-label={title}
        >
          <defs>
            <linearGradient id="patientAreaGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stopColor="#0f172a" stopOpacity="0.12" />
              <stop offset="100%" stopColor="#0f172a" stopOpacity="0.0" />
            </linearGradient>
          </defs>

          {yTicks.map((tick) => {
            const y = padding + (height - padding * 2) - (tick / maxVal) * (height - padding * 2);
            return (
              <g key={tick}>
                <line
                  x1={padding}
                  y1={y}
                  x2={width - padding}
                  y2={y}
                  stroke="#f1f5f9"
                  strokeDasharray="3 3"
                />
                <text
                  x={padding - 8}
                  y={y + 4}
                  textAnchor="end"
                  fontSize="10"
                  fill="#94a3b8"
                  className="font-mono"
                >
                  {tick}
                </text>
              </g>
            );
          })}

          {patientArea && <path d={patientArea} fill="url(#patientAreaGradient)" />}
          <polyline fill="none" stroke="#0f172a" strokeWidth="2" points={patientLine} />
          <polyline fill="none" stroke="#0284c7" strokeWidth="2" points={appointmentLine} />
          <polyline fill="none" stroke="#059669" strokeWidth="2" points={prescriptionLine} />

          {points.map((point, idx) => {
            if (idx % labelStep !== 0 && idx !== points.length - 1) return null;
            const x = padding + (idx * (width - padding * 2)) / Math.max(points.length - 1, 1);
            return (
              <text
                key={`${point.label}-${idx}`}
                x={x}
                y={height - 10}
                textAnchor="middle"
                fontSize="10"
                fill="#64748b"
                className="font-mono"
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

export default function DashboardPage() {
  const { t } = useTranslation();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [trends, setTrends] = useState<DashboardTrends | null>(null);
  const [period, setPeriod] = useState<TrendPeriod>("daily");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isTrendsLoading, setIsTrendsLoading] = useState(true);
  const [statsError, setStatsError] = useState<string | null>(null);

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
        const msg = getApiErrorMessage(error, t("common.messages.error"));
        setStatsError(msg);
        toast.error(msg);
        setStats(null);
      } finally {
        setIsLoading(false);
      }
    };

    fetchStats();
  }, [t]);

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
      } catch (error: unknown) {
        toast.error(getApiErrorMessage(error, t("common.messages.error")));
        setTrends(null);
      } finally {
        setIsTrendsLoading(false);
      }
    };

    fetchTrends();
  }, [period, fromDate, toDate, t]);

  const trendPoints = useMemo(() => trends?.points ?? [], [trends]);

  const topDiagnoses = useMemo(() => {
    return Object.entries(stats?.topDiagnoses || {})
      .map(([name, count]) => ({ name, count: Number(count) }))
      .sort((a, b) => b.count - a.count)
      .slice(0, 5);
  }, [stats]);

  if (isLoading) {
    return <LoadingState title={t("common.actions.loading")} />;
  }

  if (statsError) {
    return (
      <ErrorState
        title={t("common.messages.error")}
        description={statsError}
        onAction={() => window.location.reload()}
      />
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

  const completedPercent = clamp(stats?.completionRatePercent ?? 0, 0, 100);
  const cancelledPercent = clamp(stats?.cancellationRatePercent ?? 0, 0, 100);
  const revisitPercent = clamp(stats?.revisitRatePercent ?? 0, 0, 100);
  const collectionPercent = clamp(stats?.collectionRatePercent ?? 0, 0, 100);

  return (
    <div className="space-y-6">
      {/* Top Header & Range Filters */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border-b border-slate-200 pb-5">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-slate-900">
            {t("dashboard.title")}
          </h1>
          <p className="mt-1 text-xs text-slate-500">
            {t("dashboard.subtitle")}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-800 shadow-2xs outline-none focus:border-slate-500"
          />
          <span className="text-xs text-slate-400">-</span>
          <input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            className="rounded-lg border border-slate-300 bg-white px-2.5 py-1 text-xs text-slate-800 shadow-2xs outline-none focus:border-slate-500"
          />
          <div className="inline-flex rounded-lg border border-slate-200 bg-slate-50 p-0.5 text-xs font-semibold">
            <button
              type="button"
              onClick={() => setPeriod("daily")}
              className={`rounded-md px-2.5 py-1 transition-colors ${
                period === "daily" ? "bg-slate-900 text-white shadow-2xs" : "text-slate-600 hover:text-slate-900"
              }`}
            >
              Ngày
            </button>
            <button
              type="button"
              onClick={() => setPeriod("monthly")}
              className={`rounded-md px-2.5 py-1 transition-colors ${
                period === "monthly" ? "bg-slate-900 text-white shadow-2xs" : "text-slate-600 hover:text-slate-900"
              }`}
            >
              Tháng
            </button>
          </div>
        </div>
      </div>

      {/* KPI Cards Grid */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {/* KPI 1: Patients */}
        <Card className="flex flex-col justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">
              {t("dashboard.stats.totalPatients")}
            </p>
            <p className="mt-2 text-2xl font-bold tracking-tight text-slate-900">
              {formatNumber(patients)}
            </p>
          </div>
          <div className="mt-4 flex items-center justify-between text-xs text-slate-500 border-t border-slate-100 pt-3">
            <span>Đã đăng ký hệ thống</span>
            <span className="font-semibold text-slate-700">100% EMR</span>
          </div>
        </Card>

        {/* KPI 2: Today's Intake */}
        <Card className="flex flex-col justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">
              {t("dashboard.stats.appointmentsToday")}
            </p>
            <p className="mt-2 text-2xl font-bold tracking-tight text-slate-900">
              {formatNumber(appointmentsToday)}
            </p>
          </div>
          <div className="mt-4 flex items-center justify-between text-xs text-slate-500 border-t border-slate-100 pt-3">
            <span>{t("dashboard.stats.pendingAppointments")}:</span>
            <span className="font-semibold text-amber-600">{formatNumber(pending)} ca</span>
          </div>
        </Card>

        {/* KPI 3: Completion Rate */}
        <Card className="flex flex-col justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">
              {t("dashboard.stats.completionRate")}
            </p>
            <p className="mt-2 text-2xl font-bold tracking-tight text-emerald-700">
              {completedPercent.toFixed(1)}%
            </p>
          </div>
          <div className="mt-4 flex items-center justify-between text-xs text-slate-500 border-t border-slate-100 pt-3">
            <span>{t("dashboard.stats.completedAppointments")}:</span>
            <span className="font-semibold text-emerald-700">{formatNumber(completed)}</span>
          </div>
        </Card>

        {/* KPI 4: Financial Collection */}
        <Card className="flex flex-col justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">
              {t("dashboard.stats.collectionRate")}
            </p>
            <p className="mt-2 text-2xl font-bold tracking-tight text-sky-700">
              {collectionPercent.toFixed(1)}%
            </p>
          </div>
          <div className="mt-4 flex items-center justify-between text-xs text-slate-500 border-t border-slate-100 pt-3">
            <span>Đã thu tháng:</span>
            <span className="font-semibold text-sky-700">{formatCurrency(collectedAmountThisMonth)}</span>
          </div>
        </Card>
      </div>

      {/* Operational & Financial Deep Dive */}
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
        {/* Today's Clinical Flow */}
        <Card>
          <CardHeader
            title={t("dashboard.sections.operationalFlow")}
            subtitle="Chỉ số hiệu quả tiếp nhận và tiến độ xử lý lượt khám trong ngày"
          />
          <div className="mt-4 grid grid-cols-2 gap-3">
            <div className="rounded-lg border border-slate-200 bg-slate-50/70 p-3.5">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                {t("dashboard.stats.completedAppointments")}
              </p>
              <p className="mt-1 text-xl font-bold text-slate-900">{formatNumber(completed)}</p>
              <p className="mt-1 text-xs text-emerald-700 font-medium">{completedPercent.toFixed(0)}% hoàn thành</p>
            </div>
            <div className="rounded-lg border border-slate-200 bg-slate-50/70 p-3.5">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                {t("dashboard.stats.cancelledAppointments")}
              </p>
              <p className="mt-1 text-xl font-bold text-slate-900">{formatNumber(cancelled)}</p>
              <p className="mt-1 text-xs text-rose-600 font-medium">{cancelledPercent.toFixed(0)}% đã hủy</p>
            </div>
            <div className="rounded-lg border border-slate-200 bg-slate-50/70 p-3.5">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                {t("dashboard.stats.revisitToday")}
              </p>
              <p className="mt-1 text-xl font-bold text-slate-900">{formatNumber(revisitAppointmentsToday)}</p>
              <p className="mt-1 text-xs text-sky-700 font-medium">{revisitPercent.toFixed(0)}% tỷ lệ tái khám</p>
            </div>
            <div className="rounded-lg border border-slate-200 bg-slate-50/70 p-3.5">
              <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                {t("dashboard.stats.pendingAppointments")}
              </p>
              <p className="mt-1 text-xl font-bold text-amber-700">{formatNumber(pending)}</p>
              <p className="mt-1 text-xs text-slate-500">Đang chờ tại sảnh</p>
            </div>
          </div>
        </Card>

        {/* Financial Overview */}
        <Card>
          <CardHeader
            title={t("dashboard.sections.financialOverview")}
            subtitle="Chi tiết viện phí, công nợ và tổng số hóa đơn phát hành"
          />
          <div className="mt-4 space-y-3">
            <div className="flex items-center justify-between rounded-lg border border-slate-100 bg-slate-50/60 p-3 text-xs">
              <span className="font-medium text-slate-600">{t("dashboard.stats.issuedAmount")}</span>
              <span className="font-semibold text-slate-900">{formatCurrency(issuedAmountThisMonth)}</span>
            </div>
            <div className="flex items-center justify-between rounded-lg border border-slate-100 bg-slate-50/60 p-3 text-xs">
              <span className="font-medium text-slate-600">{t("dashboard.stats.collectedAmount")}</span>
              <span className="font-semibold text-emerald-700">{formatCurrency(collectedAmountThisMonth)}</span>
            </div>
            <div className="flex items-center justify-between rounded-lg border border-slate-100 bg-slate-50/60 p-3 text-xs">
              <span className="font-medium text-slate-600">{t("dashboard.stats.outstandingBalance")}</span>
              <span className="font-semibold text-rose-600">{formatCurrency(outstandingBalanceAmount)}</span>
            </div>
            <div className="flex items-center justify-between border-t border-slate-100 pt-3 text-xs text-slate-500">
              <span>Tổng số hóa đơn: <strong className="text-slate-800">{formatNumber(totalInvoices)}</strong></span>
              <span>Đã thanh toán: <strong className="text-emerald-700">{formatNumber(paidInvoices)}</strong></span>
            </div>
          </div>
        </Card>
      </div>

      {/* Trend & Frequent Diagnoses */}
      <div className="grid grid-cols-1 gap-5 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <Card>
            {isTrendsLoading ? (
              <LoadingState title={t("common.actions.loading")} />
            ) : (
              <TrendChart
                points={trendPoints}
                title={t("dashboard.sections.trend")}
                period={period}
              />
            )}
          </Card>
        </div>

        <div>
          <Card>
            <CardHeader
              title={t("dashboard.sections.topDiagnoses")}
              subtitle="Tần suất ghi nhận mã bệnh lâm sàng"
            />
            <div className="mt-4 space-y-2.5">
              {topDiagnoses.length === 0 ? (
                <EmptyState
                  title={t("common.labels.empty")}
                  description={t("common.labels.noResults")}
                />
              ) : (
                topDiagnoses.map((diag, index) => (
                  <div
                    key={diag.name}
                    className="flex items-center justify-between rounded-lg border border-slate-100 bg-slate-50/60 px-3 py-2 text-xs"
                  >
                    <span className="flex items-center gap-2 truncate font-medium text-slate-700">
                      <span className="h-4 w-4 shrink-0 rounded-full bg-slate-200 text-center text-[10px] font-bold text-slate-600">
                        {index + 1}
                      </span>
                      <span className="truncate">{diag.name}</span>
                    </span>
                    <span className="shrink-0 font-bold text-slate-900 font-mono">
                      {formatNumber(diag.count)} ca
                    </span>
                  </div>
                ))
              )}
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
