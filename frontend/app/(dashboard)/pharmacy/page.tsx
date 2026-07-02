import type { Metadata } from "next";
import { PharmacyPageClient } from "./_components/PharmacyPageClient";

export const metadata: Metadata = { title: "Kho dược" };

export default function PharmacyPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Kho dược</h2>
        <p className="text-sm text-muted-foreground">
          Quản lý tồn kho, nhập xuất, phát thuốc, kiểm kê
        </p>
      </div>
      <PharmacyPageClient />
    </div>
  );
}
