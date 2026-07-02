const VI_LOCALE = "vi-VN";

/**
 * Format currency in VND
 * @example formatCurrency(150000) → "150.000 ₫"
 */
export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat(VI_LOCALE, {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(amount);
}

/**
 * Format date as dd/MM/yyyy
 * @example formatDate(new Date()) → "23/05/2026"
 */
export function formatDate(date: Date | string | null | undefined): string {
  if (!date) return "";
  const d = typeof date === "string" ? new Date(date) : date;
  if (isNaN(d.getTime())) return "";
  return new Intl.DateTimeFormat(VI_LOCALE, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(d);
}

/**
 * Format datetime as dd/MM/yyyy HH:mm
 */
export function formatDateTime(date: Date | string | null | undefined): string {
  if (!date) return "";
  const d = typeof date === "string" ? new Date(date) : date;
  if (isNaN(d.getTime())) return "";
  return new Intl.DateTimeFormat(VI_LOCALE, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(d);
}

/**
 * Format time as HH:mm
 */
export function formatTime(date: Date | string | null | undefined): string {
  if (!date) return "";
  const d = typeof date === "string" ? new Date(date) : date;
  if (isNaN(d.getTime())) return "";
  return new Intl.DateTimeFormat(VI_LOCALE, {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(d);
}

/**
 * Format number with locale separator
 * @example formatNumber(1500000) → "1.500.000"
 */
export function formatNumber(value: number): string {
  return new Intl.NumberFormat(VI_LOCALE).format(value);
}
