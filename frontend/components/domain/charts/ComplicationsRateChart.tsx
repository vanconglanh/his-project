"use client";

import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";

const COLORS = [
  "var(--chart-1)",
  "var(--chart-2)",
  "var(--chart-3)",
  "var(--chart-4)",
  "var(--chart-5)",
];

interface ComplicationEntry {
  name: string;
  value: number;
}

interface Props {
  data: ComplicationEntry[];
}

export function ComplicationsRateChart({ data }: Props) {
  const rows = data ?? [];
  if (rows.length === 0) {
    return <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">Chưa có dữ liệu</div>;
  }
  return (
    <ResponsiveContainer width="100%" height={200}>
      <PieChart>
        <Pie
          data={rows}
          cx="50%"
          cy="50%"
          innerRadius={50}
          outerRadius={80}
          paddingAngle={3}
          dataKey="value"
          label={({ percent }) =>
            `${((percent ?? 0) * 100).toFixed(0)}%`
          }
          labelLine={false}
        >
          {rows.map((_, i) => (
            <Cell key={i} fill={COLORS[i % COLORS.length]} />
          ))}
        </Pie>
        <Tooltip
          formatter={(v) => [Number(v), "Bệnh nhân"]}
          contentStyle={{ fontSize: 12 }}
        />
        <Legend iconSize={10} wrapperStyle={{ fontSize: 11 }} />
      </PieChart>
    </ResponsiveContainer>
  );
}
