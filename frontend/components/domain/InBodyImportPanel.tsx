"use client";

import { useRef, useState } from "react";
import { AlertTriangle, FileUp, Upload, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { useUploadInBodyReport, useConfirmInBodyReport } from "@/lib/hooks/use-inbody-reports";
import type { InBodyFieldDto, InBodyIndicatorType, InBodyReportResponse } from "@/lib/api/inbody-reports";

// Nhan tieng Viet + don vi mac dinh cho tung chi so — xem PRD muc 5.
const INDICATOR_LABELS: Record<InBodyIndicatorType, { label: string; unit: string }> = {
  WEIGHT_KG: { label: "Cân nặng", unit: "kg" },
  BMI: { label: "BMI", unit: "kg/m²" },
  SMM: { label: "Khối cơ xương (SMM)", unit: "kg" },
  BODY_FAT_MASS: { label: "Khối lượng mỡ", unit: "kg" },
  PBF: { label: "Tỷ lệ mỡ cơ thể (PBF)", unit: "%" },
  VISCERAL_FAT: { label: "Mỡ nội tạng", unit: "" },
  TBW: { label: "Tổng nước cơ thể (TBW)", unit: "L" },
  BMR: { label: "Chuyển hoá cơ bản (BMR)", unit: "kcal" },
  INBODY_SCORE: { label: "Điểm InBody", unit: "điểm" },
};

const INDICATOR_ORDER: InBodyIndicatorType[] = [
  "WEIGHT_KG",
  "BMI",
  "SMM",
  "BODY_FAT_MASS",
  "PBF",
  "VISCERAL_FAT",
  "TBW",
  "BMR",
  "INBODY_SCORE",
];

interface EditableField {
  indicator_type: InBodyIndicatorType;
  value: string;
  unit: string;
  extracted: boolean;
  include: boolean;
}

function toEditable(fields: InBodyFieldDto[]): EditableField[] {
  const byType = new Map(fields.map((f) => [f.indicator_type, f]));
  return INDICATOR_ORDER.map((type) => {
    const f = byType.get(type);
    const meta = INDICATOR_LABELS[type];
    return {
      indicator_type: type,
      value: f?.value != null ? String(f.value) : "",
      unit: f?.unit ?? meta.unit,
      extracted: f?.extracted ?? false,
      // BMI: backend co tinh KHONG luu rieng (tinh lai tu can nang + chieu cao) -> khong tich mac dinh,
      // checkbox se bi disable ben duoi de tranh gay hieu nham cho dieu duong.
      include: type === "BMI" ? false : f?.extracted ?? false,
    };
  });
}

interface InBodyImportPanelProps {
  patientId: string;
  encounterId?: string;
  onSaved?: () => void;
}

export function InBodyImportPanel({ patientId, encounterId, onSaved }: InBodyImportPanelProps) {
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [report, setReport] = useState<InBodyReportResponse | null>(null);
  const [fields, setFields] = useState<EditableField[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const uploadMutation = useUploadInBodyReport(patientId);
  const confirmMutation = useConfirmInBodyReport(patientId, encounterId);

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) setPendingFile(file);
  };

  const handleUpload = async () => {
    if (!pendingFile) return;
    try {
      const result = await uploadMutation.mutateAsync({ file: pendingFile, encounterId });
      setReport(result);
      setFields(toEditable(result.fields));
      setPendingFile(null);
    } catch {
      // Loi da duoc xu ly qua onError cua uploadMutation (hien toast) — chan unhandled rejection.
    }
  };

  const updateField = (type: InBodyIndicatorType, patch: Partial<EditableField>) => {
    setFields((prev) => prev.map((f) => (f.indicator_type === type ? { ...f, ...patch } : f)));
  };

  const allFailedExtract = report?.fields.length ? report.fields.every((f) => !f.extracted) : false;

  const weightField = fields.find((f) => f.indicator_type === "WEIGHT_KG");
  const weightMissingEncounter = !encounterId && !!weightField?.include && weightField.value !== "";

  const handleConfirm = async () => {
    if (!report) return;
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
        {
          onSuccess: () => {
            setReport(null);
            setFields([]);
            onSaved?.();
          },
        }
      );
    } catch {
      // Loi da duoc xu ly qua onError cua confirmMutation (hien toast) — chan unhandled rejection.
    }
  };

  if (!report) {
    return (
      <div className="space-y-4">
        <Alert>
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Lưu ý</AlertTitle>
          <AlertDescription>
            Hệ thống chỉ đọc được PDF có lớp văn bản (text layer) do máy InBody xuất trực tiếp.
            Nếu file là bản scan ảnh, hệ thống sẽ không đọc được chỉ số nào — cần nhập tay.
          </AlertDescription>
        </Alert>

        <div
          className="border-2 border-dashed rounded-lg p-6 text-center transition-colors cursor-pointer border-muted-foreground/25 hover:border-primary/50"
          onClick={() => fileInputRef.current?.click()}
          role="button"
          tabIndex={0}
          aria-label="Chọn file PDF kết quả InBody"
          onKeyDown={(e) => e.key === "Enter" && fileInputRef.current?.click()}
        >
          <Upload className="h-8 w-8 mx-auto mb-2 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">
            Kéo thả hoặc <span className="text-primary font-medium">chọn file PDF</span> kết quả máy InBody
          </p>
          <input
            ref={fileInputRef}
            type="file"
            accept="application/pdf"
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
              <Button size="sm" onClick={handleUpload} disabled={uploadMutation.isPending}>
                {uploadMutation.isPending ? "Đang đọc..." : "Tải lên & đọc"}
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

  return (
    <div className="space-y-4">
      {allFailedExtract && (
        <Alert className="border-destructive/50 text-destructive [&>svg]:text-destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertTitle>Không đọc được chỉ số nào</AlertTitle>
          <AlertDescription>
            File PDF có thể là bản scan ảnh (không có lớp văn bản). Vui lòng nhập tay toàn bộ chỉ số bên dưới.
          </AlertDescription>
        </Alert>
      )}

      {weightMissingEncounter && (
        <Alert className="border-destructive/50 text-destructive [&>svg]:text-destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            Chưa chọn lượt khám (encounter) — không thể lưu cân nặng vào sinh hiệu. Vui lòng thao tác từ màn khám bệnh.
          </AlertDescription>
        </Alert>
      )}

      <div className="border rounded-lg divide-y">
        <div className="grid grid-cols-[1fr_120px_90px_70px] gap-2 px-3 py-2 text-xs font-medium text-muted-foreground bg-muted/40">
          <span>Chỉ số</span>
          <span>Giá trị</span>
          <span>Trạng thái</span>
          <span className="text-center">Dùng</span>
        </div>
        {fields.map((f) => {
          const meta = INDICATOR_LABELS[f.indicator_type];
          return (
            <div key={f.indicator_type} className="grid grid-cols-[1fr_120px_90px_70px] gap-2 px-3 py-2 items-center">
              <div className="text-sm">
                {meta.label}
                {meta.unit && <span className="text-xs text-muted-foreground"> ({meta.unit})</span>}
              </div>
              <Input
                type="number"
                step="0.1"
                className="h-8"
                value={f.value}
                aria-label={`Giá trị ${meta.label}`}
                onChange={(e) => updateField(f.indicator_type, { value: e.target.value })}
              />
              {f.extracted ? (
                <span className="text-xs text-emerald-600 font-medium">Đọc được</span>
              ) : (
                <span className="text-xs text-amber-600 font-medium">Chưa đọc được</span>
              )}
              <div className="flex justify-center">
                {f.indicator_type === "BMI" ? (
                  <Tooltip>
                    <TooltipTrigger className="cursor-not-allowed">
                      <Checkbox
                        checked={false}
                        aria-label={`Lưu chỉ số ${meta.label}`}
                        disabled
                      />
                    </TooltipTrigger>
                    <TooltipContent side="top">
                      BMI được tính tự động từ cân nặng và chiều cao, không cần xác nhận riêng
                    </TooltipContent>
                  </Tooltip>
                ) : (
                  <Checkbox
                    checked={f.include}
                    aria-label={`Lưu chỉ số ${meta.label}`}
                    onCheckedChange={(v) => updateField(f.indicator_type, { include: !!v })}
                    disabled={f.value === ""}
                  />
                )}
              </div>
            </div>
          );
        })}
      </div>

      <div className="flex gap-2">
        <Button
          onClick={handleConfirm}
          disabled={confirmMutation.isPending}
          className="min-h-[44px]"
        >
          {confirmMutation.isPending ? "Đang lưu..." : "Xác nhận & Lưu"}
        </Button>
        <Button
          variant="outline"
          className="min-h-[44px]"
          onClick={() => {
            setReport(null);
            setFields([]);
          }}
        >
          Huỷ, chọn file khác
        </Button>
      </div>
    </div>
  );
}

export function InBodyImportSkeleton() {
  return (
    <div className="space-y-2">
      {[1, 2, 3].map((i) => (
        <Skeleton key={i} className="h-10 w-full" />
      ))}
    </div>
  );
}
