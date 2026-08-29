import type { Metadata } from "next";
import { PageHeader } from "@/components/ui/page-header";
import { StockTransfersPageClient } from "./_components/StockTransfersPageClient";

export const metadata: Metadata = { title: "Điều chuyển kho" };

export default function StockTransfersPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Điều chuyển kho nội bộ"
        description="Điều chuyển thuốc/vật tư giữa các chi nhánh trong cùng tenant"
      />
      <StockTransfersPageClient />
    </div>
  );
}
