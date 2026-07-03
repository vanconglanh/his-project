import type { Metadata } from "next";
import { PageHeader } from "@/components/ui/page-header";
import { PharmacyPageClient } from "./_components/PharmacyPageClient";

export const metadata: Metadata = { title: "Kho dược" };

export default function PharmacyPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Kho dược"
        description="Quản lý tồn kho, nhập xuất, phát thuốc, kiểm kê"
      />
      <PharmacyPageClient />
    </div>
  );
}
