"use client";

import { Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { DatasetField, ReportFilterOp } from "@/lib/api/reports";
import { getOpsForDataType, newId, OP_LABELS, type FilterDraft } from "./types";

interface FilterBuilderProps {
  fields: DatasetField[];
  filters: FilterDraft[];
  onChange: (filters: FilterDraft[]) => void;
}

export function FilterBuilder({ fields, filters, onChange }: FilterBuilderProps) {
  const fieldByKey = new Map(fields.map((f) => [f.key, f]));

  function addFilter() {
    const first = fields[0];
    if (!first) return;
    onChange([
      ...filters,
      { id: newId(), field: first.key, op: getOpsForDataType(first.data_type)[0], value: "", valueTo: "" },
    ]);
  }

  function updateFilter(id: string, patch: Partial<FilterDraft>) {
    onChange(filters.map((f) => (f.id === id ? { ...f, ...patch } : f)));
  }

  function removeFilter(id: string) {
    onChange(filters.filter((f) => f.id !== id));
  }

  return (
    <div className="space-y-2">
      {filters.length === 0 && <p className="text-sm text-muted-foreground">Chưa có điều kiện lọc.</p>}

      {filters.map((filter) => {
        const field = fieldByKey.get(filter.field);
        const dataType = field?.data_type ?? "Text";
        const ops = getOpsForDataType(dataType);

        return (
          <div key={filter.id} className="flex flex-wrap items-center gap-2 rounded-md border p-2">
            <Select
              items={Object.fromEntries(fields.map((f) => [f.key, f.label]))}
              value={filter.field}
              onValueChange={(v) => {
                if (!v) return;
                const nextField = fieldByKey.get(v);
                const nextOps = getOpsForDataType(nextField?.data_type ?? "Text");
                updateFilter(filter.id, { field: v, op: nextOps[0], value: "", valueTo: "" });
              }}
            >
              <SelectTrigger className="h-9 w-44">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {fields.map((f) => (
                  <SelectItem key={f.key} value={f.key}>
                    {f.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select
              items={Object.fromEntries(ops.map((op) => [op, OP_LABELS[op]]))}
              value={filter.op}
              onValueChange={(v) => updateFilter(filter.id, { op: (v as ReportFilterOp) ?? ops[0] })}
            >
              <SelectTrigger className="h-9 w-44">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {ops.map((op) => (
                  <SelectItem key={op} value={op}>
                    {OP_LABELS[op]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <FilterValueInput dataType={dataType} filter={filter} onChange={(patch) => updateFilter(filter.id, patch)} />

            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              onClick={() => removeFilter(filter.id)}
              aria-label="Xoá điều kiện lọc"
              className="ml-auto"
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        );
      })}

      <Button type="button" variant="outline" size="sm" onClick={addFilter} disabled={fields.length === 0} className="gap-1.5">
        <Plus className="h-4 w-4" />
        Thêm điều kiện lọc
      </Button>
    </div>
  );
}

function FilterValueInput({
  dataType,
  filter,
  onChange,
}: {
  dataType: string;
  filter: FilterDraft;
  onChange: (patch: Partial<FilterDraft>) => void;
}) {
  const inputType = dataType === "Date" ? "date" : dataType === "DateTime" ? "datetime-local" : dataType === "Number" || dataType === "Money" ? "number" : "text";

  if (filter.op === "between") {
    return (
      <div className="flex items-center gap-1.5">
        <Input
          type={inputType}
          value={filter.value}
          onChange={(e) => onChange({ value: e.target.value })}
          placeholder="Từ"
          className="h-9 w-32"
          aria-label="Giá trị từ"
        />
        <span className="text-sm text-muted-foreground">–</span>
        <Input
          type={inputType}
          value={filter.valueTo}
          onChange={(e) => onChange({ valueTo: e.target.value })}
          placeholder="Đến"
          className="h-9 w-32"
          aria-label="Giá trị đến"
        />
      </div>
    );
  }

  if (filter.op === "in") {
    return (
      <Input
        value={filter.value}
        onChange={(e) => onChange({ value: e.target.value })}
        placeholder="Giá trị 1, giá trị 2, ..."
        className="h-9 w-56"
        aria-label="Danh sách giá trị, phân tách bởi dấu phẩy"
      />
    );
  }

  return (
    <Input
      type={inputType}
      value={filter.value}
      onChange={(e) => onChange({ value: e.target.value })}
      placeholder="Giá trị"
      className="h-9 w-40"
      aria-label="Giá trị"
    />
  );
}
