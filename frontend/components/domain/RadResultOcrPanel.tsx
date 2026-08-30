"use client";

import { useRef, useState } from "react";
import { AlertTriangle, FileUp, Upload, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useOcrExtractRadResult, useOcrConfirmRadResult } from "@/lib/hooks/use-rad-results";
import type { RadOcrExtractResult } from "@/lib/api/rad-results";

interface RadResultOcrPanelProps {
  /** Chỉ định CĐHA đã chọn sẵn (nếu mở từ 1 dòng chỉ định). Nếu không có, cho nhập tay ID. */
  radOrderId?: string;
  onSaved?: () => void;
}

/**
 * Panel OCR đọc phiếu kết quả CĐHA (X-quang/Siêu âm/CT). Khác lab-result (trích giá trị số theo
 * tên xét nghiệm): đây trích 2 đoạn VĂN BẢN mô tả tự do — "Mô tả" và "Kết luận" — hiển thị 2 ô lớn
 * đã điền sẵn để bác sĩ/KTV sửa tay trước khi lưu. Luôn qua màn xác nhận, không tự động ghi.
 */
export function RadResultOcrPanel({ radOrderId: fixedRadOrderId, onSaved }: RadResultOcrPanelProps) {
  const [radOrderIdInput, setRadOrderIdInput] = useState("");
  const radOrderId = fixedRadOrderId ?? radOrderIdInput.trim();

  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [result, setResult] = useState<RadOcrExtractResult | null>(null);
  const [findings, setFindings] = useState("");
  const [impression, setImpression] = useState("");
  const [conclusion, setConclusion] = useState("");
  const [recommendations, setRecommendations] = useState("");
  const [performedAt, setPerformedAt] = useState(() => new Date().toISOString().slice(0, 16));
  const fileInputRef = useRef<HTMLInputElement>(null);

  const extractMutation = useOcrExtractRadResult();
  const confirmMutation = useOcrConfirmRadResult();

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) setPendingFile(file);
  };

  const handleUpload = async () => {
    if (!pendingFile) return;
    try {
      const res = await extractMutation.mutateAsync(pendingFile);
      setResult(res);
      setFindings(res.findings ?? "");
      setImpression(res.impression ?? "");
      setConclusion(res.conclusion ?? "");
      setRecommendations(res.recommendations ?? "");
      setPendingFile(null);
    } catch {
      // Lỗi đã xử lý qua onError (toast) — chặn unhandled rejection.
    }
  };

  const handleConfirm = async () => {
    if (!radOrderId) return;
    try {
      await confirmMutation.mutateAsync(
        {
          rad_order_id: radOrderId,
          findings: findings.trim(),
          impression: impression.trim() || null,
          conclusion: conclusion.trim(),
          recommendations: recommendations.trim() || null,
          performed_at: new Date(performedAt).toISOString(),
        },
        {
          onSuccess: () => {
            setResult(null);
            onSaved?.();
          },
        }
      );
    } catch {
      // Lỗi đã xử lý qua onError (toast) — chặn unhandled rejection.
    }
  };

  // ─── Bước 1: chọn & tải file ───
  if (!result) {
    return (
      <div className="space-y-4">
        <Alert>
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Lưu ý</AlertTitle>
          <AlertDescription>
            Hệ thống đọc file PDF/ảnh phiếu kết quả CĐHA và tách 2 phần Mô tả / Kết luận. Nội dung đọc
            được cần xác nhận, sửa lại trước khi lưu vào hồ sơ.
          </AlertDescription>
        </Alert>

        {!fixedRadOrderId && (
          <div className="space-y-1.5">
            <Label htmlFor="rad-ocr-order-id">Mã chỉ định CĐHA (rad order) *</Label>
            <Input
              id="rad-ocr-order-id"
              placeholder="Nhập ID chỉ định CĐHA cần lưu kết quả..."
              value={radOrderIdInput}
              onChange={(e) => setRadOrderIdInput(e.target.value)}
            />
          </div>
        )}

        <div
          className="border-2 border-dashed rounded-lg p-6 text-center transition-colors cursor-pointer border-muted-foreground/25 hover:border-primary/50"
          onClick={() => fileInputRef.current?.click()}
          role="button"
          tabIndex={0}
          aria-label="Chọn file PDF hoặc ảnh phiếu kết quả CĐHA"
          onKeyDown={(e) => e.key === "Enter" && fileInputRef.current?.click()}
        >
          <Upload className="h-8 w-8 mx-auto mb-2 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">
            Kéo thả hoặc <span className="text-primary font-medium">chọn file PDF/ảnh</span> phiếu kết quả CĐHA
          </p>
          <input
            ref={fileInputRef}
            type="file"
            accept="application/pdf,image/*"
            className="hidden"
            onChange={handleFileInput}
          />
        </div>

        {pendingFile && (
          <div className="border rounded-lg p-3 flex items-center justify-between gap-2 bg-muted/20">
            <div className="flex items-center gap-2 min-w-0">
              <FileUp className="h-4 w-4 shrink-0 text-muted-foreground" />
              <span className="text-sm truncate">{pendingFile.name}</span>
              <span className="text-xs text-muted-foreground shrink-0">
                ({(pendingFile.size / 1024).toFixed(0)} KB)
              </span>
            </div>
            <div className="flex items-center gap-1 shrink-0">
              <Button size="sm" onClick={handleUpload} disabled={extractMutation.isPending}>
                {extractMutation.isPending ? "Đang đọc..." : "Tải lên & đọc"}
              </Button>
              <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => setPendingFile(null)}>
                <X className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </div>
    );
  }

  // ─── Bước 2: xác nhận / sửa tay 2 ô lớn ───
  const canSave = radOrderId !== "" && findings.trim().length > 0 && conclusion.trim().length > 0;

  return (
    <div className="space-y-4">
      {!result.has_any_extracted && (
        <Alert className="border-destructive/50 text-destructive [&>svg]:text-destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Không đọc được nội dung nào</AlertTitle>
          <AlertDescription>
            File có thể là bản scan mờ hoặc phiếu không có nhãn Mô tả / Kết luận. Vui lòng nhập tay bên dưới.
          </AlertDescription>
        </Alert>
      )}

      {!fixedRadOrderId && (
        <div className="space-y-1.5">
          <Label htmlFor="rad-ocr-order-id-2">Mã chỉ định CĐHA (rad order) *</Label>
          <Input
            id="rad-ocr-order-id-2"
            placeholder="Nhập ID chỉ định CĐHA..."
            value={radOrderIdInput}
            onChange={(e) => setRadOrderIdInput(e.target.value)}
          />
        </div>
      )}

      <div className="space-y-1.5">
        <Label htmlFor="rad-ocr-performed">Thời gian thực hiện *</Label>
        <Input
          id="rad-ocr-performed"
          type="datetime-local"
          value={performedAt}
          onChange={(e) => setPerformedAt(e.target.value)}
        />
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="rad-ocr-findings">Mô tả hình ảnh *</Label>
        <Textarea
          id="rad-ocr-findings"
          value={findings}
          onChange={(e) => setFindings(e.target.value)}
          rows={6}
          placeholder="Mô tả chi tiết hình ảnh quan sát được..."
        />
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="rad-ocr-impression">Ấn tượng / Đánh giá</Label>
        <Textarea
          id="rad-ocr-impression"
          value={impression}
          onChange={(e) => setImpression(e.target.value)}
          rows={2}
          placeholder="Nhận xét, ấn tượng ban đầu..."
        />
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="rad-ocr-conclusion">Kết luận *</Label>
        <Textarea
          id="rad-ocr-conclusion"
          value={conclusion}
          onChange={(e) => setConclusion(e.target.value)}
          rows={4}
          placeholder="Kết luận chẩn đoán hình ảnh..."
        />
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="rad-ocr-recommendations">Đề nghị</Label>
        <Textarea
          id="rad-ocr-recommendations"
          value={recommendations}
          onChange={(e) => setRecommendations(e.target.value)}
          rows={2}
          placeholder="Các đề nghị theo dõi, tái khám..."
        />
      </div>

      <div className="flex gap-2">
        <Button onClick={handleConfirm} disabled={confirmMutation.isPending || !canSave} className="min-h-[44px]">
          {confirmMutation.isPending ? "Đang lưu..." : "Lưu kết quả"}
        </Button>
        <Button
          variant="outline"
          className="min-h-[44px]"
          onClick={() => setResult(null)}
        >
          Huỷ, chọn file khác
        </Button>
      </div>
    </div>
  );
}
