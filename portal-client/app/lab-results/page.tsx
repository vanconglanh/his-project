"use client";

import { useState } from "react";
import { FlaskIcon } from "@/components/icons";
import { EmptyState, ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { useDownloadLabResultPdf, useLabResults } from "@/lib/hooks";
import { formatDate } from "@/lib/utils";

export default function LabResultsPage() {
  const { data: results, isLoading, isError, refetch } = useLabResults();
  const downloadPdf = useDownloadLabResultPdf();
  const [downloadError, setDownloadError] = useState<string | null>(null);

  function handleDownload(id: string, name: string) {
    setDownloadError(null);
    downloadPdf.mutate(id, {
      onSuccess: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = `ket-qua-${name}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
      onError: () => setDownloadError("Không tải được kết quả, vui lòng thử lại"),
    });
  }

  return (
    <div className="p-4">
      <h1 className="mb-5 pt-4 text-slate-900">Kết quả xét nghiệm / CLS</h1>

      {downloadError && (
        <div className="mb-4 rounded-xl border-2 border-red-200 bg-red-50 p-3 text-center text-base font-medium text-red-700">
          {downloadError}
        </div>
      )}

      {isLoading && <LoadingBlock label="Đang tải kết quả..." />}
      {isError && <ErrorBlock error={undefined} onRetry={() => refetch()} />}

      {!isLoading && !isError && (results?.length ?? 0) === 0 && (
        <EmptyState icon={<FlaskIcon className="h-16 w-16" />} title="Chưa có kết quả nào" />
      )}

      <div className="flex flex-col gap-3">
        {results?.map((r) => (
          <div key={r.id} className="rounded-2xl border-2 border-slate-200 bg-white p-4">
            <div className="mb-1 flex items-center justify-between">
              <span className="text-lg font-semibold text-slate-900">{r.testName}</span>
              <span className="rounded-full bg-slate-100 px-3 py-1 text-sm font-medium text-slate-600">
                {r.status}
              </span>
            </div>
            <p className="mb-2 text-base text-slate-600">Ngày: {formatDate(r.resultDate)}</p>
            {r.conclusion && <p className="mb-3 text-base text-slate-700">{r.conclusion}</p>}
            <button
              type="button"
              onClick={() => handleDownload(r.id, r.testName)}
              disabled={downloadPdf.isPending}
              className="min-h-11 rounded-xl border-2 border-blue-600 px-4 text-base font-semibold text-blue-600 hover:bg-blue-50 disabled:opacity-50"
            >
              Tải PDF kết quả
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
