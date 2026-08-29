import type { Metadata } from "next";
import { BranchesPageClient } from "./_components/BranchesPageClient";

export const metadata: Metadata = { title: "Chi nhánh" };

export default function BranchesPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Chi nhánh</h2>
        <p className="text-sm text-muted-foreground">
          Quản lý danh sách chi nhánh của phòng khám (đa chi nhánh)
        </p>
      </div>
      <BranchesPageClient />
    </div>
  );
}
