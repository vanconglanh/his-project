"use client";

import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from "recharts";

interface Props {
  requested: number;
  approved: number;
  rejected: number;
}

function formatVnd(value: number) {
  return new Intl.NumberFormat("vi-VN", { notation: "compact", maximumFractionDigits: 1 }).format(value);
}

export function BhytAmountChart({ requested, approved, rejected }: Props) {
  const data = [
    { name: "Yêu cầu", value: requested, color: "var(--status-waiting)" },
    { name: "Duyệt", value: approved, color: "var(--status-done)" },
    { name: "Từ chối", value: rejected, color: "var(--status-critical)" },
  ];

  return (
    <ResponsiveContainer width="100%" height={120}>
      <BarChart data={data} margin={{ top: 4, right: 8, left: 0, bottom: 0 }}>
        <XAxis dataKey="name" tick={{ fontSize: 11 }} axisLine={false} tickLine={false} />
        <YAxis hide />
        <Tooltip
          formatter={(value) => [
            new Intl.NumberFormat("vi-VN").format(Number(value)) + " ₫",
            "",
          ]}
          labelFormatter={(l) => l}
        />
        <Bar dataKey="value" radius={[4, 4, 0, 0]}>
          {data.map((entry, idx) => (
            <Cell key={idx} fill={entry.color} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
