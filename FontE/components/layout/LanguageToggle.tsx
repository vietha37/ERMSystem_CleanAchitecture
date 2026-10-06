"use client";

import { useTranslation } from "@/hooks/useTranslation";

interface LanguageToggleProps {
  className?: string;
}

export function LanguageToggle({ className = "" }: LanguageToggleProps) {
  const { language, setLanguage } = useTranslation();

  return (
    <div
      className={`inline-flex items-center rounded-lg border border-slate-200 bg-white p-0.5 text-xs font-semibold text-slate-700 shadow-2xs ${className}`}
      role="group"
      aria-label="Language selector"
    >
      <button
        type="button"
        onClick={() => setLanguage("vi")}
        aria-pressed={language === "vi"}
        className={`rounded-md px-2.5 py-1 transition-colors ${
          language === "vi"
            ? "bg-slate-900 text-white shadow-xs"
            : "text-slate-600 hover:text-slate-900"
        }`}
      >
        VI
      </button>
      <button
        type="button"
        onClick={() => setLanguage("en")}
        aria-pressed={language === "en"}
        className={`rounded-md px-2.5 py-1 transition-colors ${
          language === "en"
            ? "bg-slate-900 text-white shadow-xs"
            : "text-slate-600 hover:text-slate-900"
        }`}
      >
        EN
      </button>
    </div>
  );
}
