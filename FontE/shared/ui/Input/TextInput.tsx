"use client";

import React, { useId } from "react";

export interface TextInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helperText?: string;
  leftAddon?: React.ReactNode;
  rightAddon?: React.ReactNode;
  isRequired?: boolean;
}

export const TextInput = React.forwardRef<HTMLInputElement, TextInputProps>(
  (
    {
      label,
      error,
      helperText,
      leftAddon,
      rightAddon,
      isRequired,
      className = "",
      id,
      disabled,
      ...props
    },
    ref
  ) => {
    const generatedId = useId();
    const inputId = id || generatedId;
    const errorId = `${inputId}-error`;
    const helperId = `${inputId}-helper`;

    return (
      <div className="w-full space-y-1.5">
        {label && (
          <label
            htmlFor={inputId}
            className="block text-xs font-semibold uppercase tracking-wider text-slate-700"
          >
            {label}
            {isRequired && <span className="ml-1 text-rose-500">*</span>}
          </label>
        )}

        <div className="relative rounded-lg shadow-2xs">
          {leftAddon && (
            <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
              {leftAddon}
            </div>
          )}

          <input
            ref={ref}
            id={inputId}
            disabled={disabled}
            aria-invalid={Boolean(error)}
            aria-describedby={error ? errorId : helperText ? helperId : undefined}
            className={`w-full rounded-lg border bg-white py-2 text-sm text-slate-900 transition-colors placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-offset-1 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-400 ${
              leftAddon ? "pl-9" : "pl-3.5"
            } ${rightAddon ? "pr-9" : "pr-3.5"} ${
              error
                ? "border-rose-300 focus:border-rose-500 focus:ring-rose-200"
                : "border-slate-300 hover:border-slate-400 focus:border-sky-600 focus:ring-sky-100"
            } ${className}`}
            {...props}
          />

          {rightAddon && (
            <div className="absolute inset-y-0 right-0 flex items-center pr-3 text-slate-400">
              {rightAddon}
            </div>
          )}
        </div>

        {error ? (
          <p id={errorId} className="text-xs text-rose-600 animate-fade-in" role="alert">
            {error}
          </p>
        ) : helperText ? (
          <p id={helperId} className="text-xs text-slate-500">
            {helperText}
          </p>
        ) : null}
      </div>
    );
  }
);

TextInput.displayName = "TextInput";
