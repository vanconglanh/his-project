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
        "border-red-300 bg-red-50 text-red-800 dark:border-red-800 dark:bg-red-950/30 dark:text-red-400",
        className
      )}
    >
      <AlertTriangle className="h-4 w-4 text-red-600 dark:text-red-400" />
      <AlertDescription className="text-sm font-medium">
        Cảnh báo TT 46/2018/TT-BYT: Lượt khám đã kéo dài{" "}
        <strong>{hoursOpen.toFixed(1)} giờ</strong> (bắt đầu{" "}
        {startedDate.toLocaleString("vi-VN")}). Cần xử lý hoặc đóng lượt khám.
      </AlertDescription>
    </Alert>
  );
}
