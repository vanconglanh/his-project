"use client";

import { useMemo, useRef, useState } from "react";
import {
  AlertTriangle,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  FileUp,
  Upload,
  X,
} from "lucide-react";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useSmartUploadDocument } from "@/lib/hooks/use-documents";
import { useConfirmInBodyReport } from "@/lib/hooks/use-inbody-reports";
import { useOcrConfirmLabResult } from "@/lib/hooks/use-lab-results";
import { getErrorMessage } from "@/lib/utils/errors";
import { cn } from "@/lib/utils";
import type {
  SmartDocumentType,
  SmartUploadItemResult,
  SmartUploadResponse,
} from "@/lib/api/documents";
import type { InBodyFieldDto, InBodyIndicatorType } from "@/lib/api/inbody-reports";
import type { LabOcrExtractedField } from "@/lib/api/lab-results";

// ─── Nhãn tiếng Việt cho loại tài liệu (khớp enum backend — xem contract) ──────
const TYPE_LABELS: Record<SmartDocumentType, string> = {
  InBody: "Kết quả InBody",
  LabResult: "Kết quả xét nghiệm",
  RadResult: "Kết quả CĐHA",
  Legacy: "Hồ sơ cũ",
  Unknown: "Chưa xác định",
};

const CONFIDENCE_THRESHOLD = 0.6;

// Nhãn rút gọn cho chỉ số InBody — dùng riêng cho khối xác nhận nhanh trong dialog
// (không sửa/di chuyển INDICATOR_LABELS gốc trong InBodyImportPanel.tsx).
const INBODY_LABELS: Record<InBodyIndicatorType, string> = {
  WEIGHT_KG: "Cân nặng (kg)",
  BMI: "BMI (kg/m²)",
  SMM: "Khối cơ xương - SMM (kg)",
  BODY_FAT_MASS: "Khối lượng mỡ (kg)",
  PBF: "Tỷ lệ mỡ cơ thể - PBF (%)",
  VISCERAL_FAT: "Mỡ nội tạng",
  TBW: "Tổng nước cơ thể - TBW (L)",
  BMR: "Chuyển hoá cơ bản - BMR (kcal)",
  INBODY_SCORE: "Điểm InBody",
};

interface EncounterOption {
  id: string;
  label: string;
}

interface SmartUploadDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  patientId: string;
  encounterOptions?: EncounterOption[];
  /** Encounter gần nhất — dùng làm gợi ý mặc định (không tự động chọn để tránh nhầm). */
  defaultEncounterId?: string;
  /** Điều hướng sang tab tương ứng khi người dùng tự chọn loại mơ hồ (Legacy/Unknown). */
  onNavigateTab?: (tabId: "inbody" | "cls" | "legacy-docs") => void;
}

