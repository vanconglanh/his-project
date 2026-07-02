import type { Metadata } from "next";
import { SuppliersPageClient } from "./_components/SuppliersPageClient";

export const metadata: Metadata = { title: "Nhà cung cấp" };

export default function SuppliersPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Nhà cung cấp</h2>
        <p className="text-sm text-muted-foreground">Quản lý danh sách nhà cung cấp thuốc</p>
      </div>
      <SuppliersPageClient />
    </div>
  );
}
