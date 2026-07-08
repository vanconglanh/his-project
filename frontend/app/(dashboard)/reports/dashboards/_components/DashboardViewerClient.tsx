"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { AlertCircle, Pencil, RefreshCw } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { Can } from "@/components/auth/Can";
import { useReportCatalog, useReportDashboard, useReportDashboardData } from "@/lib/hooks/use-reports";
import { ReportGrid } from "@/components/domain/reports-engine/ReportGrid";
import { ReportKpiRow } from "@/components/domain/reports-engine/ReportKpiRow";
import { ReportChart } from "@/components/domain/reports-engine/ReportChart";
import {
  getReportPresetRange,
  REPORT_DATE_PRESET_LABELS,
  type ReportDatePreset,
} from "@/components/domain/reports-engine/report-date-presets";
import type { ReportDashboardWidgetData } from "@/lib/api/reports";

interface DashboardViewerClientProps {
  id: string;
}

export function DashboardViewerClient({ id }: DashboardViewerClientProps) {
  const router = useRouter();
  const [preset, setPreset] = useState<ReportDatePreset>("thisMonth");
  const [range, setRange] = useState(() => getReportPresetRange("thisMonth"));

  const { data: catalog = [] } = useReportCatalog();
  const { data: dashboardConfig } = useReportDashboard(id);
  const { data, isLoading, isFetching, isError, refetch } = useReportDashboardData(id, range);

  function handlePresetChange(value: string | null) {
    if (!value) return;
    const p = value as ReportDatePreset;
    setPreset(p);
    if (p !== "custom") setRange(getReportPresetRange(p));
  }

  const catalogByCode = new Map(catalog.map((r) => [r.code, r]));

  return (
    <div className="space-y-4">
      <PageHeader
        title={data?.title ?? "Bảng điều khiển"}
        description="Xem tổng hợp nhiều báo cáo trong 1 khoảng thời gian"
        actions={
          <Can permission="report.build">
            <Button
              type="button"
              variant="outline"
              className="gap-1.5"
              onClick={() => router.push(`/reports/dashboards/builder?edit=${id}`)}
            >
              <Pencil className="h-4 w-4" />
              Sửa bảng điều khiển
            </Button>
          </Can>
        }
      />

      <Card>
        <CardContent className="flex flex-wrap items-end gap-3 pt-4">
          <div className="flex flex-col gap-1">
            <Label className="text-xs">Khoảng thời gian</Label>
            <Select items={REPORT_DATE_PRESET_LABELS} value={preset} onValueChange={handlePresetChange}>
              <SelectTrigger className="h-9 w-36">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {(Object.keys(REPORT_DATE_PRESET_LABELS) as ReportDatePreset[]).map((key) => (
                  <SelectItem key={key} value={key}>
                    {REPORT_DATE_PRESET_LABELS[key]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex flex-col gap-1">
            <Label className="text-xs">Từ ngày</Label>
            <Input
              type="date"
              value={range.from}
              onChange={(e) => {
                setPreset("custom");
                setRange((r) => ({ ...r, from: e.target.value }));
              }}
              className="h-9 w-36"
            />
          </div>
          <div className="flex flex-col gap-1">
            <Label className="text-xs">Đến ngày</Label>
            <Input
              type="date"
              value={range.to}
              onChange={(e) => {
                setPreset("custom");
                setRange((r) => ({ ...r, to: e.target.value }));
              }}
              className="h-9 w-36"
            />
          </div>
          <Button type="button" variant="outline" onClick={() => refetch()} className="ml-auto gap-2">
            <RefreshCw className={isFetching ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
            Làm mới
          </Button>
        </CardContent>
      </Card>

      {isLoading && (
        <div className="grid grid-cols-12 gap-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="col-span-12 h-56 md:col-span-6" />
          ))}
        </div>
      )}

      {isError && (
        <Alert className="border-destructive/40 bg-destructive/5">
          <AlertCircle className="h-4 w-4 text-destructive" />
          <AlertTitle className="text-destructive">Không tải được bảng điều khiển</AlertTitle>
          <AlertDescription className="flex items-center justify-between gap-3">
            <span>Đã xảy ra lỗi khi tải dữ liệu. Vui lòng thử lại.</span>
            <Button size="sm" variant="outline" onClick={() => refetch()} className="gap-2 shrink-0">
              <RefreshCw className="h-3.5 w-3.5" />
              Thử lại
            </Button>
          </AlertDescription>
        </Alert>
      )}

      {!isLoading && !isError && data && data.widgets.length === 0 && (
        <EmptyState variant="generic" title="Chưa có widget" description="Bảng điều khiển này chưa có widget nào." />
      )}

      {!isLoading && !isError && data && data.widgets.length > 0 && (
        <div className="grid grid-cols-12 gap-4">
          {data.widgets.map((widget, idx) => (
            <DashboardWidgetCard
              key={`${widget.report_code}-${idx}`}
              widget={widget}
              /** BE /data không trả w/h — lấy layout từ config dashboard theo cùng thứ tự widget. */
              w={dashboardConfig?.widgets[idx]?.w ?? 6}
              chartConfig={catalogByCode.get(widget.report_code)?.chart ?? null}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function DashboardWidgetCard({
  widget,
  w,
  chartConfig,
}: {
  widget: ReportDashboardWidgetData;
  w: number;
  chartConfig: { type: "bar" | "line" | "area" | "pie"; dims: string[]; measure: string } | null;
}) {
  const rowCount = widget.payload.groups
    ? widget.payload.groups.reduce((sum, g) => sum + g.rows.length, 0)
    : widget.payload.rows?.length ?? 0;

  return (
    <Card className="col-span-12" style={{ gridColumn: `span ${Math.min(Math.max(w, 1), 12)} / span ${Math.min(Math.max(w, 1), 12)}` }}>
      <CardHeader>
        <CardTitle className="truncate">{widget.title}</CardTitle>
      </CardHeader>
      <CardContent>
        {widget.widget_type === "KPI" ? (
          <ReportKpiRow kpis={widget.payload.kpis} />
        ) : widget.widget_type === "CHART" && chartConfig ? (
          <ReportChart type={chartConfig.type} data={widget.payload} dims={chartConfig.dims} measure={chartConfig.measure} />
        ) : (
          <ReportGrid
            columns={widget.payload.columns}
            groups={widget.payload.groups}
            rows={widget.payload.rows}
            totals={widget.payload.totals}
            meta={{ page: 1, page_size: Math.max(rowCount, 1), total: rowCount }}
            page={1}
            onPageChange={() => undefined}
            isLoading={false}
          />
        )}
      </CardContent>
    </Card>
  );
}
