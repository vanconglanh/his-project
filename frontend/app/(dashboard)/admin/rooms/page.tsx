import type { Metadata } from "next";
import { RoomsPageClient } from "./_components/RoomsPageClient";

export const metadata: Metadata = { title: "Danh mục phòng khám" };

export default function RoomsAdminPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Danh mục phòng khám</h2>
        <p className="text-sm text-muted-foreground">
          Thêm, sửa, xóa phòng khám. Phòng đang hoạt động sẽ hiện ở form Tiếp đón.
        </p>
      </div>
      <RoomsPageClient />
    </div>
  );
}
