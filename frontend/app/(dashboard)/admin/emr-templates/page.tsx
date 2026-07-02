import type { Metadata } from "next";
import { EmrTemplatesPageClient } from "./_components/EmrTemplatesPageClient";

export const metadata: Metadata = { title: "Mẫu bệnh án — Pro-Diab HIS" };

export default function EmrTemplatesPage() {
  return <EmrTemplatesPageClient />;
}
