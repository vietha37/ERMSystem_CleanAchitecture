"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalPrescriptionService } from "@/services/hospitalPrescriptionService";
import {
  HospitalMedicineCatalog,
  HospitalPrescriptionEligibleEncounter,
  HospitalPrescriptionStatus,
  HospitalPrescriptionSummary,
} from "../types";

export function usePrescriptionsList() {
  const [prescriptions, setPrescriptions] = useState<HospitalPrescriptionSummary[]>([]);
  const [eligibleEncounters, setEligibleEncounters] = useState<
    HospitalPrescriptionEligibleEncounter[]
  >([]);
  const [medicines, setMedicines] = useState<HospitalMedicineCatalog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<HospitalPrescriptionStatus | "All">(
    "All"
  );
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
        const [worklist, encounters, catalog] = await Promise.all([
          hospitalPrescriptionService.getAll({
            pageNumber,
            pageSize,
            status: statusFilter,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalPrescriptionService.getEligibleEncounters(),
          hospitalPrescriptionService.getMedicineCatalog(),
        ]);

        setPrescriptions(worklist.items);
        setTotalCount(worklist.totalCount);
        setEligibleEncounters(encounters);
        setMedicines(catalog);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, "Không thể tải dữ liệu đơn thuốc.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [debouncedSearch, pageNumber, pageSize, statusFilter]
  );

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const metrics = useMemo(
    () =>
      prescriptions.reduce(
        (acc, item) => {
          acc[item.status] += 1;
          return acc;
        },
        {
          Issued: 0,
          Dispensed: 0,
          Cancelled: 0,
        } as Record<HospitalPrescriptionStatus, number>
      ),
    [prescriptions]
  );

  const availableEncounters = eligibleEncounters.filter((item) => !item.existingPrescriptionId);
  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endItem = totalCount === 0 ? 0 : Math.min(pageNumber * pageSize, totalCount);

  return {
    prescriptions,
    eligibleEncounters,
    availableEncounters,
    medicines,
    isLoading,
    isRefreshing,
    listError,
    searchQuery,
    setSearchQuery,
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
  };
}
