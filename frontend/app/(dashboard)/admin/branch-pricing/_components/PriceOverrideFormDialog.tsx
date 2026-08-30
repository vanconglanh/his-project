"use client";

import { useEffect, useState } from "react";
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
import { useBranches } from "@/lib/hooks/use-branches";
import { ItemPicker, type PickerOption } from "./ItemPicker";
import type { PriceOverrideScope } from "@/lib/api/branch-pricing";

const createSchema = z.object({
  scope: z.enum(["BRANCH", "GROUP"]),
  branch_id: z.number().int().positive().optional(),
  group_id: z.number().int().positive().optional(),
  price: z.number().positive("Giá phải lớn hơn 0"),
  is_active: z.boolean(),
  effective_from: z.string().min(1, "Bắt buộc nhập ngày hiệu lực từ"),
  effective_to: z.string().optional().or(z.literal("")),
  note: z.string().optional(),
});

type FormValues = z.infer<typeof createSchema>;

export interface PriceOverrideEditTarget {
  id: string;
  itemLabel: string;
  scope: PriceOverrideScope;
  branch_id: number | null;
  group_id: number | null;
  price: number;
  is_active: boolean;
  effective_from: string;
  effective_to: string | null;
  note: string | null;
}

interface PriceOverrideFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** "SERVICE" | "DRUG" — chỉ dùng để đổi nhãn hiển thị */
  kind: "SERVICE" | "DRUG";
  /** Bản ghi đang sửa — null nghĩa là đang thêm mới */
  editTarget: PriceOverrideEditTarget | null;
  fetchItemOptions: (q: string) => Promise<PickerOption[]>;
  isSaving?: boolean;
  onCreate: (values: {
    item_id: string;
    scope: PriceOverrideScope;
    branch_id?: number;
    group_id?: number;
    price: number;
    is_active: boolean;
    effective_from: string;
    effective_to?: string | null;
    note?: string;
  }) => void;
  onUpdate: (values: {
    price: number;
    is_active: boolean;
    effective_from: string;
    effective_to?: string | null;
    note?: string;
  }) => void;
}

