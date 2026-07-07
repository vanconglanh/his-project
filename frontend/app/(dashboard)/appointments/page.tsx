import type { Metadata } from "next";
import { AppointmentsPageClient } from "./_components/AppointmentsPageClient";

export const metadata: Metadata = { title: "Lịch hẹn" };

export default function AppointmentsPage() {
  return <AppointmentsPageClient />;
}
