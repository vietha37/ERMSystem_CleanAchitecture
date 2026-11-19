"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { useAuth } from "@/hooks/useAuth";
import { useTranslation } from "@/hooks/useTranslation";
import { getApiErrorMessage } from "@/services/error";
import { hospitalDoctorService } from "@/services/hospitalDoctorService";
import { hospitalDoctorWorklistService } from "@/services/hospitalDoctorWorklistService";
import { HospitalDoctorWorklistResponse } from "@/services/types";
import { toDateInputValue } from "../utils/doctorWorklistFormatters";

type DoctorOption = Awaited<ReturnType<typeof hospitalDoctorService.getAll>>[number];

export function useDoctorWorklist() {
  const { role } = useAuth();
  const { t } = useTranslation();
  const [workDate, setWorkDate] = useState("");
  const [doctorFilter, setDoctorFilter] = useState("");
  const [doctors, setDoctors] = useState<DoctorOption[]>([]);
  const [worklist, setWorklist] = useState<HospitalDoctorWorklistResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [listError, setListError] = useState<string | null>(null);

  const canSelectDoctor = role === "Admin";

  useEffect(() => {
    setWorkDate(toDateInputValue(new Date()));
  }, []);

  const fetchPageData = useCallback(
    async (showRefreshState = false) => {
      if (!workDate) return;

      if (showRefreshState) {
        setIsRefreshing(true);
      } else {
        setIsLoading(true);
      }

      try {
        const [doctorData, worklistData] = await Promise.all([
          canSelectDoctor ? hospitalDoctorService.getAll() : Promise.resolve([]),
          hospitalDoctorWorklistService.get(
            workDate,
            canSelectDoctor ? doctorFilter || undefined : undefined
          ),
        ]);

        setDoctors(doctorData);
        setWorklist(worklistData);
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
    [canSelectDoctor, doctorFilter, workDate, t]
  );

  useEffect(() => {
    if (!workDate) return;
    void fetchPageData();
  }, [fetchPageData, workDate]);

  const items = useMemo(() => worklist?.items ?? [], [worklist]);

  const groupedSummary = useMemo(() => {
    return items.reduce<Record<string, number>>((acc, item) => {
      acc[item.workflowStage] = (acc[item.workflowStage] ?? 0) + 1;
      return acc;
    }, {});
  }, [items]);

  return {
    t,
    canSelectDoctor,
    workDate,
    setWorkDate,
    doctorFilter,
    setDoctorFilter,
    doctors,
    worklist,
    items,
    groupedSummary,
    isLoading,
    isRefreshing,
    listError,
    fetchPageData,
  };
}
