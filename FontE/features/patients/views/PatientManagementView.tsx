"use client";

import React from "react";
import { Card } from "@/components/ui/Card";
import { ConfirmDialog } from "@/shared/ui";
import { PatientHeaderMetrics } from "../components/PatientHeaderMetrics";
import { PatientSearchFilter } from "../components/PatientSearchFilter";
import { PatientTable } from "../components/PatientTable";
import { PatientPagination } from "../components/PatientPagination";
import { PatientDetailDrawer } from "../components/PatientDetailDrawer";
import { PatientFormModal } from "../components/PatientFormModal";
import { usePatientsList } from "../hooks/usePatientsList";
import { usePatientDrawer } from "../hooks/usePatientDrawer";
import { usePatientForm } from "../hooks/usePatientForm";

export function PatientManagementView() {
  const {
    isDoctor,
    patients,
    isLoading,
    listError,
    searchQuery,
    setSearchQuery,
    debouncedSearch,
    currentPage,
    setCurrentPage,
    pageSize,
    setPageSize,
    totalPages,
    totalItems,
    patientMetrics,
    fetchPatients,
    deletingPatientId,
    isDeleting,
    requestDelete,
    cancelDelete,
    confirmDelete,
  } = usePatientsList();

  const {
    isDrawerOpen,
    selectedPatient,
    drawerData,
    latestDiagnosis,
    openDrawer,
    closeDrawer,
  } = usePatientDrawer();

  const {
    isModalOpen,
    modalMode,
    fullName,
    setFullName,
    formData,
    setFormData,
    isSubmitting,
    openCreateModal,
    openEditModal,
    closeModal,
    handleSubmit,
  } = usePatientForm(isDoctor);

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <PatientHeaderMetrics
        isDoctor={isDoctor}
        metrics={patientMetrics}
        onOpenCreateModal={openCreateModal}
      />

      <Card className="rounded-2xl border-none bg-white p-6 shadow-sm">
        <PatientSearchFilter
          searchQuery={searchQuery}
          onSearchChange={setSearchQuery}
          pageSize={pageSize}
          onPageSizeChange={(newSize) => {
            setPageSize(newSize);
            setCurrentPage(1);
          }}
        />

        <PatientTable
          patients={patients}
          isLoading={isLoading}
          listError={listError}
          debouncedSearch={debouncedSearch}
          isDoctor={isDoctor}
          onRetry={() => void fetchPatients()}
          onSelectPatient={openDrawer}
          onEditPatient={openEditModal}
          onDeletePatient={requestDelete}
        />

        {!isLoading && (
          <PatientPagination
            currentPage={currentPage}
            pageSize={pageSize}
            totalPages={totalPages}
            totalItems={totalItems}
            onPageChange={setCurrentPage}
          />
        )}
      </Card>

      <PatientDetailDrawer
        isOpen={isDrawerOpen}
        patient={selectedPatient}
        drawerData={drawerData}
        latestDiagnosis={latestDiagnosis}
        isDoctor={isDoctor}
        onClose={closeDrawer}
      />

      {!isDoctor && (
        <PatientFormModal
          isOpen={isModalOpen}
          modalMode={modalMode}
          fullName={fullName}
          onFullNameChange={setFullName}
          formData={formData}
          onFormDataChange={setFormData}
          isSubmitting={isSubmitting}
          onSubmit={(e) => handleSubmit(e, fetchPatients)}
          onClose={closeModal}
        />
      )}

      <ConfirmDialog
        isOpen={Boolean(deletingPatientId)}
        title="Xác nhận xóa hồ sơ bệnh nhân"
        message="Bạn có chắc muốn xóa hồ sơ bệnh nhân này không? Thao tác này không thể hoàn tác."
        confirmText="Xóa bệnh nhân"
        cancelText="Hủy"
        variant="danger"
        isLoading={isDeleting}
        onConfirm={() =>
          confirmDelete((deletedId) => {
            if (selectedPatient?.id === deletedId) {
              closeDrawer();
            }
          })
        }
        onClose={cancelDelete}
      />
    </div>
  );
}
