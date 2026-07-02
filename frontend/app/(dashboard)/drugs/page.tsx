import type { Metadata } from "next";
import { DrugsPageClient } from "./_components/DrugsPageClient";

export const metadata: Metadata = { title: "Danh mục thuốc" };

export default function DrugsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Danh mục thuốc</h2>
        <p className="text-sm text-muted-foreground">Quản lý danh mục thuốc của phòng khám</p>
      </div>
      <DrugsPageClient />
    </div>
  );
}
