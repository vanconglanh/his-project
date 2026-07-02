"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useCreateSupplier, useUpdateSupplier } from "@/lib/hooks/use-suppliers";
import type { SupplierResponse } from "@/lib/api/suppliers";

const schema = z.object({
  code: z.string().min(1, "Bắt buộc"),
  name: z.string().min(1, "Bắt buộc"),
  tax_code: z.string().optional(),
  address: z.string().optional(),
  phone: z.string().optional(),
  email: z.string().email("Email không hợp lệ").optional().or(z.literal("")),
  contact_person: z.string().optional(),
  status: z.enum(["ACTIVE", "INACTIVE"]).optional(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  supplier?: SupplierResponse;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function SupplierForm({ supplier, onSuccess, onCancel }: Props) {
  const createSupplier = useCreateSupplier();
  const updateSupplier = useUpdateSupplier(supplier?.id ?? "");

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: supplier
      ? { ...supplier, email: supplier.email ?? "", status: supplier.status ?? "ACTIVE" }
      : { status: "ACTIVE" },
  });

  async function onSubmit(data: FormData) {
    if (supplier) {
      await updateSupplier.mutateAsync(data);
    } else {
      await createSupplier.mutateAsync(data);
    }
    onSuccess?.();
  }

  const isPending = createSupplier.isPending || updateSupplier.isPending;

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label htmlFor="code">Mã NCC <span className="text-destructive">*</span></Label>
          <Input id="code" {...register("code")} aria-invalid={!!errors.code} />
          {errors.code && <p className="text-xs text-destructive">{errors.code.message}</p>}
        </div>
        <div className="space-y-1">
          <Label htmlFor="name">Tên NCC <span className="text-destructive">*</span></Label>
          <Input id="name" {...register("name")} aria-invalid={!!errors.name} />
          {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
        </div>
        <div className="space-y-1">
          <Label htmlFor="tax_code">Mã số thuế</Label>
          <Input id="tax_code" {...register("tax_code")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="phone">Điện thoại</Label>
          <Input id="phone" {...register("phone")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" {...register("email")} aria-invalid={!!errors.email} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>
        <div className="space-y-1">
          <Label htmlFor="contact_person">Người liên hệ</Label>
          <Input id="contact_person" {...register("contact_person")} />
        </div>
        <div className="col-span-2 space-y-1">
          <Label htmlFor="address">Địa chỉ</Label>
          <Input id="address" {...register("address")} />
        </div>
      </div>

      <div className="flex justify-end gap-2">
        {onCancel && <Button type="button" variant="ghost" onClick={onCancel}>Hủy</Button>}
        <Button type="submit" disabled={isPending}>
          {isPending ? "Đang lưu..." : supplier ? "Cập nhật" : "Tạo nhà cung cấp"}
        </Button>
      </div>
    </form>
  );
}
