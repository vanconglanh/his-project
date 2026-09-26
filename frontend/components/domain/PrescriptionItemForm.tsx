"use client";

import { useEffect, useState } from "react";
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

const schema = z
  .object({
    morning_dose: z.coerce.number().min(0).default(0),
    noon_dose: z.coerce.number().min(0).default(0),
    afternoon_dose: z.coerce.number().min(0).default(0),
    evening_dose: z.coerce.number().min(0).default(0),
    cach_dung: z.string().optional(),
    route: z.enum(["ORAL", "IV", "IM", "SC", "TOP", "INH", "OPH", "OTIC", "NAS", "REC", "OTHER"]),
    duration_days: z.coerce.number().int().min(1, "Tối thiểu 1 ngày"),
    quantity: z.coerce.number().min(0.1, "Bắt buộc"),
    instructions: z.string().optional(),
  })
  .refine(
    (data) =>
      (data.morning_dose ?? 0) + (data.noon_dose ?? 0) + (data.afternoon_dose ?? 0) + (data.evening_dose ?? 0) > 0,
    { message: "Cần nhập ít nhất 1 liều dùng theo buổi", path: ["morning_dose"] }
  );

type FormData = z.infer<typeof schema>;

interface Props {
  drug: DrugMasterResponse;
  onSubmit: (item: PrescriptionItemRequest) => void;
  onCancel: () => void;
  loading?: boolean;
}

export function PrescriptionItemForm({ drug, onSubmit, onCancel, loading }: Props) {
  const [isManualQuantity, setIsManualQuantity] = useState(false);

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
      morning_dose: 0,
      noon_dose: 0,
      afternoon_dose: 0,
      evening_dose: 0,
      quantity: 0,
      cach_dung: "",
    },
  });

  const morningDose = watch("morning_dose");
  const noonDose = watch("noon_dose");
  const afternoonDose = watch("afternoon_dose");
  const eveningDose = watch("evening_dose");
  const duration = watch("duration_days");
  const quantity = watch("quantity");

  // Tự động tính "Tổng số lượng cấp phát" = (tổng liều/ngày) × (số ngày dùng),
  // trừ khi bác sĩ đã tự sửa trực tiếp ô số lượng (isManualQuantity) — chỉ tính
  // lại khi ô đó được xóa trống (quantity === 0/NaN thì coi như reset).
  useEffect(() => {
    if (isManualQuantity) return;
    const dosesPerDay =
      (Number(morningDose) || 0) +
      (Number(noonDose) || 0) +
      (Number(afternoonDose) || 0) +
      (Number(eveningDose) || 0);
    const qty = dosesPerDay * (Number(duration) || 1);
    setValue("quantity", qty);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [morningDose, noonDose, afternoonDose, eveningDose, duration, isManualQuantity]);

  function handleQuantityChange(e: React.ChangeEvent<HTMLInputElement>) {
    const raw = e.target.value;
    setValue("quantity", raw === "" ? 0 : Number(raw));
    if (raw === "") {
      setIsManualQuantity(false);
    } else {
      setIsManualQuantity(true);
    }
  }

  function handleFormSubmit(data: FormData) {
    onSubmit({
      drug_id: drug.id,
      route: data.route,
      duration_days: data.duration_days,
      quantity: data.quantity,
      instructions: data.instructions,
      morning_dose: data.morning_dose,
      noon_dose: data.noon_dose,
      afternoon_dose: data.afternoon_dose,
      evening_dose: data.evening_dose,
      cach_dung: data.cach_dung,
      // Giữ tương thích ngược: sinh dosage/frequency dạng chuỗi từ liều theo buổi.
      dosage: `${data.morning_dose || 0}-${data.noon_dose || 0}-${data.afternoon_dose || 0}-${data.evening_dose || 0}`,
      frequency: data.cach_dung || "",
    });
  }

  return (
    <div className="border rounded-lg p-4 space-y-4 bg-muted/30">
      <div className="flex items-start justify-between">
        <div>
          <p className="font-medium">{drug.name_vi || drug.generic_name || drug.name_en || drug.code}</p>
          <p className="text-sm text-muted-foreground">
            {drug.strength} · {drug.form} · {drug.unit}
          </p>
        </div>
      </div>

      <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1">
            <Label htmlFor="route">Đường dùng</Label>
            <Select
              items={Object.fromEntries(ROUTES.map((r) => [r.value, r.label]))}
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
            <Label>Đơn vị tính</Label>
            <Input value={drug.unit ?? ""} disabled readOnly />
          </div>
        </div>

        <div className="space-y-1">
          <Label>Liều dùng theo buổi</Label>
          <div className="grid grid-cols-4 gap-2">
            <div className="space-y-1">
              <Label htmlFor="morning_dose" className="text-xs font-normal text-muted-foreground">
                Sáng
              </Label>
              <Input id="morning_dose" type="number" step="0.5" min={0} {...register("morning_dose")} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="noon_dose" className="text-xs font-normal text-muted-foreground">
                Trưa
              </Label>
              <Input id="noon_dose" type="number" step="0.5" min={0} {...register("noon_dose")} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="afternoon_dose" className="text-xs font-normal text-muted-foreground">
                Chiều
              </Label>
              <Input id="afternoon_dose" type="number" step="0.5" min={0} {...register("afternoon_dose")} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="evening_dose" className="text-xs font-normal text-muted-foreground">
                Tối
              </Label>
              <Input id="evening_dose" type="number" step="0.5" min={0} {...register("evening_dose")} />
            </div>
          </div>
          {errors.morning_dose && <p className="text-xs text-destructive">{errors.morning_dose.message}</p>}
        </div>

        <div className="grid grid-cols-3 gap-3">
          <div className="space-y-1">
            <Label htmlFor="cach_dung">Cách dùng</Label>
            <Input id="cach_dung" placeholder="VD: Sau ăn" {...register("cach_dung")} />
          </div>

          <div className="space-y-1">
            <Label htmlFor="duration_days">Số ngày dùng</Label>
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
            <Label htmlFor="quantity">Tổng số lượng cấp phát ({drug.unit})</Label>
            <Input
              id="quantity"
              type="number"
              step="0.5"
              min={0}
              value={quantity ?? 0}
              onChange={handleQuantityChange}
              aria-invalid={!!errors.quantity}
            />
            {errors.quantity && <p className="text-xs text-destructive">{errors.quantity.message}</p>}
            {isManualQuantity && (
              <p className="text-xs text-muted-foreground">Đã sửa tay — xóa trống để tính lại tự động</p>
            )}
          </div>
        </div>

        <div className="space-y-1">
          <Label htmlFor="instructions">Ghi chú cho bệnh nhân</Label>
          <textarea
            id="instructions"
            className="flex min-h-[60px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 resize-none"
            placeholder="VD: Uống nhiều nước, tái khám sau 7 ngày..."
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
