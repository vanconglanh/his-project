import type { Metadata } from "next";
import { ChainDashboardClient } from "./_components/ChainDashboardClient";

export const metadata: Metadata = { title: "Dashboard chuỗi chi nhánh" };

export default function ChainDashboardPage() {
  return <ChainDashboardClient />;
}
