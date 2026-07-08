"use client";

import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { ReportChartType } from "@/lib/api/reports";
import { type ChartDraft, type ColumnDraft, type ReportBuilderViewType } from "./types";

const CHART_TYPE_LABELS: Record<ReportChartType, string> = {
  bar: "Cột (Bar)",
  line: "Đường (Line)",
  area: "Vùng (Area)",
  pie: "Tròn (Pie)",
};

interface ViewConfigProps {
  columns: ColumnDraft[];
  viewType: ReportBuilderViewType;
  chart: ChartDraft;
  onViewTypeChange: (viewType: ReportBuilderViewType) => void;
  onChartChange: (chart: ChartDraft) => void;
}

export function ViewConfig({ columns, viewType, chart, onViewTypeChange, onChartChange }: ViewConfigProps) {
  const dimensionColumns = columns.filter((c) => c.role === "DIMENSION");
  const measureColumns = columns.filter((c) => c.role === "MEASURE");

  return (
    <div className="space-y-3">
      <Tabs value={viewType} onValueChange={(v) => v && onViewTypeChange(v as ReportBuilderViewType)}>
        <TabsList>
          <TabsTrigger value="TABLE">Bảng</TabsTrigger>
          <TabsTrigger value="CHART">Biểu đồ</TabsTrigger>
        </TabsList>
      </Tabs>

      {viewType === "CHART" && (
        <div className="flex flex-wrap items-end gap-3 rounded-lg border p-3">
          <div className="flex flex-col gap-1">
            <span className="text-xs font-medium text-muted-foreground">Loại biểu đồ</span>
            <Select
              items={CHART_TYPE_LABELS}
              value={chart.type}
              onValueChange={(v) => v && onChartChange({ ...chart, type: v as ReportChartType })}
            >
              <SelectTrigger className="h-9 w-40">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {(Object.keys(CHART_TYPE_LABELS) as ReportChartType[]).map((t) => (
                  <SelectItem key={t} value={t}>
                    {CHART_TYPE_LABELS[t]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1">
            <span className="text-xs font-medium text-muted-foreground">Chiều (trục nhãn)</span>
            <Select
              items={Object.fromEntries(dimensionColumns.map((c) => [c.field, c.label]))}
              value={chart.dims[0] ?? ""}
              onValueChange={(v) => onChartChange({ ...chart, dims: v ? [v] : [] })}
            >
              <SelectTrigger className="h-9 w-44">
                <SelectValue placeholder="Chọn cột phân loại" />
              </SelectTrigger>
              <SelectContent>
                {dimensionColumns.map((c) => (
                  <SelectItem key={c.field} value={c.field}>
                    {c.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1">
            <span className="text-xs font-medium text-muted-foreground">Số liệu (giá trị)</span>
            <Select
              items={Object.fromEntries(measureColumns.map((c) => [c.field, c.label]))}
              value={chart.measure}
              onValueChange={(v) => onChartChange({ ...chart, measure: v ?? "" })}
            >
              <SelectTrigger className="h-9 w-44">
                <SelectValue placeholder="Chọn cột số liệu" />
              </SelectTrigger>
              <SelectContent>
                {measureColumns.map((c) => (
                  <SelectItem key={c.field} value={c.field}>
                    {c.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {(dimensionColumns.length === 0 || measureColumns.length === 0) && (
            <p className="w-full text-sm text-[color:var(--status-warning)]">
              Cần ít nhất 1 cột phân loại và 1 cột số liệu trong &quot;Cột đã chọn&quot; để vẽ biểu đồ.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
