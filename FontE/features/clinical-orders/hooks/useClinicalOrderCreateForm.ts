"use client";

import { useEffect, useMemo, useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalClinicalOrderService } from "@/services/hospitalClinicalOrderService";
import {
  HospitalClinicalOrderCatalogItem,
  HospitalClinicalOrderCategory,
} from "../types";
import { buildCreatePayload } from "../utils/clinicalOrderFormatters";

interface UseClinicalOrderCreateFormProps {
  catalog: HospitalClinicalOrderCatalogItem[];
  onRefresh: () => Promise<void>;
}

export function useClinicalOrderCreateForm({
  catalog,
  onRefresh,
}: UseClinicalOrderCreateFormProps) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [selectedEncounterId, setSelectedEncounterId] = useState("");
  const [createCategory, setCreateCategory] = useState<HospitalClinicalOrderCategory>("Lab");
  const [selectedServiceId, setSelectedServiceId] = useState("");
  const [priorityCode, setPriorityCode] = useState("Routine");

  const availableServices = useMemo(
    () => catalog.filter((item) => item.category === createCategory),
    [catalog, createCategory]
  );

  useEffect(() => {
    setSelectedServiceId((current) => {
      if (availableServices.some((item) => item.serviceId === current)) {
        return current;
      }
      return availableServices[0]?.serviceId ?? "";
    });
  }, [availableServices]);

  const resetCreateForm = () => {
    setSelectedEncounterId("");
    setCreateCategory("Lab");
    setSelectedServiceId("");
    setPriorityCode("Routine");
  };

  const handleCreateOrder = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selectedEncounterId) {
      toast.error("Cần chọn hồ sơ khám để tạo chỉ định.");
      return;
    }
    if (!selectedServiceId) {
      toast.error("Cần chọn dịch vụ cận lâm sàng.");
      return;
    }

    setIsSubmitting(true);
    try {
      await hospitalClinicalOrderService.create(
        buildCreatePayload(selectedEncounterId, createCategory, selectedServiceId, priorityCode)
      );
      toast.success("Đã tạo chỉ định cận lâm sàng.");
      setIsCreateModalOpen(false);
      resetCreateForm();
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tạo chỉ định."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    isSubmitting,
    isCreateModalOpen,
    setIsCreateModalOpen,
    selectedEncounterId,
    setSelectedEncounterId,
    createCategory,
    setCreateCategory,
    selectedServiceId,
    setSelectedServiceId,
    priorityCode,
    setPriorityCode,
    availableServices,
    resetCreateForm,
    handleCreateOrder,
  };
}
