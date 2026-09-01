"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
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
import { Loader2 } from "lucide-react";
import type { AdminCodeDetail } from "@/lib/api/admin-codes";

const schema = z.object({
  code: z.string().min(1, "Bắt buộc nhập mã").max(64, "Tối đa 64 ký tự"),
  name: z.string().min(1, "Bắt buộc nhập tên").max(255),
  name_en: z.string().optional(),
  sort_order: z.coerce.number().int().optional(),
});

type FormValues = z.infer<typeof schema>;

interface CodeDetailFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  groupLabel: string;
  editTarget: AdminCodeDetail | null;
  isSaving?: boolean;
  onCreate: (values: FormValues) => void;
  onUpdate: (values: Omit<FormValues, "code">) => void;
}

export function CodeDetailFormDialog({
  open,
  onOpenChange,
  groupLabel,
  editTarget,
  isSaving,
  onCreate,
  onUpdate,
}: CodeDetailFormDialogProps) {
  const isEdit = !!editTarget;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(schema) as any,
    defaultValues: { code: "", name: "", name_en: "", sort_order: 0 },
  });

  useEffect(() => {
    if (!open) return;
    if (editTarget) {
      reset({
        code: editTarget.code,
        name: editTarget.name,
        name_en: editTarget.name_en ?? "",
        sort_order: editTarget.sort_order ?? 0,
      });
    } else {
      reset({ code: "", name: "", name_en: "", sort_order: 0 });
    }
  }, [open, editTarget, reset]);

  function handleFormSubmit(values: FormValues) {
    if (isEdit) {
      onUpdate({ name: values.name, name_en: values.name_en, sort_order: values.sort_order });
    } else {
      onCreate(values);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? "Sửa giá trị" : "Thêm giá trị mới"}</DialogTitle>
          <DialogDescription>
            {isEdit
              ? `Sửa giá trị trong nhóm "${groupLabel}".`
              : `Thêm giá trị riêng cho phòng khám vào nhóm "${groupLabel}". Giá trị này chỉ áp dụng cho phòng khám của bạn.`}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="code">
              Mã <span className="text-destructive">*</span>
            </Label>
            <Input
              id="code"
              {...register("code")}
              placeholder="VD: KHAC"
              disabled={isEdit}
              aria-invalid={!!errors.code}
            />
            {errors.code && <p className="text-xs text-destructive">{errors.code.message}</p>}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="name">
              Tên hiển thị <span className="text-destructive">*</span>
            </Label>
            <Input id="name" {...register("name")} placeholder="VD: Khác" aria-invalid={!!errors.name} />
            {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="name_en">Tên tiếng Anh (tuỳ chọn)</Label>
            <Input id="name_en" {...register("name_en")} placeholder="Other" />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="sort_order">Thứ tự hiển thị</Label>
            <Input id="sort_order" type="number" {...register("sort_order")} />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Huỷ
            </Button>
            <Button type="submit" disabled={isSaving}>
              {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isEdit ? "Lưu thay đổi" : "Thêm giá trị"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
