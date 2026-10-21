import type { Metadata } from "next";
import { VouchersPageClient } from "./_components/VouchersPageClient";

export const metadata: Metadata = { title: "Voucher giảm giá" };

export default function VouchersAdminPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Voucher giảm giá</h2>
        <p className="text-sm text-muted-foreground">
          Quản lý mã giảm giá theo phần trăm. Thu ngân nhập mã ở hoá đơn nháp để áp dụng.
        </p>
      </div>
      <VouchersPageClient />
    </div>
  );
}
