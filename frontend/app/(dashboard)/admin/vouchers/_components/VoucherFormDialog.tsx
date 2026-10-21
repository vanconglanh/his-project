"use client";

import { useEffect } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Loader2 } from "lucide-react";
import {
  VOUCHER_APPLIES_TO_LABEL,
  type VoucherAppliesTo,
  type VoucherResponse,
} from "@/lib/api/vouchers";

const schema = z
  .object({
    code: z.string().trim().min(1, "Vui lòng nhập mã voucher").max(50, "Tối đa 50 ký tự"),
    description: z.string().optional(),
    discount_percent: z
      .number({ message: "Nhập % giảm giá" })
      .min(0, "Tối thiểu 0%")
      .max(100, "Tối đa 100%"),
    applies_to: z.enum(["EXAM", "LAB", "IMAGING", "DRUG", "ALL"]),
    valid_from: z.string().optional(),
    valid_to: z.string().optional(),
    max_uses: z.number().int("Phải là số nguyên").min(1, "Tối thiểu 1 lượt").nullable(),
    is_active: z.boolean(),
  })
  .refine((v) => !v.valid_from || !v.valid_to || v.valid_from <= v.valid_to, {
    message: "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc",
    path: ["valid_to"],
  });

type FormValues = z.infer<typeof schema>;

export interface VoucherFormValues {
  code: string;
  description: string | null;
  discount_percent: number;
  applies_to: VoucherAppliesTo;
  valid_from: string | null;
  valid_to: string | null;
  max_uses: number | null;
  is_active: boolean;
}

const EMPTY: FormValues = {
  code: "",
  description: "",
  discount_percent: 10,
  applies_to: "ALL",
  valid_from: "",
  valid_to: "",
  max_uses: null,
  is_active: true,
};

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  editTarget: VoucherResponse | null;
  isSaving?: boolean;
  onSubmit: (values: VoucherFormValues) => void;
}

export function VoucherFormDialog({ open, onOpenChange, editTarget, isSaving, onSubmit }: Props) {
  const isEdit = !!editTarget;
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    control,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(schema) as any,
    defaultValues: EMPTY,
  });

  useEffect(() => {
    if (!open) return;
    if (editTarget) {
      reset({
        code: editTarget.code,
        description: editTarget.description ?? "",
        discount_percent: editTarget.discount_percent,
        applies_to: editTarget.applies_to,
        valid_from: editTarget.valid_from?.slice(0, 10) ?? "",
        valid_to: editTarget.valid_to?.slice(0, 10) ?? "",
        max_uses: editTarget.max_uses,
        is_active: editTarget.is_active,
      });
    } else {
      reset(EMPTY);
    }
  }, [open, editTarget, reset]);

  const isActive = watch("is_active");

  const submit = (v: FormValues) =>
    onSubmit({
      code: v.code.trim().toUpperCase(),
      description: v.description?.trim() || null,
      discount_percent: v.discount_percent,
      applies_to: v.applies_to,
      valid_from: v.valid_from || null,
      valid_to: v.valid_to || null,
      max_uses: v.max_uses ?? null,
      is_active: v.is_active,
    });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{isEdit ? "Sửa voucher" : "Thêm voucher"}</DialogTitle>
          <DialogDescription>
            {isEdit ? "Không thể đổi mã voucher sau khi tạo." : "Tạo mã giảm giá theo phần trăm cho hoá đơn."}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(submit)} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="code">
              Mã voucher <span className="text-destructive">*</span>
            </Label>
            <Input id="code" disabled={isEdit} className="uppercase" {...register("code")} placeholder="VD: KHAITRUONG10" />
            {errors.code && <p className="text-xs text-destructive">{errors.code.message}</p>}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="description">Mô tả</Label>
            <Textarea id="description" rows={2} {...register("description")} placeholder="Mô tả (tuỳ chọn)" />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="discount_percent">
                Giảm giá (%) <span className="text-destructive">*</span>
              </Label>
              <Input
                id="discount_percent"
                type="number"
                min={0}
                max={100}
                step="any"
                {...register("discount_percent", { valueAsNumber: true })}
              />
              {errors.discount_percent && (
                <p className="text-xs text-destructive">{errors.discount_percent.message}</p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label>Áp dụng cho</Label>
              <Controller
                control={control}
                name="applies_to"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger aria-label="Áp dụng cho">
                      <SelectValue placeholder="Chọn loại" />
                    </SelectTrigger>
                    <SelectContent>
                      {(Object.keys(VOUCHER_APPLIES_TO_LABEL) as VoucherAppliesTo[]).map((k) => (
                        <SelectItem key={k} value={k}>
                          {VOUCHER_APPLIES_TO_LABEL[k]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="valid_from">Hiệu lực từ</Label>
              <Input id="valid_from" type="date" {...register("valid_from")} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="valid_to">Hiệu lực đến</Label>
              <Input id="valid_to" type="date" {...register("valid_to")} />
              {errors.valid_to && <p className="text-xs text-destructive">{errors.valid_to.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="max_uses">Số lượt dùng tối đa</Label>
              <Input
                id="max_uses"
                type="number"
                min={1}
                step={1}
                placeholder="Không giới hạn"
                {...register("max_uses", {
                  setValueAs: (v) => (v === "" || v == null || Number.isNaN(Number(v)) ? null : Number(v)),
                })}
              />
              {errors.max_uses && <p className="text-xs text-destructive">{errors.max_uses.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Trạng thái</Label>
              <div className="flex h-9 items-center gap-2">
                <Switch
                  checked={isActive}
                  onCheckedChange={(v) => setValue("is_active", v)}
                  aria-label="Kích hoạt voucher"
                />
                <span className="text-sm">{isActive ? "Hoạt động" : "Ngừng"}</span>
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Huỷ
            </Button>
            <Button type="submit" disabled={isSaving}>
              {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isEdit ? "Lưu thay đổi" : "Thêm voucher"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
