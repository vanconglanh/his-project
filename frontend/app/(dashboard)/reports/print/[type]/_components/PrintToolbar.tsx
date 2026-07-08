"use client";

import { Printer, Download, FileSpreadsheet, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";

interface PrintToolbarProps {
  /** Blob URL của PDF đã tải từ server (QuestPDF) — null khi đang tải hoặc lỗi. */
  objectUrl: string | null;
  /** Tên file khi tải PDF về máy. */
  fileName: string;
  /** Đang tải PDF từ server. */
  loading: boolean;
  /** In chính bản PDF đang xem trong iframe. */
  onPrint: () => void;
  /** Mở dialog xuất Excel (tái dùng ExportReportDialog). */
  onExportExcel: () => void;
}

/**
 * Toolbar preview báo cáo — ẩn khi in (class `no-print`).
 * Cả 3 nút đều thao tác trên CÙNG một bản PDF server-side (QuestPDF) theo ADR-0001:
 * - In báo cáo: in trực tiếp nội dung iframe đang hiển thị.
 * - Tải PDF: dùng lại blob đã tải (không fetch lại).
 * - Tải Excel: mở ExportReportDialog (nguồn dữ liệu Excel riêng, xem ghi chú trong ReportPrintClient).
 */
export function PrintToolbar({
  objectUrl,
  fileName,
  loading,
  onPrint,
  onExportExcel,
}: PrintToolbarProps) {
  return (
    <div className="no-print sticky top-0 z-10 flex flex-wrap items-center gap-3 bg-background/95 backdrop-blur border-b border-border px-6 py-3 shadow-sm shrink-0">
      <span className="text-sm text-muted-foreground flex-1 min-w-[200px]">
        Xem trước báo cáo PDF — nhấn <strong>In báo cáo</strong> để in hoặc <strong>Tải PDF</strong> để tải về.
      </span>

      <Button
        variant="outline"
        size="sm"
        className="gap-2 min-h-[44px]"
        onClick={onPrint}
        disabled={loading || !objectUrl}
      >
        <Printer className="h-4 w-4" />
        In báo cáo
      </Button>

      <Button
        variant="outline"
        size="sm"
        className="gap-2 min-h-[44px]"
        onClick={onExportExcel}
        disabled={loading}
      >
        <FileSpreadsheet className="h-4 w-4" />
        Tải Excel
      </Button>

      {objectUrl ? (
        <Button size="sm" className="gap-2 min-h-[44px]" render={<a href={objectUrl} download={fileName} />}>
          <Download className="h-4 w-4" />
          Tải PDF
        </Button>
      ) : (
        <Button size="sm" className="gap-2 min-h-[44px]" disabled>
          {loading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
          Tải PDF
        </Button>
      )}
    </div>
  );
}
