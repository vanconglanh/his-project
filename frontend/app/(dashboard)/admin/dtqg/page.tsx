import type { Metadata } from "next";
import { DtqgAdminClient } from "./_components/DtqgAdminClient";

export const metadata: Metadata = { title: "Quản trị ĐTQG" };

export default function DtqgAdminPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Đơn Thuốc Quốc Gia (ĐTQG)</h2>
        <p className="text-sm text-muted-foreground">Cấu hình kết nối và lịch sử gửi ĐTQG</p>
      </div>
      <DtqgAdminClient />
    </div>
  );
}
