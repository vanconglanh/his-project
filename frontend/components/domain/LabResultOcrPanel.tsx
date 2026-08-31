"use client";

import { useRef, useState } from "react";
import { AlertTriangle, FileUp, Loader2, Upload, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useOcrExtractLabResult, useOcrConfirmLabResult } from "@/lib/hooks/use-lab-results";
import type { LabOcrExtractedField, LabOcrExtractResult } from "@/lib/api/lab-results";

const DEFAULT_RANGE_WARNING_NOTE =
  "Giá trị nằm ngoài khoảng thông thường, vui lòng kiểm tra lại";

interface EditableOcrField extends LabOcrExtractedField {
  editedValue: string;
  editedUnit: string;
  include: boolean;
  // GAP-2: gia tri OCR GOC (truoc khi sua) — giu song song voi ban da sua de gui khi confirm.
  ocrRawValue: string | null;
}

function toEditable(fields: LabOcrExtractedField[]): EditableOcrField[] {
  return fields.map((f) => ({
    ...f,
    editedValue: f.value ?? "",
    editedUnit: f.unit ?? "",
    // Chi tu dong tick "Dung" khi da doc duoc gia tri — chua doc duoc thi bat nguoi dung nhap tay roi tu tick.
    include: f.extracted,
    ocrRawValue: f.value ?? null,
  }));
}



interface LabResultOcrPanelProps {
  /** Encounter dang chon san (vd dang o man kham benh). Neu khong truyen, cho phep nguoi dung nhap tay. */
  encounterId?: string;
  onSaved?: () => void;
}

