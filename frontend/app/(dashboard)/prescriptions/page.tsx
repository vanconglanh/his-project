import type { Metadata } from "next";
import { PrescriptionsPageClient } from "./_components/PrescriptionsPageClient";

export const metadata: Metadata = { title: "Kê đơn thuốc" };

export default function PrescriptionsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Kê đơn thuốc</h2>
        <p className="text-sm text-muted-foreground">Quản lý đơn thuốc bệnh nhân</p>
      </div>
      <PrescriptionsPageClient />
    </div>
  );
}
