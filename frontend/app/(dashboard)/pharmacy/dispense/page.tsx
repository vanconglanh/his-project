import type { Metadata } from "next";
import { DispenseTab } from "../_components/DispenseTab";

export const metadata: Metadata = { title: "Phát thuốc" };

export default function PharmacyDispensePage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Phát thuốc</h2>
        <p className="text-sm text-muted-foreground">
          Hàng chờ phát thuốc, lịch sử phát, hoàn trả thuốc
        </p>
      </div>
      <DispenseTab />
    </div>
  );
}
