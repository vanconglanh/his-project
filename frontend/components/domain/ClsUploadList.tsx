"use client";

import { useState, useRef } from "react";
import { Upload, Trash2, FileText, Image, ExternalLink, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { useClsUploads, useUploadCls, useDeleteClsUpload } from "@/lib/hooks/use-cls-uploads";
import { formatDateTime } from "@/lib/utils/format";
import { cn } from "@/lib/utils";

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

interface ClsUploadListProps {
  patientId: string;
}

export function ClsUploadList({ patientId }: ClsUploadListProps) {
  const [isDragging, setIsDragging] = useState(false);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [docType, setDocType] = useState("");
  const [note, setNote] = useState("");
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [lightboxUrl, setLightboxUrl] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data, isLoading } = useClsUploads(patientId);
  const uploadMutation = useUploadCls(patientId);
  const deleteMutation = useDeleteClsUpload(patientId);

  const uploads = data?.data ?? [];

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) setPendingFile(file);
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) setPendingFile(file);
  };

  const handleUpload = async () => {
    if (!pendingFile || !docType.trim()) return;
    await uploadMutation.mutateAsync({ file: pendingFile, docType: docType.trim(), note: note || undefined });
    setPendingFile(null);
    setDocType("");
    setNote("");
  };

  const isImage = (mime: string) => mime.startsWith("image/");

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map((i) => <Skeleton key={i} className="h-16 w-full" />)}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Upload area */}
      <div
        className={cn(
          "border-2 border-dashed rounded-lg p-6 text-center transition-colors cursor-pointer",
          isDragging ? "border-primary bg-primary/5" : "border-muted-foreground/25 hover:border-primary/50"
        )}
        onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={handleDrop}
        onClick={() => fileInputRef.current?.click()}
        role="button"
        tabIndex={0}
        aria-label="Kéo thả file CLS hoặc click để chọn"
        onKeyDown={(e) => e.key === "Enter" && fileInputRef.current?.click()}
      >
        <Upload className="h-8 w-8 mx-auto mb-2 text-muted-foreground" />
        <p className="text-sm text-muted-foreground">
          Kéo thả file vào đây hoặc{" "}
          <span className="text-primary font-medium">chọn file</span>
        </p>
        <p className="text-xs text-muted-foreground mt-1">PNG, JPEG hoặc PDF, tối đa 10MB</p>
        <input
          ref={fileInputRef}
          type="file"
          accept="image/png,image/jpeg,application/pdf"
          className="hidden"
          onChange={handleFileInput}
          onClick={(e) => e.stopPropagation()}
        />
      </div>

      {/* Pending upload form */}
      {pendingFile && (
        <div className="border rounded-lg p-4 space-y-3 bg-muted/20">
          <div className="flex items-center justify-between">
            <p className="text-sm font-medium">File đã chọn: {pendingFile.name}</p>
            <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => setPendingFile(null)}>
              <X className="h-3.5 w-3.5" />
            </Button>
          </div>
          <div className="space-y-1">
            <Label htmlFor="doc_type">Loại hồ sơ *</Label>
            <Input
              id="doc_type"
              value={docType}
              onChange={(e) => setDocType(e.target.value)}
              placeholder="VD: X-quang phổi - BV Bạch Mai 20/05/2026"
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="cls_note">Ghi chú</Label>
            <Input
              id="cls_note"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="Ghi chú thêm (tuỳ chọn)"
            />
          </div>
          <div className="flex gap-2">
            <Button
              size="sm"
              onClick={handleUpload}
              disabled={!docType.trim() || uploadMutation.isPending}
            >
              {uploadMutation.isPending ? "Đang tải lên..." : "Tải lên"}
            </Button>
            <Button size="sm" variant="outline" onClick={() => setPendingFile(null)}>Huỷ</Button>
          </div>
        </div>
      )}

      {/* File grid */}
      {uploads.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground text-sm">
          <FileText className="h-10 w-10 mx-auto mb-2 opacity-30" />
          <p>Chưa có tài liệu CLS nào</p>
          <p className="text-xs mt-1">Tải lên kết quả xét nghiệm hoặc hình ảnh từ cơ sở y tế khác</p>
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          {uploads.map((item) => (
            <div key={item.id} className="border rounded-lg overflow-hidden group relative">
              {isImage(item.mime_type) && item.signed_url ? (
                <button
                  className="w-full"
                  onClick={() => setLightboxUrl(item.signed_url!)}
                  aria-label={`Xem ảnh ${item.file_name}`}
                >
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={item.signed_url}
                    alt={item.doc_type}
                    className="w-full h-28 object-cover"
                  />
                </button>
              ) : (
                <div className="h-28 flex items-center justify-center bg-muted">
                  <FileText className="h-10 w-10 text-muted-foreground" />
                </div>
              )}
              <div className="p-2">
                <p className="text-xs font-medium line-clamp-1" title={item.doc_type}>{item.doc_type}</p>
                <p className="text-xs text-muted-foreground">{formatBytes(item.file_size_bytes)}</p>
                <p className="text-xs text-muted-foreground">{formatDateTime(item.uploaded_at)}</p>
              </div>
              {/* Actions overlay */}
              <div className="absolute top-1 right-1 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                {item.signed_url && (
                  <a
                    href={item.signed_url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="bg-background/80 rounded p-1 hover:bg-background"
                    aria-label="Mở file"
                  >
                    <ExternalLink className="h-3 w-3" />
                  </a>
                )}
                <button
                  className="bg-background/80 rounded p-1 hover:bg-destructive hover:text-destructive-foreground"
                  onClick={() => setDeleteId(item.id)}
                  aria-label="Xoá"
                >
                  <Trash2 className="h-3 w-3" />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Lightbox */}
      {lightboxUrl && (
        <div
          className="fixed inset-0 z-50 bg-black/80 flex items-center justify-center"
          onClick={() => setLightboxUrl(null)}
          role="dialog"
          aria-modal="true"
          aria-label="Xem ảnh"
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
            alt="Kết quả CLS"
            className="max-w-[90vw] max-h-[90vh] object-contain"
            onClick={(e) => e.stopPropagation()}
          />
        </div>
      )}

      <ConfirmDialog
        open={!!deleteId}
        onOpenChange={(open) => { if (!open) setDeleteId(null); }}
        title="Xoá tài liệu CLS"
        description="Bạn có chắc muốn xoá tài liệu này? Hành động không thể hoàn tác."
        onConfirm={() => { if (deleteId) deleteMutation.mutate(deleteId); setDeleteId(null); }}
        isLoading={deleteMutation.isPending}
        variant="destructive"
      />
    </div>
  );
}
