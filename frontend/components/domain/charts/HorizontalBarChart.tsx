"use client";

import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import type { ChartDataPoint } from "@/lib/api/dashboard";

function formatVND(v: number) {
  if (v >= 1_000_000) return `${(v / 1_000_000).toFixed(1)}M`;
  if (v >= 1_000) return `${(v / 1_000).toFixed(0)}K`;
  return String(v);
}

interface Props {
  data: ChartDataPoint[];
  valueLabel?: string;
  formatValue?: (v: number) => string;
  color?: string;
}

export function HorizontalBarChart({
  data,
  valueLabel = "Giá trị",
  formatValue = formatVND,
  color = "hsl(var(--primary))",
}: Props) {
  const rows = data ?? [];
  if (rows.length === 0) {
    return (
      <div className="flex h-[180px] items-center justify-center text-sm text-muted-foreground">
        Chưa có dữ liệu
      </div>
    );
  }
  return (
    <ResponsiveContainer width="100%" height={Math.max(180, rows.length * 36)}>
      <BarChart
        data={rows}
        layout="vertical"
        margin={{ top: 4, right: 8, bottom: 0, left: 4 }}
      >
        <CartesianGrid strokeDasharray="3 3" horizontal={false} className="stroke-muted" />
        <XAxis
          type="number"
          tickFormatter={formatValue}
          tick={{ fontSize: 11 }}
          className="fill-muted-foreground"
        />
        <YAxis
          type="category"
          dataKey="label"
          tick={{ fontSize: 11 }}
          width={130}
          className="fill-muted-foreground"
        />
        <Tooltip
          formatter={(v) => [formatValue(Number(v)), valueLabel]}
          contentStyle={{ fontSize: 12 }}
        />
        <Bar dataKey="value" fill={color} radius={[0, 3, 3, 0]} maxBarSize={20} />
      </BarChart>
    </ResponsiveContainer>
  );
}
