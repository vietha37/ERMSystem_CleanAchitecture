"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalBillingService } from "@/services/hospitalBillingService";
import {
  HospitalBillingEligibleEncounter,
  HospitalInvoiceStatus,
  HospitalInvoiceSummary,
  HospitalPaymentReconciliationSummary,
} from "../types";

export function useBillingInvoices() {
  const [invoices, setInvoices] = useState<HospitalInvoiceSummary[]>([]);
  const [eligibleEncounters, setEligibleEncounters] = useState<HospitalBillingEligibleEncounter[]>([]);
  const [reconciliationSummary, setReconciliationSummary] =
    useState<HospitalPaymentReconciliationSummary | null>(null);

  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<HospitalInvoiceStatus | "All">("All");
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 10;
  const [totalCount, setTotalCount] = useState(0);

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
        const [worklist, encounters, reconciliation] = await Promise.all([
          hospitalBillingService.getAll({
            pageNumber,
            pageSize,
            invoiceStatus: statusFilter,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalBillingService.getEligibleEncounters(),
          hospitalBillingService.getReconciliationSummary(),
        ]);

        setInvoices(worklist.items);
        setTotalCount(worklist.totalCount);
        setEligibleEncounters(encounters);
        setReconciliationSummary(reconciliation);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, "Không thể tải dữ liệu hóa đơn.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [debouncedSearch, pageNumber, statusFilter]
  );

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const availableEncounters = eligibleEncounters.filter((item) => !item.existingInvoiceId);

  const metrics = useMemo(
    () =>
      invoices.reduce(
        (acc, item) => {
          acc[item.invoiceStatus] += 1;
          return acc;
        },
        {
          Issued: 0,
          PartiallyPaid: 0,
          Paid: 0,
          Cancelled: 0,
        } as Record<HospitalInvoiceStatus, number>
      ),
    [invoices]
  );

  return {
    invoices,
    eligibleEncounters,
    availableEncounters,
    reconciliationSummary,
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
    totalCount,
    totalPages,
    metrics,
    fetchData,
  };
}
