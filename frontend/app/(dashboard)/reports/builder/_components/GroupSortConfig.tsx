"use client";

import { ArrowDown, ArrowUp, Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { newId, type ColumnDraft, type SortDraft } from "./types";

interface GroupSortConfigProps {
  columns: ColumnDraft[];
  groupBy: string[];
  sort: SortDraft[];
  onGroupByChange: (groupBy: string[]) => void;
  onSortChange: (sort: SortDraft[]) => void;
}

export function GroupSortConfig({ columns, groupBy, sort, onGroupByChange, onSortChange }: GroupSortConfigProps) {
  const dimensionColumns = columns.filter((c) => c.role === "DIMENSION");

  function toggleGroupBy(field: string, checked: boolean) {
    onGroupByChange(checked ? [...groupBy, field] : groupBy.filter((f) => f !== field));
  }

  function addSort() {
    const first = columns[0];
    if (!first) return;
    onSortChange([...sort, { id: newId(), field: first.field, desc: false }]);
  }

  function updateSort(id: string, patch: Partial<SortDraft>) {
    onSortChange(sort.map((s) => (s.id === id ? { ...s, ...patch } : s)));
  }

  function removeSort(id: string) {
    onSortChange(sort.filter((s) => s.id !== id));
  }

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
      <div>
        <p className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">Nhóm theo (Group by)</p>
        {dimensionColumns.length === 0 ? (
          <p className="text-sm text-muted-foreground">Chọn cột phân loại ở bước trên để nhóm dữ liệu.</p>
        ) : (
          <ul className="space-y-1.5">
            {dimensionColumns.map((col) => (
              <li key={col.field}>
                <label className="flex min-h-9 items-center gap-2 text-sm">
                  <Checkbox
                    checked={groupBy.includes(col.field)}
                    onCheckedChange={(v) => toggleGroupBy(col.field, v === true)}
                  />
                  {col.label}
                </label>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div>
        <p className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">Sắp xếp (Sort)</p>
        <div className="space-y-1.5">
          {sort.length === 0 && <p className="text-sm text-muted-foreground">Chưa có thứ tự sắp xếp.</p>}
          {sort.map((s) => (
            <div key={s.id} className="flex items-center gap-1.5">
              <Select
                items={Object.fromEntries(columns.map((c) => [c.field, c.label]))}
                value={s.field}
                onValueChange={(v) => v && updateSort(s.id, { field: v })}
              >
                <SelectTrigger className="h-9 flex-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {columns.map((c) => (
                    <SelectItem key={c.field} value={c.field}>
                      {c.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Button
                type="button"
                variant="outline"
                size="icon-sm"
                onClick={() => updateSort(s.id, { desc: !s.desc })}
                aria-label={s.desc ? "Giảm dần" : "Tăng dần"}
              >
                {s.desc ? <ArrowDown className="h-4 w-4" /> : <ArrowUp className="h-4 w-4" />}
              </Button>
              <Button type="button" variant="ghost" size="icon-sm" onClick={() => removeSort(s.id)} aria-label="Xoá sắp xếp">
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}
          <Button type="button" variant="outline" size="sm" onClick={addSort} disabled={columns.length === 0} className="gap-1.5">
            <Plus className="h-4 w-4" />
            Thêm sắp xếp
          </Button>
        </div>
      </div>
    </div>
  );
}
