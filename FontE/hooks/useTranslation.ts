"use client";

import { useI18n } from "@/contexts/I18nContext";

export function useTranslation() {
  const { t, language, setLanguage } = useI18n();

  return {
    t,
    language,
    setLanguage,
    isVi: language === "vi",
    isEn: language === "en",
  };
}
