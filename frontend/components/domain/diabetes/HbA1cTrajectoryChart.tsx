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

export function HbA1cTrajectoryChart({ data, isLoading }: Props) {
  const chartData = useMemo(() => {
    if (!data) return [];
    return [...data.series]
      .sort((a, b) => new Date(a.assessed_at).getTime() - new Date(b.assessed_at).getTime())
      .map((p) => ({
        date: format(new Date(p.assessed_at), "dd/MM/yy", { locale: vi }),
        HbA1c: p.hba1c ?? null,
      }));
  }, [data]);

  const target = data?.targets.find((t) => t.param === "HBA1C");

  if (isLoading) {
    return <Skeleton className="h-64 w-full" />;
  }

  const hasAnyValue = chartData.some((d) => d.HbA1c != null);

  if (!data || chartData.length === 0 || !hasAnyValue) {
    return (
      <div className="flex flex-col items-center justify-center py-10 text-center text-muted-foreground gap-1">
        <p className="text-sm font-medium">Chưa có dữ liệu HbA1c</p>
        <p className="text-xs">Thêm đánh giá ĐTĐ trong lượt khám để hiển thị biểu đồ xu hướng</p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <h4 className="text-sm font-medium">Xu hướng HbA1c (%)</h4>
      <ResponsiveContainer width="100%" height={260}>
        <LineChart data={chartData} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
          <CartesianGrid {...defaultGridProps} />
          <XAxis dataKey="date" {...defaultAxisProps} />
          <YAxis {...defaultAxisProps} domain={["auto", "auto"]} />
          <Tooltip {...defaultTooltipProps} />
          <Legend wrapperStyle={{ fontSize: 12 }} />
          {target && (
            <ReferenceLine
              y={target.target_value}
              stroke="var(--status-warning)"
              strokeDasharray="4 2"
              label={{
                value: `Mục tiêu ${target.target_op} ${target.target_value}${target.unit ?? "%"}`,
                fontSize: 10,
                fill: "var(--status-warning)",
                position: "insideTopRight",
              }}
            />
          )}
          <Line
            type="monotone"
            dataKey="HbA1c"
            name="HbA1c (%)"
            stroke="var(--chart-1)"
            strokeWidth={2}
            dot={{ r: 4 }}
            connectNulls
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
