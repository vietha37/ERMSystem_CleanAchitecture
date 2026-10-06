"use client";

import React, { createContext, useContext, useState } from "react";
import viDict from "@/i18n/vi.json";
import enDict from "@/i18n/en.json";

export type SupportedLanguage = "vi" | "en";

export type I18nContextValue = {
  language: SupportedLanguage;
  setLanguage: (lang: SupportedLanguage) => void;
  t: (key: string, params?: Record<string, string | number>) => string;
};

const dictionaries: Record<SupportedLanguage, Record<string, unknown>> = {
  vi: viDict as Record<string, unknown>,
  en: enDict as Record<string, unknown>,
};

const I18nContext = createContext<I18nContextValue>({
  language: "vi",
  setLanguage: () => {},
  t: (key: string) => key,
});

const STORAGE_KEY = "erm_app_language";

function getNestedValue(obj: Record<string, unknown>, path: string): string | undefined {
  const parts = path.split(".");
  let current: unknown = obj;

  for (const part of parts) {
    if (current && typeof current === "object" && part in current) {
      current = (current as Record<string, unknown>)[part];
    } else {
      return undefined;
    }
  }

  return typeof current === "string" ? current : undefined;
}

export function I18nProvider({ children }: { children: React.ReactNode }) {
  const [language, setLanguageState] = useState<SupportedLanguage>(() => {
    if (typeof window === "undefined") return "vi";
    try {
      const saved = localStorage.getItem(STORAGE_KEY) as SupportedLanguage | null;
      if (saved === "vi" || saved === "en") return saved;
    } catch {
      // localStorage may fail
    }
    return "vi";
  });

  const setLanguage = (lang: SupportedLanguage) => {
    setLanguageState(lang);
    try {
      localStorage.setItem(STORAGE_KEY, lang);
      document.documentElement.lang = lang;
    } catch {
      // ignore
    }
  };

  const t = (key: string, params?: Record<string, string | number>): string => {
    const dict = dictionaries[language] || dictionaries.vi;
    let text = getNestedValue(dict, key);

    // Fallback to Vietnamese if English missing
    if (!text && language !== "vi") {
      text = getNestedValue(dictionaries.vi, key);
    }

    if (!text) {
      // If key is missing, return the last token or the key itself
      return key.split(".").pop() || key;
    }

    if (params) {
      return Object.entries(params).reduce((acc, [paramKey, paramVal]) => {
        return acc.replace(new RegExp(`{{\\s*${paramKey}\\s*}}`, "g"), String(paramVal));
      }, text);
    }

    return text;
  };

  return (
    <I18nContext.Provider value={{ language, setLanguage, t }}>
      {children}
    </I18nContext.Provider>
  );
}

export function useI18n() {
  return useContext(I18nContext);
}
