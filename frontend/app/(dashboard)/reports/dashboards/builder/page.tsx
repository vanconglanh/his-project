import type { Metadata } from "next";
import { DashboardBuilderClient } from "../_components/DashboardBuilderClient";

export const metadata: Metadata = { title: "Tạo bảng điều khiển" };

interface DashboardBuilderPageProps {
  searchParams: Promise<{ edit?: string }>;
}

export default async function DashboardBuilderPage({ searchParams }: DashboardBuilderPageProps) {
  const { edit } = await searchParams;
  return <DashboardBuilderClient editId={edit} />;
}
