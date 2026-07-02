"use client";

import { useRef, useState, DragEvent } from "react";
import { Button } from "@/components/ui/button";
import { Upload, FileCheck, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { useImportReconcileFile } from "@/lib/hooks/use-bhyt-reconcile";
import { toast } from "sonner";

interface Props {
  exportId: string;
}

export function BhytReconcileUploader({ exportId }: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const importMutation = useImportReconcileFile();

  function handleDrop(e: DragEvent<HTMLDivElement>) {
    e.preventDefault();
    setDragging(false);
    const file = e.dataTransfer.files[0];
    if (file && file.name.endsWith(".xml")) {
      setSelectedFile(file);
    } else {
      toast.error("Chỉ chấp nhận file XML kết quả giám định");
    }
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (file) setSelectedFile(file);
  }

  function handleUpload() {
    if (!selectedFile) return;
    importMutation.mutate(
      { exportId, file: selectedFile },
      {
        onSuccess: () => {
          toast.success("Đã tải lên file kết quả giám định. Hệ thống đang xử lý...");
          setSelectedFile(null);
        },
        onError: () => {
          toast.error("Tải file thất bại — kiểm tra định dạng XML");
        },
      }
    );
  }

  return (
    <div className="space-y-3">
      <div
        className={cn(
          "relative flex flex-col items-center justify-center gap-3 rounded-xl border-2 border-dashed p-8 text-center transition-colors cursor-pointer",
          dragging ? "border-primary bg-primary/5" : "border-muted-foreground/30 hover:border-primary/50"
        )}
        onDragOver={(e) => { e.preventDefault(); setDragging(true); }}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
        onClick={() => inputRef.current?.click()}
        role="button"
        aria-label="Kéo thả hoặc click để chọn file XML"
      >
        <Upload className="h-8 w-8 text-muted-foreground" />
        <div>
          <p className="text-sm font-medium">Kéo thả file XML kết quả giám định vào đây</p>
          <p className="text-xs text-muted-foreground mt-1">hoặc click để chọn file (.xml)</p>
        </div>
        <input
          ref={inputRef}
          type="file"
          accept=".xml"
          className="sr-only"
          onChange={handleFileChange}
          aria-hidden="true"
        />
      </div>

      {selectedFile && (
        <div className="flex items-center gap-3 rounded-lg border bg-muted/50 px-4 py-3">
          <FileCheck className="h-5 w-5 text-green-600 shrink-0" />
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium truncate">{selectedFile.name}</p>
            <p className="text-xs text-muted-foreground">{(selectedFile.size / 1024).toFixed(1)} KB</p>
          </div>
          <Button
            size="icon"
            variant="ghost"
            className="h-7 w-7 shrink-0"
            onClick={(e) => { e.stopPropagation(); setSelectedFile(null); }}
            aria-label="Xoá file"
          >
            <X className="h-4 w-4" />
          </Button>
          <Button
            size="sm"
            onClick={(e) => { e.stopPropagation(); handleUpload(); }}
            disabled={importMutation.isPending}
          >
            {importMutation.isPending ? "Đang tải..." : "Tải lên"}
          </Button>
        </div>
      )}
    </div>
  );
}
