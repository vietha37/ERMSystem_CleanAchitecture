import React from "react";

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "outline" | "danger" | "ghost";
  size?: "sm" | "md" | "lg";
  isLoading?: boolean;
}

export function Button({
  children,
  variant = "primary",
  size = "md",
  isLoading = false,
  className = "",
  disabled,
  ...props
}: ButtonProps) {
  const sizeStyles = {
    sm: "min-h-8 px-3 py-1.5 text-xs font-medium rounded-md",
    md: "min-h-10 px-4 py-2 text-sm font-semibold rounded-lg",
    lg: "min-h-12 px-5 py-2.5 text-base font-semibold rounded-lg",
  };

  const variantStyles = {
    primary:
      "bg-slate-900 text-white hover:bg-slate-800 active:bg-slate-950 shadow-xs focus-visible:ring-2 focus-visible:ring-slate-900 focus-visible:ring-offset-1",
    secondary:
      "border border-slate-300 bg-white text-slate-700 hover:bg-slate-50 hover:border-slate-400 active:bg-slate-100 shadow-2xs focus-visible:ring-2 focus-visible:ring-slate-400 focus-visible:ring-offset-1",
    outline:
      "border border-sky-600 text-sky-700 bg-transparent hover:bg-sky-50 active:bg-sky-100 focus-visible:ring-2 focus-visible:ring-sky-600 focus-visible:ring-offset-1",
    danger:
      "bg-rose-600 text-white hover:bg-rose-700 active:bg-rose-800 shadow-xs focus-visible:ring-2 focus-visible:ring-rose-600 focus-visible:ring-offset-1",
    ghost:
      "bg-transparent text-slate-700 hover:bg-slate-100 hover:text-slate-900 active:bg-slate-200 focus-visible:ring-2 focus-visible:ring-slate-300",
  };

  const baseStyle =
    "inline-flex items-center justify-center gap-2 transition-colors duration-150 outline-none select-none disabled:cursor-not-allowed disabled:opacity-50";

  return (
    <button
      className={`${baseStyle} ${sizeStyles[size]} ${variantStyles[variant]} ${className}`}
      disabled={disabled || isLoading}
      aria-busy={isLoading}
      {...props}
    >
      {isLoading && (
        <svg
          className="h-4 w-4 animate-spin text-current"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <circle
            className="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            strokeWidth="4"
          />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          />
        </svg>
      )}
      {children}
    </button>
  );
}
