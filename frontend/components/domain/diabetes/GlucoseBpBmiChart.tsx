"use client";

import { useMemo } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ReferenceLine,
  ResponsiveContainer,
} from "recharts";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { Skeleton } from "@/components/ui/skeleton";
import { defaultGridProps, defaultAxisProps, defaultTooltipProps } from "@/lib/chart-theme";
import type { DiabetesTrajectoryResponse } from "@/lib/api/types";

interface Props {
  data?: DiabetesTrajectoryResponse;
  isLoading?: boolean;
}

export function GlucoseBpBmiChart({ data, isLoading }: Props) {
  const chartData = useMemo(() => {
    if (!data) return [];
    return [...data.series]
      .sort((a, b) => new Date(a.assessed_at).getTime() - new Date(b.assessed_at).getTime())
      .map((p) => ({
        date: format(new Date(p.assessed_at), "dd/MM/yy", { locale: vi }),
        "ĐH đói": p.fasting_glucose ?? null,
        "HA tâm thu": p.bp_systolic ?? null,
        "HA tâm trương": p.bp_diastolic ?? null,
        BMI: p.bmi ?? null,
      }));
  }, [data]);

  const targetSys = data?.targets.find((t) => t.param === "BP_SYS");
  const targetDia = data?.targets.find((t) => t.param === "BP_DIA");

  if (isLoading) {
    return <Skeleton className="h-64 w-full" />;
  }

  const hasAnyValue = chartData.some(
    (d) => d["ĐH đói"] != null || d["HA tâm thu"] != null || d["HA tâm trương"] != null || d.BMI != null
  );

  if (!data || chartData.length === 0 || !hasAnyValue) {
    return (
      <div className="flex flex-col items-center justify-center py-10 text-center text-muted-foreground gap-1">
        <p className="text-sm font-medium">Chưa có dữ liệu đường huyết / huyết áp / BMI</p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <h4 className="text-sm font-medium">Đường huyết đói, huyết áp &amp; BMI</h4>
      <ResponsiveContainer width="100%" height={280}>
        <LineChart data={chartData} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
          <CartesianGrid {...defaultGridProps} />
          <XAxis dataKey="date" {...defaultAxisProps} />
          <YAxis yAxisId="left" {...defaultAxisProps} />
          <YAxis yAxisId="right" orientation="right" {...defaultAxisProps} />
          <Tooltip {...defaultTooltipProps} />
          <Legend wrapperStyle={{ fontSize: 12 }} />
          {targetSys && (
            <ReferenceLine
              yAxisId="left"
              y={targetSys.target_value}
              stroke="var(--status-warning)"
              strokeDasharray="4 2"
              label={{ value: `HA mục tiêu ${targetSys.target_op} ${targetSys.target_value}`, fontSize: 10, fill: "var(--status-warning)" }}
            />
          )}
          {targetDia && (
            <ReferenceLine yAxisId="left" y={targetDia.target_value} stroke="var(--status-warning)" strokeDasharray="4 2" />
          )}
          <Line yAxisId="left" type="monotone" dataKey="ĐH đói" stroke="var(--chart-2)" strokeWidth={2} dot={{ r: 3 }} connectNulls />
          <Line yAxisId="left" type="monotone" dataKey="HA tâm thu" stroke="var(--chart-3)" strokeWidth={2} dot={{ r: 3 }} connectNulls />
          <Line yAxisId="left" type="monotone" dataKey="HA tâm trương" stroke="var(--chart-4)" strokeWidth={2} dot={{ r: 3 }} connectNulls />
          <Line yAxisId="right" type="monotone" dataKey="BMI" stroke="var(--chart-5)" strokeWidth={2} dot={{ r: 3 }} connectNulls />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
