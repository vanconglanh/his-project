import type { Metadata } from "next";
import { ReportBuilderClient } from "./_components/ReportBuilderClient";

export const metadata: Metadata = { title: "Trình tạo báo cáo" };

interface ReportBuilderPageProps {
  searchParams: Promise<{ edit?: string }>;
}

export default async function ReportBuilderPage({ searchParams }: ReportBuilderPageProps) {
  const { edit } = await searchParams;
  return <ReportBuilderClient editId={edit} />;
}
