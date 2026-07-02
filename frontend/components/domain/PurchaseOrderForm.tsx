"use client";

import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useCreatePurchaseOrder, useWarehouses } from "@/lib/hooks/use-pharmacy-warehouse";
import { useSuppliers } from "@/lib/hooks/use-suppliers";
import { Plus, Trash2 } from "lucide-react";

const schema = z.object({
  supplier_id: z.string().min(1, "Chọn nhà cung cấp"),
  warehouse_id: z.string().min(1, "Chọn kho"),
  expected_delivery: z.string().optional(),
  note: z.string().optional(),
  items: z
    .array(
      z.object({
        drug_id: z.string().min(1, "Bắt buộc"),
        quantity_ordered: z.coerce.number().min(1),
        unit_price: z.coerce.number().min(0),
      })
    )
    .min(1),
});

type FormData = z.infer<typeof schema>;

interface Props {
  onSuccess?: () => void;
}

export function PurchaseOrderForm({ onSuccess }: Props) {
  const { data: warehouses } = useWarehouses();
  const { data: suppliersData } = useSuppliers();
  const createPo = useCreatePurchaseOrder();

  const { register, handleSubmit, setValue, control, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: { items: [{ drug_id: "", quantity_ordered: 1, unit_price: 0 }] },
  });

  const { fields, append, remove } = useFieldArray({ control, name: "items" });

  async function onSubmit(data: FormData) {
    await createPo.mutateAsync(data);
    onSuccess?.();
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label>Nhà cung cấp</Label>
          <Select onValueChange={(v) => setValue("supplier_id", String(v ?? ""))}>
            <SelectTrigger><SelectValue placeholder="-- Chọn NCC --" /></SelectTrigger>
            <SelectContent>
              {suppliersData?.data.map((s) => (
                <SelectItem key={s.id} value={s.id}>{s.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.supplier_id && <p className="text-xs text-destructive">{errors.supplier_id.message}</p>}
        </div>
        <div className="space-y-1">
          <Label>Kho nhập</Label>
          <Select onValueChange={(v) => setValue("warehouse_id", String(v ?? ""))}>
            <SelectTrigger><SelectValue placeholder="-- Chọn kho --" /></SelectTrigger>
            <SelectContent>
              {warehouses?.map((w) => (
                <SelectItem key={w.id} value={w.id}>{w.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.warehouse_id && <p className="text-xs text-destructive">{errors.warehouse_id.message}</p>}
        </div>
        <div className="space-y-1">
          <Label>Ngày giao dự kiến</Label>
          <Input type="date" {...register("expected_delivery")} />
        </div>
        <div className="space-y-1">
          <Label>Ghi chú</Label>
          <Input {...register("note")} placeholder="Ghi chú..." />
        </div>
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label>Danh sách thuốc đặt hàng</Label>
          <Button type="button" variant="outline" size="sm" onClick={() => append({ drug_id: "", quantity_ordered: 1, unit_price: 0 })}>
            <Plus className="h-4 w-4 mr-1" />Thêm dòng
          </Button>
        </div>
        {fields.map((field, idx) => (
          <div key={field.id} className="grid grid-cols-[2fr_1fr_1fr_auto] gap-2 items-end">
            <div className="space-y-1">
              {idx === 0 && <Label className="text-xs">Mã thuốc (UUID)</Label>}
              <Input placeholder="drug_id..." {...register(`items.${idx}.drug_id`)} />
            </div>
            <div className="space-y-1">
              {idx === 0 && <Label className="text-xs">Số lượng</Label>}
              <Input type="number" min={1} {...register(`items.${idx}.quantity_ordered`)} />
            </div>
            <div className="space-y-1">
              {idx === 0 && <Label className="text-xs">Đơn giá</Label>}
              <Input type="number" min={0} {...register(`items.${idx}.unit_price`)} />
            </div>
            <Button type="button" variant="ghost" size="icon" className="h-10 w-10 text-destructive" onClick={() => remove(idx)} disabled={fields.length === 1}>
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        ))}
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={createPo.isPending}>
          {createPo.isPending ? "Đang tạo..." : "Tạo đơn đặt hàng"}
        </Button>
      </div>
    </form>
  );
}
