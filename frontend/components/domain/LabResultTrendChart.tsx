"use client";

import { useMemo } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ReferenceLine,
  ResponsiveContainer,
  Legend,
} from "recharts";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import type { LabTrendResponse } from "@/lib/api/lab-results";

interface LabResultTrendChartProps {
  data: LabTrendResponse;
}

export function LabResultTrendChart({ data }: LabResultTrendChartProps) {
  const chartData = useMemo(
    () =>
      data.points.map((p) => ({
        date: format(new Date(p.performed_at), "dd/MM/yy", { locale: vi }),
        value: p.value_numeric,
        flag: p.flag,
      })),
    [data.points]
  );

  const CustomDot = (props: { cx: number; cy: number; payload: { flag: string } }) => {
    const { cx, cy, payload } = props;
    const color = payload.flag === "CRITICAL"
      ? "var(--status-critical)"
      : payload.flag === "NORMAL"
      ? "var(--status-done)"
      : "var(--status-warning)";
    return <circle cx={cx} cy={cy} r={5} fill={color} stroke="#fff" strokeWidth={2} />;
  };

  return (
    <div className="w-full">
      <p className="text-sm font-medium mb-1">
        {data.test_name} ({data.unit})
      </p>
      <ResponsiveContainer width="100%" height={220}>
        <LineChart data={chartData} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
          <XAxis dataKey="date" tick={{ fontSize: 11 }} />
          <YAxis tick={{ fontSize: 11 }} />
          <Tooltip
            formatter={(val) => [`${val ?? ""} ${data.unit}`, data.test_name]}
          />
          <Legend />
          {data.reference_range_low != null && (
            <ReferenceLine
              y={data.reference_range_low}
              stroke="var(--status-warning)"
              strokeDasharray="4 2"
              label={{ value: "Min", fontSize: 10, fill: "var(--status-warning)" }}
            />
          )}
          {data.reference_range_high != null && (
            <ReferenceLine
              y={data.reference_range_high}
              stroke="var(--status-critical)"
              strokeDasharray="4 2"
              label={{ value: "Max", fontSize: 10, fill: "var(--status-critical)" }}
            />
          )}
          <Line
            type="monotone"
            dataKey="value"
            name={data.test_name}
            stroke="var(--chart-1)"
            strokeWidth={2}
            dot={<CustomDot cx={0} cy={0} payload={{ flag: "NORMAL" }} />}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
