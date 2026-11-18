import React from "react";

export type BadgeVariant = "success" | "warning" | "danger" | "info" | "neutral";
export type BadgeSize = "sm" | "md";

export interface BadgeProps extends React.HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
  size?: BadgeSize;
  dot?: boolean;
}

export function Badge({
  children,
  variant = "neutral",
  size = "sm",
  dot = false,
  className = "",
  ...props
}: BadgeProps) {
  const variantStyles: Record<BadgeVariant, { container: string; dot: string }> = {
    success: {
      container: "border-emerald-200 bg-emerald-50 text-emerald-800",
      dot: "bg-emerald-500",
    },
    warning: {
      container: "border-amber-200 bg-amber-50 text-amber-800",
      dot: "bg-amber-500",
    },
    danger: {
      container: "border-rose-200 bg-rose-50 text-rose-800",
      dot: "bg-rose-500",
    },
    info: {
      container: "border-sky-200 bg-sky-50 text-sky-800",
      dot: "bg-sky-500",
    },
    neutral: {
      container: "border-slate-200 bg-slate-100 text-slate-700",
      dot: "bg-slate-400",
    },
  };

  const sizeStyles: Record<BadgeSize, string> = {
    sm: "px-2 py-0.5 text-xs font-semibold gap-1.5",
    md: "px-2.5 py-1 text-xs font-bold gap-2",
  };

  const style = variantStyles[variant];

  return (
    <span
      className={`inline-flex items-center rounded-full border ${style.container} ${sizeStyles[size]} ${className}`}
      {...props}
    >
      {dot && <span className={`h-1.5 w-1.5 rounded-full ${style.dot}`} aria-hidden="true" />}
      {children}
    </span>
  );
}
