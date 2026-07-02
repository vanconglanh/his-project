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

interface Props {
  data: ChartDataPoint[];
}

export function EncountersTrendChart({ data }: Props) {
  const rows = data ?? [];
  if (rows.length === 0) {
    return <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">Chưa có dữ liệu</div>;
  }
  return (
    <ResponsiveContainer width="100%" height={200}>
      <BarChart data={rows} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
        <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
        <XAxis
          dataKey="label"
          tick={{ fontSize: 11 }}
          interval="preserveStartEnd"
          className="fill-muted-foreground"
        />
        <YAxis tick={{ fontSize: 11 }} width={32} className="fill-muted-foreground" />
        <Tooltip
          formatter={(v) => [Number(v), "Lượt khám"]}
          contentStyle={{ fontSize: 12 }}
        />
        <Bar dataKey="value" fill="hsl(var(--primary))" radius={[3, 3, 0, 0]} maxBarSize={24} />
      </BarChart>
    </ResponsiveContainer>
  );
}
