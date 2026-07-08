import type { Metadata } from "next";
import { DashboardsListClient } from "./_components/DashboardsListClient";

export const metadata: Metadata = { title: "Bảng điều khiển" };

export default function ReportDashboardsPage() {
  return <DashboardsListClient />;
}
