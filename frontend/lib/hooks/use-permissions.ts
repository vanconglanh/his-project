"use client";

import { useAuthStore } from "@/lib/stores/auth-store";

export function usePermissions() {
  const roles = useAuthStore((s) => s.roles);
  const permissions = useAuthStore((s) => s.permissions);
  // Accept cả role CODE (admin/SUPER_ADMIN) lẫn role NAME tiếng Việt (Quản trị viên)
  const SUPER_ROLES = new Set([
    "SUPER_ADMIN",
    "admin",
    "ADMIN",
    "Quản trị viên",
    "Quản trị viên Hệ thống",
  ]);
  const isSuperAdmin = roles.some((r) => SUPER_ROLES.has(r));

  return {
    permissions,
    roles,
    isSuperAdmin,
    has: (code: string) => isSuperAdmin || permissions.includes(code),
    hasAny: (codes: string[]) =>
      isSuperAdmin || codes.some((c) => permissions.includes(c)),
  };
}
