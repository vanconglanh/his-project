"use client";

import { useState } from "react";
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  closestCenter,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  arrayMove,
  useSortable,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { GripVertical, Plus, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { DatasetField, ReportAggregation } from "@/lib/api/reports";
import { type ColumnDraft } from "./types";

const AGG_LABELS: Record<ReportAggregation, string> = {
  SUM: "Tổng (SUM)",
  COUNT: "Đếm (COUNT)",
  AVG: "Trung bình (AVG)",
  MIN: "Nhỏ nhất (MIN)",
  MAX: "Lớn nhất (MAX)",
  COUNT_DISTINCT: "Đếm khác nhau",
};

interface FieldTrayProps {
  fields: DatasetField[];
  columns: ColumnDraft[];
  onColumnsChange: (columns: ColumnDraft[]) => void;
}

export function FieldTray({ fields, columns, onColumnsChange }: FieldTrayProps) {
  const [draggingLabel, setDraggingLabel] = useState<string | null>(null);
  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 4 } }));

  const dimensionFields = fields.filter((f) => f.role === "DIMENSION");
  const measureFields = fields.filter((f) => f.role === "MEASURE");
  const selectedKeys = new Set(columns.map((c) => c.field));

  function addField(field: DatasetField) {
    if (selectedKeys.has(field.key)) return;
    const draft: ColumnDraft = {
      field: field.key,
      label: field.label,
      role: field.role,
      dataType: field.data_type,
      availableAggs: field.aggregations,
      agg: field.role === "MEASURE" ? (field.aggregations[0] ?? null) : null,
      isSubtotal: false,
    };
    onColumnsChange([...columns, draft]);
  }

  function removeColumn(fieldKey: string) {
    onColumnsChange(columns.filter((c) => c.field !== fieldKey));
  }

  function updateColumn(fieldKey: string, patch: Partial<ColumnDraft>) {
    onColumnsChange(columns.map((c) => (c.field === fieldKey ? { ...c, ...patch } : c)));
  }

  function handleDragStart(event: DragStartEvent) {
    const data = event.active.data.current as { type: string; field?: DatasetField } | undefined;
    if (data?.type === "field" && data.field) setDraggingLabel(data.field.label);
    else if (data?.type === "column") {
      const col = columns.find((c) => `col:${c.field}` === event.active.id);
      setDraggingLabel(col?.label ?? null);
    }
  }

  function handleDragEnd(event: DragEndEvent) {
    setDraggingLabel(null);
    const { active, over } = event;
    if (!over) return;
    const activeData = active.data.current as { type: string; field?: DatasetField } | undefined;

    if (activeData?.type === "field" && activeData.field) {
      addField(activeData.field);
      return;
    }

    if (activeData?.type === "column" && String(active.id).startsWith("col:") && String(over.id).startsWith("col:")) {
      const oldIndex = columns.findIndex((c) => `col:${c.field}` === active.id);
      const newIndex = columns.findIndex((c) => `col:${c.field}` === over.id);
      if (oldIndex >= 0 && newIndex >= 0 && oldIndex !== newIndex) {
        onColumnsChange(arrayMove(columns, oldIndex, newIndex));
      }
    }
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
    >
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <div className="space-y-3">
          <FieldGroup title="Trường phân loại (Dimension)" fields={dimensionFields} selectedKeys={selectedKeys} onAdd={addField} />
          <FieldGroup title="Trường số liệu (Measure)" fields={measureFields} selectedKeys={selectedKeys} onAdd={addField} />
        </div>

        <ColumnDropzone columns={columns}>
          {columns.length === 0 ? (
            <p className="px-2 py-6 text-center text-sm text-muted-foreground">
              Kéo thả hoặc bấm &quot;+&quot; ở trường bên trái để thêm cột.
            </p>
          ) : (
            <SortableContext items={columns.map((c) => `col:${c.field}`)} strategy={verticalListSortingStrategy}>
              <ul className="space-y-1.5">
                {columns.map((col) => (
                  <SelectedColumnRow
                    key={col.field}
                    column={col}
                    onRemove={() => removeColumn(col.field)}
                    onChangeAgg={(agg) => updateColumn(col.field, { agg })}
                    onChangeSubtotal={(isSubtotal) => updateColumn(col.field, { isSubtotal })}
                  />
                ))}
              </ul>
            </SortableContext>
          )}
        </ColumnDropzone>
      </div>

      <DragOverlay>{draggingLabel && <div className="rounded-md border bg-card px-3 py-1.5 text-sm shadow-md">{draggingLabel}</div>}</DragOverlay>
    </DndContext>
  );
}

