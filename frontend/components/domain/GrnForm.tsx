"use client";

import { useEffect } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useCreateGrn } from "@/lib/hooks/use-pharmacy-warehouse";
import { Plus, Trash2 } from "lucide-react";

const schema = z.object({
  received_at: z.string().min(1),
  note: z.string().optional(),
  items: z.array(
    z.object({
      drug_id: z.string().min(1, "Bắt buộc"),
      batch_no: z.string().min(1, "Bắt buộc"),
      manufacture_date: z.string().optional(),
      expiry_date: z.string().min(1, "Bắt buộc"),
      quantity_received: z.coerce.number().min(1),
      unit_cost: z.coerce.number().min(0),
    })
  ).min(1),
});

type FormData = z.infer<typeof schema>;

interface Props {
  poId: string;
  onSuccess?: () => void;
  /** id gắn lên thẻ <form> — trang cha (Fullpage shell) dùng để trigger submit từ ngoài */
  formId?: string;
  /** Báo trạng thái đang submit (mutation pending) lên trang cha để hiện ở StickyActionBar */
  onSubmittingChange?: (submitting: boolean) => void;
}

export function GrnForm({ poId, onSuccess, formId = "grn-form", onSubmittingChange }: Props) {
  const createGrn = useCreateGrn(poId);

  const { register, handleSubmit, control, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: {
      received_at: new Date().toISOString().slice(0, 16),
      items: [{ drug_id: "", batch_no: "", expiry_date: "", quantity_received: 1, unit_cost: 0 }],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: "items" });

  useEffect(() => {
    onSubmittingChange?.(createGrn.isPending);
  }, [createGrn.isPending, onSubmittingChange]);

  async function onSubmit(data: FormData) {
    await createGrn.mutateAsync({ ...data, received_at: new Date(data.received_at).toISOString() });
    onSuccess?.();
  }

  return (
    <form id={formId} onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label>Thời gian nhận hàng</Label>
          <Input type="datetime-local" {...register("received_at")} />
          {errors.received_at && <p className="text-xs text-destructive">{errors.received_at.message}</p>}
        </div>
        <div className="space-y-1">
          <Label>Ghi chú</Label>
          <Input {...register("note")} placeholder="Ghi chú..." />
        </div>
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label>Chi tiết nhập kho (theo lô)</Label>
          <Button type="button" variant="outline" size="sm" onClick={() => append({ drug_id: "", batch_no: "", expiry_date: "", quantity_received: 1, unit_cost: 0 })}>
            <Plus className="h-4 w-4 mr-1" />Thêm lô
          </Button>
        </div>

        {fields.map((field, idx) => (
          <div key={field.id} className="border rounded-md p-3 space-y-3 relative">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label className="text-xs">Mã thuốc (UUID)</Label>
                <Input placeholder="drug_id..." {...register(`items.${idx}.drug_id`)} />
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Số lô</Label>
                <Input placeholder="Số lô" {...register(`items.${idx}.batch_no`)} />
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Ngày sản xuất</Label>
                <Input type="date" {...register(`items.${idx}.manufacture_date`)} />
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Hạn sử dụng</Label>
                <Input type="date" {...register(`items.${idx}.expiry_date`)} />
                {errors.items?.[idx]?.expiry_date && <p className="text-xs text-destructive">{errors.items[idx]?.expiry_date?.message}</p>}
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Số lượng nhận</Label>
                <Input type="number" min={1} {...register(`items.${idx}.quantity_received`)} />
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Đơn giá nhập</Label>
                <Input type="number" min={0} {...register(`items.${idx}.unit_cost`)} />
              </div>
            </div>
            {fields.length > 1 && (
              <Button type="button" variant="ghost" size="icon" className="absolute top-2 right-2 h-7 w-7 text-destructive" onClick={() => remove(idx)}>
                <Trash2 className="h-3.5 w-3.5" />
              </Button>
            )}
          </div>
        ))}
      </div>
    </form>
  );
}
