"use client";

import { NotificationCrmSection } from "../components/NotificationCrmSection";
import { NotificationHeader } from "../components/NotificationHeader";
import { NotificationMetrics } from "../components/NotificationMetrics";
import { NotificationTable } from "../components/NotificationTable";
import { useNotifications } from "../hooks/useNotifications";

export function NotificationsView() {
  const {
    deliveries,
    statusFilter,
    engagementSummary,
    isLoading,
    isRefreshing,
    listError,
    retryingId,
    pageNumber,
    pageSize,
    totalCount,
    totalPages,
    metrics,
    fetchDeliveries,
    handleRetry,
    handleStatusChange,
    handlePageSizeChange,
    setPageNumber,
  } = useNotifications();

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <NotificationHeader
        statusFilter={statusFilter}
        onStatusChange={handleStatusChange}
        pageSize={pageSize}
        onPageSizeChange={handlePageSizeChange}
        isRefreshing={isRefreshing}
        onRefresh={() => void fetchDeliveries(true)}
      />

      <NotificationMetrics metrics={metrics} />

      <NotificationCrmSection engagementSummary={engagementSummary} />

      <NotificationTable
        deliveries={deliveries}
        isLoading={isLoading}
        listError={listError}
        retryingId={retryingId}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalCount={totalCount}
        totalPages={totalPages}
        onPageChange={setPageNumber}
        onPageSizeChange={handlePageSizeChange}
        onRefresh={() => void fetchDeliveries(true)}
        onRetry={(delivery) => void handleRetry(delivery)}
      />
    </div>
  );
}
