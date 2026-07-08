import type { Metadata } from "next";
import { DiabetesDashboardClient } from "./_components/DiabetesDashboardClient";

export const metadata: Metadata = { title: "Xu hướng ĐTĐ" };

interface Props {
  params: Promise<{ id: string }>;
}

export default async function PatientDiabetesDashboardPage({ params }: Props) {
  const { id } = await params;
  return <DiabetesDashboardClient patientId={id} />;
}
