import type { Metadata } from "next";
import { StockTransferDetailClient } from "./_components/StockTransferDetailClient";

export const metadata: Metadata = { title: "Chi tiết phiếu điều chuyển kho" };

export default async function StockTransferDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return (
    <div className="space-y-6">
      <StockTransferDetailClient id={id} />
    </div>
  );
}
