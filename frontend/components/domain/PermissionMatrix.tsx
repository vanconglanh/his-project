"use client";

import { useMemo, useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Loader2, Lock } from "lucide-react";
import { usePermissions, useUpdateRole } from "@/lib/hooks/use-roles";
import type { RoleResponse } from "@/lib/api/types";

interface PermissionMatrixProps {
  role: RoleResponse;
  /** Gọi sau khi lưu quyền thành công (vd đóng sheet) */
  onSaved?: () => void;
}

const ACTION_LABELS: Record<string, string> = {
  read: "Xem",
  write: "Sửa",
  delete: "Xoá",
  sign: "Ký",
  export: "Xuất",
  invite: "Mời",
  assign_role: "Gán quyền",
};

export function PermissionMatrix({ role, onSaved }: PermissionMatrixProps) {
  const { data: permissions = [] } = usePermissions();
  const updateMutation = useUpdateRole();

  // BM-107: vai trò hệ thống bị backend chặn sửa (ROLE_SYSTEM_PROTECTED) -> chỉ xem.
  const isSystem = role.role_type === "SYSTEM";

  const [selected, setSelected] = useState<string[]>(role.permission_codes ?? []);

  const grouped = useMemo(
    () =>
      permissions.reduce<Record<string, typeof permissions>>((acc, p) => {
        if (!acc[p.resource]) acc[p.resource] = [];
        acc[p.resource].push(p);
        return acc;
      }, {}),
    [permissions]
  );

  const original = role.permission_codes ?? [];
  const isDirty =
    selected.length !== original.length ||
    selected.some((c) => !original.includes(c));

  function toggle(code: string) {
    setSelected((prev) =>
      prev.includes(code) ? prev.filter((c) => c !== code) : [...prev, code]
    );
  }

  async function handleSave() {
    await updateMutation.mutateAsync({
      code: role.code,
      payload: { permission_codes: selected },
    });
    onSaved?.();
  }

  return (
    <div className="mt-4 space-y-4">
      <div className="flex items-center gap-2">
        <Badge variant={isSystem ? "secondary" : "outline"}>
          {isSystem ? "Hệ thống" : "Tuỳ chỉnh"}
        </Badge>
        {role.description && (
          <span className="text-sm text-muted-foreground">{role.description}</span>
        )}
      </div>

      {isSystem && (
        <div className="flex items-start gap-2 rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-800 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-300">
          <Lock className="h-4 w-4 shrink-0 mt-0.5" aria-hidden="true" />
          <span>
            Vai trò hệ thống không thể chỉnh sửa quyền. Tạo vai trò tuỳ chỉnh để
            điều chỉnh phân quyền theo nhu cầu.
          </span>
        </div>
      )}

      <div className="space-y-3">
        {Object.entries(grouped).map(([resource, perms]) => (
          <div key={resource} className="border rounded-md overflow-hidden">
            <div className="bg-muted/50 px-3 py-2">
              <p className="text-xs font-semibold uppercase tracking-wide">{resource}</p>
            </div>
            <div className="divide-y">
              {perms.map((p) => {
                const checked = selected.includes(p.code);
                return (
                  <label
                    key={p.code}
                    htmlFor={`pm-${p.code}`}
                    className="flex items-center justify-between px-3 py-2 cursor-pointer data-[disabled=true]:cursor-default"
                    data-disabled={isSystem}
                  >
                    <div>
                      <span className="text-sm">{p.description ?? p.code}</span>
                      <span className="ml-2 text-xs text-muted-foreground">
                        ({ACTION_LABELS[p.action] ?? p.action})
                      </span>
                    </div>
                    <Checkbox
                      id={`pm-${p.code}`}
                      checked={checked}
                      disabled={isSystem}
                      onCheckedChange={() => toggle(p.code)}
                      aria-label={p.description ?? p.code}
                    />
                  </label>
                );
              })}
            </div>
          </div>
        ))}
        {Object.keys(grouped).length === 0 && (
          <p className="text-sm text-muted-foreground text-center py-8">Đang tải danh sách quyền...</p>
        )}
      </div>

      {!isSystem && (
        <div className="flex items-center justify-end gap-3 pt-2">
          <span className="mr-auto text-xs text-muted-foreground">
            Đã chọn {selected.length} quyền
          </span>
          <Button
            type="button"
            variant="outline"
            disabled={!isDirty || updateMutation.isPending}
            onClick={() => setSelected(role.permission_codes ?? [])}
          >
            Hoàn tác
          </Button>
          <Button
            type="button"
            disabled={!isDirty || updateMutation.isPending}
            onClick={handleSave}
          >
            {updateMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Lưu thay đổi
          </Button>
        </div>
      )}
    </div>
  );
}
