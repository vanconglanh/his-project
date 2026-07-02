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
import { usePermissions } from "@/lib/hooks/use-roles";
import type { RoleResponse, CreateRoleRequest, UpdateRoleRequest } from "@/lib/api/types";

const createSchema = z.object({
  code: z.string().regex(/^[A-Z][A-Z0-9_]{2,30}$/, "Mã vai trò: viết hoa, gạch dưới, 3-30 ký tự"),
  name: z.string().min(2),
  description: z.string().optional(),
  permission_codes: z.array(z.string()).min(1, "Chọn ít nhất 1 quyền"),
});

const editSchema = z.object({
  name: z.string().min(2).optional(),
  description: z.string().optional(),
  permission_codes: z.array(z.string()).optional(),
});

type CreateFormData = z.infer<typeof createSchema>;
type EditFormData = z.infer<typeof editSchema>;

interface RoleFormProps {
  initialValues?: RoleResponse;
  isEdit?: boolean;
  onSubmit: (values: CreateRoleRequest | UpdateRoleRequest) => Promise<void>;
  isLoading?: boolean;
  onCancel: () => void;
}

export function RoleForm({ initialValues, isEdit, onSubmit, isLoading, onCancel }: RoleFormProps) {
  const { data: permissions = [] } = usePermissions();
  const [selectedPerms, setSelectedPerms] = useState<string[]>(
    initialValues?.permission_codes ?? []
  );

  const form = useForm<CreateFormData | EditFormData>({
    resolver: standardSchemaResolver(isEdit ? editSchema : createSchema),
    defaultValues: isEdit
      ? { name: initialValues?.name, description: initialValues?.description, permission_codes: initialValues?.permission_codes }
      : { permission_codes: [] },
  });

  const errors = form.formState.errors as Record<string, { message?: string }>;

  // Group permissions by resource
  const grouped = permissions.reduce<Record<string, typeof permissions>>((acc, p) => {
    if (!acc[p.resource]) acc[p.resource] = [];
    acc[p.resource].push(p);
    return acc;
  }, {});

  function togglePerm(code: string) {
    const next = selectedPerms.includes(code)
      ? selectedPerms.filter((p) => p !== code)
      : [...selectedPerms, code];
    setSelectedPerms(next);
    form.setValue("permission_codes", next);
  }

  async function handleSubmit(data: CreateFormData | EditFormData) {
    await onSubmit({ ...data, permission_codes: selectedPerms } as CreateRoleRequest | UpdateRoleRequest);
  }

  return (
    <form onSubmit={form.handleSubmit(handleSubmit)} noValidate className="space-y-4">
      {!isEdit && (
        <div className="space-y-1.5">
          <Label htmlFor="role-code">Mã vai trò *</Label>
          <Input id="role-code" placeholder="BACSI_TRUONG" {...form.register("code")} />
          {errors.code && <p className="text-xs text-destructive">{errors.code.message}</p>}
        </div>
      )}

      <div className="space-y-1.5">
        <Label htmlFor="role-name">Tên vai trò *</Label>
        <Input id="role-name" placeholder="Bác sĩ trưởng" {...form.register("name")} />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="role-desc">Mô tả</Label>
        <Input id="role-desc" placeholder="Mô tả vai trò..." {...form.register("description")} />
      </div>

      <div className="space-y-2">
        <Label>Quyền hạn *</Label>
        {errors.permission_codes && (
          <p className="text-xs text-destructive">{errors.permission_codes.message}</p>
        )}
        <div className="max-h-64 overflow-y-auto space-y-4 border rounded p-3">
          {Object.entries(grouped).map(([resource, perms]) => (
            <div key={resource}>
              <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">{resource}</p>
              <div className="grid grid-cols-2 gap-1.5">
                {perms.map((p) => (
                  <div key={p.code} className="flex items-center gap-2">
                    <Checkbox
                      id={`perm-${p.code}`}
                      checked={selectedPerms.includes(p.code)}
                      onCheckedChange={() => togglePerm(p.code)}
                    />
                    <Label htmlFor={`perm-${p.code}`} className="text-xs font-normal cursor-pointer">
                      {p.description ?? p.action}
                    </Label>
                  </div>
                ))}
              </div>
            </div>
          ))}
          {Object.keys(grouped).length === 0 && (
            <p className="text-sm text-muted-foreground text-center py-4">Đang tải quyền...</p>
          )}
        </div>
      </div>

      <div className="flex justify-end gap-3 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isLoading}>
          Huỷ
        </Button>
        <Button type="submit" disabled={isLoading}>
          {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {isEdit ? "Lưu thay đổi" : "Tạo vai trò"}
        </Button>
      </div>
    </form>
  );
}