export function LabResultOcrPanel({ encounterId: fixedEncounterId, onSaved }: LabResultOcrPanelProps) {
  const [encounterIdInput, setEncounterIdInput] = useState("");
  const encounterId = fixedEncounterId ?? encounterIdInput.trim();

  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [result, setResult] = useState<LabOcrExtractResult | null>(null);
  const [fields, setFields] = useState<EditableOcrField[]>([]);
  // GAP-3: nguoi dung phai tich xac nhan da kiem tra cac gia tri bi canh bao do truoc khi duoc luu.
  const [abnormalAck, setAbnormalAck] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const extractMutation = useOcrExtractLabResult();
  const confirmMutation = useOcrConfirmLabResult();

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) setPendingFile(file);
  };

  const handleUpload = async () => {
    if (!pendingFile || !encounterId) return;
    try {
      const res = await extractMutation.mutateAsync({ file: pendingFile, encounterId });
      setResult(res);
      setFields(toEditable(res.fields));
      setPendingFile(null);
      setAbnormalAck(false);
    } catch {
      // Loi da duoc xu ly qua onError cua extractMutation (hien toast) — chan unhandled rejection.
    }
  };

  const updateField = (lab_order_item_id: string, patch: Partial<EditableOcrField>) => {
    setFields((prev) =>
      prev.map((f) => (f.lab_order_item_id === lab_order_item_id ? { ...f, ...patch } : f))
    );
  };

  const allFailedExtract = result?.fields.length ? result.fields.every((f) => !f.extracted) : false;

  // GAP-3: chi chan nut khi field canh bao do DANG duoc chon de luu (include).
  const hasPendingAbnormal = fields.some((f) => f.include && f.out_of_plausible_range);

  const handleConfirm = async () => {
    if (!result) return;
    const items = fields
      .filter((f) => f.include && f.editedValue.trim() !== "")
      .map((f) => ({
        lab_order_item_id: f.lab_order_item_id,
        value: f.editedValue,
        value_numeric: Number.isFinite(Number(f.editedValue)) ? Number(f.editedValue) : null,
        unit: f.editedUnit || null,
        include: true,
        // GAP-2: gui kem gia tri OCR goc (chua sua) de backend luu diff.
        ocr_raw_value: f.ocrRawValue,
      }));

    try {
      await confirmMutation.mutateAsync(
        // GAP-8: thread lai source_file_id tu buoc extract.
        { items, source_file_id: result.source_file_id ?? null },
        {
          onSuccess: () => {
            setResult(null);
            setFields([]);
            onSaved?.();
          },
        }
      );
    } catch {
      // Loi da duoc xu ly qua onError cua confirmMutation (hien toast) — chan unhandled rejection.
    }
  };

  const noneSelected = fields.every((f) => !f.include || f.editedValue.trim() === "");

  if (!result) {
    return (
      <div className="space-y-4">
        <Alert>
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Lưu ý</AlertTitle>
          <AlertDescription>
            Hệ thống đọc file PDF/ảnh kết quả xét nghiệm và dò khớp với các chỉ định đang chờ kết quả
            của lượt khám. Kết quả đọc được cần xác nhận lại trước khi lưu vào hồ sơ.
          </AlertDescription>
        </Alert>

        {!fixedEncounterId && (
          <div className="space-y-1.5">
            <Label htmlFor="ocr-encounter-id">Mã lượt khám (encounter) *</Label>
            <Input
              id="ocr-encounter-id"
              placeholder="Nhập ID lượt khám cần đọc kết quả..."
              value={encounterIdInput}
              onChange={(e) => setEncounterIdInput(e.target.value)}
            />
          </div>
        )}

        <div
          className="border-2 border-dashed rounded-lg p-6 text-center transition-colors cursor-pointer border-muted-foreground/25 hover:border-primary/50"
          onClick={() => fileInputRef.current?.click()}
          role="button"
          tabIndex={0}
          aria-label="Chọn file PDF hoặc ảnh kết quả xét nghiệm"
          onKeyDown={(e) => e.key === "Enter" && fileInputRef.current?.click()}
        >
          <Upload className="h-8 w-8 mx-auto mb-2 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">
            Kéo thả hoặc <span className="text-primary font-medium">chọn file PDF/ảnh</span> kết quả xét nghiệm
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
              <Button
                size="sm"
                onClick={handleUpload}
                disabled={extractMutation.isPending || !encounterId}
              >
                {extractMutation.isPending && (
                  <Loader2 className="h-4 w-4 mr-1.5 animate-spin" aria-hidden="true" />
                )}
                {extractMutation.isPending ? "Đang đọc..." : "Tải lên & đọc"}
              </Button>
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8"
                onClick={() => setPendingFile(null)}
                disabled={extractMutation.isPending}
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}

        {extractMutation.isPending && (
          <div className="flex items-center gap-2 text-sm text-muted-foreground" role="status" aria-live="polite">
            <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            Đang đọc nội dung tài liệu, vui lòng đợi...
          </div>
        )}

        {!encounterId && pendingFile && (
          <p className="text-xs text-destructive">Vui lòng nhập mã lượt khám trước khi tải lên.</p>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Đã đọc được <span className="font-medium text-foreground">{result.extracted_count}</span> /{" "}
        {result.pending_count} chỉ định đang chờ kết quả.
      </p>

      {allFailedExtract && (
        <Alert className="border-destructive/50 text-destructive [&>svg]:text-destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Không đọc được chỉ số nào</AlertTitle>
          <AlertDescription>
            File có thể là bản scan mờ hoặc không đúng định dạng. Vui lòng nhập tay toàn bộ kết quả bên dưới.
          </AlertDescription>
        </Alert>
      )}

      <div className="border rounded-lg divide-y overflow-x-auto">
        <div className="grid grid-cols-[1fr_140px_100px_110px_70px] gap-2 px-3 py-2 text-xs font-medium text-muted-foreground bg-muted/40 min-w-[560px]">
          <span>Xét nghiệm</span>
          <span>Giá trị đọc được</span>
          <span>Đơn vị</span>
          <span>Trạng thái</span>
          <span className="text-center">Chọn lưu</span>
        </div>
        {fields.map((f) => {
          const showWarning = f.out_of_plausible_range;
          return (
            <div
              key={f.lab_order_item_id}
              className={
                showWarning
                  ? "px-3 py-2 space-y-1 min-w-[560px] bg-destructive/10"
                  : "px-3 py-2 space-y-1 min-w-[560px]"
              }
            >
              <div className="grid grid-cols-[1fr_140px_100px_110px_70px] gap-2 items-center">
                <div className="text-sm flex items-center gap-1.5">
                  {showWarning && (
                    <AlertTriangle className="h-3.5 w-3.5 text-destructive shrink-0" aria-hidden="true" />
                  )}
                  {f.test_name}
                  <span className="text-xs text-muted-foreground"> ({f.test_code})</span>
                </div>
                <Input
                  className={showWarning ? "h-8 border-destructive focus-visible:ring-destructive" : "h-8"}
                  value={f.editedValue}
                  aria-label={`Giá trị ${f.test_name}`}
                  aria-invalid={showWarning}
                  onChange={(e) => updateField(f.lab_order_item_id, { editedValue: e.target.value })}
                />
                <Input
                  className="h-8"
                  value={f.editedUnit}
                  aria-label={`Đơn vị ${f.test_name}`}
                  onChange={(e) => updateField(f.lab_order_item_id, { editedUnit: e.target.value })}
                />
                {f.extracted ? (
                  <span className="text-xs text-emerald-600 font-medium">Đọc được</span>
                ) : (
                  <span className="text-xs text-amber-600 font-medium">Chưa đọc được</span>
                )}
                <div className="flex justify-center">
                  <Checkbox
                    checked={f.include}
                    aria-label={`Lưu kết quả ${f.test_name}`}
                    onCheckedChange={(v) => updateField(f.lab_order_item_id, { include: !!v })}
                    disabled={f.editedValue.trim() === ""}
                  />
                </div>
              </div>
              {showWarning && (
                <p className="text-xs text-destructive">
                  {f.plausible_range_note || DEFAULT_RANGE_WARNING_NOTE}
                </p>
              )}
            </div>
          );
        })}
      </div>

      {hasPendingAbnormal && (
        <div className="flex items-start gap-2 rounded-md border border-destructive/50 bg-destructive/10 p-3">
          <Checkbox
            id="lab-ocr-abnormal-ack"
            checked={abnormalAck}
            onCheckedChange={(v) => setAbnormalAck(!!v)}
            aria-label="Tôi đã kiểm tra và xác nhận các giá trị được cảnh báo là đúng"
          />
          <label htmlFor="lab-ocr-abnormal-ack" className="text-sm text-destructive leading-snug cursor-pointer">
            Tôi đã kiểm tra và xác nhận các giá trị được cảnh báo là đúng
          </label>
        </div>
      )}

      <div className="flex gap-2">
        <Button
          onClick={handleConfirm}
          disabled={confirmMutation.isPending || noneSelected || (hasPendingAbnormal && !abnormalAck)}
          className="min-h-[44px]"
        >
          {confirmMutation.isPending && <Loader2 className="h-4 w-4 mr-1.5 animate-spin" aria-hidden="true" />}
          {confirmMutation.isPending ? "Đang lưu..." : "Lưu kết quả đã chọn"}
        </Button>
        <Button
          variant="outline"
          className="min-h-[44px]"
          onClick={() => {
            setResult(null);
            setFields([]);
          }}
        >
          Huỷ, chọn file khác
        </Button>
      </div>
    </div>
  );
}
