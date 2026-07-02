import type { Metadata } from "next";
import { PrescriptionDetailClient } from "./_components/PrescriptionDetailClient";

export const metadata: Metadata = { title: "Chi tiết đơn thuốc" };

interface Props {
  params: Promise<{ id: string }>;
}

export default async function PrescriptionDetailPage({ params }: Props) {
  const { id } = await params;
  return <PrescriptionDetailClient prescriptionId={id} />;
}
