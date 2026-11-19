"use client";

import { Button } from "@/components/ui/Button";
import { HospitalClinicalOrderSummary } from "../types";
import {
  formatDateTime,
  getCategoryClass,
  getCategoryLabel,
  getStatusClass,
  getStatusLabel,
} from "../utils/clinicalOrderFormatters";

interface ClinicalOrderTableRowProps {
  order: HospitalClinicalOrderSummary;
  onOpenDetail: (id: string) => void;
  onOpenLabModal: (order: HospitalClinicalOrderSummary) => void;
  onOpenImagingModal: (order: HospitalClinicalOrderSummary) => void;
}

export function ClinicalOrderTableRow({
  order,
  onOpenDetail,
  onOpenLabModal,
  onOpenImagingModal,
}: ClinicalOrderTableRowProps) {
  return (
    <tr className="border-t border-slate-100 align-top transition-colors hover:bg-violet-50/20">
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">
          {formatDateTime(order.requestedAtLocal)}
        </div>
        <div className="mt-1 text-sm text-slate-500">{order.orderNumber}</div>
        <div className="mt-1 text-xs text-slate-400">
          {order.orderedByUsername || "Không rõ người chỉ định"}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">{order.patientName}</div>
        <div className="mt-1 text-sm text-slate-500">{order.medicalRecordNumber}</div>
        <div className="mt-1 text-xs text-slate-400">{order.encounterNumber}</div>
      </td>
      <td className="px-6 py-4">
        <div className="font-semibold text-slate-900">{order.serviceName}</div>
        <div className="mt-1 text-sm text-slate-500">{order.serviceCode}</div>
        <div className="mt-1 text-xs text-slate-400">
          {order.doctorName} / {order.clinicName}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="flex flex-col gap-2">
          <span
            className={`inline-flex w-fit rounded-full px-3 py-1 text-xs font-bold ${getCategoryClass(
              order.category
            )}`}
          >
            {getCategoryLabel(order.category)}
          </span>
          <span
            className={`inline-flex w-fit rounded-full px-3 py-1 text-xs font-bold ${getStatusClass(
              order.status
            )}`}
          >
            {getStatusLabel(order.status)}
          </span>
          {order.priorityCode && (
            <span className="text-xs text-slate-500">
              Ưu tiên: {order.priorityCode}
            </span>
          )}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="max-w-[320px] text-sm leading-6 text-slate-600">
          {order.summaryText || "Chưa có nội dung kết quả."}
        </div>
        <div className="mt-2 text-xs text-slate-400">
          Số dòng kết quả: {order.resultItemCount}
        </div>
        <div className="mt-1 text-xs text-slate-400">
          Hoàn thành: {formatDateTime(order.completedAtLocal)}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="flex flex-col gap-2">
          <Button
            variant="secondary"
            className="justify-start"
            onClick={() => onOpenDetail(order.clinicalOrderId)}
          >
            Xem chi tiết
          </Button>
          {order.status !== "Completed" && order.category === "Lab" && (
            <Button
              className="justify-start"
              onClick={() => onOpenLabModal(order)}
            >
              Nhập kết quả
            </Button>
          )}
          {order.status !== "Completed" && order.category === "Imaging" && (
            <Button
              className="justify-start"
              onClick={() => onOpenImagingModal(order)}
            >
              Nhập báo cáo
            </Button>
          )}
        </div>
      </td>
    </tr>
  );
}
