"use client";

import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import {
  EncounterFormState,
  HospitalEncounterDetail,
  HospitalEncounterEligibleAppointment,
} from "../types";
import { EncounterAttachmentsSection } from "./EncounterAttachmentsSection";
import { EncounterBasicFields } from "./EncounterBasicFields";
import { EncounterSoapFields } from "./EncounterSoapFields";
import { EncounterVitalSignsFields } from "./EncounterVitalSignsFields";
import { EncounterWorkflowSection } from "./EncounterWorkflowSection";

interface MedicalRecordModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (e: React.FormEvent) => void;
  isSubmitting: boolean;
  editingEncounterId: string | null;
  editingDetail: HospitalEncounterDetail | null;
  form: EncounterFormState;
  onFormChange: (field: keyof EncounterFormState, value: string) => void;
  availableAppointments: HospitalEncounterEligibleAppointment[];
  isApprovedDetail: boolean;
  canSignDetail: boolean;
  canApproveDetail: boolean;
  signatureComment: string;
  onSignatureCommentChange: (val: string) => void;
  isSigningEncounter: boolean;
  onSign: () => void;
  approvalComment: string;
  onApprovalCommentChange: (val: string) => void;
  isApprovingModal: boolean;
  onApprove: () => void;
  attachmentDocumentType: string;
  onDocumentTypeChange: (val: string) => void;
  onFileChange: (file: File | null) => void;
  isUploadingAttachment: boolean;
  onUploadAttachment: () => void;
}

export function MedicalRecordModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
  editingEncounterId,
  editingDetail,
  form,
  onFormChange,
  availableAppointments,
  isApprovedDetail,
  canSignDetail,
  canApproveDetail,
  signatureComment,
  onSignatureCommentChange,
  isSigningEncounter,
  onSign,
  approvalComment,
  onApprovalCommentChange,
  isApprovingModal,
  onApprove,
  attachmentDocumentType,
  onDocumentTypeChange,
  onFileChange,
  isUploadingAttachment,
  onUploadAttachment,
}: MedicalRecordModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={editingEncounterId ? "Cập nhật hồ sơ khám" : "Mở hồ sơ khám mới"}
    >
      <form onSubmit={onSubmit} className="space-y-5">
        <EncounterBasicFields
          form={form}
          onChange={onFormChange}
          availableAppointments={availableAppointments}
          isEditing={Boolean(editingEncounterId)}
          disabled={isApprovedDetail}
        />

        <EncounterSoapFields
          form={form}
          onChange={onFormChange}
          disabled={isApprovedDetail}
        />

        <EncounterVitalSignsFields
          form={form}
          onChange={onFormChange}
          disabled={isApprovedDetail}
        />

        {editingEncounterId && editingDetail && (
          <EncounterWorkflowSection
            editingDetail={editingDetail}
            canSign={canSignDetail}
            isSigning={isSigningEncounter}
            signatureComment={signatureComment}
            onSignatureCommentChange={onSignatureCommentChange}
            onSign={() => void onSign()}
            canApprove={canApproveDetail}
            isApproving={isApprovingModal}
            approvalComment={approvalComment}
            onApprovalCommentChange={onApprovalCommentChange}
            onApprove={() => void onApprove()}
          />
        )}

        <EncounterAttachmentsSection
          encounterId={editingEncounterId}
          attachments={editingDetail?.attachments}
          attachmentDocumentType={attachmentDocumentType}
          onDocumentTypeChange={onDocumentTypeChange}
          onFileChange={onFileChange}
          isUploading={isUploadingAttachment}
          onUpload={() => void onUploadAttachment()}
        />

        <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
          <Button type="button" variant="secondary" onClick={onClose}>
            Đóng
          </Button>
          {!isApprovedDetail && (
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting
                ? "Đang lưu..."
                : editingEncounterId
                  ? "Cập nhật hồ sơ"
                  : "Tạo hồ sơ"}
            </Button>
          )}
        </div>
      </form>
    </Modal>
  );
}
