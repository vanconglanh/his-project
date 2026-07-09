"use client";

import { useState } from "react";
import { FileTextIcon } from "@/components/icons";
import { EmptyState, ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { useDownloadPrescriptionPdf, usePrescriptions } from "@/lib/hooks";
import { formatDate } from "@/lib/utils";

export default function PrescriptionsPage() {
  const { data: prescriptions, isLoading, isError, refetch } = usePrescriptions();
  const downloadPdf = useDownloadPrescriptionPdf();
  const [downloadError, setDownloadError] = useState<string | null>(null);

  function handleDownload(id: string, code: string) {
    setDownloadError(null);
    downloadPdf.mutate(id, {
      onSuccess: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = `don-thuoc-${code}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
      onError: () => setDownloadError("Không tải được đơn thuốc, vui lòng thử lại"),
    });
  }

  return (
    <div className="p-4">
      <h1 className="mb-5 pt-4 text-slate-900">Đơn thuốc của tôi</h1>

      {downloadError && (
        <div className="mb-4 rounded-xl border-2 border-red-200 bg-red-50 p-3 text-center text-base font-medium text-red-700">
          {downloadError}
        </div>
      )}

      {isLoading && <LoadingBlock label="Đang tải đơn thuốc..." />}
      {isError && <ErrorBlock error={undefined} onRetry={() => refetch()} />}

      {!isLoading && !isError && (prescriptions?.length ?? 0) === 0 && (
        <EmptyState
          icon={<FileTextIcon className="h-16 w-16" />}
          title="Chưa có đơn thuốc nào"
        />
      )}

      <div className="flex flex-col gap-3">
        {prescriptions?.map((p, idx) => (
          <div key={`${p.id}-${idx}`} className="rounded-2xl border-2 border-slate-200 bg-white p-4">
            <div className="mb-1 flex items-center justify-between">
              <span className="text-lg font-semibold text-slate-900">{formatDate(p.issuedAt)}</span>
              <span className="text-sm text-slate-500">{p.prescriptionCode}</span>
            </div>
            <p className="mb-2 text-base text-slate-600">{p.doctorName}</p>
            <ul className="mb-3 flex flex-col gap-1">
              {p.items.map((item, idx) => (
                <li key={idx} className="text-base text-slate-700">
                  • {item.drugName} — SL {item.quantity} ({item.dosage})
                </li>
              ))}
            </ul>
            <button
              type="button"
              onClick={() => handleDownload(p.id, p.prescriptionCode)}
              disabled={downloadPdf.isPending}
              className="min-h-11 rounded-xl border-2 border-teal-700 px-4 text-base font-semibold text-teal-700 hover:bg-teal-50 disabled:opacity-50"
            >
              Tải PDF đơn thuốc
            </button>
          </div>
        ))}
      </div>
    </div>
  );
}
