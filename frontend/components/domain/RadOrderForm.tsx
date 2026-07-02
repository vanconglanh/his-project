"use client";

import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useCreateRadOrder } from "@/lib/hooks/use-cls-orders";
import type { Modality } from "@/lib/api/types";

interface Props {
  encounterId: string;
}

const MODALITIES: { value: Modality; label: string }[] = [
  { value: "XRAY", label: "X-quang" },
  { value: "US", label: "Siêu âm" },
  { value: "CT", label: "CT Scan" },
  { value: "MRI", label: "MRI" },
  { value: "MAMMO", label: "Nhũ ảnh" },
  { value: "ECG", label: "Điện tim" },
  { value: "ENDO", label: "Nội soi" },
];

const schema = z.object({
  modality: z.enum(["XRAY", "US", "CT", "MRI", "MAMMO", "ECG", "ENDO"]),
  body_part: z.string().optional(),
  contrast: z.boolean().optional(),
  procedure_code: z.string().min(1, "Cần nhập mã thủ thuật"),
  priority: z.enum(["NORMAL", "URGENT", "STAT"]).optional(),
  note: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

export function RadOrderForm({ encounterId }: Props) {
  const createOrder = useCreateRadOrder(encounterId);
  const { register, handleSubmit, control, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { priority: "NORMAL", contrast: false },
  });

  async function onSubmit(data: FormValues) {
    await createOrder.mutateAsync([{
      modality: data.modality,
      body_part: data.body_part,
      contrast: data.contrast,
      procedure_code: data.procedure_code,
      priority: data.priority,
      note: data.note,
    }]);
    reset();
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label>Phương thức</Label>
          <Controller
            name="modality"
            control={control}
            render={({ field }) => (
              <Select onValueChange={(v) => field.onChange(v ?? "")} value={field.value}>
                <SelectTrigger>
                  <SelectValue placeholder="Chọn phương thức" />
                </SelectTrigger>
                <SelectContent>
                  {MODALITIES.map((m) => (
                    <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
          {errors.modality && <p className="text-xs text-destructive">{errors.modality.message}</p>}
        </div>
        <div className="space-y-1">
          <Label>Vùng cơ thể</Label>
          <Input placeholder="VD: Ngực, Bụng..." {...register("body_part")} />
        </div>
        <div className="space-y-1">
          <Label>Mã thủ thuật</Label>
          <Input placeholder="VD: US-ABD-01" {...register("procedure_code")} />
          {errors.procedure_code && <p className="text-xs text-destructive">{errors.procedure_code.message}</p>}
        </div>
        <div className="space-y-1">
          <Label>Độ ưu tiên</Label>
          <Controller
            name="priority"
            control={control}
            render={({ field }) => (
              <Select onValueChange={(v) => field.onChange(v ?? "")} value={field.value}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="NORMAL">Thường</SelectItem>
                  <SelectItem value="URGENT">Khẩn</SelectItem>
                  <SelectItem value="STAT">Cấp cứu</SelectItem>
                </SelectContent>
              </Select>
            )}
          />
        </div>
      </div>
      <div className="flex items-center gap-2">
        <Controller
          name="contrast"
          control={control}
          render={({ field }) => (
            <Checkbox
              id="contrast"
              checked={field.value ?? false}
              onCheckedChange={field.onChange}
            />
          )}
        />
        <Label htmlFor="contrast" className="cursor-pointer">Có thuốc cản quang</Label>
      </div>
      <div className="space-y-1">
        <Label>Ghi chú</Label>
        <Input placeholder="Ghi chú thêm" {...register("note")} />
      </div>
      <Button type="submit" disabled={createOrder.isPending} className="min-h-[44px]">
        {createOrder.isPending ? "Đang lưu..." : "Lưu chỉ định CĐHA"}
      </Button>
    </form>
  );
}
