"use client";

import React, { useRef } from "react";

export interface SearchInputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, "type"> {
  onClear?: () => void;
}

export const SearchInput = React.forwardRef<HTMLInputElement, SearchInputProps>(
  ({ value, onChange, onClear, placeholder = "Tìm kiếm...", className = "", ...props }, ref) => {
    const internalRef = useRef<HTMLInputElement>(null);
    const combinedRef = (ref as React.RefObject<HTMLInputElement>) || internalRef;

    const hasValue = Boolean(value && String(value).length > 0);

    const handleClear = () => {
      if (onClear) {
        onClear();
      }
      if (combinedRef && "current" in combinedRef && combinedRef.current) {
        combinedRef.current.focus();
      }
    };

    return (
      <div className={`relative w-full ${className}`}>
        <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
          <svg
            className="h-4 w-4"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
            aria-hidden="true"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z"
            />
          </svg>
        </div>

        <input
          ref={combinedRef}
          type="search"
          value={value}
          onChange={onChange}
          placeholder={placeholder}
          className="w-full rounded-lg border border-slate-300 bg-white py-2 pl-9 pr-8 text-sm text-slate-900 placeholder:text-slate-400 transition-colors hover:border-slate-400 focus:border-sky-600 focus:outline-none focus:ring-2 focus:ring-sky-100"
          {...props}
        />

        {hasValue && (
          <button
            type="button"
            onClick={handleClear}
            aria-label="Xóa nội dung tìm kiếm"
            className="absolute inset-y-0 right-0 flex items-center pr-2.5 text-slate-400 hover:text-slate-600 focus:outline-none"
          >
            <svg
              className="h-3.5 w-3.5"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={2}
            >
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>
    );
  }
);

SearchInput.displayName = "SearchInput";
