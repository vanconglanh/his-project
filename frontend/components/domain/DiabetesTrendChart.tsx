"use client";

import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";
import { useDiabetesHistory } from "@/lib/hooks/use-diabetes";
import { Skeleton } from "@/components/ui/skeleton";
import { format } from "date-fns";
import { vi } from "date-fns/locale";

interface Props {
  patientId: string;
}

export function DiabetesTrendChart({ patientId }: Props) {
  const { data, isLoading } = useDiabetesHistory(patientId);

  if (isLoading) {
    return <Skeleton className="h-64 w-full" />;
  }

  if (!data || data.length === 0) {
    return (
      <p className="text-sm text-muted-foreground text-center py-8">
        Chưa có dữ liệu lịch sử đánh giá ĐTĐ
      </p>
    );
  }

  const chartData = [...data]
    .sort((a, b) => new Date(a.assessed_at).getTime() - new Date(b.assessed_at).getTime())
    .map((item) => ({
      date: format(new Date(item.assessed_at), "dd/MM/yy", { locale: vi }),
      HbA1c: item.hba1c,
      "ĐH đói": item.fasting_glucose,
      "HA tâm thu": item.bp_systolic,
    }));

  return (
    <div className="space-y-2">
      <h4 className="text-sm font-medium">Xu hướng HbA1c & Đường huyết</h4>
      <ResponsiveContainer width="100%" height={280}>
        <LineChart data={chartData} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
          <XAxis dataKey="date" tick={{ fontSize: 12 }} />
          <YAxis yAxisId="left" tick={{ fontSize: 12 }} />
          <YAxis yAxisId="right" orientation="right" tick={{ fontSize: 12 }} />
          <Tooltip
            contentStyle={{
              backgroundColor: "hsl(var(--card))",
              border: "1px solid hsl(var(--border))",
              borderRadius: "8px",
            }}
          />
          <Legend />
          <Line
            yAxisId="left"
            type="monotone"
            dataKey="HbA1c"
            stroke="var(--chart-1)"
            strokeWidth={2}
            dot={{ r: 4 }}
            connectNulls
          />
          <Line
            yAxisId="right"
            type="monotone"
            dataKey="ĐH đói"
            stroke="var(--chart-2)"
            strokeWidth={2}
            dot={{ r: 4 }}
            connectNulls
          />
          <Line
            yAxisId="right"
            type="monotone"
            dataKey="HA tâm thu"
            stroke="var(--chart-3)"
            strokeWidth={2}
            dot={{ r: 4 }}
            connectNulls
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
