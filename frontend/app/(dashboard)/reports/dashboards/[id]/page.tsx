import type { Metadata } from "next";
import { DashboardViewerClient } from "../_components/DashboardViewerClient";

export const metadata: Metadata = { title: "Bảng điều khiển" };

interface DashboardViewerPageProps {
  params: Promise<{ id: string }>;
}

export default async function DashboardViewerPage({ params }: DashboardViewerPageProps) {
  const { id } = await params;
  return <DashboardViewerClient id={id} />;
}
