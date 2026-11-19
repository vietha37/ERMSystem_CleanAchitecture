"use client";

import { useBillingInvoices } from "../hooks/useBillingInvoices";
import { useBillingModals } from "../hooks/useBillingModals";
import { BillingHeader } from "../components/BillingHeader";
import { BillingMetrics } from "../components/BillingMetrics";
import { BillingTable } from "../components/BillingTable";
import { BillingCreateInvoiceModal } from "../components/BillingCreateInvoiceModal";
import { BillingPaymentModal } from "../components/BillingPaymentModal";
import { BillingGatewayModal } from "../components/BillingGatewayModal";
import { BillingGatewayCallbackModal } from "../components/BillingGatewayCallbackModal";
import { BillingDetailModal } from "../components/BillingDetailModal";

export function BillingManagementView() {
  const {
    invoices,
    availableEncounters,
    reconciliationSummary,
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
    totalCount,
    totalPages,
    metrics,
    fetchData,
  } = useBillingInvoices();

  const {
    isSubmitting,
    isCreateModalOpen,
    setIsCreateModalOpen,
    isPaymentModalOpen,
    setIsPaymentModalOpen,
    isGatewayModalOpen,
    setIsGatewayModalOpen,
    isGatewayCallbackModalOpen,
    setIsGatewayCallbackModalOpen,
    isDetailModalOpen,
    setIsDetailModalOpen,
    selectedInvoice,
    paymentInvoice,
    gatewayIntent,
    selectedEncounterId,
    setSelectedEncounterId,
    discountAmount,
    setDiscountAmount,
    encounterPreview,
    isLoadingPreview,
    previewBreakdown,
    detailBreakdown,
    paymentMethod,
    setPaymentMethod,
    paymentAmount,
    setPaymentAmount,
    paymentReference,
    setPaymentReference,
    gatewayMethod,
    setGatewayMethod,
    gatewayAmount,
    setGatewayAmount,
    gatewayStatus,
    setGatewayStatus,
    openDetail,
    openCreateModal,
    handleCreateInvoice,
    openPaymentModal,
    handleReceivePayment,
    openGatewayModal,
    handleCreateGatewayIntent,
    handleConfirmGatewayCallback,
  } = useBillingModals(() => fetchData(true));

  return (
    <div className="mx-auto max-w-7xl space-y-6">
      <BillingHeader
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
        statusFilter={statusFilter}
        onStatusChange={setStatusFilter}
        isRefreshing={isRefreshing}
        onRefresh={() => void fetchData(true)}
        onCreateOpen={openCreateModal}
      />

      <BillingMetrics
        metrics={metrics}
        waitingEncountersCount={availableEncounters.length}
        reconciliationSummary={reconciliationSummary}
      />

      <BillingTable
        invoices={invoices}
        isLoading={isLoading}
        listError={listError}
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalCount={totalCount}
        totalPages={totalPages}
        onPageChange={setPageNumber}
        onRetry={() => void fetchData(true)}
        onOpenDetail={openDetail}
        onOpenPayment={openPaymentModal}
        onOpenGateway={openGatewayModal}
      />

      <BillingCreateInvoiceModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onSubmit={handleCreateInvoice}
        isSubmitting={isSubmitting}
        selectedEncounterId={selectedEncounterId}
        onEncounterChange={setSelectedEncounterId}
        availableEncounters={availableEncounters}
        isLoadingPreview={isLoadingPreview}
        encounterPreview={encounterPreview}
        previewBreakdown={previewBreakdown}
        discountAmount={discountAmount}
        onDiscountAmountChange={setDiscountAmount}
      />

      <BillingPaymentModal
        isOpen={isPaymentModalOpen}
        onClose={() => setIsPaymentModalOpen(false)}
        onSubmit={handleReceivePayment}
        isSubmitting={isSubmitting}
        paymentInvoice={paymentInvoice}
        paymentMethod={paymentMethod}
        onPaymentMethodChange={setPaymentMethod}
        paymentAmount={paymentAmount}
        onPaymentAmountChange={setPaymentAmount}
        paymentReference={paymentReference}
        onPaymentReferenceChange={setPaymentReference}
      />

      <BillingGatewayModal
        isOpen={isGatewayModalOpen}
        onClose={() => setIsGatewayModalOpen(false)}
        onSubmit={handleCreateGatewayIntent}
        isSubmitting={isSubmitting}
        gatewayMethod={gatewayMethod}
        onGatewayMethodChange={setGatewayMethod}
        gatewayAmount={gatewayAmount}
        onGatewayAmountChange={setGatewayAmount}
      />

      <BillingGatewayCallbackModal
        isOpen={isGatewayCallbackModalOpen}
        onClose={() => setIsGatewayCallbackModalOpen(false)}
        onSubmit={handleConfirmGatewayCallback}
        isSubmitting={isSubmitting}
        gatewayIntent={gatewayIntent}
        gatewayStatus={gatewayStatus}
        onGatewayStatusChange={setGatewayStatus}
      />

      <BillingDetailModal
        isOpen={isDetailModalOpen}
        onClose={() => setIsDetailModalOpen(false)}
        selectedInvoice={selectedInvoice}
        detailBreakdown={detailBreakdown}
      />
    </div>
  );
}
