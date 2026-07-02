"use client";

import {
  AreaChart,
  Area,
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
}

export function RevenueTrendChart({ data }: Props) {
  const rows = data ?? [];
  if (rows.length === 0) {
    return <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">Chưa có dữ liệu</div>;
  }
  return (
    <ResponsiveContainer width="100%" height={200}>
      <AreaChart data={rows} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
        <defs>
          <linearGradient id="revGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.35} />
            <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
          </linearGradient>
        </defs>
        <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
        <XAxis
          dataKey="label"
          tick={{ fontSize: 11 }}
          interval="preserveStartEnd"
          className="fill-muted-foreground"
        />
        <YAxis
          tickFormatter={formatVND}
          tick={{ fontSize: 11 }}
          width={48}
          className="fill-muted-foreground"
        />
        <Tooltip
          formatter={(v) => [`${Number(v).toLocaleString("vi-VN")} ₫`, "Doanh thu"]}
          contentStyle={{ fontSize: 12 }}
        />
        <Area
          type="monotone"
          dataKey="value"
          stroke="hsl(var(--primary))"
          strokeWidth={2}
          fill="url(#revGrad)"
        />
      </AreaChart>
    </ResponsiveContainer>
  );
}
