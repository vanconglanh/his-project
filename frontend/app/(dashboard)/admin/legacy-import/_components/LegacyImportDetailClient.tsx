"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, ShieldAlert, User, X, Check, Ban, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { usePatientSearch } from "@/lib/hooks/use-patients";
import {
  useLegacyImportBatch,
  useLegacyImportItems,
  useMatchLegacyImportItem,
  useConfirmLegacyImportItem,
  useRejectLegacyImportItem,
} from "@/lib/hooks/use-legacy-import";
import type { LegacyImportItem, LegacyImportItemStatus, LegacyImportDocType } from "@/lib/api/legacy-import";
import { LEGACY_IMPORT_DOC_TYPE_LABEL } from "@/lib/api/legacy-import";
import { formatDateTime } from "@/lib/utils/format";
import { cn } from "@/lib/utils";

const BATCH_STATUS_LABEL: Record<string, string> = {
  pending: "Chờ xử lý",
  processing: "Đang OCR",
  done: "Hoàn tất",
  failed: "Thất bại",
};

const ITEM_STATUS_LABEL: Record<LegacyImportItemStatus, string> = {
  pending_match: "Chờ ghép bệnh nhân",
  pending_review: "Chờ duyệt",
  confirmed: "Đã xác nhận",
  rejected: "Đã từ chối",
  failed: "Lỗi",
};

const ITEM_STATUS_CLASS: Record<LegacyImportItemStatus, string> = {
  pending_match: "bg-amber-100 text-amber-800 border-amber-300",
  pending_review: "bg-blue-100 text-blue-800 border-blue-300",
  confirmed: "bg-green-100 text-green-800 border-green-300",
  rejected: "bg-gray-100 text-gray-600 border-gray-300",
  failed: "bg-red-100 text-red-800 border-red-300",
};

const STATUS_FILTERS: { value: LegacyImportItemStatus | "all"; label: string }[] = [
  { value: "all", label: "Tất cả" },
  { value: "pending_match", label: "Chờ ghép bệnh nhân" },
  { value: "pending_review", label: "Chờ duyệt" },
  { value: "confirmed", label: "Đã xác nhận" },
  { value: "rejected", label: "Đã từ chối" },
  { value: "failed", label: "Lỗi" },
];

interface LegacyImportDetailClientProps {
  batchId: string;
}

export function LegacyImportDetailClient({ batchId }: LegacyImportDetailClientProps) {
  const { has } = usePermissions();
  const router = useRouter();
  const [statusFilter, setStatusFilter] = useState<LegacyImportItemStatus | "all">("all");
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);

  const { data: batch, isLoading: isBatchLoading } = useLegacyImportBatch(batchId);
  const { data: itemsData, isLoading: isItemsLoading } = useLegacyImportItems(batchId, {
    status: statusFilter === "all" ? undefined : statusFilter,
    page_size: 100,
  });

  if (!has("legacy_import.write")) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed py-16 text-center">
        <ShieldAlert className="h-10 w-10 text-muted-foreground" />
        <p className="font-medium">Bạn không có quyền truy cập chức năng này</p>
      </div>
    );
  }

  const items = itemsData?.data ?? [];
  const pct =
    batch && batch.total_items > 0
      ? Math.min(100, Math.round((batch.processed_items / batch.total_items) * 100))
      : 0;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => router.push("/admin/legacy-import")}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <div>
          <h2 className="text-xl font-bold tracking-tight">
            {isBatchLoading ? <Skeleton className="h-6 w-48" /> : batch?.zip_file_name}
          </h2>
          <p className="text-sm text-muted-foreground">Review từng ảnh và ghép bệnh nhân trước khi lưu</p>
        </div>
      </div>

      {/* Tiến độ batch */}
      {batch && (
        <div className="rounded-lg border bg-card p-4 flex flex-wrap items-center gap-4">
          <Badge variant="outline">{BATCH_STATUS_LABEL[batch.status] ?? batch.status}</Badge>
          <div className="flex items-center gap-2 flex-1 min-w-[200px]">
            <div className="h-2 flex-1 rounded-full bg-muted overflow-hidden max-w-md">
              <div className="h-full bg-primary transition-all" style={{ width: `${pct}%` }} />
            </div>
            <span className="text-xs text-muted-foreground tabular-nums whitespace-nowrap">
              {batch.processed_items}/{batch.total_items}
            </span>
          </div>
          <span className="text-xs text-muted-foreground">{formatDateTime(batch.created_at)}</span>
        </div>
      )}

      {/* Bộ lọc trạng thái */}
      <div className="flex flex-wrap gap-2">
        {STATUS_FILTERS.map((f) => (
          <Button
            key={f.value}
            variant={statusFilter === f.value ? "default" : "outline"}
            size="sm"
            onClick={() => setStatusFilter(f.value)}
          >
            {f.label}
          </Button>
        ))}
      </div>

      {/* Danh sách item */}
      {isItemsLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[1, 2, 3, 4].map((i) => (
            <Skeleton key={i} className="h-64 w-full" />
          ))}
        </div>
      ) : items.length === 0 ? (
        <div className="rounded-lg border border-dashed py-16 text-center text-sm text-muted-foreground">
          Không có ảnh nào phù hợp bộ lọc
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {items.map((item) => (
            <LegacyImportItemCard
              key={item.id}
              batchId={batchId}
              item={item}
              onZoom={() => item.image_url && setLightboxUrl(item.image_url)}
            />
          ))}
        </div>
      )}

      {lightboxUrl && (
        <div
          className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center"
          onClick={() => setLightboxUrl(null)}
          role="dialog"
          aria-modal="true"
          aria-label="Xem ảnh phóng to"
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
            alt="Ảnh scan hồ sơ cũ"
            className="max-w-[90vw] max-h-[90vh] object-contain"
            onClick={(e) => e.stopPropagation()}
          />
        </div>
      )}
    </div>
  );
}

