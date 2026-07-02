"use client";

import { Badge } from "@/components/ui/badge";
import { CheckCircle2, XCircle } from "lucide-react";
import { usePermissions } from "@/lib/hooks/use-roles";
import type { RoleResponse } from "@/lib/api/types";

interface PermissionMatrixProps {
  role: RoleResponse;
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

export function PermissionMatrix({ role }: PermissionMatrixProps) {
  const { data: permissions = [] } = usePermissions();

  const grouped = permissions.reduce<Record<string, typeof permissions>>((acc, p) => {
    if (!acc[p.resource]) acc[p.resource] = [];
    acc[p.resource].push(p);
    return acc;
  }, {});

  return (
    <div className="mt-4 space-y-4">
      <div className="flex items-center gap-2">
        <Badge variant={role.role_type === "SYSTEM" ? "secondary" : "outline"}>
          {role.role_type === "SYSTEM" ? "Hệ thống" : "Tuỳ chỉnh"}
        </Badge>
        {role.description && (
          <span className="text-sm text-muted-foreground">{role.description}</span>
        )}
      </div>

      <div className="space-y-3">
        {Object.entries(grouped).map(([resource, perms]) => (
          <div key={resource} className="border rounded-md overflow-hidden">
            <div className="bg-muted/50 px-3 py-2">
              <p className="text-xs font-semibold uppercase tracking-wide">{resource}</p>
            </div>
            <div className="divide-y">
              {perms.map((p) => {
                const has = role.permission_codes.includes(p.code);
                return (
                  <div key={p.code} className="flex items-center justify-between px-3 py-2">
                    <div>
                      <span className="text-sm">{p.description ?? p.code}</span>
                      <span className="ml-2 text-xs text-muted-foreground">
                        ({ACTION_LABELS[p.action] ?? p.action})
                      </span>
                    </div>
                    {has ? (
                      <CheckCircle2 className="h-4 w-4 text-green-500" />
                    ) : (
                      <XCircle className="h-4 w-4 text-muted-foreground/40" />
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        ))}
        {Object.keys(grouped).length === 0 && (
          <p className="text-sm text-muted-foreground text-center py-8">Đang tải danh sách quyền...</p>
        )}
      </div>
    </div>
  );
}
