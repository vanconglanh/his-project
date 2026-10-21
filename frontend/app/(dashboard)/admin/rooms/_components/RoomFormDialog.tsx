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
import { Switch } from "@/components/ui/switch";
import { Loader2 } from "lucide-react";
import type { RoomAdminResponse } from "@/lib/api/rooms";

const schema = z.object({
  room_code: z.string().trim().min(1, "Bắt buộc nhập mã phòng").max(50, "Tối đa 50 ký tự"),
  name: z.string().trim().min(1, "Bắt buộc nhập tên phòng").max(200, "Tối đa 200 ký tự"),
  max_per_day: z
    .number({ message: "Nhập số hợp lệ" })
    .int("Phải là số nguyên")
    .min(1, "Tối thiểu 1")
    .max(9999, "Tối đa 9999"),
  is_active: z.boolean(),
});

export type RoomFormValues = z.infer<typeof schema>;

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** null = thêm mới */
  editTarget: RoomAdminResponse | null;
  isSaving?: boolean;
  onSubmit: (values: RoomFormValues) => void;
}

const EMPTY: RoomFormValues = { room_code: "", name: "", max_per_day: 40, is_active: true };

export function RoomFormDialog({ open, onOpenChange, editTarget, isSaving, onSubmit }: Props) {
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<RoomFormValues>({ resolver: zodResolver(schema), defaultValues: EMPTY });

  useEffect(() => {
    if (!open) return;
    reset(
      editTarget
        ? {
            room_code: editTarget.room_code,
            name: editTarget.name,
            max_per_day: editTarget.max_per_day,
            is_active: editTarget.is_active,
          }
        : EMPTY,
    );
  }, [open, editTarget, reset]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{editTarget ? "Sửa phòng khám" : "Thêm phòng khám"}</DialogTitle>
          <DialogDescription>
            Phòng đang hoạt động sẽ hiện trong danh sách chọn phòng ở form Tiếp đón.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div className="space-y-1.5">
            <Label htmlFor="room_code">Mã phòng *</Label>
            <Input id="room_code" className="h-11" aria-invalid={!!errors.room_code} {...register("room_code")} />
            {errors.room_code && <p className="text-xs text-destructive">{errors.room_code.message}</p>}
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="room_name">Tên phòng *</Label>
            <Input id="room_name" className="h-11" aria-invalid={!!errors.name} {...register("name")} />
            {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="max_per_day">Số lượt khám tối đa mỗi ngày</Label>
            <Input
              id="max_per_day"
              type="number"
              min={1}
              className="h-11"
              aria-invalid={!!errors.max_per_day}
              {...register("max_per_day", { valueAsNumber: true })}
            />
            {errors.max_per_day && <p className="text-xs text-destructive">{errors.max_per_day.message}</p>}
          </div>
          <div className="flex items-center justify-between rounded-md border p-3">
            <Label htmlFor="room_active">Đang hoạt động</Label>
            <Controller
              control={control}
              name="is_active"
              render={({ field }) => (
                <Switch id="room_active" checked={field.value} onCheckedChange={field.onChange} />
              )}
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSaving}>
              Hủy
            </Button>
            <Button type="submit" disabled={isSaving}>
              {isSaving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
              Lưu
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
