import type { Metadata } from "next";
import { Icd10PageClient } from "./_components/Icd10PageClient";

export const metadata: Metadata = { title: "Tra cứu ICD-10 — Pro-Diab HIS" };

export default function Icd10Page() {
  return <Icd10PageClient />;
}
