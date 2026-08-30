"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { FileScan, ExternalLink, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { listClsUploads } from "@/lib/api/cls-uploads";
import { formatDateTime } from "@/lib/utils/format";

const LEGACY_DOC_TYPE = "HO_SO_CU_SCAN";

interface LegacyDocsListProps {
  patientId: string;
}

export function LegacyDocsList({ patientId }: LegacyDocsListProps) {
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ["legacy-docs", patientId],
    queryFn: () => listClsUploads(patientId, { doc_type: LEGACY_DOC_TYPE, page_size: 100 }),
    enabled: !!patientId,
  });

  const docs = data?.data ?? [];

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    );
  }

  if (docs.length === 0) {
    return (
      <div className="text-center py-8 text-muted-foreground text-sm">
        <FileScan className="h-10 w-10 mx-auto mb-2 opacity-30" />
        <p>Chưa có tài liệu cũ nào được số hoá</p>
        <p className="text-xs mt-1">
          Tài liệu sẽ xuất hiện ở đây sau khi được xác nhận từ chức năng &quot;Nhập hồ sơ cũ (OCR)&quot;
        </p>
      </div>
    );
  }

  const isImage = (mime: string) => mime.startsWith("image/");

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        {docs.map((item) => (
          <div key={item.id} className="border rounded-lg overflow-hidden group relative">
            {isImage(item.mime_type) && item.signed_url ? (
              <button
                className="w-full"
                onClick={() => setLightboxUrl(item.signed_url!)}
                aria-label={`Xem ảnh ${item.file_name}`}
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={item.signed_url} alt={item.file_name} className="w-full h-28 object-cover" />
              </button>
            ) : (
              <div className="h-28 flex items-center justify-center bg-muted">
                <FileScan className="h-10 w-10 text-muted-foreground" />
              </div>
            )}
            <div className="p-2">
              <p className="text-xs font-medium line-clamp-1" title={item.file_name}>
                {item.file_name}
              </p>
              <p className="text-xs text-muted-foreground">{formatDateTime(item.uploaded_at)}</p>
            </div>
            {item.signed_url && (
              <a
                href={item.signed_url}
                target="_blank"
                rel="noopener noreferrer"
                className="absolute top-1 right-1 bg-background/80 rounded p-1 hover:bg-background opacity-0 group-hover:opacity-100 transition-opacity"
                aria-label="Mở file"
              >
                <ExternalLink className="h-3 w-3" />
              </a>
            )}
          </div>
        ))}
      </div>

      {lightboxUrl && (
        <div
          className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center"
          onClick={() => setLightboxUrl(null)}
          role="dialog"
          aria-modal="true"
          aria-label="Xem ảnh tài liệu cũ"
        >
          <Button
            variant="ghost"
            size="icon"
            className="absolute top-4 right-4 text-white hover:bg-white/20"
            onClick={() => setLightboxUrl(null)}
          >
            <X className="h-6 w-6" />
          </Button>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={lightboxUrl}
            alt="Tài liệu cũ đã số hoá"
            className="max-w-[90vw] max-h-[90vh] object-contain"
            onClick={(e) => e.stopPropagation()}
          />
        </div>
      )}
    </div>
  );
}
