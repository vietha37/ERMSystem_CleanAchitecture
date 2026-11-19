"use client";

import { useEncountersList } from "../hooks/useEncountersList";
import { useEncounterModalForm } from "../hooks/useEncounterModalForm";
import { MedicalRecordHeader } from "../components/MedicalRecordHeader";
import { MedicalRecordMetrics } from "../components/MedicalRecordMetrics";
import { MedicalRecordTable } from "../components/MedicalRecordTable";
import { MedicalRecordModal } from "../components/MedicalRecordModal";

export function MedicalRecordsView() {
  const {
    encounters,
    eligibleAppointments,
    isLoading,
    isRefreshing,
    listError,
    approvingEncounterId,
    searchQuery,
    setSearchQuery,
    appointmentDate,
    setAppointmentDate,
    statusFilter,
    setStatusFilter,
    pageNumber,
    setPageNumber,
    pageSize,
    setPageSize,
    totalCount,
    totalPages,
    startItem,
    endItem,
    metrics,
    fetchData,
    handleApprove,
  } = useEncountersList();

  const {
    isModalOpen,
    isSubmitting,
    editingEncounterId,
    editingDetail,
    form,
    setForm,
    availableAppointments,
    isApprovedDetail,
    canSignDetail,
    canApproveDetail,
    signatureComment,
    setSignatureComment,
    isSigningEncounter,
    approvalComment,
    setApprovalComment,
    isApprovingModal,
    attachmentDocumentType,
    setAttachmentDocumentType,
    setAttachmentFile,
    isUploadingAttachment,
    openCreateModal,
    openEditModal,
    closeModal,
    handleSubmit,
    handleSignEncounter,
    handleApproveFromModal,
    handleUploadAttachment,
  } = useEncounterModalForm({
    eligibleAppointments,
    onRefresh: () => fetchData(true),
  });

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <MedicalRecordHeader
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
        appointmentDate={appointmentDate}
        onAppointmentDateChange={setAppointmentDate}
        statusFilter={statusFilter}
        onStatusChange={setStatusFilter}
        isRefreshing={isRefreshing}
        onRefresh={() => void fetchData(true)}
        onCreateOpen={openCreateModal}
      />

      <MedicalRecordMetrics
        metrics={metrics}
        availableAppointmentsCount={availableAppointments.length}
      />

      <MedicalRecordTable
        encounters={encounters}
        isLoading={isLoading}
        listError={listError}
        approvingEncounterId={approvingEncounterId}
        startItem={startItem}
        endItem={endItem}
        totalCount={totalCount}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setPageNumber(1);
        }}
        onPageChange={setPageNumber}
        onRetry={() => void fetchData(true)}
        onOpenEdit={openEditModal}
        onApprove={handleApprove}
      />

      <MedicalRecordModal
        isOpen={isModalOpen}
        onClose={closeModal}
        onSubmit={handleSubmit}
        isSubmitting={isSubmitting}
        editingEncounterId={editingEncounterId}
        editingDetail={editingDetail}
        form={form}
        onFormChange={(field, value) =>
          setForm((current) => ({ ...current, [field]: value }))
        }
        availableAppointments={availableAppointments}
        isApprovedDetail={isApprovedDetail}
        canSignDetail={canSignDetail}
        canApproveDetail={canApproveDetail}
        signatureComment={signatureComment}
        onSignatureCommentChange={setSignatureComment}
        isSigningEncounter={isSigningEncounter}
        onSign={handleSignEncounter}
        approvalComment={approvalComment}
        onApprovalCommentChange={setApprovalComment}
        isApprovingModal={isApprovingModal}
        onApprove={handleApproveFromModal}
        attachmentDocumentType={attachmentDocumentType}
        onDocumentTypeChange={setAttachmentDocumentType}
        onFileChange={setAttachmentFile}
        isUploadingAttachment={isUploadingAttachment}
        onUploadAttachment={handleUploadAttachment}
      />
    </div>
  );
}
