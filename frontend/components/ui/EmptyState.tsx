import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  Users,
  Stethoscope,
  ClipboardList,
  Pill,
  Receipt,
  FlaskConical,
  FileText,
  Search,
  Inbox,
} from "lucide-react";

export type EmptyStateVariant =
  | "patients"
  | "encounters"
  | "prescriptions"
  | "pharmacy"
  | "cashier"
  | "labrad"
  | "search"
  | "generic";

interface EmptyStateProps {
  variant?: EmptyStateVariant;
  title?: string;
  description?: string;
  action?: {
    label: string;
    onClick: () => void;
  };
  className?: string;
}

const VARIANT_CONFIG: Record<
  EmptyStateVariant,
  {
    icon: React.ElementType;
    title: string;
    description: string;
    color: string;
    bgColor: string;
  }
> = {
  patients: {
    icon: Users,
    title: "Chưa có bệnh nhân",
    description: "Tạo hồ sơ bệnh nhân đầu tiên để bắt đầu quản lý khám bệnh.",
    color: "text-blue-500",
    bgColor: "bg-blue-50 dark:bg-blue-950/30",
  },
  encounters: {
    icon: Stethoscope,
    title: "Chưa có lượt khám",
    description: "Chưa có lượt khám nào. Tiếp đón bệnh nhân để tạo lượt khám mới.",
    color: "text-teal-500",
    bgColor: "bg-teal-50 dark:bg-teal-950/30",
  },
  prescriptions: {
    icon: ClipboardList,
    title: "Chưa có đơn thuốc",
    description: "Chưa có đơn thuốc nào được tạo trong khoảng thời gian này.",
    color: "text-violet-500",
    bgColor: "bg-violet-50 dark:bg-violet-950/30",
  },
  pharmacy: {
    icon: Pill,
    title: "Tồn kho trống",
    description: "Chưa có thuốc trong kho. Thêm phiếu nhập kho để bắt đầu.",
    color: "text-green-500",
    bgColor: "bg-green-50 dark:bg-green-950/30",
  },
  cashier: {
    icon: Receipt,
    title: "Chưa có hoá đơn",
    description: "Chưa có hoá đơn nào trong ca làm việc này.",
    color: "text-amber-500",
    bgColor: "bg-amber-50 dark:bg-amber-950/30",
  },
  labrad: {
    icon: FlaskConical,
    title: "Chưa có kết quả CLS",
    description: "Chưa có kết quả xét nghiệm hoặc CĐHA nào.",
    color: "text-rose-500",
    bgColor: "bg-rose-50 dark:bg-rose-950/30",
  },
  search: {
    icon: Search,
    title: "Không tìm thấy kết quả",
    description: "Thử thay đổi từ khoá hoặc bộ lọc tìm kiếm.",
    color: "text-muted-foreground",
    bgColor: "bg-muted/30",
  },
  generic: {
    icon: Inbox,
    title: "Không có dữ liệu",
    description: "Chưa có dữ liệu nào để hiển thị.",
    color: "text-muted-foreground",
    bgColor: "bg-muted/30",
  },
};

export function EmptyState({
  variant = "generic",
  title,
  description,
  action,
  className,
}: EmptyStateProps) {
  const config = VARIANT_CONFIG[variant];
  const Icon = config.icon;

  return (
    <div
      className={cn(
        "flex flex-col items-center justify-center py-16 px-6 text-center",
        className
      )}
    >
      {/* Illustration circle */}
      <div
        className={cn(
          "flex h-20 w-20 items-center justify-center rounded-full mb-5",
          config.bgColor
        )}
      >
        {/* SVG Medical illustration */}
        <Icon className={cn("h-10 w-10", config.color)} aria-hidden="true" />
      </div>

      <h3 className="text-base font-semibold text-foreground mb-1.5">
        {title ?? config.title}
      </h3>
      <p className="text-sm text-muted-foreground max-w-xs leading-relaxed">
        {description ?? config.description}
      </p>

      {action && (
        <Button onClick={action.onClick} className="mt-5" size="sm">
          {action.label}
        </Button>
      )}
    </div>
  );
}
