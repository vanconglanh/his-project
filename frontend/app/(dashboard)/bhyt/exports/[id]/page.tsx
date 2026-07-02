"use client";

import { use } from "react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Separator } from "@/components/ui/separator";
import { ChevronLeft, Download, RefreshCw } from "lucide-react";
import { BhytExportStatusBadge } from "@/components/domain/bhyt/BhytExportStatusBadge";
import { BhytExportStepper } from "@/components/domain/bhyt/BhytExportStepper";
import { BhytTablePreview } from "@/components/domain/bhyt/BhytTablePreview";
import { BhytAmountChart } from "@/components/domain/bhyt/BhytAmountChart";
import { useBhytExport, useRegenerateBhytXml } from "@/lib/hooks/use-bhyt-export";
import { getBhytAllXmlDownloadUrl } from "@/lib/api/bhyt-export";
import { toast } from "sonner";

function formatVnd(n: number) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(n);
}

export default function BhytExportDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { data: exportData, isLoading } = useBhytExport(id);
  const regenerate = useRegenerateBhytXml();

  function handleRegenerate() {
    regenerate.mutate(id, {
      onSuccess: () => toast.success("Đã sinh lại XML"),
      onError: () => toast.error("Không thể sinh lại — kỳ đã bị khoá"),
    });
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <div className="grid gap-4 sm:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-24" />)}
        </div>
      </div>
    );
  }

  if (!exportData) {
    return (
      <div className="py-16 text-center">
        <p className="text-muted-foreground">Không tìm thấy kỳ export</p>
        <Button variant="link" className="mt-2" render={<Link href="/bhyt" />}>
          Quay lại danh sách
        </Button>
      </div>
    );
  }

  const isLocked = ["SUBMITTED", "APPROVED", "PARTIALLY_REJECTED", "REJECTED"].includes(exportData.status);
  const canRegenerate = exportData.status === "GENERATED" || exportData.status === "VALIDATED";

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start gap-4">
        <Button variant="ghost" size="icon" className="h-8 w-8 mt-0.5" render={<Link href="/bhyt" aria-label="Quay lại" />}>
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-3 flex-wrap">
            <h2 className="text-xl font-bold">Kỳ BHYT {exportData.period_month}</h2>
            <BhytExportStatusBadge status={exportData.status} />
          </div>
          <p className="text-sm text-muted-foreground mt-0.5">
            {exportData.encounter_count.toLocaleString()} lượt khám — Cập nhật {new Date(exportData.updated_at).toLocaleDateString("vi-VN")}
          </p>
        </div>
        <div className="flex gap-2 flex-wrap">
          {canRegenerate && (
            <Button variant="outline" size="sm" onClick={handleRegenerate} disabled={regenerate.isPending}>
              <RefreshCw className="mr-2 h-3.5 w-3.5" />
              Sinh lại
            </Button>
          )}
          {exportData.status !== "DRAFT" && (
            <Button variant="outline" size="sm" render={<a href={getBhytAllXmlDownloadUrl(id)} target="_blank" rel="noopener noreferrer" />}>
              <Download className="mr-2 h-3.5 w-3.5" />
              Tải ZIP 5 bảng
            </Button>
          )}
        </div>
      </div>

      {/* Amount summary cards */}
      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-xs font-medium text-muted-foreground uppercase">Tổng yêu cầu</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums">{formatVnd(exportData.total_requested_amount)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-xs font-medium text-green-700 uppercase">Được duyệt</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums text-green-700">{formatVnd(exportData.total_approved_amount)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1">
            <CardTitle className="text-xs font-medium text-red-700 uppercase">Từ chối</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums text-red-700">{formatVnd(exportData.total_rejected_amount)}</p>
          </CardContent>
        </Card>
      </div>

      {/* Chart */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm">Biểu đồ số tiền</CardTitle>
        </CardHeader>
        <CardContent>
          <BhytAmountChart
            requested={exportData.total_requested_amount}
            approved={exportData.total_approved_amount}
            rejected={exportData.total_rejected_amount}
          />
        </CardContent>
      </Card>

      <Separator />

      {/* Stepper */}
      <div>
        <h3 className="text-base font-semibold mb-4">Bước xuất khẩu</h3>
        <BhytExportStepper exportData={exportData} />
      </div>

      <Separator />

      {/* Table preview */}
      {exportData.status !== "DRAFT" && (
        <div>
          <h3 className="text-base font-semibold mb-4">Preview Bảng 1-5</h3>
          <BhytTablePreview exportId={id} />
        </div>
      )}

      {/* Reconcile link */}
      {["SUBMITTED", "APPROVED", "PARTIALLY_REJECTED", "REJECTED"].includes(exportData.status) && (
        <div className="rounded-lg border bg-muted/50 p-4 flex items-center justify-between">
          <div>
            <p className="text-sm font-medium">Đối soát kết quả giám định</p>
            <p className="text-xs text-muted-foreground mt-0.5">Upload file kết quả từ cổng BHYT để đối soát từng dòng</p>
          </div>
          <Button variant="outline" size="sm" render={<Link href={`/bhyt?tab=doi-soat&exportId=${id}`} />}>
            Sang tab Đối soát
          </Button>
        </div>
      )}
    </div>
  );
}
