import type { ReportCellValue, ReportColumnType } from "@/lib/api/reports";

function formatDateVi(raw: string, withTime: boolean): string {
  const d = new Date(raw);
  if (Number.isNaN(d.getTime())) return raw;
  const dd = String(d.getDate()).padStart(2, "0");
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const yyyy = d.getFullYear();
  if (!withTime) return `${dd}/${mm}/${yyyy}`;
  const hh = String(d.getHours()).padStart(2, "0");
  const min = String(d.getMinutes()).padStart(2, "0");
  return `${dd}/${mm}/${yyyy} ${hh}:${min}`;
}

/** Định dạng số theo locale vi-VN, dùng chung cho cột Money/Number và KPI card. */
export function formatNumberVi(value: number): string {
  return value.toLocaleString("vi-VN");
}

/** Định dạng 1 ô dữ liệu lưới báo cáo theo column.type — null/undefined/"" hiển thị "–". */
export function formatReportCell(value: ReportCellValue, type: ReportColumnType): string {
  if (value === null || value === undefined || value === "") return "–";
  switch (type) {
    case "Money":
    case "Number":
      return typeof value === "number" ? formatNumberVi(value) : String(value);
    case "Date":
      return formatDateVi(String(value), false);
    case "DateTime":
      return formatDateVi(String(value), true);
    default:
      return String(value);
  }
}
