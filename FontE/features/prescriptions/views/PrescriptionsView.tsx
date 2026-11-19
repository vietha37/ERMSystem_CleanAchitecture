"use client";

import { useAuth } from "@/hooks/useAuth";
import { ConfirmDialog } from "@/shared/ui/Modal/ConfirmDialog";
import { usePrescriptionsList } from "../hooks/usePrescriptionsList";
import { usePrescriptionActions } from "../hooks/usePrescriptionActions";
import { PrescriptionHeader } from "../components/PrescriptionHeader";
import { PrescriptionMetrics } from "../components/PrescriptionMetrics";
import { PrescriptionTable } from "../components/PrescriptionTable";
import { PrescriptionCreateModal } from "../components/PrescriptionCreateModal";
import { PrescriptionDetailModal } from "../components/PrescriptionDetailModal";

export function PrescriptionsView() {
  const { role } = useAuth();

  const {
    prescriptions,
    availableEncounters,
    medicines,
    isLoading,
    isRefreshing,
    listError,
    searchQuery,
    setSearchQuery,
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
  } = usePrescriptionsList();

  const {
    isSubmitting,
    isCreateModalOpen,
    setIsCreateModalOpen,
    selectedEncounterId,
    setSelectedEncounterId,
    notes,
    setNotes,
    prescriptionItems,
    isDetailModalOpen,
    setIsDetailModalOpen,
    selectedPrescription,
    setSelectedPrescription,
    isConfirmDeleteOpen,
    resetCreateForm,
    handleAddRow,
    handleRemoveRow,
    handleUpdateRow,
    handleOpenDetail,
    requestDelete,
    cancelDelete,
    confirmDelete,
    handleCreatePrescription,
  } = usePrescriptionActions(() => fetchData(true));

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PrescriptionHeader
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
        statusFilter={statusFilter}
        onStatusChange={setStatusFilter}
        isRefreshing={isRefreshing}
        onRefresh={() => void fetchData(true)}
        onCreateOpen={() => setIsCreateModalOpen(true)}
      />

      <PrescriptionMetrics
        metrics={metrics}
        availableEncountersCount={availableEncounters.length}
      />

      <PrescriptionTable
        prescriptions={prescriptions}
        isLoading={isLoading}
        listError={listError}
        role={role}
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
        onOpenDetail={handleOpenDetail}
        onDelete={requestDelete}
      />

      <PrescriptionCreateModal
        isOpen={isCreateModalOpen}
        onClose={() => {
          setIsCreateModalOpen(false);
          resetCreateForm();
        }}
        onSubmit={handleCreatePrescription}
        isSubmitting={isSubmitting}
        selectedEncounterId={selectedEncounterId}
        onEncounterChange={setSelectedEncounterId}
        availableEncounters={availableEncounters}
        notes={notes}
        onNotesChange={setNotes}
        prescriptionItems={prescriptionItems}
        medicines={medicines}
        onAddRow={handleAddRow}
        onRemoveRow={handleRemoveRow}
        onUpdateRow={handleUpdateRow}
      />

      <PrescriptionDetailModal
        isOpen={isDetailModalOpen}
        onClose={() => {
          setIsDetailModalOpen(false);
          setSelectedPrescription(null);
        }}
        selectedPrescription={selectedPrescription}
      />

      <ConfirmDialog
        isOpen={isConfirmDeleteOpen}
        onClose={cancelDelete}
        onConfirm={() => void confirmDelete()}
        title="Xác nhận xóa đơn thuốc"
        message="Bạn có chắc chắn muốn xóa đơn thuốc này không? Hành động này không thể hoàn tác."
        confirmText="Xóa đơn thuốc"
        cancelText="Hủy"
        variant="danger"
      />
    </div>
  );
}
