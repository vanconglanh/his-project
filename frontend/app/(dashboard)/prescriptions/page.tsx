import type { Metadata } from "next";
import { PrescriptionsPageClient } from "./_components/PrescriptionsPageClient";

export const metadata: Metadata = { title: "Kê đơn thuốc" };

export default function PrescriptionsPage() {
  return <PrescriptionsPageClient />;
}
