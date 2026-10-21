"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Loader2 } from "lucide-react";
import type { InviteUserRequest } from "@/lib/api/types";
import { useRoles } from "@/lib/hooks/use-roles";

const schema = z.object({
  email: z.string().email("Email không hợp lệ"),
  full_name: z.string().min(2, "Họ tên tối thiểu 2 ký tự"),
  phone: z.string().optional(),
  role_codes: z.array(z.string()).min(1, "Chọn ít nhất 1 vai trò"),
});

type FormData = z.infer<typeof schema>;

interface InviteUserFormProps {
  /** id gắn vào <form> để FullPageFormShell trigger submit từ ngoài */
  formId?: string;
  onSubmit: (values: InviteUserRequest) => Promise<void>;
  isLoading?: boolean;
  /** Ẩn nút Huỷ/Gửi trong form (khi dùng trong FullPageFormShell) */
  hideActions?: boolean;
  onCancel?: () => void;
}

export function InviteUserForm({ formId, onSubmit, isLoading, hideActions, onCancel }: InviteUserFormProps) {
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  // BM-96: lấy danh sách vai trò từ API (gồm cả Điều dưỡng và vai trò tự tạo),
  // thay vì danh sách cứng vốn thiếu role mới tạo ở Tab Vai trò.
  const { data: roles, isLoading: rolesLoading } = useRoles();
  const roleOptions = (roles ?? []).map((r) => ({ code: r.code, label: r.name }));

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<FormData>({
    resolver: standardSchemaResolver(schema),
    defaultValues: { role_codes: [] },
  });

  function toggleRole(code: string) {
    const next = selectedRoles.includes(code)
      ? selectedRoles.filter((r) => r !== code)
      : [...selectedRoles, code];
    setSelectedRoles(next);
    setValue("role_codes", next);
  }

  async function handleFormSubmit(data: FormData) {
    await onSubmit(data as InviteUserRequest);
  }

  return (
    <form id={formId} onSubmit={handleSubmit(handleFormSubmit)} noValidate className="space-y-4">
      <div className="space-y-1.5">
        <Label htmlFor="inv-email">Email *</Label>
        <Input id="inv-email" type="email" placeholder="ten@phongkham.vn" {...register("email")} />
        {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="inv-full-name">Họ tên *</Label>
        <Input id="inv-full-name" placeholder="Nguyễn Văn A" {...register("full_name")} />
        {errors.full_name && <p className="text-xs text-destructive">{errors.full_name.message}</p>}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="inv-phone">Số điện thoại</Label>
        <Input id="inv-phone" placeholder="0901234567" {...register("phone")} />
      </div>

      <div className="space-y-2">
        <Label>Vai trò *</Label>
        {rolesLoading ? (
          <p className="text-xs text-muted-foreground">Đang tải danh sách vai trò...</p>
        ) : roleOptions.length === 0 ? (
          <p className="text-xs text-muted-foreground">Chưa có vai trò nào. Hãy tạo vai trò ở mục Vai trò trước.</p>
        ) : (
          <div className="grid grid-cols-2 gap-2">
            {roleOptions.map((role) => (
              <div key={role.code} className="flex items-center gap-2">
                <Checkbox
                  id={`role-${role.code}`}
                  checked={selectedRoles.includes(role.code)}
                  onCheckedChange={() => toggleRole(role.code)}
                />
                <Label htmlFor={`role-${role.code}`} className="font-normal cursor-pointer">
                  {role.label}
                </Label>
              </div>
            ))}
          </div>
        )}
        {errors.role_codes && (
          <p className="text-xs text-destructive">{errors.role_codes.message}</p>
        )}
      </div>

      {!hideActions && (
        <div className="flex justify-end gap-3 pt-2">
          <Button type="button" variant="outline" onClick={onCancel} disabled={isLoading}>
            Huỷ
          </Button>
          <Button type="submit" disabled={isLoading}>
            {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Gửi lời mời
          </Button>
        </div>
      )}
    </form>
  );
}
