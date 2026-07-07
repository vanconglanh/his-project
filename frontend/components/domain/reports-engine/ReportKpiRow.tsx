import type { ReportKpi, ReportKpiTintToken } from "@/lib/api/reports";
import { cn } from "@/lib/utils";
import { formatNumberVi } from "./report-format";

interface ReportKpiRowProps {
  kpis: ReportKpi[];
}

// Bảng màu tint KPI báo cáo → token, xem docs/design/design-system-standards.md mục 6.4.
const KPI_TINT_TOKEN_CLASS: Record<ReportKpiTintToken, string> = {
  brand: "bg-accent-primary/10",
  done: "bg-[color:var(--status-done)]/10",
  warning: "bg-[color:var(--status-warning)]/10",
  critical: "bg-[color:var(--status-critical)]/10",
  insurance: "bg-[color:var(--status-insurance)]/10",
  neutral: "bg-muted/40 text-[color:var(--text-muted)]",
};

// Fallback FE-only cho BE cũ chưa trả tint_token (chỉ có hex cố định), xem mục 6.4.
const KPI_TINT_HEX_CLASS: Record<string, string> = {
  "#F0FDFA": KPI_TINT_TOKEN_CLASS.brand,
  "#ECFDF5": KPI_TINT_TOKEN_CLASS.done,
  "#FFFBEB": KPI_TINT_TOKEN_CLASS.warning,
  "#FEF2F2": KPI_TINT_TOKEN_CLASS.critical,
  "#EFF6FF": KPI_TINT_TOKEN_CLASS.insurance,
};

function tintClass(tintToken: ReportKpiTintToken | null | undefined, tint: string | null | undefined): string {
  if (tintToken && KPI_TINT_TOKEN_CLASS[tintToken]) return KPI_TINT_TOKEN_CLASS[tintToken];
  if (tint) return KPI_TINT_HEX_CLASS[tint.toUpperCase()] ?? KPI_TINT_TOKEN_CLASS.neutral;
  return KPI_TINT_TOKEN_CLASS.neutral;
}

/** Dải KPI card đầu trang báo cáo — tint nền map từ tint_token (BE) sang token status (xem mục 6.4). */
export function ReportKpiRow({ kpis }: ReportKpiRowProps) {
  if (!kpis.length) return null;
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
      {kpis.map((kpi) => (
        <div key={kpi.label} className={cn("rounded-xl border p-4", tintClass(kpi.tint_token, kpi.tint))}>
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide truncate">
            {kpi.label}
          </p>
          <p className="mt-1 text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums text-foreground">
            {formatNumberVi(kpi.value)}
            {kpi.is_money && <span className="ml-1 text-sm font-medium text-muted-foreground">đ</span>}
          </p>
        </div>
      ))}
    </div>
  );
}
