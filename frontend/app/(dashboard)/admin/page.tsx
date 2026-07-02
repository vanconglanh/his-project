import type { Metadata } from "next";

export const metadata: Metadata = { title: "Quản trị" };

export default function AdminPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Quản trị hệ thống</h2>
        <p className="text-sm text-muted-foreground">
          Người dùng, phân quyền, cài đặt phòng khám
        </p>
      </div>
      <div className="flex h-64 items-center justify-center rounded-xl border border-dashed text-muted-foreground text-sm">
        Quản trị — Sprint 3
      </div>
    </div>
  );
}
