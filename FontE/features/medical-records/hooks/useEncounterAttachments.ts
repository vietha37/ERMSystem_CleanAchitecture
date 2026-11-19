"use client";

import { useState } from "react";
import toast from "react-hot-toast";
import { getApiErrorMessage } from "@/services/error";
import { hospitalEncounterService } from "@/services/hospitalEncounterService";
import { HospitalEncounterDetail } from "../types";

export function useEncounterAttachments(
  onUpdateDetail: (detail: HospitalEncounterDetail) => void,
  onRefresh: () => Promise<void>
) {
  const [attachmentDocumentType, setAttachmentDocumentType] = useState("EncounterAttachment");
  const [attachmentFile, setAttachmentFile] = useState<File | null>(null);
  const [isUploadingAttachment, setIsUploadingAttachment] = useState(false);

  const handleUploadAttachment = async (encounterId: string | null) => {
    if (!encounterId) {
      toast.error("Cần mở hồ sơ khám trước khi tải tài liệu.");
      return;
    }
    if (!attachmentFile) {
      toast.error("Vui lòng chọn tệp cần tải lên.");
      return;
    }

    setIsUploadingAttachment(true);
    try {
      const updated = await hospitalEncounterService.uploadAttachment(
        encounterId,
        attachmentFile,
        attachmentDocumentType.trim() || "EncounterAttachment"
      );
      onUpdateDetail(updated);
      setAttachmentFile(null);
      setAttachmentDocumentType("EncounterAttachment");
      toast.success("Đã tải tài liệu đính kèm.");
      await onRefresh();
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải tài liệu đính kèm."));
    } finally {
      setIsUploadingAttachment(false);
    }
  };

  const resetAttachments = () => {
    setAttachmentDocumentType("EncounterAttachment");
    setAttachmentFile(null);
  };

  return {
    attachmentDocumentType,
    setAttachmentDocumentType,
    attachmentFile,
    setAttachmentFile,
    isUploadingAttachment,
    handleUploadAttachment,
    resetAttachments,
  };
}
