"use client";

import { useClinicalOrdersList } from "../hooks/useClinicalOrdersList";
import { useClinicalOrderActions } from "../hooks/useClinicalOrderActions";
import { ClinicalOrderHeader } from "../components/ClinicalOrderHeader";
import { ClinicalOrderMetrics } from "../components/ClinicalOrderMetrics";
import { ClinicalOrderTable } from "../components/ClinicalOrderTable";
import { ClinicalOrderCreateModal } from "../components/ClinicalOrderCreateModal";
import { ClinicalOrderDetailModal } from "../components/ClinicalOrderDetailModal";
import { ClinicalOrderLabModal } from "../components/ClinicalOrderLabModal";
import { ClinicalOrderImagingModal } from "../components/ClinicalOrderImagingModal";

export function ClinicalOrdersView() {
  const {
    orders,
    eligibleEncounters,
    catalog,
    isLoading,
    isRefreshing,
    listError,
    searchQuery,
    setSearchQuery,
    categoryFilter,
    setCategoryFilter,
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
  } = useClinicalOrdersList();

  const {
    isSubmitting,
    isCreateModalOpen,
    setIsCreateModalOpen,
    isDetailModalOpen,
    setIsDetailModalOpen,
    isLabResultModalOpen,
    setIsLabResultModalOpen,
    isImagingReportModalOpen,
    setIsImagingReportModalOpen,
    selectedDetail,
    setSelectedDetail,
    labResultTarget,
    imagingReportTarget,
    selectedEncounterId,
    setSelectedEncounterId,
    createCategory,
    setCreateCategory,
    selectedServiceId,
    setSelectedServiceId,
    priorityCode,
    setPriorityCode,
    availableServices,
    specimenCode,
    setSpecimenCode,
    labResultItems,
    findings,
    setFindings,
    impression,
    setImpression,
    reportUri,
    setReportUri,
    resetCreateForm,
    resetLabResultForm,
    resetImagingReportForm,
    openDetail,
    handleCreateOrder,
    openLabResultModal,
    handleAddLabResultRow,
    handleRemoveLabResultRow,
    handleUpdateLabResultRow,
    handleRecordLabResult,
    openImagingReportModal,
    handleRecordImagingReport,
  } = useClinicalOrderActions({
    catalog,
    onRefresh: () => fetchData(true),
  });

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <ClinicalOrderHeader
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
        categoryFilter={categoryFilter}
        onCategoryChange={setCategoryFilter}
        statusFilter={statusFilter}
        onStatusChange={setStatusFilter}
        isRefreshing={isRefreshing}
        onRefresh={() => void fetchData(true)}
        onCreateOpen={() => setIsCreateModalOpen(true)}
      />

      <ClinicalOrderMetrics metrics={metrics} />

      <ClinicalOrderTable
        orders={orders}
        isLoading={isLoading}
        listError={listError}
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
        onOpenDetail={openDetail}
        onOpenLabModal={openLabResultModal}
        onOpenImagingModal={openImagingReportModal}
      />

      <ClinicalOrderCreateModal
        isOpen={isCreateModalOpen}
        onClose={() => {
          setIsCreateModalOpen(false);
          resetCreateForm();
        }}
        onSubmit={handleCreateOrder}
        isSubmitting={isSubmitting}
        selectedEncounterId={selectedEncounterId}
        onEncounterChange={setSelectedEncounterId}
        eligibleEncounters={eligibleEncounters}
        createCategory={createCategory}
        onCategoryChange={setCreateCategory}
        priorityCode={priorityCode}
        onPriorityChange={setPriorityCode}
        selectedServiceId={selectedServiceId}
        onServiceChange={setSelectedServiceId}
        availableServices={availableServices}
      />

      <ClinicalOrderDetailModal
        isOpen={isDetailModalOpen}
        onClose={() => {
          setIsDetailModalOpen(false);
          setSelectedDetail(null);
        }}
        selectedDetail={selectedDetail}
      />

      <ClinicalOrderLabModal
        isOpen={isLabResultModalOpen}
        onClose={() => {
          setIsLabResultModalOpen(false);
          resetLabResultForm();
        }}
        onSubmit={handleRecordLabResult}
        isSubmitting={isSubmitting}
        labResultTarget={labResultTarget}
        specimenCode={specimenCode}
        onSpecimenCodeChange={setSpecimenCode}
        labResultItems={labResultItems}
        onAddRow={handleAddLabResultRow}
        onRemoveRow={handleRemoveLabResultRow}
        onUpdateRow={handleUpdateLabResultRow}
      />

      <ClinicalOrderImagingModal
        isOpen={isImagingReportModalOpen}
        onClose={() => {
          setIsImagingReportModalOpen(false);
          resetImagingReportForm();
        }}
        onSubmit={handleRecordImagingReport}
        isSubmitting={isSubmitting}
        imagingReportTarget={imagingReportTarget}
        findings={findings}
        onFindingsChange={setFindings}
        impression={impression}
        onImpressionChange={setImpression}
        reportUri={reportUri}
        onReportUriChange={setReportUri}
      />
    </div>
  );
}
