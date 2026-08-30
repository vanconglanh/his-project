"use client";

import { Fragment, useMemo } from "react";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";
import type { EmrFormField, EmrFormSchema, EmrFormValues } from "@/lib/api/types";

interface DynamicFormRendererProps {
  schema: EmrFormSchema;
  values: EmrFormValues;
  onChange: (key: string, value: unknown) => void;
  readOnly?: boolean;
  className?: string;
}

interface NormalizedOption {
  value: string;
  label: string;
}

const UNGROUPED = "__ungrouped__";

function normalizeOptions(field: EmrFormField): NormalizedOption[] {
  if (!field.options) return [];
  return field.options.map((opt) =>
    typeof opt === "string" ? { value: opt, label: opt } : opt
  );
}

/**
 * Render form động theo schema (EmrFormSchema) — dùng chung cho:
 * - Màn khám bệnh: bác sĩ nhập structured_values theo template.structured_json
 * - Hiển thị lại bệnh án đã lưu/ký: readOnly, schema = schema_snapshot của bản ghi
 * - Preview trong màn admin quản lý mẫu bệnh án
 *
 * Thứ tự hiển thị = thứ tự phần tử trong mảng schema. Field được nhóm theo
 * field.group (field không có group xếp vào 1 khối không tiêu đề, hiển thị trước).
 */
export function DynamicFormRenderer({
  schema,
  values,
  onChange,
  readOnly = false,
  className,
}: DynamicFormRendererProps) {
  const groups = useMemo(() => {
    const order: string[] = [];
    const map = new Map<string, EmrFormField[]>();
    for (const field of schema) {
      const key = field.group?.trim() || UNGROUPED;
      if (!map.has(key)) {
        map.set(key, []);
        order.push(key);
      }
      map.get(key)!.push(field);
    }
    return order.map((key) => ({ key, title: key === UNGROUPED ? null : key, fields: map.get(key)! }));
  }, [schema]);

  if (!schema || schema.length === 0) return null;

  return (
    <div className={cn("space-y-6", className)}>
      {groups.map((group) => (
        <div key={group.key} className="space-y-3">
          {group.title && (
            <h4 className="text-sm font-semibold text-foreground">{group.title}</h4>
          )}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {group.fields.map((field) => (
              <FieldRenderer
                key={field.key}
                field={field}
                value={values?.[field.key]}
                onChange={(value) => onChange(field.key, value)}
                readOnly={readOnly}
              />
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

function FieldRenderer({
  field,
  value,
  onChange,
  readOnly,
}: {
  field: EmrFormField;
  value: unknown;
  onChange: (value: unknown) => void;
  readOnly: boolean;
}) {
  const colSpan = field.colSpan ?? (field.type === "textarea" || field.type === "checklist" ? 2 : 1);
  const inputId = `emr-field-${field.key}`;

  return (
    <div className={cn("space-y-1.5", colSpan === 2 && "sm:col-span-2")}>
      {field.type !== "checkbox" && (
        <Label htmlFor={inputId}>
          {field.label}
          {field.required && <span className="ml-0.5 text-destructive">*</span>}
        </Label>
      )}

      {field.type === "text" && (
        <Input
          id={inputId}
          value={typeof value === "string" ? value : ""}
          onChange={(e) => onChange(e.target.value)}
          readOnly={readOnly}
          disabled={readOnly}
          aria-required={field.required}
        />
      )}

      {field.type === "number" && (
        <div className="flex items-center gap-2">
          <Input
            id={inputId}
            type="number"
            value={typeof value === "number" || typeof value === "string" ? value : ""}
            onChange={(e) => onChange(e.target.value === "" ? undefined : Number(e.target.value))}
            readOnly={readOnly}
            disabled={readOnly}
            aria-required={field.required}
            className="flex-1"
          />
          {field.unit && <span className="shrink-0 text-sm text-muted-foreground">{field.unit}</span>}
        </div>
      )}

      {field.type === "textarea" && (
        <Textarea
          id={inputId}
          value={typeof value === "string" ? value : ""}
          onChange={(e) => onChange(e.target.value)}
          readOnly={readOnly}
          disabled={readOnly}
          aria-required={field.required}
          className="min-h-[88px]"
        />
      )}

      {field.type === "date" && (
        <Input
          id={inputId}
          type="date"
          value={typeof value === "string" ? value : ""}
          onChange={(e) => onChange(e.target.value)}
          readOnly={readOnly}
          disabled={readOnly}
          aria-required={field.required}
        />
      )}

      {field.type === "select" && (
        <Select
          value={typeof value === "string" ? value : ""}
          onValueChange={(v) => onChange(v)}
          disabled={readOnly}
        >
          <SelectTrigger id={inputId} aria-required={field.required}>
            <SelectValue placeholder="Chọn..." />
          </SelectTrigger>
          <SelectContent>
            {normalizeOptions(field).map((opt) => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}

      {field.type === "checkbox" && (
        <div className="flex min-h-9 items-center gap-2">
          <Checkbox
            id={inputId}
            checked={value === true}
            onCheckedChange={(checked) => onChange(checked === true)}
            disabled={readOnly}
          />
          <Label htmlFor={inputId} className="cursor-pointer font-normal">
            {field.label}
            {field.required && <span className="ml-0.5 text-destructive">*</span>}
          </Label>
        </div>
      )}

      {field.type === "radio" && (
        <RadioGroup
          value={typeof value === "string" ? value : ""}
          onValueChange={(v) => onChange(v)}
          className="flex flex-wrap gap-4"
          disabled={readOnly}
        >
          {normalizeOptions(field).map((opt) => (
            <Fragment key={opt.value}>
              <div className="flex items-center gap-2">
                <RadioGroupItem
                  value={opt.value}
                  id={`${inputId}-${opt.value}`}
                  disabled={readOnly}
                />
                <Label htmlFor={`${inputId}-${opt.value}`} className="cursor-pointer font-normal">
                  {opt.label}
                </Label>
              </div>
            </Fragment>
          ))}
        </RadioGroup>
      )}

      {field.type === "checklist" && (
        <div className="flex flex-wrap gap-4">
          {normalizeOptions(field).map((opt) => {
            const arr = Array.isArray(value) ? (value as string[]) : [];
            const checked = arr.includes(opt.value);
            return (
              <div key={opt.value} className="flex items-center gap-2">
                <Checkbox
                  id={`${inputId}-${opt.value}`}
                  checked={checked}
                  disabled={readOnly}
                  onCheckedChange={(v) => {
                    const next = v === true
                      ? [...arr, opt.value]
                      : arr.filter((x) => x !== opt.value);
                    onChange(next);
                  }}
                />
                <Label htmlFor={`${inputId}-${opt.value}`} className="cursor-pointer font-normal">
                  {opt.label}
                </Label>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
