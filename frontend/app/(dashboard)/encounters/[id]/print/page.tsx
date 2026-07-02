import type { Metadata } from "next";
import EncounterPrintClient from "./_components/EncounterPrintClient";

export const metadata: Metadata = { title: "In phiếu khám — Pro-Diab HIS" };

interface Props {
  params: Promise<{ id: string }>;
}

export default async function EncounterPrintPage({ params }: Props) {
  const { id } = await params;
  return <EncounterPrintClient encounterId={id} />;
}
