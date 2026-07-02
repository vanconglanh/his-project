"use client";

/**
 * KpiCard — card KPI dashboard với sparkline mini + delta
 * Spec: docs/design/research-his-ui-patterns.md mục 5
 * Stack: Recharts AreaChart compact (no axes, no tooltip)
 */
import { TrendingUp, TrendingDown, Minus } from "lucide-react";
import { AreaChart, Area, ResponsiveContainer } from "recharts";
import { cn } from "@/lib/utils";
import { chartPalette } from "@/lib/chart-theme";

export interface KpiCardProps {
  /** Nhãn KPI (text-sm muted) */
  label: string;
  /** Giá trị chính (text-kpi bold tabular-nums) */
  value: string | number;
  /**
   * Delta % so với kỳ trước (dương = tốt, âm = xấu)
   * VD: "+12%" hoặc -3.5
   */
  delta?: string | number;
  /**
   * Dữ liệu sparkline 7 điểm (số tuần/ngày gần nhất)
   * VD: [142, 138, 155, 162, 149, 170, 165]
   */
  trend?: number[];
  /** Click drill-down */
  onClick?: () => void;
  className?: string;
  /** Đơn vị hiển thị cạnh value (VD: "VND", "lượt") */
  unit?: string;
}

function parseDelta(delta: string | number): number {
  if (typeof delta === "number") return delta;
  return parseFloat(delta.replace(/[^-\d.]/g, "")) || 0;
}

/**
 * KpiCard chuẩn HIS — label + giá trị lớn + delta % + sparkline 7 ngày.
 *
 * @example
 * <KpiCard label="Lượt khám hôm nay" value={142} delta="+12%" trend={[130,138,145,142,149,155,142]} onClick={...} />
 */
export function KpiCard({
  label,
  value,
  delta,
  trend,
  onClick,
  className,
  unit,
}: KpiCardProps) {
  const deltaNum = delta !== undefined ? parseDelta(delta) : undefined;
  const isPositive = deltaNum !== undefined && deltaNum > 0;
  const isNegative = deltaNum !== undefined && deltaNum < 0;

  const sparkData = (trend ?? []).map((v, i) => ({ i, v }));

  return (
    <div
      role={onClick ? "button" : undefined}
      tabIndex={onClick ? 0 : undefined}
      onClick={onClick}
      onKeyDown={
        onClick
          ? (e) => {
              if (e.key === "Enter" || e.key === " ") onClick();
            }
          : undefined
      }
      className={cn(
        "relative flex flex-col gap-1 rounded-xl border border-[color:var(--border-default)] bg-[color:var(--bg-surface)] p-4",
        onClick &&
          "cursor-pointer transition-shadow hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[color:var(--focus-ring)]",
        className
      )}
      aria-label={`${label}: ${value}${unit ? " " + unit : ""}${delta !== undefined ? `, thay đổi ${delta}` : ""}`}
    >
      {/* Label */}
      <span className="text-xs font-medium text-[color:var(--text-muted)] leading-none truncate">
        {label}
      </span>

      {/* Value */}
      <div className="flex items-baseline gap-1.5 mt-0.5">
        <span
          className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums text-[color:var(--text-primary)]"
          style={{ fontVariantNumeric: "tabular-nums slashed-zero" }}
        >
          {typeof value === "number" ? value.toLocaleString("vi-VN") : value}
        </span>
        {unit && (
          <span className="text-xs text-[color:var(--text-muted)]">{unit}</span>
        )}
      </div>

      {/* Delta */}
      {deltaNum !== undefined && (
        <div
          className={cn(
            "flex items-center gap-0.5 text-xs font-medium",
            isPositive && "text-[color:var(--status-done)]",
            isNegative && "text-[color:var(--status-critical)]",
            !isPositive && !isNegative && "text-[color:var(--text-muted)]"
          )}
          aria-label={`Thay đổi: ${delta}`}
        >
          {isPositive && <TrendingUp className="h-3 w-3" aria-hidden="true" />}
          {isNegative && <TrendingDown className="h-3 w-3" aria-hidden="true" />}
          {!isPositive && !isNegative && <Minus className="h-3 w-3" aria-hidden="true" />}
          <span>
            {isPositive && "+"}
            {typeof delta === "number"
              ? `${delta.toFixed(1)}%`
              : delta}
          </span>
          <span className="text-[color:var(--text-muted)] font-normal">so với kỳ trước</span>
        </div>
      )}

      {/* Sparkline mini */}
      {sparkData.length > 0 && (
        <div className="mt-1 h-10" aria-hidden="true">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={sparkData} margin={{ top: 2, right: 0, left: 0, bottom: 2 }}>
              <defs>
                <linearGradient id="sparkGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={chartPalette[0]} stopOpacity={0.4} />
                  <stop offset="100%" stopColor={chartPalette[0]} stopOpacity={0.02} />
                </linearGradient>
              </defs>
              <Area
                type="monotone"
                dataKey="v"
                stroke={chartPalette[0]}
                strokeWidth={1.5}
                fill="url(#sparkGrad)"
                dot={false}
                isAnimationActive={false}
              />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
}
