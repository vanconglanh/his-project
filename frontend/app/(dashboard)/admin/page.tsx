import type { Metadata } from "next";
import { AdminHubClient } from "./_components/AdminHubClient";

export const metadata: Metadata = { title: "Quản trị" };

export default function AdminPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Quản trị hệ thống</h2>
        <p className="text-sm text-muted-foreground">
          Chọn khu vực để quản lý: người dùng, phân quyền, danh mục mã, cấu hình phòng khám.
        </p>
      </div>
      <AdminHubClient />
    </div>
  );
}
