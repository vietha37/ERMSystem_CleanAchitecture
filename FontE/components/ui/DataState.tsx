import { Button } from "@/components/ui/Button";

type DataStateTone = "cyan" | "blue" | "emerald" | "rose" | "violet";

type DataStateProps = {
  title: string;
  description?: string;
  tone?: DataStateTone;
  actionLabel?: string;
  onAction?: () => void;
};

const toneStyles: Record<DataStateTone, { spinner: string; icon: string }> = {
  cyan: {
    spinner: "border-cyan-100 border-t-cyan-600",
    icon: "border-cyan-100 bg-cyan-50 text-cyan-700",
  },
  blue: {
    spinner: "border-blue-100 border-t-blue-600",
    icon: "border-blue-100 bg-blue-50 text-blue-700",
  },
  emerald: {
    spinner: "border-emerald-100 border-t-emerald-600",
    icon: "border-emerald-100 bg-emerald-50 text-emerald-700",
  },
  rose: {
    spinner: "border-rose-100 border-t-rose-600",
    icon: "border-rose-100 bg-rose-50 text-rose-700",
  },
  violet: {
    spinner: "border-violet-100 border-t-violet-600",
    icon: "border-violet-100 bg-violet-50 text-violet-700",
  },
};

export function LoadingState({
  title,
  tone = "cyan",
}: Pick<DataStateProps, "title" | "tone">) {
  return (
    <div className="flex min-h-64 flex-col items-center justify-center px-6 py-16 text-center" aria-live="polite">
      <div className={`mb-4 h-10 w-10 animate-spin rounded-full border-4 ${toneStyles[tone].spinner}`} />
      <p className="text-sm font-medium text-slate-500">{title}</p>
    </div>
  );
}

export function EmptyState({
  title,
  description,
  tone = "cyan",
  actionLabel,
  onAction,
}: DataStateProps) {
  return (
    <div className="flex min-h-64 flex-col items-center justify-center px-6 py-16 text-center" aria-live="polite">
      <div className={`mb-4 grid h-11 w-11 place-items-center rounded-full border text-lg font-bold ${toneStyles[tone].icon}`}>
        --
      </div>
      <p className="text-sm font-semibold text-slate-700">{title}</p>
      {description && <p className="mt-2 max-w-md text-sm text-slate-500">{description}</p>}
      {actionLabel && onAction && (
        <Button type="button" variant="secondary" className="mt-5" onClick={onAction}>
          {actionLabel}
        </Button>
      )}
    </div>
  );
}

export function ErrorState({
  title,
  description,
  actionLabel = "Tải lại",
  onAction,
}: DataStateProps) {
  return (
    <div
      className="flex min-h-64 flex-col items-center justify-center px-6 py-16 text-center"
      role="alert"
      aria-live="assertive"
    >
      <div className={`mb-4 grid h-11 w-11 place-items-center rounded-full border text-lg font-bold ${toneStyles.rose.icon}`}>
        !
      </div>
      <p className="text-sm font-semibold text-slate-800">{title}</p>
      {description && <p className="mt-2 max-w-md text-sm text-slate-500">{description}</p>}
      {onAction && (
        <Button type="button" variant="secondary" className="mt-5 border-rose-200 text-rose-700 hover:bg-rose-50" onClick={onAction}>
          {actionLabel}
        </Button>
      )}
    </div>
  );
}
