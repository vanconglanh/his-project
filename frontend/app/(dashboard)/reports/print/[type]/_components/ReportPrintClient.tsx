"use client";

import { useEffect, useRef, useState, use } from "react";
import apiClient from "@/lib/api/client";
import { reserveReportCode, type ExportReportType } from "@/lib/api/reports";
import { ExportReportDialog } from "@/components/domain/ExportReportDialog";
import { PrintToolbar } from "./PrintToolbar";
import { ErrorCard } from "./ErrorCard";

type ReportType = "financial" | "clinical" | "pharmacy";

const VALID_TYPES: readonly string[] = ["financial", "clinical", "pharmacy"];

/**
 * ExportReportType mặc định dùng cho nút "Tải Excel" của từng loại báo cáo in.
 * Theo đúng quy ước reportType mặc định đã dùng ở FinancialTab/ClinicalTab/PharmacyTab
 * (xem ExportReportDialog reportType="REVENUE" | "ENCOUNTERS_COUNT" | "TOP_DRUGS").
 * Việc map ngược 1-1 từ PrintReportType sang ExportReportType là không thể vì
 * PRINT_TYPE_MAP (ExportReportDialog.tsx) là quan hệ nhiều-1 (nhiều ExportReportType
 * cùng thuộc 1 trang preview) — nên chọn đại diện thay vì tự gọi API xuất Excel.
 */
const EXCEL_REPORT_TYPE: Record<ReportType, ExportReportType> = {
  financial: "REVENUE",
  clinical: "ENCOUNTERS_COUNT",
  pharmacy: "TOP_DRUGS",
};

interface Props {
  paramsPromise: Promise<{ type: string }>;
  searchParamsPromise: Promise<{ from?: string; to?: string; clinicId?: string }>;
}

/**
 * Preview + in + tải báo cáo — TOÀN BỘ đều dùng bản PDF render server-side (QuestPDF),
 * đúng ADR-0001 (docs/adr/0001-pdf-rendering-strategy.md). Không còn render HTML song song.
 *
 * Luồng: reserveReportCode(type) → GET /reports/{type}/pdf?...&reportCode=... (qua apiClient,
 * JWT tự gắn qua interceptor) → blob → objectUrl → hiển thị trong <iframe>. In/tải đều thao tác
 * trên chính objectUrl này (không fetch lại), đảm bảo mã báo cáo trên preview = mã trên file tải.
 */
export default function ReportPrintClient({ paramsPromise, searchParamsPromise }: Props) {
  const { type } = use(paramsPromise);
  const { from, to, clinicId } = use(searchParamsPromise);

  const isValidType = VALID_TYPES.includes(type);
  const reportType = type as ReportType;
  const fromDate = from ?? new Date(Date.now() - 30 * 86400_000).toISOString().slice(0, 10);
  const toDate = to ?? new Date().toISOString().slice(0, 10);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [reportCode, setReportCode] = useState("");
  const [objectUrl, setObjectUrl] = useState<string | null>(null);
  const [exportOpen, setExportOpen] = useState(false);
  const [retryTick, setRetryTick] = useState(0);
  const iframeRef = useRef<HTMLIFrameElement>(null);

  useEffect(() => {
    if (!isValidType) return;

    let cancelled = false;
    let createdUrl: string | null = null;

    setLoading(true);
    setError(false);

    (async () => {
      try {
        const code = await reserveReportCode(reportType).catch(() => "");
        if (cancelled) return;

        const params = new URLSearchParams({ from: fromDate, to: toDate });
        if (code) params.set("reportCode", code);
        if (clinicId) params.set("clinicId", clinicId);

        const res = await apiClient.get(`/reports/${reportType}/pdf?${params.toString()}`, {
          responseType: "blob",
        });
        if (cancelled) return;

        const blob =
          res.data instanceof Blob ? res.data : new Blob([res.data], { type: "application/pdf" });
        createdUrl = URL.createObjectURL(blob);
        setReportCode(code);
        setObjectUrl(createdUrl);
      } catch {
        if (!cancelled) setError(true);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
      if (createdUrl) URL.revokeObjectURL(createdUrl);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isValidType, reportType, fromDate, toDate, clinicId, retryTick]);

  if (!isValidType) {
    return (
      <ErrorCard
        title="Loại báo cáo không hợp lệ"
        description="Chỉ hỗ trợ financial / clinical / pharmacy."
      />
    );
  }

  if (error) {
    return (
      <ErrorCard
        title="Không tạo được PDF báo cáo"
        description="Vui lòng thử lại. Nếu lỗi tiếp diễn, liên hệ quản trị viên hệ thống."
        onRetry={() => setRetryTick((t) => t + 1)}
      />
    );
  }

  function handlePrint() {
    const win = iframeRef.current?.contentWindow;
    if (win) {
      try {
        win.focus();
        win.print();
        return;
      } catch {
        // rơi xuống fallback bên dưới nếu trình duyệt chặn in trong iframe
      }
    }
    if (objectUrl) window.open(objectUrl, "_blank");
  }

  return (
    <div className="flex flex-col h-screen bg-muted print:h-auto print:bg-white">
      <PrintToolbar
        objectUrl={objectUrl}
        fileName={`${reportCode || `bao-cao-${reportType}`}.pdf`}
        loading={loading}
        onPrint={handlePrint}
        onExportExcel={() => setExportOpen(true)}
      />

      <div className="flex-1 min-h-0">
        {loading || !objectUrl ? (
          <div className="p-12 text-center text-sm text-muted-foreground">
            Đang tạo PDF báo cáo...
          </div>
        ) : (
          <iframe
            ref={iframeRef}
            src={objectUrl}
            title="Xem trước báo cáo PDF"
            className="w-full h-full border-0 bg-white"
          />
        )}
      </div>

      <ExportReportDialog
        open={exportOpen}
        onOpenChange={setExportOpen}
        reportType={EXCEL_REPORT_TYPE[reportType]}
        filters={{ from: fromDate, to: toDate, ...(clinicId ? { clinicId } : {}) }}
        filterSummary={`${fromDate} → ${toDate}`}
      />
    </div>
  );
}
