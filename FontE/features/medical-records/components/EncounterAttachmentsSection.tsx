"use client";

import toast from "react-hot-toast";
import { Button } from "@/components/ui/Button";
import { getApiErrorMessage } from "@/services/error";
import { hospitalEncounterService } from "@/services/hospitalEncounterService";
import { HospitalEncounterAttachment } from "../types";
import { formatDateTime } from "../utils/medicalRecordFormatters";

interface EncounterAttachmentsSectionProps {
  encounterId: string | null;
  attachments?: HospitalEncounterAttachment[];
  attachmentDocumentType: string;
  onDocumentTypeChange: (val: string) => void;
  onFileChange: (file: File | null) => void;
  isUploading: boolean;
  onUpload: () => void;
}

function AttachmentRow({
  encounterId,
  attachment,
}: {
  encounterId: string;
  attachment: HospitalEncounterAttachment;
}) {
  const handleDownload = async () => {
    try {
      const ticket = await hospitalEncounterService.createAttachmentDownloadTicket(
        encounterId,
        attachment.attachmentId
      );
      if (!ticket.downloadUrl) {
        throw new Error("Download URL is unavailable for this attachment.");
      }
      const link = document.createElement("a");
      link.href = ticket.downloadUrl;
      link.download = attachment.fileName;
      link.rel = "noopener";
      if (ticket.accessMode === "DirectUrl") {
        link.target = "_blank";
      }
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải tài liệu đính kèm."));
    }
  };

  return (
    <div className="rounded-2xl border border-cyan-100 bg-white/90 px-4 py-4">
      <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div className="min-w-0">
          <p className="truncate text-sm font-semibold text-slate-900">
            {attachment.fileName}
          </p>
          <p className="mt-1 text-xs text-slate-500">
            {attachment.documentType} · {attachment.contentType}
          </p>
          <p className="mt-1 text-xs text-slate-500">
            Tải lên {formatDateTime(attachment.uploadedAtLocal)}
            {attachment.uploadedByUsername ? ` · ${attachment.uploadedByUsername}` : ""}
          </p>
        </div>

        <button
          type="button"
          onClick={() => void handleDownload()}
          aria-label={`Tải xuống tài liệu ${attachment.fileName}`}
          className="inline-flex items-center justify-center rounded-2xl border border-cyan-200 px-4 py-2 text-sm font-medium text-cyan-700 transition hover:bg-cyan-50"
        >
          Tải xuống
        </button>
      </div>
    </div>
  );
}

export function EncounterAttachmentsSection({
  encounterId,
  attachments,
  attachmentDocumentType,
  onDocumentTypeChange,
  onFileChange,
  isUploading,
  onUpload,
}: EncounterAttachmentsSectionProps) {
  if (!encounterId) return null;

  return (
    <div className="space-y-4 rounded-[1.5rem] border border-cyan-100 bg-cyan-50/60 p-4">
      <div className="flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
        <div>
          <p className="text-sm font-bold text-cyan-900">Tài liệu đính kèm</p>
          <p className="mt-1 text-xs leading-6 text-cyan-800">
            Tải trực tiếp PDF, hình ảnh hoặc tài liệu văn phòng vào hồ sơ khám.
          </p>
        </div>
        <p className="text-xs text-cyan-800">Giới hạn 20 MB mỗi tệp.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)_auto] md:items-end">
        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Loại tài liệu
          </label>
          <input
            type="text"
            value={attachmentDocumentType}
            onChange={(event) => onDocumentTypeChange(event.target.value)}
            className="w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm outline-none transition focus:border-cyan-500 focus:ring-4 focus:ring-cyan-100"
            placeholder="EncounterAttachment"
          />
        </div>

        <div>
          <label className="mb-2 block text-sm font-medium text-slate-700">
            Chọn tệp
          </label>
          <input
            type="file"
            onChange={(event) => onFileChange(event.target.files?.[0] ?? null)}
            className="block w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700 file:mr-4 file:rounded-xl file:border-0 file:bg-cyan-600 file:px-3 file:py-2 file:text-sm file:font-medium file:text-white"
          />
        </div>

        <Button
          type="button"
          onClick={onUpload}
          disabled={isUploading}
        >
          {isUploading ? "Đang tải..." : "Tải tài liệu"}
        </Button>
      </div>

      <div className="space-y-3">
        {attachments?.length ? (
          attachments.map((attachment) => (
            <AttachmentRow
              key={attachment.attachmentId}
              encounterId={encounterId}
              attachment={attachment}
            />
          ))
        ) : (
          <div className="rounded-2xl border border-dashed border-cyan-200 bg-white/70 px-4 py-4 text-sm text-slate-500">
            Hồ sơ này chưa có tài liệu đính kèm.
          </div>
        )}
      </div>
    </div>
  );
}
