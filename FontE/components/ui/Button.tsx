import React from "react";

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger";
}

export function Button({
  children,
  variant = "primary",
  className = "",
  ...props
}: ButtonProps) {
  const baseStyle =
    "inline-flex min-h-11 items-center justify-center rounded-[1.1rem] px-4 py-2.5 text-sm font-semibold transition-all duration-200 disabled:cursor-not-allowed disabled:opacity-60";
  const variants = {
    primary:
      "bg-[linear-gradient(135deg,#0f766e,#0891b2)] text-white shadow-[0_10px_24px_rgba(8,145,178,0.24)] hover:-translate-y-0.5 hover:shadow-[0_14px_30px_rgba(8,145,178,0.28)]",
    secondary:
      "border border-slate-200 bg-white text-slate-700 shadow-sm hover:-translate-y-0.5 hover:border-slate-300 hover:bg-slate-50 hover:shadow-md",
    danger:
      "bg-[linear-gradient(135deg,#ef4444,#dc2626)] text-white shadow-[0_10px_24px_rgba(239,68,68,0.2)] hover:-translate-y-0.5 hover:shadow-[0_14px_30px_rgba(239,68,68,0.24)]",
  };

  return (
    <button className={`${baseStyle} ${variants[variant]} ${className}`} {...props}>
      {children}
    </button>
  );
}
