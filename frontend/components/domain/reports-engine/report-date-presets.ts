import { format, startOfWeek, endOfWeek, startOfMonth, endOfMonth, subDays, subMonths } from "date-fns";

export type ReportDatePreset = "today" | "yesterday" | "thisWeek" | "thisMonth" | "lastMonth" | "custom";

export interface ReportDateRange {
  from: string;
  to: string;
}

export const REPORT_DATE_PRESET_LABELS: Record<ReportDatePreset, string> = {
  today: "Hôm nay",
  yesterday: "Hôm qua",
  thisWeek: "Tuần này",
  thisMonth: "Tháng này",
  lastMonth: "Tháng trước",
  custom: "Tùy chọn",
};

const fmt = (d: Date) => format(d, "yyyy-MM-dd");

/** Tính khoảng ngày (yyyy-MM-dd) theo preset — dùng chung cho toàn bộ báo cáo engine. */
export function getReportPresetRange(preset: ReportDatePreset): ReportDateRange {
  const today = new Date();
  switch (preset) {
    case "today":
      return { from: fmt(today), to: fmt(today) };
    case "yesterday": {
      const y = subDays(today, 1);
      return { from: fmt(y), to: fmt(y) };
    }
    case "thisWeek":
      return { from: fmt(startOfWeek(today, { weekStartsOn: 1 })), to: fmt(endOfWeek(today, { weekStartsOn: 1 })) };
    case "thisMonth":
      return { from: fmt(startOfMonth(today)), to: fmt(endOfMonth(today)) };
    case "lastMonth": {
      const lastM = subMonths(today, 1);
      return { from: fmt(startOfMonth(lastM)), to: fmt(endOfMonth(lastM)) };
    }
    default:
      return { from: fmt(startOfMonth(today)), to: fmt(today) };
  }
}
