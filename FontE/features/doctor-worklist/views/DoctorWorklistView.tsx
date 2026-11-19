"use client";

import { useDoctorWorklist } from "../hooks/useDoctorWorklist";
import { DoctorWorklistHeader } from "../components/DoctorWorklistHeader";
import { DoctorWorklistMetrics } from "../components/DoctorWorklistMetrics";
import { DoctorWorklistTable } from "../components/DoctorWorklistTable";

export function DoctorWorklistView() {
  const {
    t,
    canSelectDoctor,
    workDate,
    setWorkDate,
    doctorFilter,
    setDoctorFilter,
    doctors,
    worklist,
    items,
    groupedSummary,
    isLoading,
    isRefreshing,
    listError,
    fetchPageData,
  } = useDoctorWorklist();

  return (
    <div className="space-y-6">
      <DoctorWorklistHeader
        workDate={workDate}
        onWorkDateChange={setWorkDate}
        canSelectDoctor={canSelectDoctor}
        doctorFilter={doctorFilter}
        onDoctorFilterChange={setDoctorFilter}
        doctors={doctors}
        isRefreshing={isRefreshing}
        onRefresh={() => void fetchPageData(true)}
        worklist={worklist}
        t={t}
      />

      <DoctorWorklistMetrics worklist={worklist} />

      <DoctorWorklistTable
        worklist={worklist}
        workDate={workDate}
        groupedSummary={groupedSummary}
        items={items}
        isLoading={isLoading}
        listError={listError}
        onRetry={() => void fetchPageData(true)}
        t={t}
      />
    </div>
  );
}
