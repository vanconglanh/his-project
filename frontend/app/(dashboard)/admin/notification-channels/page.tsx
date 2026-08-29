import type { Metadata } from "next";
import { NotificationChannelsManager } from "@/components/domain/NotificationChannelsManager";

export const metadata: Metadata = { title: "Kênh nhắc lịch (SMS/Zalo)" };

export default function NotificationChannelsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Kênh nhắc lịch hẹn (SMS / Zalo ZNS)</h2>
        <p className="text-sm text-muted-foreground">
          Cấu hình kết nối nhà cung cấp SMS (eSMS) và Zalo Notification Service để gửi nhắc lịch hẹn tự động.
          Thông tin bí mật được mã hóa; có thể đổi/reset bất cứ lúc nào mà không cần triển khai lại.
        </p>
      </div>
      <NotificationChannelsManager />
    </div>
  );
}
