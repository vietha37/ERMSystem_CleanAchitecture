"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalNotificationDeliveryService } from "@/services/hospitalNotificationDeliveryService";
import {
  DeliveryStatusFilter,
  HospitalCrmEngagementSummary,
  NotificationDelivery,
  NotificationDeliveryStatus,
  NotificationDeliverySummary,
} from "../types";

export function useNotifications() {
  const [deliveries, setDeliveries] = useState<NotificationDelivery[]>([]);
  const [statusFilter, setStatusFilter] = useState<DeliveryStatusFilter>("All");
  const [summary, setSummary] = useState<NotificationDeliverySummary | null>(null);
  const [engagementSummary, setEngagementSummary] =
    useState<HospitalCrmEngagementSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);
  const [retryingId, setRetryingId] = useState<string | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const fetchDeliveries = useCallback(
    async (showRefreshState = false) => {
      if (showRefreshState) {
        setIsRefreshing(true);
      } else {
        setIsLoading(true);
      }

      try {
        const [response, summaryResponse, engagementResponse] = await Promise.all([
          hospitalNotificationDeliveryService.getAll(
            statusFilter,
            pageNumber,
            pageSize
          ),
          hospitalNotificationDeliveryService.getSummary(),
          hospitalNotificationDeliveryService.getEngagementSummary(),
        ]);

        setDeliveries(response.items);
        setTotalCount(response.totalCount);
        setSummary(summaryResponse);
        setEngagementSummary(engagementResponse);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, "Không thể tải danh sách gửi thông báo.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [pageNumber, pageSize, statusFilter]
  );

  useEffect(() => {
    void fetchDeliveries();
  }, [fetchDeliveries]);

  useEffect(() => {
    const timer = window.setInterval(() => {
      void fetchDeliveries(true);
    }, 15000);

    return () => window.clearInterval(timer);
  }, [fetchDeliveries]);

  const metrics = useMemo(() => {
    return {
      Queued: summary?.queuedCount ?? 0,
      Delivered: summary?.deliveredCount ?? 0,
      Failed: summary?.failedCount ?? 0,
      Skipped: summary?.skippedCount ?? 0,
    } as Record<NotificationDeliveryStatus, number>;
  }, [summary]);

  const handleRetry = async (delivery: NotificationDelivery) => {
    setRetryingId(delivery.id);

    try {
      await hospitalNotificationDeliveryService.retry(delivery.id);
      toast.success("Đã đưa thông báo về hàng đợi gửi lại.");
      await fetchDeliveries(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể gửi lại thông báo."));
    } finally {
      setRetryingId(null);
    }
  };

  const handleStatusChange = (nextStatus: DeliveryStatusFilter) => {
    setStatusFilter(nextStatus);
    setPageNumber(1);
  };

  const handlePageSizeChange = (nextSize: number) => {
    setPageSize(nextSize);
    setPageNumber(1);
  };

  return {
    deliveries,
    statusFilter,
    summary,
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
  };
}
