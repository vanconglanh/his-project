import type { Metadata } from "next";
import { EncountersPageClient } from "./_components/EncountersPageClient";

export const metadata: Metadata = { title: "Khám bệnh — Pro-Diab HIS" };

export default function EncountersPage() {
  return <EncountersPageClient />;
}
