"use client";

import { useCallback, useRef, useState } from "react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

interface DicomUploadZoneProps {
  onUpload: (files: File[]) => Promise<void>;
  isUploading?: boolean;
  className?: string;
}

export function DicomUploadZone({ onUpload, isUploading, className }: DicomUploadZoneProps) {
  const [files, setFiles] = useState<File[]>([]);
  const [isDragActive, setIsDragActive] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const addFiles = useCallback((newFiles: FileList | null) => {
    if (!newFiles) return;
    setFiles((prev) => [...prev, ...Array.from(newFiles)]);
  }, []);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragActive(true);
  };

  const handleDragLeave = () => setIsDragActive(false);

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragActive(false);
    addFiles(e.dataTransfer.files);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    addFiles(e.target.files);
  };

  const handleUpload = async () => {
    if (!files.length) return;
    await onUpload(files);
    setFiles([]);
  };

  const removeFile = (index: number) => {
    setFiles((prev) => prev.filter((_, i) => i !== index));
  };

  return (
    <div className={cn("space-y-3", className)}>
      <div
        role="button"
        tabIndex={0}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        onClick={() => !isUploading && inputRef.current?.click()}
        onKeyDown={(e) => e.key === "Enter" && !isUploading && inputRef.current?.click()}
        aria-label="Kéo thả file DICOM vào đây hoặc nhấn để chọn"
        className={cn(
          "border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-ring",
          isDragActive
            ? "border-primary bg-primary/5"
            : "border-muted-foreground/30 hover:border-primary/50",
          isUploading && "opacity-50 cursor-not-allowed pointer-events-none"
        )}
      >
        <input
          ref={inputRef}
          type="file"
          multiple
          accept=".dcm,.jpg,.jpeg,.png,application/dicom,image/*"
          className="sr-only"
          onChange={handleInputChange}
          disabled={isUploading}
        />
        <div className="flex flex-col items-center gap-2 text-muted-foreground">
          <span className="text-3xl" aria-hidden>
            🏥
          </span>
          {isDragActive ? (
            <p className="font-medium text-primary">Thả file vào đây...</p>
          ) : (
            <>
              <p className="font-medium">Kéo thả file DICOM hoặc click để chọn</p>
              <p className="text-xs">Hỗ trợ .dcm, .jpg, .png (nhiều file)</p>
            </>
          )}
        </div>
      </div>

      {files.length > 0 && (
        <div className="space-y-2">
          <p className="text-sm font-medium">{files.length} file được chọn:</p>
          <ul className="space-y-1 max-h-40 overflow-y-auto">
            {files.map((f, i) => (
              <li key={i} className="flex items-center justify-between text-sm bg-muted rounded px-3 py-1.5">
                <span className="truncate">{f.name}</span>
                <div className="flex items-center gap-2 ml-2 shrink-0">
                  <span className="text-muted-foreground text-xs">{(f.size / 1024).toFixed(0)} KB</span>
                  <button
                    type="button"
                    onClick={() => removeFile(i)}
                    className="text-destructive hover:text-destructive/80 text-xs font-bold"
                    aria-label={`Xoá ${f.name}`}
                  >
                    x
                  </button>
                </div>
              </li>
            ))}
          </ul>
          <Button onClick={handleUpload} disabled={isUploading} className="w-full">
            {isUploading ? "Đang tải lên..." : `Tải lên ${files.length} file`}
          </Button>
        </div>
      )}
    </div>
  );
}
