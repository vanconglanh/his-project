import type { Metadata } from "next";
import { CashierPageClient } from "./_components/CashierPageClient";

export const metadata: Metadata = { title: "Thu ngân" };

export default function CashierPage() {
  return <CashierPageClient />;
}
