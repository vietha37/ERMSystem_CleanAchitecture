"use client";

import { useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalClinicalOrderService } from "@/services/hospitalClinicalOrderService";
import {
  EMPTY_LAB_RESULT_ITEM,
  HospitalClinicalOrderCatalogItem,
  HospitalClinicalOrderDetail,
  HospitalClinicalOrderSummary,
  RecordHospitalLabResultItemPayload,
} from "../types";
import { buildLabResultItems } from "../utils/clinicalOrderFormatters";
import { useClinicalOrderCreateForm } from "./useClinicalOrderCreateForm";

interface UseClinicalOrderActionsProps {
  catalog: HospitalClinicalOrderCatalogItem[];
  onRefresh: () => Promise<void>;
}

export function useClinicalOrderActions({
  catalog,
  onRefresh,
}: UseClinicalOrderActionsProps) {
  const createForm = useClinicalOrderCreateForm({ catalog, onRefresh });
  const [isResultSubmitting, setIsResultSubmitting] = useState(false);

  // Detail Modal
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [selectedDetail, setSelectedDetail] = useState<HospitalClinicalOrderDetail | null>(null);

  // Lab Modal
  const [isLabResultModalOpen, setIsLabResultModalOpen] = useState(false);
  const [labResultTarget, setLabResultTarget] = useState<HospitalClinicalOrderSummary | null>(null);
  const [specimenCode, setSpecimenCode] = useState("");
  const [labResultItems, setLabResultItems] = useState<RecordHospitalLabResultItemPayload[]>([
    EMPTY_LAB_RESULT_ITEM,
  ]);

  // Imaging Modal
  const [isImagingReportModalOpen, setIsImagingReportModalOpen] = useState(false);
  const [imagingReportTarget, setImagingReportTarget] = useState<HospitalClinicalOrderSummary | null>(null);
  const [findings, setFindings] = useState("");
  const [impression, setImpression] = useState("");
  const [reportUri, setReportUri] = useState("");

  const resetLabResultForm = () => {
    setSpecimenCode("");
    setLabResultItems([EMPTY_LAB_RESULT_ITEM]);
  };

  const resetImagingReportForm = () => {
    setFindings("");
    setImpression("");
    setReportUri("");
  };

  const openDetail = async (clinicalOrderId: string) => {
    try {
      const detail = await hospitalClinicalOrderService.getById(clinicalOrderId);
      setSelectedDetail(detail);
      setIsDetailModalOpen(true);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải chi tiết chỉ định."));
    }
  };

  const openLabResultModal = (order: HospitalClinicalOrderSummary) => {
    setLabResultTarget(order);
    resetLabResultForm();
    setIsLabResultModalOpen(true);
  };

  const handleAddLabResultRow = () => {
    setLabResultItems((current) => [...current, { ...EMPTY_LAB_RESULT_ITEM }]);
  };

  const handleRemoveLabResultRow = (index: number) => {
    setLabResultItems((current) =>
      current.length === 1 ? current : current.filter((_, itemIndex) => itemIndex !== index)
    );
  };

  const handleUpdateLabResultRow = (
    index: number,
    field: keyof RecordHospitalLabResultItemPayload,
    value: string
  ) => {
    setLabResultItems((current) =>
      current.map((item, itemIndex) =>
        itemIndex === index ? { ...item, [field]: value } : item
      )
    );
  };

  const handleRecordLabResult = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!labResultTarget) {
      toast.error("Không xác định được chỉ định xét nghiệm.");
      return;
    }
    const resultItems = buildLabResultItems(labResultItems);
    if (resultItems.length === 0) {
      toast.error("Cần có ít nhất một dòng kết quả xét nghiệm hợp lệ.");
      return;
    }

    setIsResultSubmitting(true);
    try {
      const updated = await hospitalClinicalOrderService.recordLabResult(
        labResultTarget.clinicalOrderId,
        {
          specimenCode: specimenCode.trim() || undefined,
          resultItems,
        }
      );
      toast.success("Đã ghi nhận kết quả xét nghiệm.");
      setIsLabResultModalOpen(false);
      setLabResultTarget(null);
      resetLabResultForm();
      if (selectedDetail?.clinicalOrderId === updated.clinicalOrderId) {
        setSelectedDetail(updated);
      }
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể ghi nhận kết quả xét nghiệm."));
    } finally {
      setIsResultSubmitting(false);
    }
  };

  const openImagingReportModal = (order: HospitalClinicalOrderSummary) => {
    setImagingReportTarget(order);
    resetImagingReportForm();
    setIsImagingReportModalOpen(true);
  };

  const handleRecordImagingReport = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!imagingReportTarget) {
      toast.error("Không xác định được chỉ định chẩn đoán hình ảnh.");
      return;
    }
    if (!findings.trim() && !impression.trim()) {
      toast.error("Cần có mô tả hoặc kết luận để lưu báo cáo.");
      return;
    }

    setIsResultSubmitting(true);
    try {
      const updated = await hospitalClinicalOrderService.recordImagingReport(
        imagingReportTarget.clinicalOrderId,
        {
          findings: findings.trim() || undefined,
          impression: impression.trim() || undefined,
          reportUri: reportUri.trim() || undefined,
        }
      );
      toast.success("Đã ghi nhận báo cáo chẩn đoán hình ảnh.");
      setIsImagingReportModalOpen(false);
      setImagingReportTarget(null);
      resetImagingReportForm();
      if (selectedDetail?.clinicalOrderId === updated.clinicalOrderId) {
        setSelectedDetail(updated);
      }
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể ghi nhận báo cáo."));
    } finally {
      setIsResultSubmitting(false);
    }
  };

  return {
    ...createForm,
    isSubmitting: createForm.isSubmitting || isResultSubmitting,
    isDetailModalOpen,
    setIsDetailModalOpen,
    isLabResultModalOpen,
    setIsLabResultModalOpen,
    isImagingReportModalOpen,
    setIsImagingReportModalOpen,
    selectedDetail,
    setSelectedDetail,
    labResultTarget,
    imagingReportTarget,
    specimenCode,
    setSpecimenCode,
    labResultItems,
    findings,
    setFindings,
    impression,
    setImpression,
    reportUri,
    setReportUri,
    resetLabResultForm,
    resetImagingReportForm,
    openDetail,
    openLabResultModal,
    handleAddLabResultRow,
    handleRemoveLabResultRow,
    handleUpdateLabResultRow,
    handleRecordLabResult,
    openImagingReportModal,
    handleRecordImagingReport,
  };
}
