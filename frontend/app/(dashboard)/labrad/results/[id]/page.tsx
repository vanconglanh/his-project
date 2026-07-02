import type { Metadata } from "next";
import { LabResultDetailClient } from "./_components/LabResultDetailClient";

export const metadata: Metadata = { title: "Chi tiết kết quả XN" };

interface Props {
  params: Promise<{ id: string }>;
}

export default async function LabResultDetailPage({ params }: Props) {
  const { id } = await params;
  return <LabResultDetailClient id={id} />;
}
