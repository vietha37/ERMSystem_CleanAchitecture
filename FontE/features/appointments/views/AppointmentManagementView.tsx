"use client";

import React from "react";
import { AppointmentHeader } from "../components/AppointmentHeader";
import { AppointmentKpiMetrics } from "../components/AppointmentKpiMetrics";
import { AppointmentTable } from "../components/AppointmentTable";
import { AppointmentPagination } from "../components/AppointmentPagination";
import { AppointmentModals } from "../components/AppointmentModals";
import { useAppointmentsList } from "../hooks/useAppointmentsList";
import { useAppointmentActions } from "../hooks/useAppointmentActions";

export function AppointmentManagementView() {
  const {
    appointments,
    isLoading,
    isRefreshing,
    listError,
    searchQuery,
    setSearchQuery,
    statusFilter,
    setStatusFilter,
    appointmentDate,
    setAppointmentDate,
    pageNumber,
    setPageNumber,
    pageSize,
    setPageSize,
    totalCount,
    totalPages,
    metrics,
    fetchAppointments,
  } = useAppointmentsList();

  const {
    canCheckIn,
    canManageAppointment,
    isCheckInModalOpen,
    isCancelModalOpen,
    isRescheduleModalOpen,
    selectedAppointment,
    counterLabel,
    setCounterLabel,
    cancelReason,
    setCancelReason,
    rescheduleReason,
    setRescheduleReason,
    rescheduleDate,
    setRescheduleDate,
    rescheduleTime,
    setRescheduleTime,
    actionId,
    openCheckInModal,
    openCancelModal,
    openRescheduleModal,
    resetCheckInModal,
    resetCancelModal,
    resetRescheduleModal,
    handleCheckIn,
    handleComplete,
    handleCancel,
    handleReschedule,
  } = useAppointmentActions(() => fetchAppointments(true));

  return (
    <div className="space-y-6">
      <AppointmentHeader
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
        appointmentDate={appointmentDate}
        onDateChange={(val) => {
          setAppointmentDate(val);
          setPageNumber(1);
        }}
        statusFilter={statusFilter}
        onStatusFilterChange={(val) => {
          setStatusFilter(val);
          setPageNumber(1);
        }}
        isRefreshing={isRefreshing}
        onRefresh={() => void fetchAppointments(true)}
      />

      <AppointmentKpiMetrics metrics={metrics} />

      <AppointmentTable
        appointments={appointments}
        isLoading={isLoading}
        listError={listError}
        totalCount={totalCount}
        pageSize={pageSize}
        onPageSizeChange={(newSize) => {
          setPageSize(newSize);
          setPageNumber(1);
        }}
        canCheckIn={canCheckIn}
        canManageAppointment={canManageAppointment}
        actionId={actionId}
        onRefresh={() => void fetchAppointments(true)}
        onOpenCheckIn={openCheckInModal}
        onOpenReschedule={openRescheduleModal}
        onComplete={(appt) => void handleComplete(appt)}
        onOpenCancel={openCancelModal}
      >
        {!isLoading && (
          <AppointmentPagination
            pageNumber={pageNumber}
            totalPages={totalPages}
            totalCount={totalCount}
            onPageChange={setPageNumber}
          />
        )}
      </AppointmentTable>

      <AppointmentModals
        selectedAppointment={selectedAppointment}
        actionId={actionId}
        isCheckInModalOpen={isCheckInModalOpen}
        counterLabel={counterLabel}
        onCounterLabelChange={setCounterLabel}
        onCheckInSubmit={handleCheckIn}
        onCheckInClose={resetCheckInModal}
        isRescheduleModalOpen={isRescheduleModalOpen}
        rescheduleDate={rescheduleDate}
        onRescheduleDateChange={setRescheduleDate}
        rescheduleTime={rescheduleTime}
        onRescheduleTimeChange={setRescheduleTime}
        rescheduleReason={rescheduleReason}
        onRescheduleReasonChange={setRescheduleReason}
        onRescheduleSubmit={handleReschedule}
        onRescheduleClose={resetRescheduleModal}
        isCancelModalOpen={isCancelModalOpen}
        cancelReason={cancelReason}
        onCancelReasonChange={setCancelReason}
        onCancelSubmit={handleCancel}
        onCancelClose={resetCancelModal}
      />
    </div>
  );
}
