"use client";

import { useState, useRef } from "react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useImportDrugs } from "@/lib/hooks/use-drugs";
import { Upload, FileSpreadsheet, CheckCircle, XCircle } from "lucide-react";
import { cn } from "@/lib/utils";

interface Props {
  onSuccess?: () => void;
}

export function DrugImportDropzone({ onSuccess }: Props) {
  const [dragging, setDragging] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [mode, setMode] = useState<"INSERT" | "UPSERT">("UPSERT");
  const inputRef = useRef<HTMLInputElement>(null);
  const importMutation = useImportDrugs();

  function handleDrop(e: React.DragEvent) {
    e.preventDefault();
    setDragging(false);
    const f = e.dataTransfer.files[0];
    if (f && (f.name.endsWith(".xlsx") || f.name.endsWith(".xls"))) {
      setFile(f);
    }
  }

  async function handleImport() {
    if (!file) return;
    const result = await importMutation.mutateAsync({ file, mode });
    if (result.failed === 0) onSuccess?.();
  }

  return (
    <div className="space-y-4">
      <div
        className={cn(
          "border-2 border-dashed rounded-xl p-10 flex flex-col items-center gap-3 cursor-pointer transition-colors",
          dragging ? "border-primary bg-primary/5" : "border-muted-foreground/30 hover:border-primary/50"
        )}
        onDragOver={(e) => { e.preventDefault(); setDragging(true); }}
        onDragLeave={() => setDragging(false)}
        onDrop={handleDrop}
        onClick={() => inputRef.current?.click()}
        role="button"
        aria-label="Kéo thả file Excel hoặc nhấp để chọn"
        tabIndex={0}
        onKeyDown={(e) => e.key === "Enter" && inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".xlsx,.xls"
          className="hidden"
          onChange={(e) => {
            const f = e.target.files?.[0];
            if (f) setFile(f);
          }}
        />
        {file ? (
          <>
            <FileSpreadsheet className="h-10 w-10 text-green-600" />
            <p className="text-sm font-medium">{file.name}</p>
            <p className="text-xs text-muted-foreground">{(file.size / 1024).toFixed(0)} KB</p>
          </>
        ) : (
          <>
            <Upload className="h-10 w-10 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">Kéo thả file .xlsx vào đây hoặc nhấp để chọn</p>
          </>
        )}
      </div>

      {/* Import result */}
      {importMutation.data && (
        <div className="rounded-md border p-4 space-y-2">
          <p className="text-sm font-medium">Kết quả import:</p>
          <div className="flex gap-4 text-sm flex-wrap">
            <span className="flex items-center gap-1">
              <CheckCircle className="h-4 w-4 text-green-600" />
              Thêm: {importMutation.data.inserted}
            </span>
            <span className="flex items-center gap-1">
              <CheckCircle className="h-4 w-4 text-blue-600" />
              Cập nhật: {importMutation.data.updated}
            </span>
            <span className="flex items-center gap-1 text-destructive">
              <XCircle className="h-4 w-4" />
              Lỗi: {importMutation.data.failed}
            </span>
          </div>
          {importMutation.data.errors.length > 0 && (
            <div className="rounded bg-destructive/5 border border-destructive/20 p-2 max-h-32 overflow-y-auto">
              {importMutation.data.errors.map((err, i) => (
                <p key={i} className="text-xs text-destructive">
                  Dòng {err.row}: {err.message}
                </p>
              ))}
            </div>
          )}
        </div>
      )}

      <div className="flex items-center justify-between">
        <div className="flex gap-2">
          {(["UPSERT", "INSERT"] as const).map((m) => (
            <Badge
              key={m}
              variant={mode === m ? "default" : "outline"}
              className="cursor-pointer"
              onClick={() => setMode(m)}
            >
              {m === "UPSERT" ? "UPSERT (mặc định)" : "INSERT mới"}
            </Badge>
          ))}
        </div>
        <Button onClick={handleImport} disabled={!file || importMutation.isPending}>
          {importMutation.isPending ? "Đang import..." : "Bắt đầu Import"}
        </Button>
      </div>
    </div>
  );
}
