"use client";

import { useState } from "react";
import { Plus, Save, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { EmptyState } from "@/components/ui/EmptyState";
import { Icd10Picker } from "@/components/domain/Icd10Picker";
import { getIcd10, searchIcd10 } from "@/lib/api/icd10";
import type { DiagnosisResponse, DiagnosisType, Icd10Response } from "@/lib/api/types";

interface DiagnosisRow {
  icd10_code: string;
  icd10_name: string;
  type: DiagnosisType;
  note: string;
}

const DIAG_EMPTY: DiagnosisRow = { icd10_code: "", icd10_name: "", type: "PRIMARY", note: "" };

interface Props {
  diagnoses: DiagnosisResponse[];
  canEdit: boolean;
  onAddSingle: (item: Icd10Response, type: DiagnosisType) => void;
  onDelete: (id: string) => void;
}

export function DiagnosisTabPanel({ diagnoses, canEdit, onAddSingle, onDelete }: Props) {
  const [rows, setRows] = useState<DiagnosisRow[]>([{ ...DIAG_EMPTY }]);

  const updateRow = (index: number, key: keyof DiagnosisRow, value: string | DiagnosisType) => {
    setRows((prev) => prev.map((r, i) => (i === index ? { ...r, [key]: value } : r)));
  };

  // Tự điền tên bệnh khi rời ô mã ICD-10 (nếu chưa nhập tên)
  const lookupName = async (index: number, rawCode: string) => {
    const code = rawCode.trim().toUpperCase();
    if (!code) return;
    if (code !== rawCode) updateRow(index, "icd10_code", code);
    try {
      let item: Icd10Response | undefined;
      try {
        item = await getIcd10(code);
      } catch {
        item = (await searchIcd10({ q: code, type: "code", limit: 1 })).find(
          (r) => r.code.toUpperCase() === code
        );
      }
      if (item?.name_vi) {
        setRows((prev) =>
          prev.map((r, i) =>
            i === index && r.icd10_code.trim().toUpperCase() === code && !r.icd10_name.trim()
              ? { ...r, icd10_name: item!.name_vi }
              : r
          )
        );
      }
    } catch {
      // Ma khong co trong danh muc — de nguyen cho user tu nhap ten
    }
  };

  const handleBulkSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    rows
      .filter((r) => r.icd10_code.trim())
      .forEach((row) => {
        onAddSingle(
          {
            code: row.icd10_code.trim(),
            name_vi: row.icd10_name,
            name_en: "",
            category: "",
            is_billable: false,
          },
          row.type
        );
      });
    setRows([{ ...DIAG_EMPTY }]);
  };

  return (
    <div className="space-y-6">
      {canEdit && (
        <Card>
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-sm font-semibold">Thêm chẩn đoán ICD-10</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4 space-y-3">
            <Icd10Picker onSelect={onAddSingle} />
          </CardContent>
        </Card>
      )}

      {canEdit && (
        <Card>
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-sm font-semibold">Nhập nhanh nhiều chẩn đoán</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <form onSubmit={handleBulkSubmit} className="space-y-3">
              {rows.map((row, index) => (
                <div key={index} className="grid grid-cols-12 gap-2 items-end border rounded-lg p-3">
                  <div className="col-span-3 space-y-1">
                    <Label className="text-xs" htmlFor={`diag-code-${index}`}>
                      Mã ICD-10
                    </Label>
                    <Input
                      id={`diag-code-${index}`}
                      placeholder="VD: E11, I10..."
                      value={row.icd10_code}
                      onChange={(e) => updateRow(index, "icd10_code", e.target.value)}
                      onBlur={(e) => lookupName(index, e.target.value)}
                      className="min-h-[44px] text-sm"
                    />
                  </div>
                  <div className="col-span-4 space-y-1">
                    <Label className="text-xs" htmlFor={`diag-name-${index}`}>
                      Tên bệnh
                    </Label>
                    <Input
                      id={`diag-name-${index}`}
                      placeholder="Tên chẩn đoán"
                      value={row.icd10_name}
                      onChange={(e) => updateRow(index, "icd10_name", e.target.value)}
                      className="min-h-[44px] text-sm"
                    />
                  </div>
                  <div className="col-span-2 space-y-1">
                    <Label className="text-xs" htmlFor={`diag-type-${index}`}>
                      Loại
                    </Label>
                    <Select
                      items={{ PRIMARY: "Chính", SECONDARY: "Phụ" }}
                      value={row.type}
                      onValueChange={(v) => updateRow(index, "type", v as DiagnosisType)}
                    >
                      <SelectTrigger id={`diag-type-${index}`} className="min-h-[44px] text-sm">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="PRIMARY">Chính</SelectItem>
                        <SelectItem value="SECONDARY">Phụ</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="col-span-2 space-y-1">
                    <Label className="text-xs" htmlFor={`diag-note-${index}`}>
                      Mô tả thêm
                    </Label>
                    <Textarea
                      id={`diag-note-${index}`}
                      placeholder="Ghi chú..."
                      value={row.note}
                      onChange={(e) => updateRow(index, "note", e.target.value)}
                      className="min-h-[44px] text-sm resize-none"
                      rows={1}
                    />
                  </div>
                  <div className="col-span-1">
                    {rows.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => setRows((prev) => prev.filter((_, i) => i !== index))}
                        aria-label="Xoá dòng chẩn đoán"
                        className="min-h-[44px] w-full text-destructive"
                      >
                        <Trash2 className="h-4 w-4" aria-hidden="true" />
                      </Button>
                    )}
                  </div>
                </div>
              ))}
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="gap-1 min-h-[44px]"
                  onClick={() => setRows((prev) => [...prev, { ...DIAG_EMPTY }])}
                >
                  <Plus className="h-4 w-4" aria-hidden="true" />
                  Thêm dòng
                </Button>
                <Button type="submit" size="sm" className="gap-1 min-h-[44px]">
                  <Save className="h-4 w-4" aria-hidden="true" />
                  Lưu chẩn đoán
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      <div className="space-y-2">
        <h3 className="text-lg font-semibold">Danh sách chẩn đoán ({diagnoses.length})</h3>
        {diagnoses.length === 0 ? (
          <EmptyState
            variant="encounters"
            title="Chưa có chẩn đoán"
            description="Thêm mã ICD-10 để hoàn tất bệnh án."
          />
        ) : (
          <div className="space-y-2">
            {diagnoses.map((d) => (
              <div key={d.id} className="flex items-center gap-2 rounded-lg border p-2">
                <Badge
                  variant={d.type === "PRIMARY" ? "default" : "outline"}
                  className="shrink-0 font-mono"
                >
                  {d.icd10_code}
                </Badge>
                <span className="text-sm flex-1">{d.name}</span>
                <Badge
                  variant={d.type === "PRIMARY" ? "default" : "outline"}
                  className="text-xs shrink-0"
                >
                  {d.type === "PRIMARY" ? "Chính" : "Phụ"}
                </Badge>
                {d.note && (
                  <span className="text-xs text-muted-foreground hidden sm:block">{d.note}</span>
                )}
                {canEdit && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="shrink-0 text-destructive min-h-[44px]"
                    onClick={() => onDelete(d.id)}
                    aria-label={`Xoá chẩn đoán ${d.icd10_code}`}
                  >
                    <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
                  </Button>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
