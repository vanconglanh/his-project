import type { Metadata } from "next";
import { NursePageClient } from "./_components/NursePageClient";

export const metadata: Metadata = { title: "Điều dưỡng — Pro-Diab HIS" };

export default function NursePage() {
  return <NursePageClient />;
}
