import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/** Định dạng ngày kiểu Việt Nam dd/MM/yyyy (nhận ISO string hoặc Date) */
export function formatDate(value: string | Date | null | undefined): string {
  if (!value) return "--";
  const d = typeof value === "string" ? new Date(value) : value;
  if (Number.isNaN(d.getTime())) return "--";
  return new Intl.DateTimeFormat("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(d);
}

/** Định dạng ngày giờ kiểu Việt Nam dd/MM/yyyy HH:mm */
export function formatDateTime(value: string | Date | null | undefined): string {
  if (!value) return "--";
  const d = typeof value === "string" ? new Date(value) : value;
  if (Number.isNaN(d.getTime())) return "--";
  const date = formatDate(d);
  const time = new Intl.DateTimeFormat("vi-VN", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(d);
  return `${date} ${time}`;
}

/** Định dạng tiền VND */
export function formatCurrency(value: number | null | undefined): string {
  if (value == null) return "--";
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value);
}
