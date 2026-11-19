"use client";

import { ConfirmDialog } from "@/shared/ui/Modal/ConfirmDialog";
import { useStaffList } from "../hooks/useStaffList";
import { useStaffForm } from "../hooks/useStaffForm";
import { StaffHeader } from "../components/StaffHeader";
import { StaffTable } from "../components/StaffTable";
import { StaffModal } from "../components/StaffModal";

export function StaffManagementView() {
  const {
    t,
    items,
    isLoading,
    listError,
    isSyncing,
    pageNumber,
    setPageNumber,
    pageSize,
    setPageSize,
    totalPages,
    totalCount,
    roleFilter,
    setRoleFilter,
    search,
    setSearch,
    userToDelete,
    isConfirmDeleteOpen,
    isDeleting,
    fetchData,
    handleSyncHospitalIdentity,
    requestDelete,
    cancelDelete,
    confirmDelete,
  } = useStaffList();

  const {
    isModalOpen,
    mode,
    form,
    setForm,
    showPassword,
    setShowPassword,
    isSubmitting,
    titleByMode,
    openCreateModal,
    openEditModal,
    closeModal,
    handleSubmit,
  } = useStaffForm(() => fetchData());

  return (
    <div className="space-y-6">
      <StaffHeader
        isSyncing={isSyncing}
        onSync={() => void handleSyncHospitalIdentity()}
        onCreateOpen={openCreateModal}
        t={t}
      />

      <StaffTable
        items={items}
        isLoading={isLoading}
        listError={listError}
        search={search}
        onSearchChange={setSearch}
        roleFilter={roleFilter}
        onRoleFilterChange={setRoleFilter}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalPages={totalPages}
        totalCount={totalCount}
        onPageChange={setPageNumber}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setPageNumber(1);
        }}
        onRetry={() => void fetchData()}
        onEdit={openEditModal}
        onDelete={requestDelete}
        t={t}
      />

      <StaffModal
        isOpen={isModalOpen}
        onClose={closeModal}
        title={titleByMode}
        form={form}
        onFormChange={(field, value) =>
          setForm((current) => ({ ...current, [field]: value }))
        }
        mode={mode}
        showPassword={showPassword}
        onToggleShowPassword={() => setShowPassword((v) => !v)}
        onSubmit={handleSubmit}
        isSubmitting={isSubmitting}
        t={t}
      />

      <ConfirmDialog
        isOpen={isConfirmDeleteOpen}
        onClose={cancelDelete}
        onConfirm={() => void confirmDelete()}
        title={t("common.confirmations.deleteTitle") || "Xác nhận xóa tài khoản"}
        message={`${t("common.confirmations.deleteMessage")} (${userToDelete?.username || ""})`}
        confirmText={t("common.actions.delete")}
        cancelText={t("common.actions.cancel")}
        isLoading={isDeleting}
        variant="danger"
      />
    </div>
  );
}
