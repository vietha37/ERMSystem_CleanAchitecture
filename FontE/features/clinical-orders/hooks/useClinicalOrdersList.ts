"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalClinicalOrderService } from "@/services/hospitalClinicalOrderService";
import {
  HospitalClinicalOrderCatalogItem,
  HospitalClinicalOrderCategory,
  HospitalClinicalOrderEligibleEncounter,
  HospitalClinicalOrderStatus,
  HospitalClinicalOrderSummary,
} from "../types";

export function useClinicalOrdersList() {
  const [orders, setOrders] = useState<HospitalClinicalOrderSummary[]>([]);
  const [eligibleEncounters, setEligibleEncounters] = useState<
    HospitalClinicalOrderEligibleEncounter[]
  >([]);
  const [catalog, setCatalog] = useState<HospitalClinicalOrderCatalogItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [categoryFilter, setCategoryFilter] = useState<
    HospitalClinicalOrderCategory | "All"
  >("All");
  const [statusFilter, setStatusFilter] = useState<HospitalClinicalOrderStatus | "All">(
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
        const [worklist, encounterData, catalogData] = await Promise.all([
          hospitalClinicalOrderService.getAll({
            pageNumber,
            pageSize,
            category: categoryFilter,
            status: statusFilter,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalClinicalOrderService.getEligibleEncounters(),
          hospitalClinicalOrderService.getCatalog(),
        ]);

        setOrders(worklist.items);
        setTotalCount(worklist.totalCount);
        setEligibleEncounters(encounterData);
        setCatalog(catalogData);
        setListError(null);
      } catch (error: unknown) {
        const message = getApiErrorMessage(error, "Không thể tải dữ liệu chỉ định cận lâm sàng.");
        setListError(message);
        toast.error(message);
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [categoryFilter, debouncedSearch, pageNumber, pageSize, statusFilter]
  );

  useEffect(() => {
    void fetchData();
  }, [fetchData]);

  const metrics = useMemo(
    () =>
      orders.reduce(
        (acc, item) => {
          acc.total += 1;
          acc[item.category] += 1;
          acc[item.status] += 1;
          return acc;
        },
        {
          total: 0,
          Lab: 0,
          Imaging: 0,
          Requested: 0,
          Completed: 0,
        } as Record<"total" | HospitalClinicalOrderCategory | HospitalClinicalOrderStatus, number>
      ),
    [orders]
  );

  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endItem = totalCount === 0 ? 0 : Math.min(pageNumber * pageSize, totalCount);

  return {
    orders,
    eligibleEncounters,
    catalog,
    isLoading,
    isRefreshing,
    listError,
    searchQuery,
    setSearchQuery,
    categoryFilter,
    setCategoryFilter,
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
