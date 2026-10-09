"use client";

/**
 * BM-62: Guard component — redirect về trang chính nếu user không có quyền truy cập.
 * Dùng trong các page cần bảo vệ theo permission/role:
 *
 *   <RequirePermission permission="admin.view">
 *     <AdminPage />
 *   </RequirePermission>
 *
 *   <RequirePermission role="SUPER_ADMIN" redirectTo="/dashboard">
 *     <SuperAdminPage />
 *   </RequirePermission>
 */

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { useAuthStore } from "@/lib/stores/auth-store";
import { Loader2 } from "lucide-react";

interface RequirePermissionProps {
  children: React.ReactNode;
  /** Permission code cần kiểm tra (ví dụ: "admin.view", "user.manage") */
  permission?: string;
  /** Role code cần kiểm tra (ví dụ: "SUPER_ADMIN", "admin") */
  role?: string;
  /** URL redirect nếu không có quyền. Mặc định "/" */
  redirectTo?: string;
}

export function RequirePermission({
  children,
  permission,
  role,
  redirectTo = "/",
}: RequirePermissionProps) {
  const router = useRouter();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const { has, roles, isSuperAdmin } = usePermissions();

  const hasAccess =
    isSuperAdmin ||
    (permission ? has(permission) : true) &&
    (role ? roles.includes(role) : true);

  useEffect(() => {
    // Nếu đã xác thực nhưng không có quyền → redirect
    if (isAuthenticated && !hasAccess) {
      router.replace(redirectTo);
    }
  }, [isAuthenticated, hasAccess, redirectTo, router]);

  // Chưa xác thực (đang load) → không render gì
  if (!isAuthenticated) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  // Không có quyền → không render (đang redirect)
  if (!hasAccess) {
    return null;
  }

  return <>{children}</>;
}
