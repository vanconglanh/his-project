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
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import { useApiPartnerUsageStats } from "@/lib/hooks/use-api-partners";

interface ApiPartnerUsageChartProps {
  partnerId: string;
}

export function ApiPartnerUsageChart({ partnerId }: ApiPartnerUsageChartProps) {
  const { data, isLoading } = useApiPartnerUsageStats(partnerId);

  if (isLoading) {
    return (
      <div className="h-40 w-full animate-pulse rounded-lg bg-muted" />
    );
  }

  if (!data?.by_day?.length) {
    return (
      <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">
        Chưa có dữ liệu usage
      </div>
    );
  }

  const chartData = data.by_day.map((d) => ({
    date: format(parseISO(d.date), "dd/MM", { locale: vi }),
    count: d.count,
  }));

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-3 gap-3 text-center text-sm">
        <div className="rounded-lg bg-muted/50 p-2">
          <p className="text-xl font-bold">{data.total_requests.toLocaleString("vi-VN")}</p>
          <p className="text-xs text-muted-foreground">Tổng request</p>
        </div>
        <div className="rounded-lg bg-green-50 p-2 dark:bg-green-950">
          <p className="text-xl font-bold text-green-700 dark:text-green-300">
            {data.success_count.toLocaleString("vi-VN")}
          </p>
          <p className="text-xs text-muted-foreground">Thành công</p>
        </div>
        <div className="rounded-lg bg-red-50 p-2 dark:bg-red-950">
          <p className="text-xl font-bold text-red-700 dark:text-red-300">
            {data.error_count.toLocaleString("vi-VN")}
          </p>
          <p className="text-xs text-muted-foreground">Lỗi</p>
        </div>
      </div>

      <ResponsiveContainer width="100%" height={160}>
        <AreaChart data={chartData} margin={{ top: 4, right: 4, left: -16, bottom: 0 }}>
          <defs>
            <linearGradient id="colorCount" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.3} />
              <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis dataKey="date" tick={{ fontSize: 10 }} />
          <YAxis tick={{ fontSize: 10 }} />
          <Tooltip
            formatter={(v) => [Number(v).toLocaleString("vi-VN"), "Requests"]}
          />
          <Area
            type="monotone"
            dataKey="count"
            stroke="hsl(var(--primary))"
            fill="url(#colorCount)"
            strokeWidth={2}
          />
        </AreaChart>
      </ResponsiveContainer>

      {data.by_endpoint?.length > 0 && (
        <div className="space-y-1">
          <p className="text-xs font-medium text-muted-foreground uppercase">Top endpoints</p>
          {data.by_endpoint.slice(0, 5).map((ep) => (
            <div key={ep.path} className="flex items-center justify-between text-xs">
              <code className="truncate text-foreground/70 max-w-[70%]">{ep.path}</code>
              <span className="font-medium">{ep.count.toLocaleString("vi-VN")}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
