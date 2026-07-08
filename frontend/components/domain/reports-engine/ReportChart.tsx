"use client";

import type { ReportChartType, ReportDataPayload } from "@/lib/api/reports";
import type { ChartDataPoint } from "@/lib/api/dashboard";
import { RevenueTrendChart } from "@/components/domain/charts/RevenueTrendChart";
import { HorizontalBarChart } from "@/components/domain/charts/HorizontalBarChart";
import { ComplicationsRateChart } from "@/components/domain/charts/ComplicationsRateChart";
import { formatNumberVi } from "./report-format";

interface ReportChartProps {
  type: ReportChartType;
  data: ReportDataPayload;
  /** Cột chiều (dimension) dùng làm nhãn trục X / lát cắt — chỉ dùng phần tử đầu tiên. */
  dims: string[];
  /** Cột số liệu (measure) dùng làm giá trị. */
  measure: string;
}

/**
 * Wrapper map ReportDataPayload (columns/rows|groups) → ChartDataPoint[] rồi tái dùng
 * các chart recharts sẵn có trong components/domain/charts — KHÔNG viết chart mới.
 */
export function ReportChart({ type, data, dims, measure }: ReportChartProps) {
  const dimKey = dims[0];

  if (!dimKey || !measure) {
    return (
      <div className="flex h-[200px] items-center justify-center text-sm text-muted-foreground">
        Chưa chọn đủ chiều/số liệu cho biểu đồ.
      </div>
    );
  }

  const flatRows = data.groups ? data.groups.flatMap((g) => g.rows) : data.rows ?? [];

  const points: ChartDataPoint[] = flatRows.map((row) => {
    const rawValue = row[measure];
    return {
      label: row[dimKey] === null || row[dimKey] === undefined ? "–" : String(row[dimKey]),
      value: typeof rawValue === "number" ? rawValue : Number(rawValue ?? 0) || 0,
    };
  });

  if (type === "pie") {
    const pieData = points.map((p) => ({ name: p.label, value: p.value }));
    return <ComplicationsRateChart data={pieData} />;
  }

  if (type === "line" || type === "area") {
    return <RevenueTrendChart data={points} />;
  }

  // "bar" (mặc định)
  return <HorizontalBarChart data={points} formatValue={formatNumberVi} valueLabel="Giá trị" />;
}
