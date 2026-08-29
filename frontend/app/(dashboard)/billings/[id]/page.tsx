import type { Metadata } from "next";
import { BillingDetailClient } from "./_components/BillingDetailClient";

export const metadata: Metadata = { title: "Chi tiết hoá đơn" };

export default async function BillingDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <BillingDetailClient id={id} />;
}
