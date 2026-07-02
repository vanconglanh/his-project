import type { Metadata } from "next";
import { ReportsPageClient } from "./_components/ReportsPageClient";

export const metadata: Metadata = { title: "Báo cáo & Thống kê" };

export default function ReportsPage() {
  return <ReportsPageClient />;
}
