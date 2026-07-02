/**
 * chart-theme.ts — Recharts dark theme defaults cho Pro-Diab HIS
 * Spec: docs/design/research-his-ui-patterns.md mục 5
 * Color-blind safe palette (Okabe-Ito inspired)
 */

/** 6 màu chart lấy từ CSS vars (phải match globals.css) */
export const chartPalette = [
  "var(--chart-1)", // teal — doanh thu / chính
  "var(--chart-2)", // blue — BHYT
  "var(--chart-3)", // amber — viện phí
  "var(--chart-4)", // violet — phụ
  "var(--chart-5)", // pink — phụ
  "var(--chart-6)", // slate — baseline
] as const;

/** Màu tài chính: dương xanh lá, âm đỏ (chuẩn quốc tế) */
export const chartFinanceColors = {
  positive: "var(--status-done)",   // #10B981
  negative: "var(--status-critical)", // #EF4444
} as const;

/** Cấu hình mặc định cho Recharts components trong dark mode */
export const chartTheme = {
  /** Màu text trục, legend */
  axisStroke: "var(--text-muted)",
  /** Màu grid lines */
  gridStroke: "var(--border-subtle)",
  /** Background tooltip */
  tooltipBg: "var(--bg-elevated)",
  /** Border tooltip */
  tooltipBorder: "var(--border-default)",
  /** Text tooltip */
  tooltipText: "var(--text-primary)",
  /** Font size trục / tooltip */
  fontSize: 12,
  /** Margin mặc định cho ResponsiveContainer */
  margin: { top: 4, right: 4, left: 4, bottom: 4 },
} as const;

/** Props mặc định cho <CartesianGrid> */
export const defaultGridProps = {
  strokeDasharray: "3 3",
  stroke: chartTheme.gridStroke,
  strokeOpacity: 0.5,
} as const;

/** Props mặc định cho <XAxis> / <YAxis> */
export const defaultAxisProps = {
  tick: { fontSize: chartTheme.fontSize, fill: chartTheme.axisStroke },
  axisLine: { stroke: chartTheme.gridStroke },
  tickLine: false,
} as const;

/** Props mặc định cho <Tooltip> */
export const defaultTooltipProps = {
  contentStyle: {
    backgroundColor: chartTheme.tooltipBg,
    border: `1px solid ${chartTheme.tooltipBorder}`,
    borderRadius: "8px",
    color: chartTheme.tooltipText,
    fontSize: chartTheme.fontSize,
  },
  cursor: { fill: "var(--border-subtle)", fillOpacity: 0.5 },
} as const;
