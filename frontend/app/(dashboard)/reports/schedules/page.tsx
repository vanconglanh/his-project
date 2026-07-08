import type { Metadata } from "next";
import { SchedulesPageClient } from "./_components/SchedulesPageClient";

export const metadata: Metadata = { title: "Lịch báo cáo" };

export default function ReportSchedulesPage() {
  return <SchedulesPageClient />;
}
