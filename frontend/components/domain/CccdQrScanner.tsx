"use client";

import { useRef, useState, useCallback, type KeyboardEvent } from "react";
import { ScanLine, X, AlertTriangle } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { parseCccdQr, type CccdQrData } from "@/lib/utils/cccd-qr";

export interface CccdQrScannerProps {
  onScanned: (data: CccdQrData) => void;
  onError?: (message: string) => void;
  className?: string;
}

/**
 * Ô nhận chuỗi quét từ máy quét USB keyboard-wedge (US-QR-001/002).
 * Máy quét "gõ" chuỗi ký tự nhanh rồi gửi Enter — ta bắt sự kiện Enter (hoặc nút "Đọc mã")
 * để parse, không phụ thuộc timing để không bỏ lỡ trường hợp trình duyệt gộp sự kiện.
 */
export function CccdQrScanner({ onScanned, onError, className }: CccdQrScannerProps) {
  const [raw, setRaw] = useState("");
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleParse = useCallback(
    (value: string) => {
      const result = parseCccdQr(value);
      if (!result.success || !result.data) {
        const msg = result.error_message ?? "Không đọc được mã QR CCCD";
        setErrorMsg(msg);
        onError?.(msg);
        return;
      }
      setErrorMsg(null);
      onScanned(result.data);
    },
    [onScanned, onError]
  );

  // Máy quét keyboard-wedge gửi Enter (CR/LF) sau khi "gõ" xong chuỗi QR (GA-001).
  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      handleParse(raw);
    }
  };

  const handleClear = () => {
    setRaw("");
    setErrorMsg(null);
    inputRef.current?.focus();
  };

  return (
    <div className={className}>
      <div className="rounded-lg border bg-muted/30 p-3 space-y-2">
        <div className="flex items-center gap-2 text-sm font-medium">
          <ScanLine className="h-4 w-4 text-primary" />
          Quét CCCD
        </div>
        <p className="text-xs text-muted-foreground">
          Đặt con trỏ vào ô bên dưới, sau đó quét mã QR trên thẻ CCCD bằng máy quét USB.
        </p>
        <div className="flex items-center gap-2">
          <Input
            ref={inputRef}
            value={raw}
            onChange={(e) => setRaw(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Đưa con trỏ vào đây rồi quét thẻ CCCD..."
            aria-label="Ô nhận chuỗi quét mã QR CCCD"
            autoComplete="off"
            className="font-mono text-sm"
          />
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => handleParse(raw)}
            disabled={!raw}
          >
            Đọc mã
          </Button>
          {raw && (
            <Button
              type="button"
              variant="ghost"
              size="icon"
              onClick={handleClear}
              aria-label="Xóa chuỗi quét"
            >
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>
        {errorMsg && (
          <div className="flex items-start gap-1.5 text-xs text-destructive">
            <AlertTriangle className="h-3.5 w-3.5 mt-0.5 shrink-0" />
            <span>{errorMsg} — vui lòng thử lại hoặc nhập thông tin thủ công.</span>
          </div>
        )}
      </div>
      <Label className="sr-only" htmlFor="cccd-qr-scanner-hint">
        Quét CCCD
      </Label>
    </div>
  );
}
