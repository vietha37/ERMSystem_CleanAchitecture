"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { HospitalClinicalOrderSummary } from "../types";

interface ClinicalOrderImagingModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (e: React.FormEvent) => void;
  isSubmitting: boolean;
  imagingReportTarget: HospitalClinicalOrderSummary | null;
  findings: string;
  onFindingsChange: (val: string) => void;
  impression: string;
  onImpressionChange: (val: string) => void;
  reportUri: string;
  onReportUriChange: (val: string) => void;
}

export function ClinicalOrderImagingModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
  imagingReportTarget,
  findings,
  onFindingsChange,
  impression,
  onImpressionChange,
  reportUri,
  onReportUriChange,
}: ClinicalOrderImagingModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Nhập báo cáo chẩn đoán hình ảnh"
    >
      <form className="space-y-4" onSubmit={onSubmit}>
        {imagingReportTarget && (
          <div className="rounded-2xl border border-violet-100 bg-violet-50 px-4 py-3 text-sm text-violet-800">
            {imagingReportTarget.orderNumber} - {imagingReportTarget.patientName} -{" "}
            {imagingReportTarget.serviceName}
          </div>
        )}

        <div>
          <label className="mb-2 block text-sm font-semibold text-slate-700">
            Mô tả
          </label>
          <textarea
            value={findings}
            onChange={(event) => onFindingsChange(event.target.value)}
            rows={5}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
            placeholder="Mô tả chi tiết kết quả chẩn đoán hình ảnh..."
          />
        </div>

        <div>
          <label className="mb-2 block text-sm font-semibold text-slate-700">
            Kết luận
          </label>
          <textarea
            value={impression}
            onChange={(event) => onImpressionChange(event.target.value)}
            rows={4}
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
            placeholder="Kết luận chẩn đoán..."
          />
        </div>

        <div>
          <label className="mb-2 block text-sm font-semibold text-slate-700">
            Liên kết báo cáo
          </label>
          <input
            type="text"
            value={reportUri}
            onChange={(event) => onReportUriChange(event.target.value)}
            placeholder="https://... hoặc đường dẫn nội bộ"
            className="w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-violet-500 focus:ring-4 focus:ring-violet-100"
          />
        </div>

        <div className="flex justify-end gap-3 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Đóng
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Đang lưu..." : "Lưu báo cáo"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
