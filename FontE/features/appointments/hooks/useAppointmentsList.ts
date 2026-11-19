"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { useTranslation } from "@/hooks/useTranslation";
import { getApiErrorMessage } from "@/services/error";
import { hospitalAppointmentWorklistService } from "@/services/hospitalAppointmentWorklistService";
import {
  AppointmentMetrics,
  HospitalAppointmentWorklistItem,
  HospitalAppointmentWorklistStatus,
} from "../types";

export function useAppointmentsList() {
  const { t } = useTranslation();
  const [appointments, setAppointments] = useState<HospitalAppointmentWorklistItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<HospitalAppointmentWorklistStatus | "All">("All");
  const [appointmentDate, setAppointmentDate] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setDebouncedSearch(searchQuery.trim());
      setPageNumber(1);
    }, 400);

    return () => window.clearTimeout(timer);
  }, [searchQuery]);

  const fetchAppointments = useCallback(
    async (showRefreshState = false) => {
      if (showRefreshState) {
        setIsRefreshing(true);
      } else {
        setIsLoading(true);
      }

      try {
        const data = await hospitalAppointmentWorklistService.getAll({
          pageNumber,
          pageSize,
          status: statusFilter,
          appointmentDate: appointmentDate || undefined,
          textSearch: debouncedSearch || undefined,
        });

        setAppointments(data.items);
        setTotalCount(data.totalCount);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, t("common.messages.error"));
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [appointmentDate, debouncedSearch, pageNumber, pageSize, statusFilter, t]
  );

  useEffect(() => {
    void fetchAppointments();
  }, [fetchAppointments]);

  const metrics: AppointmentMetrics = useMemo(() => {
    return appointments.reduce(
      (acc, item) => {
        acc[item.status] = (acc[item.status] || 0) + 1;
        return acc;
      },
      {
        Scheduled: 0,
        CheckedIn: 0,
        Completed: 0,
        Cancelled: 0,
      } as AppointmentMetrics
    );
  }, [appointments]);

  return {
    appointments,
    isLoading,
    isRefreshing,
    listError,
    searchQuery,
    setSearchQuery,
    statusFilter,
    setStatusFilter,
    appointmentDate,
    setAppointmentDate,
    pageNumber,
    setPageNumber,
    pageSize,
    setPageSize,
    totalCount,
    totalPages,
    metrics,
    fetchAppointments,
  };
}
