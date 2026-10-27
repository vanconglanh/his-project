"use client";

import { useEffect } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { DrugAutocomplete } from "@/components/domain/DrugAutocomplete";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useCreateGrn, useWarehouses } from "@/lib/hooks/use-pharmacy-warehouse";
import { Plus, Trash2 } from "lucide-react";

const schema = z.object({
  warehouse_id: z.string().optional(),
  received_at: z.string().min(1),
  note: z.string().optional(),
  items: z.array(
    z.object({
      drug_id: z.string().min(1, "Chọn thuốc"),
      // Nhan hien thi "code — name_vi" gan lien voi dong du lieu (khong gui len BE).
      drug_label: z.string().optional(),
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
  /** Có poId -> luồng gắn PO (kho suy từ PO). Không có -> nhập kho không PO, phải chọn kho. */
  poId?: string;
  onSuccess?: () => void;
  /** id gắn lên thẻ <form> — trang cha (Fullpage shell) dùng để trigger submit từ ngoài */
  formId?: string;
  /** Báo trạng thái đang submit (mutation pending) lên trang cha để hiện ở StickyActionBar */
  onSubmittingChange?: (submitting: boolean) => void;
}

export function GrnForm({ poId, onSuccess, formId = "grn-form", onSubmittingChange }: Props) {
  const createGrn = useCreateGrn(poId);
  const { data: warehouses } = useWarehouses();

  const { register, handleSubmit, control, setValue, watch, setError, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: {
      received_at: new Date().toISOString().slice(0, 16),
      items: [{ drug_id: "", drug_label: "", batch_no: "", expiry_date: "", quantity_received: 1, unit_cost: 0 }],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: "items" });

  useEffect(() => {
    onSubmittingChange?.(createGrn.isPending);
  }, [createGrn.isPending, onSubmittingChange]);

  async function onSubmit(data: FormData) {
    if (!poId && !data.warehouse_id) {
      setError("warehouse_id", { message: "Cần chọn kho khi không có đơn đặt hàng" });
      return;
    }
    // Chuẩn hoá items: ngày rỗng ("") -> undefined (DateOnly? backend không parse được ""), ép kiểu số.
    const items = (data.items ?? []).map(({ drug_label: _drugLabel, ...it }) => ({
      ...it,
      manufacture_date: it.manufacture_date ? it.manufacture_date : undefined,
      quantity_received: Number(it.quantity_received),
      unit_cost: Number(it.unit_cost),
    }));
    await createGrn.mutateAsync({ ...data, items, warehouse_id: poId ? undefined : data.warehouse_id, received_at: new Date(data.received_at).toISOString() });
    onSuccess?.();
  }

  return (
    <form id={formId} onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        {!poId && (
          <div className="space-y-1 col-span-2">
            <Label>Kho nhập</Label>
            <Select
              items={Object.fromEntries((warehouses ?? []).map((w) => [w.id, w.name]))}
              onValueChange={(v) => setValue("warehouse_id", String(v ?? ""), { shouldValidate: true })}
            >
              <SelectTrigger><SelectValue placeholder="-- Chọn kho --" /></SelectTrigger>
              <SelectContent>
                {warehouses?.map((w) => (
                  <SelectItem key={w.id} value={w.id}>{w.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            {errors.warehouse_id && <p className="text-xs text-destructive">{errors.warehouse_id.message}</p>}
          </div>
        )}
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
          <Button type="button" variant="outline" size="sm" onClick={() => append({ drug_id: "", drug_label: "", batch_no: "", expiry_date: "", quantity_received: 1, unit_cost: 0 })}>
            <Plus className="h-4 w-4 mr-1" />Thêm lô
          </Button>
        </div>

        {fields.map((field, idx) => (
          <div key={field.id} className="border rounded-md p-3 space-y-3 relative">
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <Label className="text-xs">Thuốc</Label>
                {watch(`items.${idx}.drug_id`) ? (
                  <div className="flex items-center gap-1">
                    <div className="flex-1 truncate rounded-md border px-3 h-10 flex items-center text-sm" title={watch(`items.${idx}.drug_label`)}>
                      {watch(`items.${idx}.drug_label`) || watch(`items.${idx}.drug_id`)}
                    </div>
                    <Button type="button" variant="ghost" size="sm" onClick={() => { setValue(`items.${idx}.drug_id`, ""); setValue(`items.${idx}.drug_label`, ""); }}>
                      Đổi
                    </Button>
                  </div>
                ) : (
                  <DrugAutocomplete
                    placeholder="Tìm theo tên hoặc mã..."
                    onSelect={(d) => {
                      setValue(`items.${idx}.drug_id`, d.id, { shouldValidate: true });
                      setValue(`items.${idx}.drug_label`, `${d.code} — ${d.name_vi}`);
                    }}
                  />
                )}
                {errors.items?.[idx]?.drug_id && <p className="text-xs text-destructive">{errors.items[idx]?.drug_id?.message}</p>}
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
