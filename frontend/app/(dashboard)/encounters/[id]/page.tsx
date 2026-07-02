import type { Metadata } from "next";
import { EncounterDetailClient } from "./_components/EncounterDetailClient";

export const metadata: Metadata = { title: "Chi tiết lượt khám — Pro-Diab HIS" };

interface Props {
  params: Promise<{ id: string }>;
}

export default async function EncounterDetailPage({ params }: Props) {
  const { id } = await params;
  return <EncounterDetailClient encounterId={id} />;
}
