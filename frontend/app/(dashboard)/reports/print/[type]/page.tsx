import type { Metadata } from "next";
import ReportPrintClient from "./_components/ReportPrintClient";

type ReportType = "financial" | "clinical" | "pharmacy";

const META_LABEL: Record<ReportType, string> = {
  financial: "doanh thu",
  clinical: "lượt khám",
  pharmacy: "tồn kho",
};

interface PageProps {
  params: Promise<{ type: string }>;
  searchParams: Promise<{ from?: string; to?: string; clinicId?: string }>;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { type } = await params;
  const label = type in META_LABEL ? META_LABEL[type as ReportType] : "báo cáo";
  return { title: `In báo cáo ${label} — dIaB HIS` };
}

/**
 * Route preview in báo cáo A4 dọc.
 * Server shell — gọi Client Component để fetch BE với JWT từ localStorage.
 */
export default function ReportPrintPage({ params, searchParams }: PageProps) {
  return (
    <ReportPrintClient
      paramsPromise={params}
      searchParamsPromise={searchParams}
    />
  );
}
