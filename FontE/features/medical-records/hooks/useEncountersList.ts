"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalEncounterService } from "@/services/hospitalEncounterService";
import {
  HospitalEncounterEligibleAppointment,
  HospitalEncounterStatus,
  HospitalEncounterSummary,
} from "../types";

export function useEncountersList() {
  const [encounters, setEncounters] = useState<HospitalEncounterSummary[]>([]);
  const [eligibleAppointments, setEligibleAppointments] = useState<
    HospitalEncounterEligibleAppointment[]
  >([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);
  const [approvingEncounterId, setApprovingEncounterId] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<HospitalEncounterStatus | "All">(
    "All"
  );
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

  const fetchData = useCallback(
    async (showRefreshState = false) => {
      if (showRefreshState) {
        setIsRefreshing(true);
      } else {
        setIsLoading(true);
      }

      try {
        const [worklist, appointments] = await Promise.all([
          hospitalEncounterService.getAll({
            pageNumber,
            pageSize,
            encounterStatus: statusFilter,
            appointmentDate: appointmentDate || undefined,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalEncounterService.getEligibleAppointments(),
        ]);

        setEncounters(worklist.items);
        setTotalCount(worklist.totalCount);
        setEligibleAppointments(appointments);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, "Không thể tải dữ liệu hồ sơ khám.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [appointmentDate, debouncedSearch, pageNumber, pageSize, statusFilter]
  );

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const metrics = useMemo(
    () =>
      encounters.reduce(
        (acc, item) => {
          acc[item.encounterStatus] += 1;
          return acc;
        },
        {
          InProgress: 0,
          Finalized: 0,
          Approved: 0,
        } as Record<HospitalEncounterStatus, number>
      ),
    [encounters]
  );

  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endItem = totalCount === 0 ? 0 : Math.min(pageNumber * pageSize, totalCount);

  const handleApprove = async (encounterId: string) => {
    setApprovingEncounterId(encounterId);
    try {
      await hospitalEncounterService.approve(encounterId, {});
      toast.success("Đã duyệt hồ sơ khám.");
      await fetchData(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể duyệt hồ sơ khám."));
    } finally {
      setApprovingEncounterId(null);
    }
  };

  return {
    encounters,
    eligibleAppointments,
    isLoading,
    isRefreshing,
    listError,
    approvingEncounterId,
    searchQuery,
    setSearchQuery,
    appointmentDate,
    setAppointmentDate,
    statusFilter,
    setStatusFilter,
    pageNumber,
    setPageNumber,
    pageSize,
    setPageSize,
    totalCount,
    totalPages,
    startItem,
    endItem,
    metrics,
    fetchData,
    handleApprove,
  };
}
