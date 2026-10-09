"use client";

/**
 * BM-62: Layout guard cho toàn bộ khu vực /admin.
 * Chỉ SUPER_ADMIN / admin mới được truy cập. Các role khác bị redirect về "/".
 */

import { RequirePermission } from "@/components/auth/RequirePermission";

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  return (
    <RequirePermission role="SUPER_ADMIN" redirectTo="/">
      {children}
    </RequirePermission>
  );
}