export function SmartUploadDialog({
  open,
  onOpenChange,
  patientId,
  encounterOptions = [],
  defaultEncounterId,
  onNavigateTab,
}: SmartUploadDialogProps) {
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);
  const [encounterId, setEncounterId] = useState<string>("");
  const [batch, setBatch] = useState<SmartUploadItemResult[] | null>(null);
  const [savedIdx, setSavedIdx] = useState<Set<number>>(new Set());
  const fileInputRef = useRef<HTMLInputElement>(null);

  const smartUploadMutation = useSmartUploadDocument();

  function resetAll() {
    setPendingFiles([]);
    setEncounterId("");
    setBatch(null);
    setSavedIdx(new Set());
  }

  function handleClose(nextOpen: boolean) {
    if (!nextOpen) resetAll();
    onOpenChange(nextOpen);
  }

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const picked = Array.from(e.target.files ?? []);
    if (picked.length === 0) return;
    // Cộng dồn file mới, khử trùng theo (tên + kích thước) để không thêm nhầm 2 lần.
    setPendingFiles((prev) => {
      const seen = new Set(prev.map((f) => `${f.name}:${f.size}`));
      const next = [...prev];
      for (const f of picked) {
        const key = `${f.name}:${f.size}`;
        if (!seen.has(key)) {
          seen.add(key);
          next.push(f);
        }
      }
      return next;
    });
    // Cho phép chọn lại cùng file sau khi đã xoá.
    e.target.value = "";
  };

  function removePendingFile(index: number) {
    setPendingFiles((prev) => prev.filter((_, i) => i !== index));
  }

  async function handleAnalyze() {
    if (pendingFiles.length === 0) return;
    try {
      const res = await smartUploadMutation.mutateAsync({
        files: pendingFiles,
        patientId,
        encounterId: encounterId || undefined,
      });
      setBatch(res.items);
      setSavedIdx(new Set());
    } catch {
      // Loi da duoc xu ly qua onError cua smartUploadMutation (hien toast).
    }
  }

  function markSaved(index: number) {
    toast.success("Đã lưu tài liệu vào hồ sơ bệnh nhân");
    setSavedIdx((prev) => {
      const next = new Set(prev);
      next.add(index);
      return next;
    });
  }

  const hasZip = pendingFiles.some((f) => f.name.toLowerCase().endsWith(".zip"));
  const totalKb = pendingFiles.reduce((s, f) => s + f.size, 0) / 1024;

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-2xl max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Upload className="h-5 w-5" />
            Tải tài liệu lên — tự nhận diện loại
          </DialogTitle>
          <DialogDescription>
            Chọn <strong>nhiều tệp cùng lúc</strong> hoặc <strong>1 tệp ZIP</strong>. Mỗi tệp được đọc (OCR) và
            tự phân loại độc lập: InBody / Kết quả xét nghiệm / Kết quả CĐHA / Hồ sơ cũ.
          </DialogDescription>
        </DialogHeader>

        {!batch && (
          <div className="space-y-4">
            {encounterOptions.length > 0 && (
              <div className="space-y-1.5">
                <label className="text-sm font-medium" htmlFor="smart-upload-encounter">
                  Lượt khám liên quan (tuỳ chọn — áp dụng cho tất cả tệp)
                </label>
                <Select value={encounterId || undefined} onValueChange={(v) => setEncounterId(v ?? "")}>
                  <SelectTrigger id="smart-upload-encounter">
                    <SelectValue placeholder="Không chọn — để trống nếu chưa rõ" />
                  </SelectTrigger>
                  <SelectContent>
                    {encounterOptions.map((opt) => (
                      <SelectItem key={opt.id} value={opt.id}>
                        {opt.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {defaultEncounterId && !encounterId && (
                  <p className="text-xs text-muted-foreground">
                    Gợi ý: lượt khám gần nhất — chọn ở trên nếu muốn gắn kết quả vào lượt khám đó.
                  </p>
                )}
              </div>
            )}

            <div
              className="border-2 border-dashed rounded-lg p-6 text-center transition-colors cursor-pointer border-muted-foreground/25 hover:border-primary/50"
              onClick={() => fileInputRef.current?.click()}
              role="button"
              tabIndex={0}
              aria-label="Chọn nhiều tệp PDF/ảnh hoặc 1 tệp ZIP"
              onKeyDown={(e) => e.key === "Enter" && fileInputRef.current?.click()}
            >
              <Upload className="h-8 w-8 mx-auto mb-2 text-muted-foreground" />
              <p className="text-sm text-muted-foreground">
                Kéo thả hoặc{" "}
                <span className="text-primary font-medium">chọn nhiều tệp PDF/ảnh, hoặc 1 tệp ZIP</span> để hệ
                thống tự nhận diện từng tệp
              </p>
              <input
                ref={fileInputRef}
                type="file"
                accept=".pdf,image/*,.zip,application/zip"
                multiple
                className="hidden"
                onChange={handleFileInput}
              />
            </div>

            {hasZip && pendingFiles.length > 1 && (
              <Alert className="border-amber-300 bg-amber-50 dark:border-amber-800 dark:bg-amber-950/30">
                <AlertTriangle className="h-4 w-4 text-amber-600" />
                <AlertDescription className="text-xs">
                  Tệp ZIP chỉ được tự giải nén khi tải <strong>riêng một mình</strong>. Nếu chọn kèm tệp khác,
                  ZIP sẽ không được đọc — vui lòng bỏ bớt để chỉ còn tệp ZIP, hoặc bỏ tệp ZIP ra.
                </AlertDescription>
              </Alert>
            )}

            {pendingFiles.length > 0 && (
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <p className="text-xs text-muted-foreground">
                    {pendingFiles.length} tệp — tổng {totalKb.toFixed(0)} KB
                  </p>
                  <Button variant="ghost" size="sm" className="h-7 text-xs" onClick={() => setPendingFiles([])}>
                    Xoá tất cả
                  </Button>
                </div>
                <div className="border rounded-lg divide-y max-h-52 overflow-y-auto">
                  {pendingFiles.map((f, i) => (
                    <div key={`${f.name}:${f.size}:${i}`} className="p-2.5 flex items-center justify-between gap-2 bg-muted/10">
                      <div className="flex items-center gap-2 min-w-0">
                        <FileUp className="h-4 w-4 shrink-0 text-muted-foreground" />
                        <span className="text-sm truncate">{f.name}</span>
                        <span className="text-xs text-muted-foreground shrink-0">
                          ({(f.size / 1024).toFixed(0)} KB)
                        </span>
                      </div>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-7 w-7 shrink-0"
                        aria-label={`Bỏ tệp ${f.name}`}
                        onClick={() => removePendingFile(i)}
                      >
                        <X className="h-4 w-4" />
                      </Button>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}

        {batch && (
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">
              Đã xử lý <strong>{batch.length}</strong> tệp — nhấn từng dòng để xem chi tiết và xác nhận riêng.
            </p>
            {batch.map((item, i) => (
              <FileResultCard
                key={`${item.file_name}:${i}`}
                item={item}
                patientId={patientId}
                encounterId={encounterId}
                encounterOptions={encounterOptions}
                onNavigateTab={onNavigateTab}
                onCloseDialog={() => handleClose(false)}
                onReanalyze={handleAnalyze}
                isReanalyzing={smartUploadMutation.isPending}
                onEncounterChange={setEncounterId}
                saved={savedIdx.has(i)}
                onSaved={() => markSaved(i)}
              />
            ))}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => handleClose(false)}>
            {batch ? "Đóng" : "Huỷ"}
          </Button>
          {!batch && (
            <Button
              onClick={handleAnalyze}
              disabled={pendingFiles.length === 0 || smartUploadMutation.isPending}
              className="min-h-[44px]"
            >
              {smartUploadMutation.isPending
                ? "Đang phân tích..."
                : pendingFiles.length > 1
                  ? `Phân tích ${pendingFiles.length} tệp`
                  : "Phân tích"}
            </Button>
          )}
          {batch && (
            <Button
              variant="outline"
              onClick={() => {
                setBatch(null);
                setPendingFiles([]);
                setSavedIdx(new Set());
              }}
            >
              Tải tệp khác
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

// ─── Thẻ kết quả theo TỪNG tệp (mở rộng xem chi tiết + xác nhận riêng) ──────────

function FileResultCard({
  item,
  patientId,
  encounterId,
  encounterOptions,
  onNavigateTab,
  onCloseDialog,
  onReanalyze,
  isReanalyzing,
  onEncounterChange,
  saved,
  onSaved,
}: {
  item: SmartUploadItemResult;
  patientId: string;
  encounterId: string;
  encounterOptions: EncounterOption[];
  onNavigateTab?: (tabId: "inbody" | "cls" | "legacy-docs") => void;
  onCloseDialog: () => void;
  onReanalyze: () => void;
  isReanalyzing: boolean;
  onEncounterChange: (id: string) => void;
  saved: boolean;
  onSaved: () => void;
}) {
  const [open, setOpen] = useState(true);
  const result = item.result;

  const typeLabel =
    item.success && result ? TYPE_LABELS[result.classification.type] : "Lỗi xử lý";
  const confidencePercent = result ? Math.round(result.classification.confidence * 100) : 0;

  const isAmbiguous =
    !!result &&
    !result.in_body &&
    !result.lab_result &&
    !result.rad_result &&
    !result.requires_encounter &&
    (result.classification.type === "Legacy" ||
      result.classification.type === "Unknown" ||
      result.classification.confidence < CONFIDENCE_THRESHOLD);

  const [manualType, setManualType] = useState<SmartDocumentType | null>(null);

  return (
    <div className="border rounded-lg overflow-hidden">
      <button
        type="button"
        className="w-full flex items-center gap-2 px-3 py-2.5 text-left hover:bg-muted/40 transition-colors"
        onClick={() => setOpen((o) => !o)}
        aria-expanded={open}
      >
        {open ? (
          <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
        ) : (
          <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
        )}
        <span className="text-sm font-medium truncate min-w-0 flex-1">{item.file_name}</span>
        {saved && <CheckCircle2 className="h-4 w-4 shrink-0 text-emerald-600" aria-label="Đã lưu" />}
        <Badge
          variant={
            !item.success
              ? "destructive"
              : result && result.classification.type === "Unknown"
                ? "secondary"
                : "default"
          }
          className="shrink-0"
        >
          {typeLabel}
        </Badge>
        {item.success && result && (
          <span className="text-xs text-muted-foreground shrink-0">{confidencePercent}%</span>
        )}
      </button>

      {open && (
        <div className="px-3 pb-3 space-y-3 border-t pt-3">
          {!item.success || !result ? (
            <Alert className="border-destructive/50 text-destructive dark:border-destructive [&>svg]:text-destructive">
              <AlertTriangle className="h-4 w-4" />
              <AlertTitle>Không xử lý được tệp này</AlertTitle>
              <AlertDescription className="text-xs">
                {item.error_message ?? "Đã xảy ra lỗi khi đọc tệp."}
              </AlertDescription>
            </Alert>
          ) : saved ? (
            <Alert className="border-emerald-300 bg-emerald-50 dark:border-emerald-800 dark:bg-emerald-950/30">
              <CheckCircle2 className="h-4 w-4 text-emerald-600" />
              <AlertTitle>Đã lưu vào hồ sơ bệnh nhân</AlertTitle>
            </Alert>
          ) : (
            <>
              <ClassificationSummary result={result} confidencePercent={confidencePercent} />

              {result.in_body && (
                <>
                  <p className="text-sm font-medium text-emerald-700 dark:text-emerald-400">
                    Đã nhận diện: Kết quả InBody — xác nhận chỉ số bên dưới trước khi lưu.
                  </p>
                  <InBodySmartConfirm
                    patientId={patientId}
                    encounterId={encounterId || undefined}
                    report={result.in_body}
                    onSaved={onSaved}
                  />
                </>
              )}

              {!result.in_body && result.lab_result && (
                <>
                  <p className="text-sm font-medium text-emerald-700 dark:text-emerald-400">
                    Đã nhận diện: Kết quả xét nghiệm — xác nhận giá trị đọc được bên dưới trước khi lưu.
                  </p>
                  <LabResultSmartConfirm result={result.lab_result} onSaved={onSaved} />
                </>
              )}

              {!result.in_body && !result.lab_result && result.rad_result && (
                <>
                  <p className="text-sm font-medium text-emerald-700 dark:text-emerald-400">
                    Đã nhận diện: Kết quả CĐHA — vui lòng chuyển sang tab &quot;Kết quả CLS&quot; để chọn chỉ định
                    CĐHA tương ứng và xác nhận.
                  </p>
                  <RadResultSmartConfirm
                    result={result.rad_result}
                    onNavigateTab={onNavigateTab}
                    onClose={onCloseDialog}
                  />
                </>
              )}

              {!result.in_body && !result.lab_result && !result.rad_result && result.requires_encounter && (
                <RequiresEncounterPanel
                  encounterOptions={encounterOptions}
                  encounterId={encounterId}
                  onEncounterChange={onEncounterChange}
                  onRetry={onReanalyze}
                  isPending={isReanalyzing}
                />
              )}

              {isAmbiguous && (
                <AmbiguousTypePanel
                  result={result}
                  manualType={manualType}
                  onManualTypeChange={setManualType}
                  onNavigateTab={onNavigateTab}
                  onClose={onCloseDialog}
                />
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
}

// ─── Khối tóm tắt kết quả nhận diện ─────────────────────────────────────────────

function ClassificationSummary({
  result,
  confidencePercent,
}: {
  result: SmartUploadResponse;
  confidencePercent: number;
}) {
  const { classification } = result;
  return (
    <div className="space-y-2 rounded-lg border p-3">
      <div className="flex items-center justify-between gap-2 flex-wrap">
        <span className="text-sm text-muted-foreground">Kết quả nhận diện</span>
        <Badge variant={classification.type === "Unknown" ? "secondary" : "default"}>
          {TYPE_LABELS[classification.type]}
        </Badge>
      </div>
      <div className="space-y-1">
        <div className="h-2 w-full rounded-full bg-muted overflow-hidden">
          <div
            className={cn(
              "h-full rounded-full transition-all",
              confidencePercent >= 60 ? "bg-emerald-500" : "bg-amber-500"
            )}
            style={{ width: `${confidencePercent}%` }}
          />
        </div>
        <p className="text-xs text-muted-foreground">Độ tin cậy: {confidencePercent}%</p>
      </div>
      {classification.evidence.length > 0 && (
        <p className="text-xs text-muted-foreground">
          Căn cứ: {classification.evidence.join(", ")}
        </p>
      )}
    </div>
  );
}

// ─── Khối yêu cầu chọn lượt khám (LabResult chưa có encounter_id) ──────────────

function RequiresEncounterPanel({
  encounterOptions,
  encounterId,
  onEncounterChange,
  onRetry,
  isPending,
}: {
  encounterOptions: EncounterOption[];
  encounterId: string;
  onEncounterChange: (id: string) => void;
  onRetry: () => void;
  isPending: boolean;
}) {
  return (
    <Alert>
      <AlertTriangle className="h-4 w-4" />
      <AlertTitle>Cần chọn lượt khám</AlertTitle>
      <AlertDescription className="space-y-3">
        <p>Đây là kết quả xét nghiệm — vui lòng chọn lượt khám để hệ thống dò khớp chỉ định đang chờ kết quả.</p>
        {encounterOptions.length > 0 ? (
          <Select value={encounterId || undefined} onValueChange={(v) => onEncounterChange(v ?? "")}>
            <SelectTrigger aria-label="Chọn lượt khám">
              <SelectValue placeholder="Chọn lượt khám..." />
            </SelectTrigger>
            <SelectContent>
              {encounterOptions.map((opt) => (
                <SelectItem key={opt.id} value={opt.id}>
                  {opt.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        ) : (
          <p className="text-xs">Bệnh nhân chưa có lượt khám nào để chọn.</p>
        )}
        <Button size="sm" onClick={onRetry} disabled={!encounterId || isPending} className="min-h-[44px]">
          {isPending ? "Đang phân tích lại..." : "Phân tích lại"}
        </Button>
      </AlertDescription>
    </Alert>
  );
}

// ─── Khối mơ hồ: người dùng tự chọn loại (Legacy / Unknown / confidence thấp) ──

function AmbiguousTypePanel({
  result,
  manualType,
  onManualTypeChange,
  onNavigateTab,
  onClose,
}: {
  result: SmartUploadResponse;
  manualType: SmartDocumentType | null;
  onManualTypeChange: (t: SmartDocumentType) => void;
  onNavigateTab?: (tabId: "inbody" | "cls" | "legacy-docs") => void;
  onClose: () => void;
}) {
  const candidates = result.classification.candidates;

  return (
    <Alert className="border-amber-300 bg-amber-50 dark:border-amber-800 dark:bg-amber-950/30">
      <AlertTriangle className="h-4 w-4 text-amber-600" />
      <AlertTitle>Chưa chắc chắn về loại tài liệu</AlertTitle>
      <AlertDescription className="space-y-3">
        <p>Vui lòng tự chọn loại tài liệu phù hợp bên dưới.</p>

        {candidates.length > 0 && (
          <ul className="text-xs space-y-1">
            {candidates.map((c) => (
              <li key={c.type}>
                <span className="font-medium">{TYPE_LABELS[c.type]}</span> ({Math.round(c.score * 100)}%)
                {c.evidence.length > 0 ? ` — ${c.evidence.join(", ")}` : ""}
              </li>
            ))}
          </ul>
        )}

        <Select value={manualType ?? undefined} onValueChange={(v) => onManualTypeChange(v as SmartDocumentType)}>
          <SelectTrigger aria-label="Chọn loại tài liệu">
            <SelectValue placeholder="Chọn loại tài liệu..." />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="InBody">Kết quả InBody</SelectItem>
            <SelectItem value="LabResult">Kết quả xét nghiệm</SelectItem>
            <SelectItem value="RadResult">Kết quả CĐHA</SelectItem>
            <SelectItem value="Legacy">Hồ sơ cũ</SelectItem>
          </SelectContent>
        </Select>

        {manualType === "Legacy" && (
          <a href="/admin/legacy-import" className="inline-block">
            <Button size="sm" className="min-h-[44px]">Đi tới màn nhập hồ sơ cũ</Button>
          </a>
        )}

        {manualType === "InBody" && (
          <div className="space-y-2">
            <p className="text-xs">
              Vui lòng chuyển sang tab &quot;Lịch sử InBody&quot; để tải lại file và xác nhận chỉ số.
            </p>
            <Button
              size="sm"
              className="min-h-[44px]"
              onClick={() => {
                onNavigateTab?.("inbody");
                onClose();
              }}
            >
              Chuyển tới tab InBody
            </Button>
          </div>
        )}

        {manualType === "LabResult" && (
          <div className="space-y-2">
            <p className="text-xs">
              Vui lòng chuyển sang tab &quot;Kết quả CLS&quot; để tải lại file và xác nhận kết quả xét nghiệm.
            </p>
            <Button
              size="sm"
              className="min-h-[44px]"
              onClick={() => {
                onNavigateTab?.("cls");
                onClose();
              }}
            >
              Chuyển tới tab Kết quả CLS
            </Button>
          </div>
        )}

        {manualType === "RadResult" && (
          <div className="space-y-2">
            <p className="text-xs">
              Vui lòng chuyển sang tab &quot;Kết quả CLS&quot; để tải lại file và xác nhận kết quả CĐHA.
            </p>
            <Button
              size="sm"
              className="min-h-[44px]"
              onClick={() => {
                onNavigateTab?.("cls");
                onClose();
              }}
            >
              Chuyển tới tab Kết quả CLS
            </Button>
          </div>
        )}
      </AlertDescription>
    </Alert>
  );
}

// ─── Khối xác nhận nhanh InBody (dùng report đã có sẵn từ smart-upload) ────────

interface EditableInBodyField {
  indicator_type: InBodyIndicatorType;
  value: string;
  unit: string;
  extracted: boolean;
  include: boolean;
}

function toEditableInBody(fields: InBodyFieldDto[]): EditableInBodyField[] {
  return fields.map((f) => ({
    indicator_type: f.indicator_type,
    value: f.value != null ? String(f.value) : "",
    unit: f.unit ?? "",
    extracted: f.extracted,
    include: f.indicator_type === "BMI" ? false : f.extracted,
  }));
}

function InBodySmartConfirm({
  patientId,
  encounterId,
  report,
  onSaved,
}: {
  patientId: string;
  encounterId?: string;
  report: NonNullable<SmartUploadResponse["in_body"]>;
  onSaved: () => void;
}) {
  const [fields, setFields] = useState<EditableInBodyField[]>(() => toEditableInBody(report.fields));
  const confirmMutation = useConfirmInBodyReport(patientId, encounterId);

  const updateField = (type: InBodyIndicatorType, patch: Partial<EditableInBodyField>) => {
    setFields((prev) => prev.map((f) => (f.indicator_type === type ? { ...f, ...patch } : f)));
  };

  const handleConfirm = async () => {
    const payloadFields = fields
      .filter((f) => f.include && f.value !== "")
      .map((f) => ({
        indicator_type: f.indicator_type,
        value: Number(f.value),
        unit: f.unit || null,
        include: true,
      }));
    try {
      await confirmMutation.mutateAsync(
        { id: report.id, encounter_id: encounterId, fields: payloadFields },
        { onSuccess: onSaved }
      );
    } catch {
      // Loi da duoc xu ly qua onError cua confirmMutation (hien toast).
    }
  };

  return (
    <div className="space-y-3">
      <div className="border rounded-lg divide-y max-h-64 overflow-y-auto">
        {fields.map((f) => (
          <div key={f.indicator_type} className="grid grid-cols-[1fr_100px_60px] gap-2 px-3 py-2 items-center">
            <span className="text-sm">{INBODY_LABELS[f.indicator_type]}</span>
            <Input
              type="number"
              step="0.1"
              className="h-8"
              value={f.value}
              aria-label={`Giá trị ${INBODY_LABELS[f.indicator_type]}`}
              onChange={(e) => updateField(f.indicator_type, { value: e.target.value })}
            />
            <div className="flex justify-center">
              <Checkbox
                checked={f.include}
                aria-label={`Lưu chỉ số ${INBODY_LABELS[f.indicator_type]}`}
                onCheckedChange={(v) => updateField(f.indicator_type, { include: !!v })}
                disabled={f.value === "" || f.indicator_type === "BMI"}
              />
            </div>
          </div>
        ))}
      </div>
      <Button onClick={handleConfirm} disabled={confirmMutation.isPending} className="min-h-[44px]">
        {confirmMutation.isPending ? "Đang lưu..." : "Xác nhận & Lưu"}
      </Button>
    </div>
  );
}

// ─── Khối kết quả CĐHA (chỉ đọc trước, cần chọn RadOrder ở tab CLS mới xác nhận
// được — khác InBody/LabResult vì luồng xác nhận CĐHA yêu cầu chọn radOrderId) ─

function RadResultSmartConfirm({
  result,
  onNavigateTab,
  onClose,
}: {
  result: NonNullable<SmartUploadResponse["rad_result"]>;
  onNavigateTab?: (tabId: "inbody" | "cls" | "legacy-docs") => void;
  onClose: () => void;
}) {
  return (
    <div className="space-y-3">
      {result.findings && (
        <div className="space-y-1">
          <p className="text-xs font-medium text-muted-foreground">Mô tả</p>
          <p className="text-sm whitespace-pre-wrap">{result.findings}</p>
        </div>
      )}
      {result.conclusion && (
        <div className="space-y-1">
          <p className="text-xs font-medium text-muted-foreground">Kết luận</p>
          <p className="text-sm whitespace-pre-wrap">{result.conclusion}</p>
        </div>
      )}
      <Button
        size="sm"
        className="min-h-[44px]"
        onClick={() => {
          onNavigateTab?.("cls");
          onClose();
        }}
      >
        Chuyển tới tab Kết quả CLS để chọn chỉ định &amp; xác nhận
      </Button>
    </div>
  );
}

// ─── Khối xác nhận nhanh kết quả xét nghiệm (dùng result đã có sẵn) ────────────

interface EditableLabField extends LabOcrExtractedField {
  editedValue: string;
  editedUnit: string;
  include: boolean;
}

function toEditableLab(fields: LabOcrExtractedField[]): EditableLabField[] {
  return fields.map((f) => ({
    ...f,
    editedValue: f.value ?? "",
    editedUnit: f.unit ?? "",
    include: f.extracted,
  }));
}

function LabResultSmartConfirm({
  result,
  onSaved,
}: {
  result: NonNullable<SmartUploadResponse["lab_result"]>;
  onSaved: () => void;
}) {
  const [fields, setFields] = useState<EditableLabField[]>(() => toEditableLab(result.fields));
  const confirmMutation = useOcrConfirmLabResult();

  const updateField = (id: string, patch: Partial<EditableLabField>) => {
    setFields((prev) => prev.map((f) => (f.lab_order_item_id === id ? { ...f, ...patch } : f)));
  };

  const noneSelected = useMemo(
    () => fields.every((f) => !f.include || f.editedValue.trim() === ""),
    [fields]
  );

  const handleConfirm = async () => {
    const items = fields
      .filter((f) => f.include && f.editedValue.trim() !== "")
      .map((f) => ({
        lab_order_item_id: f.lab_order_item_id,
        value: f.editedValue,
        value_numeric: Number.isFinite(Number(f.editedValue)) ? Number(f.editedValue) : null,
        unit: f.editedUnit || null,
        include: true,
      }));
    try {
      await confirmMutation.mutateAsync({ items }, { onSuccess: onSaved });
    } catch (e) {
      toast.error(getErrorMessage(e, "Lưu kết quả thất bại"));
    }
  };

  return (
    <div className="space-y-3">
      <p className="text-xs text-muted-foreground">
        Đã đọc được {result.extracted_count}/{result.pending_count} chỉ định đang chờ kết quả.
      </p>
      <div className="border rounded-lg divide-y max-h-64 overflow-y-auto">
        {fields.map((f) => (
          <div
            key={f.lab_order_item_id}
            className="grid grid-cols-[1fr_100px_80px_60px] gap-2 px-3 py-2 items-center"
          >
            <span className="text-sm truncate">{f.test_name}</span>
            <Input
              className="h-8"
              value={f.editedValue}
              aria-label={`Giá trị ${f.test_name}`}
              onChange={(e) => updateField(f.lab_order_item_id, { editedValue: e.target.value })}
            />
            <Input
              className="h-8"
              value={f.editedUnit}
              aria-label={`Đơn vị ${f.test_name}`}
              onChange={(e) => updateField(f.lab_order_item_id, { editedUnit: e.target.value })}
            />
            <div className="flex justify-center">
              <Checkbox
                checked={f.include}
                aria-label={`Lưu kết quả ${f.test_name}`}
                onCheckedChange={(v) => updateField(f.lab_order_item_id, { include: !!v })}
                disabled={f.editedValue.trim() === ""}
              />
            </div>
          </div>
        ))}
      </div>
      <Button onClick={handleConfirm} disabled={confirmMutation.isPending || noneSelected} className="min-h-[44px]">
        {confirmMutation.isPending ? "Đang lưu..." : "Lưu kết quả đã chọn"}
      </Button>
    </div>
  );
}
