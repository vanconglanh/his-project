"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { LabResultCreateRequest, LabResultUpdateRequest } from "@/lib/api/lab-results";
import type { LabResultResponse } from "@/lib/api/lab-results";

const schema = z.object({
  lab_order_item_id: z.string().uuid("ID không hợp lệ").optional(),
  value: z.string().min(1, "Giá trị không được để trống"),
  value_numeric: z.number().nullable().optional(),
  unit: z.string().nullable().optional(),
  method: z.string().nullable().optional(),
  performed_at: z.string().min(1, "Vui lòng nhập thời gian thực hiện"),
  note: z.string().nullable().optional(),
  amend_reason: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

interface LabResultFormProps {
  /** Edit mode: pass existing result */
  existing?: LabResultResponse;
  labOrderItemId?: string;
  onSubmit: (data: LabResultCreateRequest | LabResultUpdateRequest) => Promise<void>;
  onCancel?: () => void;
  isSubmitting?: boolean;
}

export function LabResultForm({ existing, labOrderItemId, onSubmit, onCancel, isSubmitting }: LabResultFormProps) {
  const isEdit = !!existing;

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      lab_order_item_id: existing?.lab_order_item_id ?? labOrderItemId ?? "",
      value: existing?.value ?? "",
      value_numeric: existing?.value_numeric ?? null,
      unit: existing?.unit ?? "",
      method: existing?.method ?? "",
      performed_at: existing?.performed_at
        ? new Date(existing.performed_at).toISOString().slice(0, 16)
        : new Date().toISOString().slice(0, 16),
      note: existing?.note ?? "",
      amend_reason: "",
    },
  });

  async function handleSubmit(values: FormValues) {
    if (isEdit) {
      await onSubmit({
        value: values.value,
        value_numeric: values.value_numeric,
        unit: values.unit,
        method: values.method,
        note: values.note,
        amend_reason: values.amend_reason,
      } satisfies LabResultUpdateRequest);
    } else {
      await onSubmit({
        lab_order_item_id: values.lab_order_item_id!,
        value: values.value,
        value_numeric: values.value_numeric,
        unit: values.unit,
        method: values.method,
        performed_at: new Date(values.performed_at).toISOString(),
        note: values.note,
      } satisfies LabResultCreateRequest);
    }
  }

  return (
    <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5 col-span-2">
          <Label htmlFor="lr-value">Giá trị kết quả *</Label>
          <Input
            id="lr-value"
            {...form.register("value")}
            placeholder="Vd: 6.2, Âm tính..."
            aria-invalid={!!form.formState.errors.value}
          />
          {form.formState.errors.value && (
            <p className="text-xs text-destructive">{form.formState.errors.value.message}</p>
          )}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="lr-value-num">Giá trị số</Label>
          <Input
            id="lr-value-num"
            type="number"
            step="any"
            placeholder="Vd: 6.2"
            {...form.register("value_numeric", {
              setValueAs: (v) => (v === "" || v === null ? null : Number(v)),
            })}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="lr-unit">Đơn vị</Label>
          <Input id="lr-unit" {...form.register("unit")} placeholder="mmol/L, mg/dL..." />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="lr-method">Phương pháp</Label>
          <Input id="lr-method" {...form.register("method")} placeholder="Enzymatic..." />
        </div>

        {!isEdit && (
          <div className="space-y-1.5">
            <Label htmlFor="lr-performed">Thời gian thực hiện *</Label>
            <Input
              id="lr-performed"
              type="datetime-local"
              {...form.register("performed_at")}
              aria-invalid={!!form.formState.errors.performed_at}
            />
            {form.formState.errors.performed_at && (
              <p className="text-xs text-destructive">{form.formState.errors.performed_at.message}</p>
            )}
          </div>
        )}

        <div className="space-y-1.5 col-span-2">
          <Label htmlFor="lr-note">Ghi chú</Label>
          <Textarea id="lr-note" {...form.register("note")} rows={2} placeholder="Ghi chú thêm..." />
        </div>

        {isEdit && existing?.status === "VERIFIED" && (
          <div className="space-y-1.5 col-span-2">
            <Label htmlFor="lr-amend">Lý do sửa (bắt buộc khi sửa kết quả đã xác thực)</Label>
            <Textarea
              id="lr-amend"
              {...form.register("amend_reason")}
              rows={2}
              placeholder="Lý do sửa đổi..."
            />
          </div>
        )}
      </div>

      <div className="flex justify-end gap-2 pt-2">
        {onCancel && (
          <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Huỷ
          </Button>
        )}
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Đang lưu..." : isEdit ? "Cập nhật" : "Nhập kết quả"}
        </Button>
      </div>
    </form>
  );
}
