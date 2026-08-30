"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { Upload, FileArchive, Eye, ShieldAlert } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { usePermissions } from "@/lib/hooks/use-permissions";
import {
  useLegacyImportBatches,
  useUploadLegacyImportBatch,
} from "@/lib/hooks/use-legacy-import";
import type { LegacyImportBatch, LegacyImportBatchStatus } from "@/lib/api/legacy-import";
import { formatDateTime } from "@/lib/utils/format";

const STATUS_LABEL: Record<LegacyImportBatchStatus, string> = {
  pending: "Chờ xử lý",
  processing: "Đang OCR",
  done: "Hoàn tất",
  failed: "Thất bại",
};

const STATUS_CLASS: Record<LegacyImportBatchStatus, string> = {
  pending: "bg-amber-100 text-amber-800 border-amber-300",
  processing: "bg-blue-100 text-blue-800 border-blue-300",
  done: "bg-green-100 text-green-800 border-green-300",
  failed: "bg-red-100 text-red-800 border-red-300",
};

function BatchStatusBadge({ status }: { status: LegacyImportBatchStatus }) {
  return (
    <Badge className={STATUS_CLASS[status]} variant="outline">
      {STATUS_LABEL[status]}
    </Badge>
  );
}

function ProgressBar({ processed, total }: { processed: number; total: number }) {
  const pct = total > 0 ? Math.min(100, Math.round((processed / total) * 100)) : 0;
  return (
    <div className="flex items-center gap-2 min-w-[140px]">
      <div className="h-2 flex-1 rounded-full bg-muted overflow-hidden">
        <div className="h-full bg-primary transition-all" style={{ width: `${pct}%` }} />
      </div>
      <span className="text-xs text-muted-foreground tabular-nums whitespace-nowrap">
        {processed}/{total}
      </span>
    </div>
  );
}

export function LegacyImportPageClient() {
  const { has } = usePermissions();
  const router = useRouter();
  const [page, setPage] = useState(1);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const { data, isLoading } = useLegacyImportBatches({ page, page_size: 20 });
  const uploadMutation = useUploadLegacyImportBatch();

  if (!has("legacy_import.write")) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed py-16 text-center">
        <ShieldAlert className="h-10 w-10 text-muted-foreground" />
        <p className="font-medium">Bạn không có quyền truy cập chức năng này</p>
        <p className="text-sm text-muted-foreground">
          Vui lòng liên hệ quản trị viên để được cấp quyền &quot;legacy_import.write&quot;
        </p>
      </div>
    );
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (file) setSelectedFile(file);
  }

  function handleUpload() {
    if (!selectedFile) return;
    uploadMutation.mutate(selectedFile, {
      onSuccess: () => {
        setSelectedFile(null);
        if (fileInputRef.current) fileInputRef.current.value = "";
      },
    });
  }

  const batches = data?.data ?? [];

  const columns: Column<LegacyImportBatch>[] = [
    {
      key: "zip_file_name",
      header: "Tên file ZIP",
      cell: (b) => (
        <span className="font-medium inline-flex items-center gap-2">
          <FileArchive className="h-4 w-4 text-muted-foreground" />
          {b.zip_file_name}
        </span>
      ),
    },
    { key: "status", header: "Trạng thái", cell: (b) => <BatchStatusBadge status={b.status} /> },
    {
      key: "progress",
      header: "Tiến độ",
      cell: (b) => <ProgressBar processed={b.processed_items} total={b.total_items} />,
    },
    { key: "created_at", header: "Thời gian tạo", cell: (b) => formatDateTime(b.created_at) },
    {
      key: "actions",
      header: "Thao tác",
      className: "text-right",
      cell: (b) => (
        <div className="flex justify-end">
          <Button
            variant="outline"
            size="sm"
            onClick={() => router.push(`/admin/legacy-import/${b.id}`)}
          >
            <Eye className="h-4 w-4" />
            Xem chi tiết
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div className="rounded-lg border bg-card p-6 space-y-4">
        <div>
          <h3 className="text-base font-semibold">Tải lên file ZIP hồ sơ giấy cũ</h3>
          <p className="text-sm text-muted-foreground mt-0.5">
            Chọn file ZIP chứa các ảnh scan hồ sơ giấy cũ (JPG/PNG). Hệ thống sẽ tự động OCR nền
            sau khi tải lên.
          </p>
        </div>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <input
            ref={fileInputRef}
            type="file"
            accept=".zip"
            onChange={handleFileChange}
            aria-label="Chọn file ZIP hồ sơ cũ"
            className="block w-full max-w-sm text-sm text-foreground file:mr-3 file:rounded-md file:border file:border-input file:bg-background file:px-3 file:py-2 file:text-sm file:font-medium hover:file:bg-accent"
          />
          <Button
            onClick={handleUpload}
            disabled={!selectedFile || uploadMutation.isPending}
            className="sm:w-auto w-full"
          >
            <Upload className="h-4 w-4" />
            {uploadMutation.isPending ? "Đang tải lên..." : "Tải lên & OCR"}
          </Button>
        </div>
        {selectedFile && (
          <p className="text-xs text-muted-foreground">Đã chọn: {selectedFile.name}</p>
        )}
      </div>

      <div className="rounded-lg border bg-card">
        <div className="p-4 border-b">
          <h3 className="text-base font-semibold">Danh sách lô đã tải lên</h3>
        </div>
        <div className="p-4">
          <DataTable
            columns={columns}
            data={batches}
            isLoading={isLoading}
            meta={data?.meta}
            onPageChange={setPage}
            emptyState={
              <p className="text-sm text-muted-foreground">
                Chưa có lô hồ sơ cũ nào được tải lên
              </p>
            }
          />
        </div>
      </div>
    </div>
  );
}
