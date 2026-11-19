"use client";

import { Button } from "@/components/ui/Button";
import { HospitalEncounterDetail } from "../types";
import { formatDateTime } from "../utils/medicalRecordFormatters";

interface EncounterWorkflowSectionProps {
  editingDetail: HospitalEncounterDetail;
  canSign: boolean;
  isSigning: boolean;
  signatureComment: string;
  onSignatureCommentChange: (val: string) => void;
  onSign: () => void;
  canApprove: boolean;
  isApproving: boolean;
  approvalComment: string;
  onApprovalCommentChange: (val: string) => void;
  onApprove: () => void;
}

function InfoTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/80 bg-white/80 px-4 py-4">
      <p className="text-xs font-medium uppercase tracking-[0.18em] text-slate-500">
        {label}
      </p>
      <p className="mt-2 text-sm font-semibold text-slate-900">{value}</p>
    </div>
  );
}

export function EncounterWorkflowSection({
  editingDetail,
  canSign,
  isSigning,
  signatureComment,
  onSignatureCommentChange,
  onSign,
  canApprove,
  isApproving,
  approvalComment,
  onApprovalCommentChange,
  onApprove,
}: EncounterWorkflowSectionProps) {
  return (
    <div className="space-y-4 rounded-[1.5rem] border border-emerald-100 bg-emerald-50/60 p-4">
      <div className="grid gap-3 md:grid-cols-2">
        <InfoTile
          label="Ký lâm sàng"
          value={
            editingDetail.isClinicalNoteSigned
              ? `${formatDateTime(editingDetail.clinicalNoteSignedAtLocal)}${
                  editingDetail.clinicalNoteSignedByUsername
                    ? ` · ${editingDetail.clinicalNoteSignedByUsername}`
                    : ""
                }`
              : "Chưa ký"
          }
        />
        <InfoTile
          label="Phê duyệt"
          value={
            editingDetail.isApproved
              ? `${formatDateTime(editingDetail.approvalSignedAtLocal)}${
                  editingDetail.approvedByUsername
                    ? ` · ${editingDetail.approvedByUsername}`
                    : ""
                }`
              : "Chưa duyệt"
          }
        />
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div className="rounded-2xl border border-white/80 bg-white/80 p-4">
          <p className="text-sm font-bold text-slate-900">Ký xác nhận hồ sơ</p>
          <textarea
            rows={3}
            value={signatureComment}
            onChange={(event) => onSignatureCommentChange(event.target.value)}
            placeholder="Nội dung xác nhận hoặc ghi chú chữ ký..."
            disabled={!canSign || isSigning}
            className="mt-3 w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100 disabled:bg-slate-100"
          />
          <div className="mt-3 flex justify-end">
            <Button
              type="button"
              variant="secondary"
              onClick={onSign}
              disabled={!canSign || isSigning}
            >
              {isSigning ? "Đang ký..." : "Ký xác nhận"}
            </Button>
          </div>
        </div>

        <div className="rounded-2xl border border-white/80 bg-white/80 p-4">
          <p className="text-sm font-bold text-slate-900">Duyệt hồ sơ</p>
          <textarea
            rows={3}
            value={approvalComment}
            onChange={(event) => onApprovalCommentChange(event.target.value)}
            placeholder="Ghi chú phê duyệt, phạm vi duyệt hoặc nhận xét..."
            disabled={!canApprove || isApproving}
            className="mt-3 w-full rounded-2xl border border-slate-200 px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100 disabled:bg-slate-100"
          />
          <div className="mt-3 flex justify-end">
            <Button
              type="button"
              onClick={onApprove}
              disabled={!canApprove || isApproving}
            >
              {isApproving ? "Đang duyệt..." : "Duyệt hồ sơ"}
            </Button>
          </div>
        </div>
      </div>

      <div className="space-y-3">
        <p className="text-sm font-bold text-slate-900">Tiến trình xử lý</p>
        {editingDetail.workflowEvents.length > 0 ? (
          editingDetail.workflowEvents.map((event, index) => (
            <div
              key={`${event.eventType}-${event.occurredAtLocal}-${index}`}
              className="rounded-2xl border border-white/80 bg-white/80 px-4 py-4"
            >
              <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
                <div>
                  <p className="text-sm font-semibold text-slate-900">{event.label}</p>
                  <p className="mt-1 text-xs text-slate-500">
                    {formatDateTime(event.occurredAtLocal)}
                    {event.performedByUsername ? ` · ${event.performedByUsername}` : ""}
                  </p>
                </div>
                <span className="inline-flex rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-medium text-slate-600">
                  {event.eventType}
                </span>
              </div>
              {event.comment ? (
                <p className="mt-3 text-sm leading-6 text-slate-700">{event.comment}</p>
              ) : null}
            </div>
          ))
        ) : (
          <div className="rounded-2xl border border-dashed border-emerald-200 bg-white/70 px-4 py-4 text-sm text-slate-500">
            Chưa có cập nhật tiến trình.
          </div>
        )}
      </div>
    </div>
  );
}
