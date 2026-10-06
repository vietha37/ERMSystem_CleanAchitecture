import React from "react";

export function Card({
  children,
  className = "",
  padding = "md",
}: {
  children: React.ReactNode;
  className?: string;
  padding?: "none" | "sm" | "md" | "lg";
}) {
  const paddingStyles = {
    none: "",
    sm: "p-3",
    md: "p-5",
    lg: "p-6",
  };

  return (
    <div
      className={`rounded-xl border border-slate-200/90 bg-white text-slate-900 shadow-2xs transition-shadow duration-150 ${paddingStyles[padding]} ${className}`}
    >
      {children}
    </div>
  );
}

export function CardHeader({
  title,
  subtitle,
  action,
  className = "",
}: {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  action?: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={`flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between pb-4 border-b border-slate-100 ${className}`}>
      <div>
        <h3 className="text-base font-semibold text-slate-900 tracking-tight">{title}</h3>
        {subtitle && <p className="text-xs text-slate-500 mt-0.5">{subtitle}</p>}
      </div>
      {action && <div className="mt-2 sm:mt-0 flex items-center gap-2">{action}</div>}
    </div>
  );
}
