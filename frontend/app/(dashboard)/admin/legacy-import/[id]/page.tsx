import type { Metadata } from "next";
import { LegacyImportDetailClient } from "../_components/LegacyImportDetailClient";

export const metadata: Metadata = { title: "Chi tiết lô nhập hồ sơ cũ" };

export default async function LegacyImportDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <div className="space-y-6">
      <LegacyImportDetailClient batchId={id} />
    </div>
  );
}
