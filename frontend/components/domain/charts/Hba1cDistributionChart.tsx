"use client";

import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Cell,
  ResponsiveContainer,
} from "recharts";
import type { ChartDataPoint } from "@/lib/api/dashboard";

const COLORS = [
  "var(--scale-hba1c-1)", "var(--scale-hba1c-2)", "var(--scale-hba1c-3)", "var(--scale-hba1c-4)",
  "var(--scale-hba1c-5)", "var(--scale-hba1c-6)", "var(--scale-hba1c-7)", "var(--scale-hba1c-8)",
  "var(--scale-hba1c-9)", "var(--scale-hba1c-10)",
];

interface Props {
  data: ChartDataPoint[];
}

export function Hba1cDistributionChart({ data }: Props) {
  const rows = data ?? [];
  if (rows.length === 0) {
    return <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">Chưa có dữ liệu</div>;
  }
  return (
    <ResponsiveContainer width="100%" height={200}>
      <BarChart data={rows} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
        <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
        <XAxis dataKey="label" tick={{ fontSize: 11 }} className="fill-muted-foreground" />
        <YAxis tick={{ fontSize: 11 }} width={32} className="fill-muted-foreground" />
        <Tooltip
          formatter={(v) => [Number(v), "Bệnh nhân"]}
          contentStyle={{ fontSize: 12 }}
        />
        <Bar dataKey="value" radius={[3, 3, 0, 0]} maxBarSize={32}>
          {rows.map((_, i) => (
            <Cell key={i} fill={COLORS[i % COLORS.length]} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
