import type { Metadata } from "next";
import { ReportEngineClient } from "./_components/engine/ReportEngineClient";

export const metadata: Metadata = { title: "Báo cáo & Thống kê" };

interface ReportsPageProps {
  searchParams: Promise<{ report?: string }>;
}

export default async function ReportsPage({ searchParams }: ReportsPageProps) {
  const { report } = await searchParams;
  return <ReportEngineClient initialReportCode={report} />;
}
