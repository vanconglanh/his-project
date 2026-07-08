"use client";

import { AlertCircle } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { EmptyState } from "@/components/ui/EmptyState";
import { Skeleton } from "@/components/ui/skeleton";
import type { ReportDataResult } from "@/lib/api/reports";
import { getErrorMessage } from "@/lib/utils/errors";
import { ReportGrid } from "@/components/domain/reports-engine/ReportGrid";
import { ReportKpiRow } from "@/components/domain/reports-engine/ReportKpiRow";
import { ReportChart } from "@/components/domain/reports-engine/ReportChart";
import { type ChartDraft, type ReportBuilderViewType } from "./types";

interface PreviewPaneProps {
  result: ReportDataResult | undefined;
  isPending: boolean;
  isError: boolean;
  /** Lỗi gốc từ mutation (nếu có) — dùng để hiển thị message backend rõ ràng (vd lỗi công thức calc field 400). */
  error?: unknown;
  hasRun: boolean;
  viewType: ReportBuilderViewType;
  chart: ChartDraft;
}

export function PreviewPane({ result, isPending, isError, error, hasRun, viewType, chart }: PreviewPaneProps) {
  if (!hasRun) {
    return (
      <EmptyState
        variant="search"
        title="Chưa có bản xem trước"
        description='Cấu hình cột/bộ lọc rồi bấm "Xem trước" để xem kết quả (giới hạn 200 dòng).'
      />
    );
  }

  if (isPending) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (isError || !result) {
    return (
      <Alert className="border-destructive/40 bg-destructive/5">
        <AlertCircle className="h-4 w-4 text-destructive" />
        <AlertTitle className="text-destructive">Không xem trước được báo cáo</AlertTitle>
        <AlertDescription>
          {getErrorMessage(error, "Đã xảy ra lỗi khi xem trước dữ liệu. Vui lòng kiểm tra lại cấu hình và thử lại.")}
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="space-y-4">
      {result.data.kpis.length > 0 && <ReportKpiRow kpis={result.data.kpis} />}
      {viewType === "CHART" ? (
        <div className="rounded-lg border p-4">
          <ReportChart type={chart.type} data={result.data} dims={chart.dims} measure={chart.measure} />
        </div>
      ) : (
        <ReportGrid
          columns={result.data.columns}
          groups={result.data.groups}
          rows={result.data.rows}
          totals={result.data.totals}
          meta={result.meta}
          page={1}
          onPageChange={() => undefined}
          isLoading={false}
        />
      )}
    </div>
  );
}
