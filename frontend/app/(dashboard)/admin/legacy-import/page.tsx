import type { Metadata } from "next";
import { LegacyImportPageClient } from "./_components/LegacyImportPageClient";

export const metadata: Metadata = { title: "Nhập hồ sơ cũ (OCR)" };

export default function LegacyImportPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Nhập hồ sơ cũ (OCR)</h2>
        <p className="text-sm text-muted-foreground">
          Tải lên file ZIP ảnh scan hồ sơ giấy cũ, hệ thống OCR tự động rồi cho review từng ảnh
          trước khi lưu vào hồ sơ bệnh nhân
        </p>
      </div>
      <LegacyImportPageClient />
    </div>
  );
}
