const VI_LOCALE = "vi-VN";
const VI_TIME_ZONE = "Asia/Ho_Chi_Minh";

const dateFormatter = new Intl.DateTimeFormat(VI_LOCALE, {
  timeZone: VI_TIME_ZONE,
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
});

const dateTimeFormatter = new Intl.DateTimeFormat(VI_LOCALE, {
  timeZone: VI_TIME_ZONE,
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
});

const timeFormatter = new Intl.DateTimeFormat(VI_LOCALE, {
  timeZone: VI_TIME_ZONE,
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
});

function parseDate(value?: string | null): Date | null {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return date;
}

export function formatDateValue(value?: string | null, fallback = "--"): string {
  const date = parseDate(value);
  return date ? dateFormatter.format(date) : fallback;
}

export function formatDateTimeValue(value?: string | null, fallback = "--"): string {
  const date = parseDate(value);
  return date ? dateTimeFormatter.format(date) : fallback;
}

export function formatTimeValue(value?: string | null, fallback = "--:--"): string {
  const date = parseDate(value);
  return date ? timeFormatter.format(date) : fallback;
}
