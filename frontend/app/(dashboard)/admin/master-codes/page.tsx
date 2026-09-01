import type { Metadata } from "next";
import { MasterCodesPageClient } from "./_components/MasterCodesPageClient";

export const metadata: Metadata = { title: "Danh mục mã" };

export default function MasterCodesPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Danh mục mã</h2>
        <p className="text-sm text-muted-foreground">
          Quản lý các nhóm mã dùng chung trong hệ thống. Có thể thêm/sửa/ẩn mã riêng cho phòng
          khám mà không ảnh hưởng đến các phòng khám khác.
        </p>
      </div>
      <MasterCodesPageClient />
    </div>
  );
}
