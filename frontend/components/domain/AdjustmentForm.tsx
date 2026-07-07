"use client";

import { useEffect } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useCreateAdjustment, useWarehouses } from "@/lib/hooks/use-pharmacy-warehouse";
import { Plus, Trash2 } from "lucide-react";

const REASONS = [
  { value: "STOCKTAKE", label: "Kiểm kê" },
  { value: "DAMAGED", label: "Hư hỏng" },
  { value: "EXPIRED", label: "Hết hạn" },
  { value: "LOST", label: "Thất thoát" },
  { value: "OTHER", label: "Khác" },
] as const;

const schema = z.object({
  warehouse_id: z.string().min(1, "Chọn kho"),
  reason: z.enum(["STOCKTAKE", "DAMAGED", "EXPIRED", "LOST", "OTHER"]),
  note: z.string().optional(),
  items: z
    .array(
      z.object({
        drug_id: z.string().min(1, "Bắt buộc"),
        batch_no: z.string().min(1, "Bắt buộc"),
        quantity_diff: z.coerce.number(),
      })
    )
    .min(1, "Cần ít nhất 1 dòng"),
});

type FormData = z.infer<typeof schema>;

interface Props {
  onSuccess?: () => void;
  /** id gắn lên thẻ <form> — trang cha (Fullpage shell) dùng để trigger submit từ ngoài */
  formId?: string;
  /** Báo trạng thái đang submit (mutation pending) lên trang cha để hiện ở StickyActionBar */
  onSubmittingChange?: (submitting: boolean) => void;
}

export function AdjustmentForm({ onSuccess, formId = "adjustment-form", onSubmittingChange }: Props) {
  const { data: warehouses } = useWarehouses();
  const createAdjustment = useCreateAdjustment();

  const {
    register,
    handleSubmit,
    control,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: {
      reason: "STOCKTAKE",
      items: [{ drug_id: "", batch_no: "", quantity_diff: 0 }],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: "items" });

  useEffect(() => {
    onSubmittingChange?.(createAdjustment.isPending);
  }, [createAdjustment.isPending, onSubmittingChange]);

  async function onSubmit(data: FormData) {
    await createAdjustment.mutateAsync(data as any);
    onSuccess?.();
  }

  return (
    <form id={formId} onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label>Kho</Label>
          <Select
            items={Object.fromEntries((warehouses ?? []).map((w) => [w.id, w.name]))}
            onValueChange={(v) => setValue("warehouse_id", String(v ?? ""))}
          >
            <SelectTrigger>
              <SelectValue placeholder="-- Chọn kho --" />
            </SelectTrigger>
            <SelectContent>
              {warehouses?.map((w) => (
                <SelectItem key={w.id} value={w.id}>{w.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.warehouse_id && <p className="text-xs text-destructive">{errors.warehouse_id.message}</p>}
        </div>

        <div className="space-y-1">
          <Label>Lý do điều chỉnh</Label>
          <Select
            items={Object.fromEntries(REASONS.map((r) => [r.value, r.label]))}
            defaultValue="STOCKTAKE"
            onValueChange={(v) => setValue("reason", (v ?? "STOCKTAKE") as FormData["reason"])}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {REASONS.map((r) => (
                <SelectItem key={r.value} value={r.value}>{r.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="space-y-1">
        <Label htmlFor="note">Ghi chú</Label>
        <Input id="note" {...register("note")} placeholder="Ghi chú thêm..." />
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label>Danh sách thuốc điều chỉnh</Label>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => append({ drug_id: "", batch_no: "", quantity_diff: 0 })}
          >
            <Plus className="h-4 w-4 mr-1" />
            Thêm dòng
          </Button>
        </div>

        {fields.map((field, idx) => (
          <div key={field.id} className="grid grid-cols-[1fr_1fr_1fr_auto] gap-2 items-end">
            <div className="space-y-1">
              {idx === 0 && <Label className="text-xs">Mã thuốc (UUID)</Label>}
              <Input
                placeholder="drug_id..."
                {...register(`items.${idx}.drug_id`)}
              />
            </div>
            <div className="space-y-1">
              {idx === 0 && <Label className="text-xs">Số lô</Label>}
              <Input
                placeholder="Số lô"
                {...register(`items.${idx}.batch_no`)}
              />
            </div>
            <div className="space-y-1">
              {idx === 0 && <Label className="text-xs">Số lượng (+/-)</Label>}
              <Input
                type="number"
                step="0.5"
                {...register(`items.${idx}.quantity_diff`)}
              />
            </div>
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="h-10 w-10 text-destructive"
              onClick={() => remove(idx)}
              disabled={fields.length === 1}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        ))}
      </div>
    </form>
  );
}