export function PriceOverrideFormDialog({
  open,
  onOpenChange,
  kind,
  editTarget,
  fetchItemOptions,
  isSaving,
  onCreate,
  onUpdate,
}: PriceOverrideFormDialogProps) {
  const isEdit = !!editTarget;
  const [selectedItem, setSelectedItem] = useState<PickerOption | null>(null);
  const { data: branchesData } = useBranches();
  const branches = branchesData?.data ?? [];

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
    resolver: zodResolver(createSchema) as any,
    defaultValues: {
      scope: "BRANCH",
      is_active: true,
      price: 0,
      effective_from: "",
      effective_to: "",
      note: "",
    },
  });

  useEffect(() => {
    if (!open) return;
    if (editTarget) {
      setSelectedItem({ id: "", name: editTarget.itemLabel });
      reset({
        scope: editTarget.scope,
        branch_id: editTarget.branch_id ?? undefined,
        group_id: editTarget.group_id ?? undefined,
        price: editTarget.price,
        is_active: editTarget.is_active,
        effective_from: editTarget.effective_from?.slice(0, 10) ?? "",
        effective_to: editTarget.effective_to?.slice(0, 10) ?? "",
        note: editTarget.note ?? "",
      });
    } else {
      setSelectedItem(null);
      reset({
        scope: "BRANCH",
        branch_id: undefined,
        group_id: undefined,
        price: 0,
        is_active: true,
        effective_from: "",
        effective_to: "",
        note: "",
      });
    }
  }, [open, editTarget, reset]);

  const scope = watch("scope");
  const isActive = watch("is_active");

  function handleFormSubmit(values: FormValues) {
    if (isEdit) {
      onUpdate({
        price: values.price,
        is_active: values.is_active,
        effective_from: values.effective_from,
        effective_to: values.effective_to || null,
        note: values.note || undefined,
      });
      return;
    }

    if (!selectedItem) return;
    onCreate({
      item_id: selectedItem.id,
      scope: values.scope,
      branch_id: values.scope === "BRANCH" ? values.branch_id : undefined,
      group_id: values.scope === "GROUP" ? values.group_id : undefined,
      price: values.price,
      is_active: values.is_active,
      effective_from: values.effective_from,
      effective_to: values.effective_to || null,
      note: values.note || undefined,
    });
  }

  const itemLabel = kind === "SERVICE" ? "dịch vụ" : "thuốc";

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {isEdit ? `Sửa override giá ${itemLabel}` : `Thêm override giá ${itemLabel}`}
          </DialogTitle>
          <DialogDescription>
            {isEdit
              ? "Chỉ có thể sửa giá, trạng thái hiển thị, hiệu lực và ghi chú."
              : `Áp dụng giá riêng và/hoặc ẩn/hiện ${itemLabel} theo chi nhánh hoặc nhóm chi nhánh.`}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
          <div className="space-y-1.5">
            <Label>
              Chọn {itemLabel} <span className="text-destructive">*</span>
            </Label>
            {isEdit ? (
              <Input value={editTarget?.itemLabel ?? ""} disabled />
            ) : (
              <ItemPicker
                queryKeyPrefix={kind === "SERVICE" ? "service-picker" : "drug-picker"}
                fetchOptions={fetchItemOptions}
                value={selectedItem}
                onChange={setSelectedItem}
                placeholder={`Tìm ${itemLabel} theo tên/mã...`}
              />
            )}
            {!isEdit && !selectedItem && (
              <p className="text-xs text-muted-foreground">
                Bắt buộc chọn {itemLabel} trước khi lưu.
              </p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label>Phạm vi</Label>
              <Controller
                control={control}
                name="scope"
                render={({ field }) => (
                  <Select
                    value={field.value}
                    onValueChange={field.onChange}
                    disabled={isEdit}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Chọn phạm vi" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="BRANCH">Theo chi nhánh</SelectItem>
                      <SelectItem value="GROUP">Theo nhóm chi nhánh</SelectItem>
                    </SelectContent>
                  </Select>
                )}
              />
            </div>

            {scope === "BRANCH" ? (
              <div className="space-y-1.5">
                <Label>
                  Chi nhánh <span className="text-destructive">*</span>
                </Label>
                <Controller
                  control={control}
                  name="branch_id"
                  render={({ field }) => (
                    <Select
                      value={field.value ? String(field.value) : undefined}
                      onValueChange={(v) => field.onChange(Number(v))}
                      disabled={isEdit}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Chọn chi nhánh" />
                      </SelectTrigger>
                      <SelectContent>
                        {branches.map((b) => (
                          <SelectItem key={b.id} value={String(b.id)}>
                            {b.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>
            ) : (
              <div className="space-y-1.5">
                <Label htmlFor="group_id">
                  Mã nhóm chi nhánh <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="group_id"
                  type="number"
                  disabled={isEdit}
                  {...register("group_id", { valueAsNumber: true })}
                  placeholder="VD: 1"
                />
              </div>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="price">
                Giá (VNĐ) <span className="text-destructive">*</span>
              </Label>
              <Input
                id="price"
                type="number"
                min={1}
                step={1000}
                {...register("price", { valueAsNumber: true })}
              />
              {errors.price && <p className="text-xs text-destructive">{errors.price.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label>Trạng thái hiển thị</Label>
              <div className="flex h-8 items-center gap-2">
                <Switch
                  checked={isActive}
                  onCheckedChange={(v) => setValue("is_active", v)}
                  aria-label="Hiện/Ẩn"
                />
                <span className="text-sm">{isActive ? "Hiện" : "Ẩn"}</span>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label htmlFor="effective_from">
                Hiệu lực từ <span className="text-destructive">*</span>
              </Label>
              <Input id="effective_from" type="date" {...register("effective_from")} />
              {errors.effective_from && (
                <p className="text-xs text-destructive">{errors.effective_from.message}</p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="effective_to">Hiệu lực đến</Label>
              <Input id="effective_to" type="date" {...register("effective_to")} />
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="note">Ghi chú</Label>
            <Textarea id="note" rows={2} {...register("note")} placeholder="Ghi chú (tuỳ chọn)" />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Huỷ
            </Button>
            <Button type="submit" disabled={isSaving || (!isEdit && !selectedItem)}>
              {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isEdit ? "Lưu thay đổi" : "Thêm override"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
