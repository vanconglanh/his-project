"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { AlertCircle, Pencil, RefreshCw, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/EmptyState";
import { Can } from "@/components/auth/Can";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import type { ReportCatalogItem } from "@/lib/api/reports";
import { exportReportEngine, type ReportDataQueryParams } from "@/lib/api/reports";
import { useDeleteReportDefinition, useReportData, useReportDefinitions } from "@/lib/hooks/use-reports";
import { ReportFilterPanel, type ReportFilterDraft } from "./ReportFilterPanel";
import { ReportKpiRow } from "./ReportKpiRow";
import { ReportGrid } from "./ReportGrid";
import { getReportPresetRange } from "./report-date-presets";

interface ReportRunnerProps {
  descriptor: ReportCatalogItem;
}

/** Nút "Sửa"/"Xoá" cho báo cáo tự tạo (nhóm UserDefined) — chỉ hiện với quyền report.build. */
function UserDefinedReportActions({ descriptor }: { descriptor: ReportCatalogItem }) {
  const router = useRouter();
  const isUserDefined = descriptor.group === "UserDefined";
  const { data: definitions = [] } = useReportDefinitions(isUserDefined);
  const deleteMutation = useDeleteReportDefinition();
  const [confirmOpen, setConfirmOpen] = useState(false);

  if (!isUserDefined) return null;
  const definition = definitions.find((d) => d.code === descriptor.code);
  if (!definition) return null;

  function handleDelete() {
    deleteMutation.mutate(definition!.id, {
      onSuccess: () => {
        toast.success("Đã xoá báo cáo.");
        setConfirmOpen(false);
        router.replace("/reports");
      },
      onError: () => toast.error("Không xoá được báo cáo. Vui lòng thử lại."),
    });
  }

  return (
    <Can permission="report.build">
      <div className="flex items-center gap-2 mb-3">
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() => router.push(`/reports/builder?edit=${definition.id}`)}
        >
          <Pencil className="h-3.5 w-3.5" />
          Sửa báo cáo
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="gap-1.5 text-destructive"
          onClick={() => setConfirmOpen(true)}
        >
          <Trash2 className="h-3.5 w-3.5" />
          Xoá báo cáo
        </Button>
        <ConfirmDialog
          open={confirmOpen}
          onOpenChange={setConfirmOpen}
          title="Xoá báo cáo tự tạo"
          description={`Bạn có chắc muốn xoá báo cáo "${descriptor.title}"? Hành động này không thể hoàn tác.`}
          variant="destructive"
          isLoading={deleteMutation.isPending}
          onConfirm={handleDelete}
        />
      </div>
    </Can>
  );
}

function buildDefaultDraft(): ReportFilterDraft {
  return { ...getReportPresetRange("thisMonth") };
}

export function ReportRunner({ descriptor }: ReportRunnerProps) {
  const [draft, setDraft] = useState<ReportFilterDraft>(buildDefaultDraft);
  const [appliedParams, setAppliedParams] = useState<ReportDataQueryParams | null>(null);
  const [page, setPage] = useState(1);
  const [exporting, setExporting] = useState<"pdf" | "excel" | null>(null);

  // Đổi báo cáo → về trạng thái ban đầu, bắt buộc bấm lại "Lấy dữ liệu"
  useEffect(() => {
    setDraft(buildDefaultDraft());
    setAppliedParams(null);
    setPage(1);
  }, [descriptor.code]);

  const queryParams: ReportDataQueryParams | null = appliedParams
    ? { ...appliedParams, page, page_size: 100 }
    : null;

  const { data, isLoading, isFetching, isError, refetch } = useReportData(descriptor.code, queryParams);

  function handleDraftChange(patch: Partial<ReportFilterDraft>) {
    setDraft((prev) => ({ ...prev, ...patch }));
  }

  function handleApply() {
    if (!draft.from || !draft.to) {
      toast.error("Vui lòng chọn khoảng thời gian.");
      return;
    }
    setPage(1);
    setAppliedParams({ ...draft });
  }

  function handlePageChange(nextPage: number) {
    setPage(nextPage);
  }

  const hasData = !!data && !isLoading && !isError;

  async function handleExport(format: "pdf" | "excel") {
    if (!appliedParams) return;
    setExporting(format);
    try {
      const { from, to, ...rest } = appliedParams;
      const { blob, fileName } = await exportReportEngine(descriptor.code, { from, to, ...rest }, format);
      const url = URL.createObjectURL(blob);
      if (format === "pdf") {
        window.open(url, "_blank", "noopener,noreferrer");
        // Không revoke ngay — tab mới cần thời gian tải blob; trình duyệt tự dọn khi đóng tab.
      } else {
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 5000);
      }
      toast.success(format === "pdf" ? "Đã mở PDF báo cáo." : "Đã xuất Excel báo cáo.");
    } catch {
      toast.error(
        format === "pdf" ? "Không tạo được PDF báo cáo. Vui lòng thử lại." : "Không xuất được Excel. Vui lòng thử lại."
      );
    } finally {
      setExporting(null);
    }
  }

  return (
    <div className="flex flex-col">
      <UserDefinedReportActions descriptor={descriptor} />
      <ReportFilterPanel
        descriptor={descriptor}
        draft={draft}
        onDraftChange={handleDraftChange}
        onApply={handleApply}
        onExportPdf={() => handleExport("pdf")}
        onExportExcel={() => handleExport("excel")}
        canExport={hasData}
        isExportingPdf={exporting === "pdf"}
        isExportingExcel={exporting === "excel"}
        isFetching={isFetching}
      />

      {!appliedParams && (
        <EmptyState
          variant="search"
          title="Chưa có dữ liệu để xem"
          description='Chọn khoảng thời gian và bộ lọc, sau đó bấm "Lấy dữ liệu" để xem báo cáo.'
        />
      )}

      {appliedParams && isError && (
        <Alert className="border-destructive/40 bg-destructive/5">
          <AlertCircle className="h-4 w-4 text-destructive" />
          <AlertTitle className="text-destructive">Không tải được báo cáo</AlertTitle>
          <AlertDescription className="flex items-center justify-between gap-3">
            <span>Đã xảy ra lỗi khi tải dữ liệu báo cáo. Vui lòng thử lại.</span>
            <Button size="sm" variant="outline" onClick={() => refetch()} className="gap-2 shrink-0">
              <RefreshCw className="h-3.5 w-3.5" />
              Thử lại
            </Button>
          </AlertDescription>
        </Alert>
      )}

      {appliedParams && !isError && (isLoading || data) && (
        <div className="space-y-4">
          {data && <ReportKpiRow kpis={data.data.kpis} />}
          <ReportGrid
            columns={data?.data.columns ?? []}
            groups={data?.data.groups ?? null}
            rows={data?.data.rows ?? null}
            totals={data?.data.totals ?? {}}
            meta={data?.meta ?? { page: 1, page_size: 100, total: 0 }}
            page={page}
            onPageChange={handlePageChange}
            isLoading={isLoading}
          />
        </div>
      )}

      {appliedParams && !isError && !isLoading && !data && (
        <EmptyState variant="generic" title="Không có dữ liệu" description="Không tìm thấy dữ liệu báo cáo." />
      )}
    </div>
  );
}
