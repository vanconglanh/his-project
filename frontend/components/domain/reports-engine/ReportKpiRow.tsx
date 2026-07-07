import type { ReportKpi } from "@/lib/api/reports";
import { formatNumberVi } from "./report-format";

interface ReportKpiRowProps {
  kpis: ReportKpi[];
}

/** Dải KPI card đầu trang báo cáo — tint nền lấy trực tiếp từ BE (hex), không hard-code màu. */
export function ReportKpiRow({ kpis }: ReportKpiRowProps) {
  if (!kpis.length) return null;
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
      {kpis.map((kpi) => (
        <div
          key={kpi.label}
          className="rounded-xl border p-4"
          style={{ backgroundColor: kpi.tint ?? undefined }}
        >
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide truncate">
            {kpi.label}
          </p>
          <p className="mt-1 text-2xl font-bold tabular-nums text-foreground">
            {formatNumberVi(kpi.value)}
            {kpi.is_money && <span className="ml-1 text-sm font-medium text-muted-foreground">đ</span>}
          </p>
        </div>
      ))}
    </div>
  );
}
