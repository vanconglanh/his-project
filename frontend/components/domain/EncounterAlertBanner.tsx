import { AlertTriangle } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { cn } from "@/lib/utils";

interface Props {
  hoursOpen: number;
  startedAt: string;
  className?: string;
}

export function EncounterAlertBanner({ hoursOpen, startedAt, className }: Props) {
  const startedDate = new Date(startedAt);
  return (
    <Alert
      className={cn(
        "border-[color:var(--status-critical)]/30 bg-[color:var(--status-critical)]/10 text-[color:var(--status-critical)]",
        className
      )}
    >
      <AlertTriangle className="h-4 w-4 text-[color:var(--status-critical)]" />
      <AlertDescription className="text-sm font-medium">
        Cảnh báo TT 46/2018/TT-BYT: Lượt khám đã kéo dài{" "}
        <strong>{hoursOpen.toFixed(1)} giờ</strong> (bắt đầu{" "}
        {startedDate.toLocaleString("vi-VN")}). Cần xử lý hoặc đóng lượt khám.
      </AlertDescription>
    </Alert>
  );
}
