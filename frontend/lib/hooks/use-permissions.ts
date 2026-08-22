"use client";

import { useAuthStore } from "@/lib/stores/auth-store";

export function usePermissions() {
  const roles = useAuthStore((s) => s.roles);
  const permissions = useAuthStore((s) => s.permissions);
  // Chỉ nhận diện super admin qua ROLE CODE ổn định (vd "ADMIN", "SUPER_ADMIN").
  // KHÔNG so theo tên hiển thị tiếng Việt (vd "Quản trị viên") — role tùy chỉnh
  // trùng tên hiển thị sẽ không còn bị coi nhầm là super admin ở UI.
  const SUPER_ROLE_CODES = new Set(["SUPER_ADMIN", "ADMIN"]);
  const isSuperAdmin = roles.some((r) => SUPER_ROLE_CODES.has(r.toUpperCase()));

  return {
    permissions,
    roles,
    isSuperAdmin,
    has: (code: string) => isSuperAdmin || permissions.includes(code),
    hasAny: (codes: string[]) =>
      isSuperAdmin || codes.some((c) => permissions.includes(c)),
  };
}
