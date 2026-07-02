import type { Metadata } from "next";
import { BillingsPageClient } from "./_components/BillingsPageClient";

export const metadata: Metadata = { title: "Hoá đơn" };

export default function BillingsPage() {
  return <BillingsPageClient />;
}
