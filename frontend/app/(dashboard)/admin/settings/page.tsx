import type { Metadata } from "next";
import { SettingsPageClient } from "./_components/SettingsPageClient";

export const metadata: Metadata = { title: "Cấu hình hệ thống" };

export default function SettingsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Cấu hình hệ thống</h2>
        <p className="text-sm text-muted-foreground">
          Các tham số nghiệp vụ có thể tuỳ chỉnh cho phòng khám (ngưỡng duyệt, thông báo,...).
        </p>
      </div>
      <SettingsPageClient />
    </div>
  );
}