// ─── Card item ───────────────────────────────────────────────────────────────

function LegacyImportItemCard({
  batchId,
  item,
  onZoom,
}: {
  batchId: string;
  item: LegacyImportItem;
  onZoom: () => void;
}) {
  const [ocrText, setOcrText] = useState(item.ocr_text ?? "");
  const [search, setSearch] = useState("");
  const [showList, setShowList] = useState(false);
  const [docType, setDocType] = useState<LegacyImportDocType>(item.doc_type ?? "HO_SO_CU_SCAN");

  const { data: searchData } = usePatientSearch({ q: search, page_size: 8 }, search.length >= 2);
  const candidates = searchData?.data ?? [];

  const matchMutation = useMatchLegacyImportItem(batchId);
  const confirmMutation = useConfirmLegacyImportItem(batchId);
  const rejectMutation = useRejectLegacyImportItem(batchId);

  const isFinal = item.status === "confirmed" || item.status === "rejected";
  const canConfirm = !!item.matched_patient_id && !isFinal;

  function handleSelectPatient(patientId: string) {
    matchMutation.mutate({ itemId: item.id, patientId });
    setShowList(false);
    setSearch("");
  }

  function handleConfirm() {
    if (!item.matched_patient_id) return;
    confirmMutation.mutate({
      itemId: item.id,
      ocr_text: ocrText,
      patient_id: item.matched_patient_id,
      doc_type: docType,
    });
  }

  function handleReject() {
    rejectMutation.mutate(item.id);
  }

  return (
    <div className="rounded-lg border bg-card overflow-hidden flex flex-col">
      <div className="flex items-center justify-between px-4 py-2 border-b bg-muted/30">
        <span className="text-sm font-medium truncate" title={item.original_filename}>
          {item.original_filename}
        </span>
        <div className="flex items-center gap-1.5 shrink-0">
          {item.doc_type && (
            <Badge variant="outline" className="text-[10px]">
              {LEGACY_IMPORT_DOC_TYPE_LABEL[item.doc_type]}
            </Badge>
          )}
          <Badge className={ITEM_STATUS_CLASS[item.status]} variant="outline">
            {ITEM_STATUS_LABEL[item.status]}
          </Badge>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 p-4">
        {/* Ảnh gốc */}
        <div>
          {item.image_url ? (
            <button
              type="button"
              className="w-full"
              onClick={onZoom}
              aria-label={`Xem ảnh ${item.original_filename}`}
            >
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={item.image_url}
                alt={item.original_filename}
                className="w-full h-40 object-cover rounded-md border"
              />
            </button>
          ) : (
            <div className="w-full h-40 flex items-center justify-center rounded-md border bg-muted text-xs text-muted-foreground">
              Không có ảnh
            </div>
          )}
          {item.ocr_confidence != null && (
            <p className="text-xs text-muted-foreground mt-1">
              Độ tin cậy OCR: {Math.round(item.ocr_confidence * 100)}%
            </p>
          )}
        </div>

        {/* Text OCR sửa được */}
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">Nội dung OCR (sửa được)</label>
          <Textarea
            value={ocrText}
            onChange={(e) => setOcrText(e.target.value)}
            className="min-h-[160px] text-xs"
            disabled={isFinal}
            aria-label="Nội dung OCR"
          />
        </div>
      </div>

      {item.item_error && (
        <p className="px-4 pb-2 text-xs text-destructive">Lỗi: {item.item_error}</p>
      )}

      {/* Match bệnh nhân */}
      <div className="px-4 pb-4 space-y-2">
        <label className="text-xs font-medium text-muted-foreground">Ghép bệnh nhân</label>
        {item.matched_patient_name && !showList ? (
          <div className="flex items-center justify-between rounded-md border px-3 py-2 bg-muted/20">
            <span className="text-sm flex items-center gap-1.5">
              <User className="h-3.5 w-3.5 text-muted-foreground" />
              {item.matched_patient_name}
              {item.match_method === "filename_auto" && (
                <Badge variant="outline" className="ml-1 text-[10px]">
                  tự động theo tên file
                </Badge>
              )}
            </span>
            {!isFinal && (
              <Button variant="ghost" size="sm" onClick={() => setShowList(true)}>
                Đổi
              </Button>
            )}
          </div>
        ) : (
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              className="pl-9"
              placeholder="Tìm theo tên, SĐT bệnh nhân..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              disabled={isFinal}
              aria-label="Tìm bệnh nhân"
            />
            {search.length >= 2 && candidates.length > 0 && (
              <div className="absolute z-20 left-0 right-0 top-full mt-1 border rounded-lg bg-background shadow-lg max-h-48 overflow-auto">
                {candidates.map((p) => (
                  <button
                    key={p.id}
                    type="button"
                    className="w-full text-left px-3 py-2 text-sm hover:bg-accent flex items-center justify-between"
                    onClick={() => handleSelectPatient(p.id)}
                  >
                    <span className="font-medium">{p.full_name}</span>
                    {p.phone && <span className="text-muted-foreground text-xs">{p.phone}</span>}
                  </button>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Loại tài liệu */}
      {!isFinal && (
        <div className="px-4 pb-3 space-y-1">
          <label className="text-xs font-medium text-muted-foreground" htmlFor={`doc-type-${item.id}`}>
            Loại tài liệu
          </label>
          <Select
            items={LEGACY_IMPORT_DOC_TYPE_LABEL}
            value={docType}
            onValueChange={(v) => setDocType(v as LegacyImportDocType)}
          >
            <SelectTrigger id={`doc-type-${item.id}`} className="h-9" aria-label="Loại tài liệu">
              <SelectValue placeholder="Loại tài liệu" />
            </SelectTrigger>
            <SelectContent>
              {(Object.keys(LEGACY_IMPORT_DOC_TYPE_LABEL) as LegacyImportDocType[]).map((dt) => (
                <SelectItem key={dt} value={dt}>
                  {LEGACY_IMPORT_DOC_TYPE_LABEL[dt]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      )}

      {/* Hành động */}
      {!isFinal && (
        <div className="flex gap-2 px-4 pb-4">
          <Button
            size="sm"
            disabled={!canConfirm || confirmMutation.isPending}
            onClick={handleConfirm}
            className={cn("flex-1")}
          >
            <Check className="h-4 w-4" />
            {confirmMutation.isPending ? "Đang lưu..." : "Xác nhận lưu"}
          </Button>
          <Button
            size="sm"
            variant="outline"
            disabled={rejectMutation.isPending}
            onClick={handleReject}
          >
            <Ban className="h-4 w-4" />
            Từ chối
          </Button>
        </div>
      )}

      {item.confirmed_at && (
        <p className="px-4 pb-3 text-xs text-muted-foreground">
          Đã xác nhận lúc {formatDateTime(item.confirmed_at)}
        </p>
      )}
    </div>
  );
}
