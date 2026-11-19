"use client";

import { useCallback, useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useTranslation } from "@/hooks/useTranslation";
import { getApiErrorMessage } from "@/services/error";
import { staffUserService } from "@/services/staffUserService";
import { StaffRoleFilter, StaffUser } from "../types";

export function useStaffList() {
  const { t } = useTranslation();
  const [items, setItems] = useState<StaffUser[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [isSyncing, setIsSyncing] = useState(false);

  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);

  const [roleFilter, setRoleFilter] = useState<StaffRoleFilter>("");
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  // Delete modal state replacing window.confirm
  const [userToDelete, setUserToDelete] = useState<StaffUser | null>(null);
  const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPageNumber(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [search]);

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await staffUserService.getAll(pageNumber, pageSize, roleFilter, debouncedSearch);
      setItems(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages || 1);
      setListError(null);
    } catch (error) {
      const message = getApiErrorMessage(error, t("common.messages.error"));
      setListError(message);
      setItems([]);
      setTotalCount(0);
      setTotalPages(1);
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  }, [pageNumber, pageSize, roleFilter, debouncedSearch, t]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleSyncHospitalIdentity = async () => {
    setIsSyncing(true);
    try {
      const res = await staffUserService.syncHospitalIdentity();
      toast.success(
        t("common.messages.syncSuccess", {
          synced: res.syncedUsers,
          total: res.totalUsers,
        })
      );
      fetchData();
    } catch (error) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setIsSyncing(false);
    }
  };

  const requestDelete = (user: StaffUser) => {
    setUserToDelete(user);
    setIsConfirmDeleteOpen(true);
  };

  const cancelDelete = () => {
    setUserToDelete(null);
    setIsConfirmDeleteOpen(false);
  };

  const confirmDelete = async () => {
    if (!userToDelete) return;

    setIsDeleting(true);
    try {
      await staffUserService.delete(userToDelete.id);
      toast.success(t("common.messages.deleteSuccess"));
      cancelDelete();
      if (items.length === 1 && pageNumber > 1) {
        setPageNumber((current) => current - 1);
      } else {
        fetchData();
      }
    } catch (error) {
      toast.error(getApiErrorMessage(error, t("common.messages.error")));
    } finally {
      setIsDeleting(false);
    }
  };

  return {
    t,
    items,
    isLoading,
    listError,
    isSyncing,
    pageNumber,
    setPageNumber,
    pageSize,
    setPageSize,
    totalPages,
    totalCount,
    roleFilter,
    setRoleFilter,
    search,
    setSearch,
    userToDelete,
    isConfirmDeleteOpen,
    isDeleting,
    fetchData,
    handleSyncHospitalIdentity,
    requestDelete,
    cancelDelete,
    confirmDelete,
  };
}
