"use client";

import { useState } from "react";
import { Printer, Download, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import apiClient from "@/lib/api/client";
import { toast } from "sonner";

interface PrintToolbarProps {
  /** Đường dẫn tương đối đến endpoint PDF, vd `/reports/financial/pdf?from=...&to=...` */
  pdfPath: string;
  /** Tên file tải về */
  fileName?: string;
}

/**
 * Toolbar in báo cáo — ẩn khi print qua class no-print.
 * Tải PDF qua axios để gắn JWT từ localStorage; sau đó tạo blob URL + trigger download.
 */
export function PrintToolbar({ pdfPath, fileName = "bao-cao.pdf" }: PrintToolbarProps) {
  const [downloading, setDownloading] = useState(false);

  async function handleDownload() {
    setDownloading(true);
    try {
      const res = await apiClient.get(pdfPath, {
        responseType: "blob",
      });
      const blob = res.data instanceof Blob ? res.data : new Blob([res.data], { type: "application/pdf" });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = fileName;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch {
      toast.error("Không tải được PDF báo cáo. Vui lòng thử lại.");
    } finally {
      setDownloading(false);
    }
  }

  return (
    <div className="no-print sticky top-0 z-10 flex items-center gap-3 bg-white/95 backdrop-blur border-b border-gray-200 px-6 py-3 shadow-sm">
      <span className="text-sm text-gray-600 flex-1">
        Xem trước báo cáo — nhấn <strong>In báo cáo</strong> để in hoặc <strong>Tải PDF</strong> để tải về.
      </span>
      <Button
        variant="outline"
        size="sm"
        className="gap-2 min-h-[44px]"
        onClick={() => window.print()}
      >
        <Printer className="h-4 w-4" />
        In báo cáo
      </Button>
      <Button
        size="sm"
        className="gap-2 min-h-[44px]"
        onClick={handleDownload}
        disabled={downloading}
      >
        {downloading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}
        Tải PDF
      </Button>
    </div>
  );
}
