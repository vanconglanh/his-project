"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { DrugMasterResponse } from "@/lib/api/drugs";
import type { PrescriptionItemRequest } from "@/lib/api/prescriptions";

const ROUTES = [
  { value: "ORAL", label: "Uống" },
  { value: "IV", label: "Tiêm tĩnh mạch (IV)" },
  { value: "IM", label: "Tiêm bắp (IM)" },
  { value: "SC", label: "Tiêm dưới da (SC)" },
  { value: "TOP", label: "Bôi ngoài da" },
  { value: "INH", label: "Hít" },
  { value: "OPH", label: "Nhỏ mắt" },
  { value: "OTIC", label: "Nhỏ tai" },
  { value: "NAS", label: "Nhỏ mũi" },
  { value: "REC", label: "Đặt hậu môn" },
  { value: "OTHER", label: "Khác" },
] as const;

const schema = z.object({
  dosage: z.string().min(1, "Bắt buộc"),
  frequency: z.string().min(1, "Bắt buộc"),
  route: z.enum(["ORAL", "IV", "IM", "SC", "TOP", "INH", "OPH", "OTIC", "NAS", "REC", "OTHER"]),
  duration_days: z.coerce.number().int().min(1, "Tối thiểu 1 ngày"),
  quantity: z.coerce.number().min(0.1, "Bắt buộc"),
  instructions: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  drug: DrugMasterResponse;
  onSubmit: (item: PrescriptionItemRequest) => void;
  onCancel: () => void;
  loading?: boolean;
}

export function PrescriptionItemForm({ drug, onSubmit, onCancel, loading }: Props) {
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: {
      route: "ORAL",
      duration_days: 7,
      quantity: 0,
    },
  });

  // Auto-compute quantity hint when frequency or duration changes
  const frequency = watch("frequency");
  const duration = watch("duration_days");
  const dosage = watch("dosage");

  // Simple parse: "2 lần/ngày" → 2, "3 lần" → 3
  function parseDosesPerDay(freq: string): number {
    const match = freq.match(/(\d+)/);
    return match ? parseInt(match[1]) : 1;
  }

  useEffect(() => {
    if (frequency && duration) {
      const dosesPerDay = parseDosesPerDay(frequency);
      const qty = dosesPerDay * (Number(duration) || 1);
      setValue("quantity", qty);
    }
  }, [frequency, duration, setValue]);

  function handleFormSubmit(data: FormData) {
    onSubmit({
      drug_id: drug.id,
      ...data,
    });
  }

  return (
    <div className="border rounded-lg p-4 space-y-4 bg-muted/30">
      <div className="flex items-start justify-between">
        <div>
          <p className="font-medium">{drug.name_vi}</p>
          <p className="text-sm text-muted-foreground">
            {drug.strength} · {drug.form} · {drug.unit}
          </p>
        </div>
      </div>

      <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1">
            <Label htmlFor="dosage">Liều dùng</Label>
            <Input
              id="dosage"
              placeholder="VD: 1 viên"
              {...register("dosage")}
              aria-invalid={!!errors.dosage}
            />
            {errors.dosage && <p className="text-xs text-destructive">{errors.dosage.message}</p>}
          </div>

          <div className="space-y-1">
            <Label htmlFor="frequency">Tần suất</Label>
            <Input
              id="frequency"
              placeholder="VD: 2 lần/ngày"
              {...register("frequency")}
              aria-invalid={!!errors.frequency}
            />
            {errors.frequency && <p className="text-xs text-destructive">{errors.frequency.message}</p>}
          </div>

          <div className="space-y-1">
            <Label htmlFor="route">Đường dùng</Label>
            <Select
              defaultValue="ORAL"
              onValueChange={(v) => setValue("route", v as FormData["route"])}
            >
              <SelectTrigger id="route">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {ROUTES.map((r) => (
                  <SelectItem key={r.value} value={r.value}>
                    {r.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1">
            <Label htmlFor="duration_days">Số ngày</Label>
            <Input
              id="duration_days"
              type="number"
              min={1}
              {...register("duration_days")}
              aria-invalid={!!errors.duration_days}
            />
            {errors.duration_days && <p className="text-xs text-destructive">{errors.duration_days.message}</p>}
          </div>

          <div className="space-y-1">
            <Label htmlFor="quantity">Số lượng ({drug.unit})</Label>
            <Input
              id="quantity"
              type="number"
              step="0.5"
              min={0}
              {...register("quantity")}
              aria-invalid={!!errors.quantity}
            />
            {errors.quantity && <p className="text-xs text-destructive">{errors.quantity.message}</p>}
          </div>
        </div>

        <div className="space-y-1">
          <Label htmlFor="instructions">Hướng dẫn dùng thuốc</Label>
          <textarea
            id="instructions"
            className="flex min-h-[60px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 resize-none"
            placeholder="VD: Uống sau ăn, không nhai..."
            {...register("instructions")}
          />
        </div>

        <div className="flex gap-2 justify-end">
          <Button type="button" variant="ghost" size="sm" onClick={onCancel}>
            Hủy
          </Button>
          <Button type="submit" size="sm" disabled={loading}>
            {loading ? "Đang thêm..." : "Thêm vào đơn"}
          </Button>
        </div>
      </form>
    </div>
  );
}
