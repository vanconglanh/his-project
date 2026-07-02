import type { Metadata } from "next";
import { BillingDetailClient } from "./_components/BillingDetailClient";

export const metadata: Metadata = { title: "Chi tiết hoá đơn" };

export default function BillingDetailPage({ params }: { params: { id: string } }) {
  return <BillingDetailClient id={params.id} />;
}