interface FieldGroupProps {
  title: string;
  fields: DatasetField[];
  selectedKeys: Set<string>;
  onAdd: (field: DatasetField) => void;
}

function FieldGroup({ title, fields, selectedKeys, onAdd }: FieldGroupProps) {
  return (
    <div className="rounded-lg border p-3">
      <p className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">{title}</p>
      {fields.length === 0 ? (
        <p className="text-sm text-muted-foreground">Không có trường.</p>
      ) : (
        <ul className="flex flex-wrap gap-1.5">
          {fields.map((field) => (
            <FieldChip key={field.key} field={field} disabled={selectedKeys.has(field.key)} onAdd={() => onAdd(field)} />
          ))}
        </ul>
      )}
    </div>
  );
}

function FieldChip({ field, disabled, onAdd }: { field: DatasetField; disabled: boolean; onAdd: () => void }) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: `field:${field.key}`,
    data: { type: "field", field },
    disabled,
  });

  return (
    <li
      ref={setNodeRef}
      {...attributes}
      {...listeners}
      className={cn(
        "flex min-h-9 cursor-grab items-center gap-1 rounded-full border bg-background px-2.5 py-1 text-sm active:cursor-grabbing",
        disabled && "cursor-default opacity-50",
        isDragging && "opacity-30"
      )}
    >
      <span className="truncate">{field.label}</span>
      {!disabled && (
        <button
          type="button"
          onClick={onAdd}
          className="ml-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full hover:bg-muted"
          aria-label={`Thêm cột ${field.label}`}
        >
          <Plus className="h-3.5 w-3.5" />
        </button>
      )}
    </li>
  );
}

function ColumnDropzone({ columns, children }: { columns: ColumnDraft[]; children: React.ReactNode }) {
  const { setNodeRef, isOver } = useDroppable({ id: "columns-dropzone" });
  return (
    <div
      ref={setNodeRef}
      className={cn("rounded-lg border p-3 transition-colors", isOver && "border-primary bg-primary/5")}
    >
      <p className="mb-2 text-xs font-medium uppercase tracking-wide text-muted-foreground">
        Cột đã chọn ({columns.length})
      </p>
      {children}
    </div>
  );
}

interface SelectedColumnRowProps {
  column: ColumnDraft;
  onRemove: () => void;
  onChangeAgg: (agg: ReportAggregation | null) => void;
  onChangeSubtotal: (value: boolean) => void;
}

function SelectedColumnRow({ column, onRemove, onChangeAgg, onChangeSubtotal }: SelectedColumnRowProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: `col:${column.field}`,
    data: { type: "column" },
  });

  const style = { transform: CSS.Transform.toString(transform), transition };
  const isMeasure = column.role === "MEASURE";

  return (
    <li
      ref={setNodeRef}
      style={style}
      className={cn(
        "flex min-h-11 flex-wrap items-center gap-2 rounded-md border bg-background px-2 py-1.5",
        isDragging && "opacity-50"
      )}
    >
      <button
        type="button"
        {...attributes}
        {...listeners}
        className="flex h-7 w-7 shrink-0 items-center justify-center rounded hover:bg-muted cursor-grab active:cursor-grabbing"
        aria-label={`Kéo sắp xếp cột ${column.label}`}
      >
        <GripVertical className="h-4 w-4 text-muted-foreground" />
      </button>

      <span className="min-w-0 flex-1 truncate text-sm font-medium">{column.label}</span>
      <Badge variant={isMeasure ? "default" : "outline"} className="shrink-0">
        {isMeasure ? "Số liệu" : "Phân loại"}
      </Badge>

      {isMeasure && column.availableAggs.length > 0 && (
        <Select
          items={Object.fromEntries(column.availableAggs.map((a) => [a, AGG_LABELS[a]]))}
          value={column.agg ?? column.availableAggs[0]}
          onValueChange={(v) => onChangeAgg((v as ReportAggregation) ?? null)}
        >
          <SelectTrigger className="h-8 w-36 shrink-0">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {column.availableAggs.map((a) => (
              <SelectItem key={a} value={a}>
                {AGG_LABELS[a]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}

      {isMeasure && (
        <label className="flex shrink-0 items-center gap-1.5 text-xs text-muted-foreground">
          <Checkbox checked={column.isSubtotal} onCheckedChange={(v) => onChangeSubtotal(v === true)} />
          Tổng phụ
        </label>
      )}

      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        onClick={onRemove}
        aria-label={`Xoá cột ${column.label}`}
        className="shrink-0"
      >
        <X className="h-4 w-4" />
      </Button>
    </li>
  );
}
