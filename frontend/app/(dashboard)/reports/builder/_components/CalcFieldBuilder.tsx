"use client";

import { Calculator, Plus, X } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { DatasetField, DatasetFieldDataType } from "@/lib/api/reports";
import { newId, type CalcFieldDraft, type ColumnDraft } from "./types";

const DATA_TYPE_LABELS: Record<DatasetFieldDataType, string> = {
  Money: "Tiền tệ",
  Number: "Số",
  Date: "Ngày",
  DateTime: "Ngày giờ",
  Text: "Văn bản",
  Enum: "Danh mục",
};

interface CalcFieldBuilderProps {
  /** Trường measure của dataset — gợi ý dùng trong công thức. */
  measureFields: DatasetField[];
  calcFields: CalcFieldDraft[];
  columns: ColumnDraft[];
  onCalcFieldsChange: (calcFields: CalcFieldDraft[]) => void;
  onColumnsChange: (columns: ColumnDraft[]) => void;
}

/**
 * Khu "Cột tính toán" — cho phép định nghĩa cột mới từ công thức trên các trường measure
 * (formula chỉ dùng field measure + số + `+ - * / ( )`, khớp BE). Có thể tick để thêm/gỡ
 * calc field khỏi "Cột đã chọn" (giống như 1 cột số liệu, luôn agg=null).
 */
export function CalcFieldBuilder({
  measureFields,
  calcFields,
  columns,
  onCalcFieldsChange,
  onColumnsChange,
}: CalcFieldBuilderProps) {
  function addCalcField() {
    onCalcFieldsChange([...calcFields, { id: newId(), key: "", label: "", formula: "", dataType: "Number" }]);
  }

  function updateCalcField(id: string, patch: Partial<CalcFieldDraft>) {
    const prev = calcFields.find((c) => c.id === id);
    onCalcFieldsChange(calcFields.map((c) => (c.id === id ? { ...c, ...patch } : c)));

    // Đồng bộ cột đã chọn tương ứng (nếu đã bật) khi đổi key/label/dataType
    if (prev && columns.some((col) => col.isCalc && col.field === prev.key)) {
      const nextKey = patch.key ?? prev.key;
      const nextLabel = patch.label ?? prev.label;
      const nextDataType = patch.dataType ?? prev.dataType;
      onColumnsChange(
        columns.map((col) =>
          col.isCalc && col.field === prev.key
            ? { ...col, field: nextKey, label: nextLabel || nextKey, dataType: nextDataType }
            : col
        )
      );
    }
  }

  function removeCalcField(id: string) {
    const target = calcFields.find((c) => c.id === id);
    onCalcFieldsChange(calcFields.filter((c) => c.id !== id));
    if (target) {
      onColumnsChange(columns.filter((col) => !(col.isCalc && col.field === target.key)));
    }
  }

  function toggleInColumns(calc: CalcFieldDraft, checked: boolean) {
    if (!calc.key.trim()) return;
    if (checked) {
      if (columns.some((c) => c.field === calc.key)) return;
      const draft: ColumnDraft = {
        field: calc.key,
        label: calc.label || calc.key,
        role: "MEASURE",
        dataType: calc.dataType,
        availableAggs: [],
        agg: null,
        isSubtotal: false,
        isCalc: true,
      };
      onColumnsChange([...columns, draft]);
    } else {
      onColumnsChange(columns.filter((c) => c.field !== calc.key));
    }
  }

  const measureHints = measureFields.map((f) => f.key).join(", ");

  return (
    <div className="space-y-3">
      {calcFields.length === 0 && (
        <p className="text-sm text-muted-foreground">
          Chưa có cột tính toán nào. Tạo công thức từ các trường số liệu (measure) của nguồn dữ liệu.
        </p>
      )}

      {measureFields.length === 0 ? (
        <p className="text-sm text-muted-foreground">Nguồn dữ liệu này chưa có trường số liệu để tính toán.</p>
      ) : (
        <>
          <ul className="space-y-2">
            {calcFields.map((calc) => {
              const isInColumns = columns.some((c) => c.isCalc && c.field === calc.key);
              return (
                <li key={calc.id} className="rounded-lg border p-3 space-y-2">
                  <div className="flex flex-wrap items-center gap-2">
                    <Calculator className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                    <Input
                      value={calc.key}
                      onChange={(e) => updateCalcField(calc.id, { key: e.target.value.trim() })}
                      placeholder="Mã cột (vd: ty_le_lai)"
                      className="h-9 w-40"
                      aria-label="Mã cột tính toán"
                    />
                    <Input
                      value={calc.label}
                      onChange={(e) => updateCalcField(calc.id, { label: e.target.value })}
                      placeholder="Tên hiển thị"
                      className="h-9 w-48"
                      aria-label="Tên hiển thị cột tính toán"
                    />
                    <Select
                      items={DATA_TYPE_LABELS}
                      value={calc.dataType}
                      onValueChange={(v) => v && updateCalcField(calc.id, { dataType: v as DatasetFieldDataType })}
                    >
                      <SelectTrigger className="h-9 w-32">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {(Object.keys(DATA_TYPE_LABELS) as DatasetFieldDataType[]).map((t) => (
                          <SelectItem key={t} value={t}>
                            {DATA_TYPE_LABELS[t]}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => removeCalcField(calc.id)}
                      aria-label={`Xoá cột tính toán ${calc.label || calc.key}`}
                      className="ml-auto shrink-0"
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  </div>

                  <Input
                    value={calc.formula}
                    onChange={(e) => updateCalcField(calc.id, { formula: e.target.value })}
                    placeholder="Công thức, vd: doanh_thu - chi_phi"
                    className="h-9 font-mono text-sm"
                    aria-label="Công thức"
                  />
                  <p className="text-xs text-muted-foreground">
                    Chỉ dùng trường số liệu + số + <code>+ - * / ( )</code>. Trường khả dụng:{" "}
                    <span className="font-mono">{measureHints || "—"}</span>
                  </p>

                  <label className="flex min-h-9 w-fit items-center gap-2 text-sm">
                    <Checkbox
                      checked={isInColumns}
                      onCheckedChange={(v) => toggleInColumns(calc, v === true)}
                      disabled={!calc.key.trim() || !calc.formula.trim()}
                    />
                    Thêm vào Cột đã chọn
                    {isInColumns && (
                      <Badge variant="outline" className="ml-1">
                        Đã thêm
                      </Badge>
                    )}
                  </label>
                </li>
              );
            })}
          </ul>

          <Button type="button" variant="outline" size="sm" onClick={addCalcField} className="gap-1.5">
            <Plus className="h-4 w-4" />
            Thêm cột tính toán
          </Button>
        </>
      )}
    </div>
  );
}
