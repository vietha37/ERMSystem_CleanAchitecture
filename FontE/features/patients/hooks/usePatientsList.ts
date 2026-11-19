"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { useAuth } from "@/hooks/useAuth";
import { getApiErrorMessage } from "@/services/error";
import { hospitalAppointmentWorklistService } from "@/services/hospitalAppointmentWorklistService";
import { hospitalEncounterService } from "@/services/hospitalEncounterService";
import { patientService } from "@/services/patientService";
import { PatientMetrics, PatientViewModel } from "../types";
import {
  filterDoctorPatients,
  mapDoctorPatients,
} from "../utils/doctorPatientHelpers";

export function usePatientsList() {
  const { role } = useAuth();
  const isDoctor = role === "Doctor";

  const [patients, setPatients] = useState<PatientViewModel[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [listError, setListError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);

  const [deletingPatientId, setDeletingPatientId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchQuery);
      setCurrentPage(1);
    }, 400);

    return () => clearTimeout(handler);
  }, [searchQuery]);

  const fetchPatients = useCallback(async () => {
    setIsLoading(true);

    try {
      if (isDoctor) {
        const [appointmentData, encounterData] = await Promise.all([
          hospitalAppointmentWorklistService.getAll({
            pageNumber: 1,
            pageSize: 100,
            textSearch: debouncedSearch || undefined,
          }),
          hospitalEncounterService.getAll({
            pageNumber: 1,
            pageSize: 100,
            textSearch: debouncedSearch || undefined,
          }),
        ]);

        const allPatients = mapDoctorPatients(
          appointmentData.items,
          encounterData.items
        );
        const filtered = filterDoctorPatients(allPatients, debouncedSearch);
        const startIndex = (currentPage - 1) * pageSize;
        const paged = filtered.slice(startIndex, startIndex + pageSize);

        setPatients(paged);
        setTotalItems(filtered.length);
        setTotalPages(Math.max(1, Math.ceil(filtered.length / pageSize)));
        setListError(null);
        return;
      }

      const data = await patientService.getAll(currentPage, pageSize, debouncedSearch);
      const mapped: PatientViewModel[] = data.items.map((patient) => ({
        ...patient,
        status: "Đang theo dõi",
      }));

      setPatients(mapped);
      setTotalPages(Math.max(1, Math.ceil((data.totalCount || 0) / pageSize)));
      setTotalItems(data.totalCount || 0);
      setListError(null);
    } catch (error: unknown) {
      const msg = getApiErrorMessage(error, "Không thể tải danh sách bệnh nhân.");
      setListError(msg);
      toast.error(msg);
      setPatients([]);
      setTotalPages(1);
      setTotalItems(0);
    } finally {
      setIsLoading(false);
    }
  }, [currentPage, debouncedSearch, isDoctor, pageSize]);

  useEffect(() => {
    fetchPatients();
  }, [fetchPatients]);

  const requestDelete = (id: string, event?: React.MouseEvent) => {
    event?.stopPropagation();

    if (isDoctor) {
      toast.error("Bác sĩ không có quyền xóa hồ sơ bệnh nhân.");
      return;
    }

    setDeletingPatientId(id);
  };

  const cancelDelete = () => setDeletingPatientId(null);

  const confirmDelete = async (onDeleted?: (id: string) => void) => {
    if (!deletingPatientId) return;

    setIsDeleting(true);
    const id = deletingPatientId;

    try {
      await patientService.delete(id);
      toast.success("Đã xóa hồ sơ bệnh nhân.");
      setDeletingPatientId(null);

      if (patients.length === 1 && currentPage > 1) {
        setCurrentPage((value) => value - 1);
      } else {
        await fetchPatients();
      }

      onDeleted?.(id);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể xóa hồ sơ bệnh nhân."));
    } finally {
      setIsDeleting(false);
    }
  };

  const patientMetrics: PatientMetrics = useMemo(() => {
    const activePatients = patients.filter(
      (patient) => patient.status !== "Tạm dừng"
    ).length;
    const withRecentVisit = patients.filter((patient) => patient.latestVisitDate).length;

    return { activePatients, withRecentVisit };
  }, [patients]);

  return {
    isDoctor,
    patients,
    isLoading,
    listError,
    searchQuery,
    setSearchQuery,
    debouncedSearch,
    currentPage,
    setCurrentPage,
    pageSize,
    setPageSize,
    totalPages,
    totalItems,
    patientMetrics,
    fetchPatients,
    deletingPatientId,
    isDeleting,
    requestDelete,
    cancelDelete,
    confirmDelete,
  };
}
