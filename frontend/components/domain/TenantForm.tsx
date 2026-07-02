"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import type { TenantResponse, CreateTenantRequest, UpdateTenantRequest } from "@/lib/api/types";

const createSchema = z.object({
  code: z.string().min(3).max(20).regex(/^[A-Z0-9]{3,20}$/, "Mã phòng khám viết hoa, chữ số, 3-20 ký tự"),
  name: z.string().min(3).max(200),
  subdomain: z.string().regex(/^[a-z0-9-]{3,63}$/, "Subdomain lowercase, chữ số, gạch ngang"),
  email: z.string().email("Email không hợp lệ"),
  cskcb_code: z.string().optional(),
  tax_code: z.string().optional(),
  address: z.string().optional(),
  phone: z.string().optional(),
  storage_quota_gb: z.coerce.number().min(1).max(1000).optional(),
  admin_email: z.string().email("Email quản trị không hợp lệ"),
  admin_full_name: z.string().min(2),
});

const editSchema = z.object({
  name: z.string().min(3).max(200).optional(),
  cskcb_code: z.string().optional(),
  tax_code: z.string().optional(),
  address: z.string().optional(),
  phone: z.string().optional(),
  email: z.string().email().optional(),
  storage_quota_gb: z.coerce.number().min(1).optional(),
  expires_at: z.string().optional(),
});

type CreateFormData = z.infer<typeof createSchema>;
type EditFormData = z.infer<typeof editSchema>;

interface TenantFormProps {
  initialValues?: TenantResponse;
  isEdit?: boolean;
  onSubmit: (values: CreateTenantRequest | UpdateTenantRequest) => Promise<void>;
  isLoading?: boolean;
  onCancel: () => void;
}

function CreateForm({ onSubmit, isLoading, onCancel }: {
  onSubmit: (values: CreateTenantRequest) => Promise<void>;
  isLoading?: boolean;
  onCancel: () => void;
}) {
  const { register, handleSubmit, formState: { errors } } = useForm<CreateFormData>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(createSchema) as any,
    defaultValues: { storage_quota_gb: 20 },
  });

  return (
    <form onSubmit={handleSubmit((d) => onSubmit(d as unknown as CreateTenantRequest))} noValidate className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="code">Mã phòng khám *</Label>
          <Input id="code" placeholder="PKAB001" {...register("code")} />
          {errors.code && <p className="text-xs text-destructive">{errors.code.message}</p>}
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="subdomain">Subdomain *</Label>
          <Input id="subdomain" placeholder="anbinh" {...register("subdomain")} />
          {errors.subdomain && <p className="text-xs text-destructive">{errors.subdomain.message}</p>}
        </div>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="name">Tên phòng khám *</Label>
        <Input id="name" placeholder="Phòng khám Đa khoa An Bình" {...register("name")} />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="email">Email phòng khám *</Label>
          <Input id="email" type="email" {...register("email")} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="phone">Số điện thoại</Label>
          <Input id="phone" {...register("phone")} />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="cskcb_code">Mã CSKCB</Label>
          <Input id="cskcb_code" placeholder="79001" {...register("cskcb_code")} />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="tax_code">Mã số thuế</Label>
          <Input id="tax_code" placeholder="0312345678" {...register("tax_code")} />
        </div>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="address">Địa chỉ</Label>
        <Input id="address" {...register("address")} />
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="storage_quota_gb">Dung lượng lưu trữ (GB)</Label>
        <Input id="storage_quota_gb" type="number" min={1} max={1000} {...register("storage_quota_gb")} />
      </div>

      <hr className="my-4" />
      <p className="text-sm font-medium text-muted-foreground">Tài khoản quản trị viên đầu tiên</p>
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="admin_email">Email quản trị *</Label>
          <Input id="admin_email" type="email" {...register("admin_email")} />
          {errors.admin_email && <p className="text-xs text-destructive">{errors.admin_email.message}</p>}
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="admin_full_name">Họ tên quản trị *</Label>
          <Input id="admin_full_name" {...register("admin_full_name")} />
          {errors.admin_full_name && <p className="text-xs text-destructive">{errors.admin_full_name.message}</p>}
        </div>
      </div>

      <div className="flex justify-end gap-3 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isLoading}>Huỷ</Button>
        <Button type="submit" disabled={isLoading}>
          {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Tạo phòng khám
        </Button>
      </div>
    </form>
  );
}

function EditForm({ initialValues, onSubmit, isLoading, onCancel }: {
  initialValues?: TenantResponse;
  onSubmit: (values: UpdateTenantRequest) => Promise<void>;
  isLoading?: boolean;
  onCancel: () => void;
}) {
  const { register, handleSubmit, formState: { errors } } = useForm<EditFormData>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(editSchema) as any,
    defaultValues: {
      name: initialValues?.name,
      cskcb_code: initialValues?.cskcb_code,
      tax_code: initialValues?.tax_code,
      address: initialValues?.address,
      phone: initialValues?.phone,
      email: initialValues?.email,
      storage_quota_gb: initialValues?.storage_quota_gb,
    },
  });

  return (
    <form onSubmit={handleSubmit((d) => onSubmit(d as UpdateTenantRequest))} noValidate className="space-y-4">
      <div className="space-y-1.5">
        <Label htmlFor="edit-name">Tên phòng khám</Label>
        <Input id="edit-name" {...register("name")} />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="edit-email">Email</Label>
          <Input id="edit-email" type="email" {...register("email")} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="edit-phone">Điện thoại</Label>
          <Input id="edit-phone" {...register("phone")} />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="edit-cskcb">Mã CSKCB</Label>
          <Input id="edit-cskcb" {...register("cskcb_code")} />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="edit-tax">Mã số thuế</Label>
          <Input id="edit-tax" {...register("tax_code")} />
        </div>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="edit-address">Địa chỉ</Label>
        <Input id="edit-address" {...register("address")} />
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="edit-quota">Dung lượng (GB)</Label>
        <Input id="edit-quota" type="number" min={1} {...register("storage_quota_gb")} />
      </div>

      <div className="flex justify-end gap-3 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isLoading}>Huỷ</Button>
        <Button type="submit" disabled={isLoading}>
          {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Lưu thay đổi
        </Button>
      </div>
    </form>
  );
}

export function TenantForm({ initialValues, isEdit, onSubmit, isLoading, onCancel }: TenantFormProps) {
  if (isEdit) {
    return (
      <EditForm
        initialValues={initialValues}
        onSubmit={(v) => onSubmit(v)}
        isLoading={isLoading}
        onCancel={onCancel}
      />
    );
  }
  return (
    <CreateForm
      onSubmit={(v) => onSubmit(v)}
      isLoading={isLoading}
      onCancel={onCancel}
    />
  );
}
