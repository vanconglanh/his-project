import type { Metadata } from "next";
import { LabRadPageClient } from "./_components/LabRadPageClient";

export const metadata: Metadata = { title: "Cận lâm sàng (CLS)" };

export default function LabRadPage() {
  return <LabRadPageClient />;
}
