import type { Metadata } from "next";
import { BranchPricingPageClient } from "./_components/BranchPricingPageClient";

export const metadata: Metadata = { title: "Giá theo chi nhánh" };

export default function BranchPricingPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Giá theo chi nhánh</h2>
        <p className="text-sm text-muted-foreground">
          Override giá và ẩn/hiện dịch vụ, thuốc theo chi nhánh hoặc nhóm chi nhánh
        </p>
      </div>
      <BranchPricingPageClient />
    </div>
  );
}
