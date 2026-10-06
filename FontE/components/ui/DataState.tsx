"use client";

import { Button } from "@/components/ui/Button";
import { useTranslation } from "@/hooks/useTranslation";

type DataStateTone = "cyan" | "blue" | "emerald" | "rose" | "violet" | "slate";

export type DataStateProps = {
  title: string;
  description?: string;
  tone?: DataStateTone;
  actionLabel?: string;
  onAction?: () => void;
};

export function LoadingState({
  title,
}: Pick<DataStateProps, "title" | "tone">) {
  const { t } = useTranslation();
  const displayTitle = title || t("common.actions.loading");

  return (
    <div
      className="flex min-h-64 flex-col items-center justify-center px-6 py-16 text-center"
      role="status"
      aria-live="polite"
    >
      <div className="mb-4 h-9 w-9 animate-spin rounded-full border-3 border-slate-200 border-t-sky-600" />
      <p className="text-sm font-medium text-slate-600">{displayTitle}</p>
    </div>
  );
}

export function EmptyState({
  title,
  description,
  actionLabel,
  onAction,
}: DataStateProps) {
  const { t } = useTranslation();
  const displayTitle = title || t("common.labels.empty");

  return (
    <div
      className="flex min-h-64 flex-col items-center justify-center px-6 py-14 text-center"
      role="status"
      aria-live="polite"
    >
      <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-xl border border-slate-200 bg-slate-50 text-slate-400">
        <svg
          className="h-6 w-6"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={1.5}
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5m8.25 3v6.75m0 0l-3-3m3 3l3-3M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
          />
        </svg>
      </div>
      <p className="text-sm font-semibold text-slate-800">{displayTitle}</p>
      {description && (
        <p className="mt-1.5 max-w-md text-xs leading-relaxed text-slate-500">
          {description}
        </p>
      )}
      {actionLabel && onAction && (
        <Button
          type="button"
          variant="secondary"
          size="sm"
          className="mt-5"
          onClick={onAction}
        >
          {actionLabel}
        </Button>
      )}
    </div>
  );
}

export function ErrorState({
  title,
  description,
  actionLabel,
  onAction,
}: DataStateProps) {
  const { t } = useTranslation();
  const displayTitle = title || t("common.messages.error");
  const displayAction = actionLabel || t("common.actions.reload");

  return (
    <div
      className="flex min-h-64 flex-col items-center justify-center px-6 py-14 text-center"
      role="alert"
      aria-live="assertive"
    >
      <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-xl border border-rose-200 bg-rose-50 text-rose-600">
        <svg
          className="h-6 w-6"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={1.5}
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z"
          />
        </svg>
      </div>
      <p className="text-sm font-semibold text-slate-900">{displayTitle}</p>
      {description && (
        <p className="mt-1.5 max-w-md text-xs leading-relaxed text-slate-500">
          {description}
        </p>
      )}
      {onAction && (
        <Button
          type="button"
          variant="secondary"
          size="sm"
          className="mt-5 border-rose-200 text-rose-700 hover:bg-rose-50 hover:border-rose-300"
          onClick={onAction}
        >
          {displayAction}
        </Button>
      )}
    </div>
  );
}
