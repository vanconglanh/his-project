"use client";

import { useMemo } from "react";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from "recharts";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { useIntegrationStats } from "@/lib/hooks/use-lab-integration";

export function LabIntegrationDashboard() {
  const { data: stats, isLoading } = useIntegrationStats(7);

  const chartData = useMemo(() => {
    if (!stats) return [];
    return stats.by_partner.map((p) => ({
      name: p.partner_name,
      "Gửi đi": p.outbound_sent,
      "Nhận về": p.inbound_received,
      "Thời gian TB (ph)": Math.round(p.avg_turnaround_minutes),
    }));
  }, [stats]);

  if (isLoading) {
    return <Skeleton className="h-64 w-full rounded-xl" />;
  }

  if (!stats) return null;

  const successRate =
    stats.outbound_total > 0
      ? (((stats.outbound_total - stats.outbound_failed) / stats.outbound_total) * 100).toFixed(1)
      : "N/A";

  return (
    <div className="space-y-6">
      {/* Summary cards */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-sm text-muted-foreground font-normal">Tổng gửi đi</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{stats.outbound_total}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-sm text-muted-foreground font-normal">Gửi thất bại</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold text-red-600">{stats.outbound_failed}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-sm text-muted-foreground font-normal">Tổng nhận về</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold">{stats.inbound_total}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-sm text-muted-foreground font-normal">Tỷ lệ thành công</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold text-green-600">{successRate}%</p>
          </CardContent>
        </Card>
      </div>

      {/* Chart */}
      {chartData.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Thống kê theo đối tác (7 ngày qua)</CardTitle>
          </CardHeader>
          <CardContent>
            <ResponsiveContainer width="100%" height={220}>
              <BarChart data={chartData} margin={{ top: 4, right: 16, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip />
                <Legend />
                <Bar dataKey="Gửi đi" fill="var(--chart-1)" radius={[3, 3, 0, 0]} />
                <Bar dataKey="Nhận về" fill="var(--chart-2)" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
