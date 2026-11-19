"use client";

import { Button } from "@/components/ui/Button";

interface StaffHeaderProps {
  isSyncing: boolean;
  onSync: () => void;
  onCreateOpen: () => void;
  t: (key: string) => string;
}

export function StaffHeader({
  isSyncing,
  onSync,
  onCreateOpen,
  t,
}: StaffHeaderProps) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border-b border-slate-200 pb-5">
      <div>
        <h1 className="text-xl font-bold tracking-tight text-slate-900">
          {t("staff.title")}
        </h1>
        <p className="mt-1 text-xs text-slate-500">
          {t("staff.subtitle")}
        </p>
      </div>
      <div className="flex flex-wrap items-center gap-2.5">
        <Button
          variant="secondary"
          size="sm"
          isLoading={isSyncing}
          onClick={onSync}
        >
          {t("common.actions.syncHospitalIdentity")}
        </Button>
        <Button variant="primary" size="sm" onClick={onCreateOpen}>
          + {t("staff.createStaff")}
        </Button>
      </div>
    </div>
  );
}
