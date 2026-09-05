import type { Metadata } from "next";
import { PendingEncountersClient } from "./_components/PendingEncountersClient";

export const metadata: Metadata = { title: "Hàng chờ thu ngân" };

export default function CashierPendingPage() {
  return <PendingEncountersClient />;
}
