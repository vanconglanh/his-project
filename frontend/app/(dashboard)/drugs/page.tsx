import type { Metadata } from "next";
import { PageHeader } from "@/components/ui/page-header";
import { DrugsPageClient } from "./_components/DrugsPageClient";

export const metadata: Metadata = { title: "Danh mục thuốc" };

export default function DrugsPage() {
  return (
    <div className="space-y-6">
      <PageHeader title="Danh mục thuốc" description="Quản lý danh mục thuốc của phòng khám" />
      <DrugsPageClient />
    </div>
  );
}
