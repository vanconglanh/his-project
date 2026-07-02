"use client";

import { useState } from "react";
import { Download, Loader2, ExternalLink, Printer } from "lucide-react";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { useExportReport } from "@/lib/hooks/use-reports";
import type { ExportFormat, ExportReportType } from "@/lib/api/reports";

const FORMAT_LABELS: Record<ExportFormat, string> = {
  CSV: "CSV (Excel-compatible)",
  EXCEL: "Excel (.xlsx)",
  PDF: "PDF",
};

type PrintReportType = "financial" | "clinical" | "pharmacy";

const PRINT_TYPE_MAP: Partial<Record<ExportReportType, PrintReportType>> = {
  REVENUE: "financial",
  REVENUE_BY_DOCTOR: "financial",
  REVENUE_BY_SERVICE: "financial",
  CASHIER_DAILY: "financial",
  DEBTS_AGING: "financial",
  BHYT_SUMMARY: "financial",
  ENCOUNTERS_COUNT: "clinical",
  TOP_DIAGNOSES: "clinical",
  DIABETES_COHORT: "clinical",
  TOP_DRUGS: "pharmacy",
  INVENTORY_VALUE: "pharmacy",
};

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  reportType: ExportReportType;
  filters?: Record<string, unknown>;
  filterSummary?: string;
}

export function ExportReportDialog({ open, onOpenChange, reportType, filters, filterSummary }: Props) {
  const [format, setFormat] = useState<ExportFormat>("EXCEL");
  const { mutateAsync, isPending } = useExportReport();
  const [downloadUrl, setDownloadUrl] = useState<string | null>(null);

  const printType = PRINT_TYPE_MAP[reportType];
  const printParams = new URLSearchParams();
  if (filters?.from && typeof filters.from === "string") printParams.set("from", filters.from);
  if (filters?.to && typeof filters.to === "string") printParams.set("to", filters.to);
  if (filters?.clinicId && typeof filters.clinicId === "string") printParams.set("clinicId", filters.clinicId);

  async function handleExport() {
    try {
      const result = await mutateAsync({ report_type: reportType, format, filters });
      setDownloadUrl(result.file_url);
      toast.success("Xuất báo cáo thành công!");
    } catch {
      toast.error("Xuất báo cáo thất bại. Vui lòng thử lại.");
    }
  }

  function handleClose() {
    setDownloadUrl(null);
    onOpenChange(false);
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Download className="h-5 w-5" />
            Xuất báo cáo
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-2">
          {/* Nút xem trước & in */}
          {printType && (
            <Button
              variant="outline"
              className="w-full gap-2 min-h-[44px]"
              onClick={() =>
                window.open(`/reports/print/${printType}?${printParams.toString()}`, "_blank")
              }
            >
              <Printer className="h-4 w-4" />
              Xem trước &amp; In
            </Button>
          )}

          {filterSummary && (
            <div className="rounded-md bg-muted px-3 py-2 text-sm text-muted-foreground">
              <span className="font-medium">Bộ lọc:</span> {filterSummary}
            </div>
          )}

          <div className="space-y-2">
            <Label className="text-sm font-medium">Định dạng xuất</Label>
            <div className="grid grid-cols-3 gap-2">
              {(["CSV", "EXCEL", "PDF"] as ExportFormat[]).map((f) => (
                <button
                  key={f}
                  onClick={() => setFormat(f)}
                  className={`rounded-lg border px-3 py-2 text-sm font-medium transition-colors text-center ${
                    format === f
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-input hover:bg-accent hover:text-accent-foreground"
                  }`}
                >
                  {FORMAT_LABELS[f]}
                </button>
              ))}
            </div>
          </div>

          {downloadUrl && (
            <div className="flex items-center gap-2 rounded-md border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800 dark:border-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-300">
              <span className="flex-1">File sẵn sàng tải xuống</span>
              <a
                href={downloadUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1 font-medium underline"
              >
                Tải xuống <ExternalLink className="h-3 w-3" />
              </a>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleClose}>
            Đóng
          </Button>
          {!downloadUrl && (
            <Button onClick={handleExport} disabled={isPending} className="gap-2">
              {isPending ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" />
                  Đang xuất...
                </>
              ) : (
                <>
                  <Download className="h-4 w-4" />
                  Xuất
                </>
              )}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
