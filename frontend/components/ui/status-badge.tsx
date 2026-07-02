/**
 * HIS StatusBadge — badge trạng thái nghiệp vụ (encounter, CLS, đơn thuốc, BHYT)
 * 6 variant: waiting | progress | done | warning | critical | insurance
 * Spec: docs/design/research-his-ui-patterns.md mục 1 + 3
 */
import { Clock, Activity, CheckCircle, AlertTriangle, AlertCircle, Shield } from "lucide-react";
import { cn } from "@/lib/utils";

export type HisStatusVariant =
  | "waiting"
  | "progress"
  | "done"
  | "warning"
  | "critical"
  | "insurance";

const VARIANT_CONFIG: Record<
  HisStatusVariant,
  {
    icon: React.ComponentType<{ className?: string }>;
    classes: string;
    label: string;
  }
> = {
  waiting: {
    icon: Clock,
    classes:
      "bg-[color:var(--status-waiting)]/10 text-[color:var(--status-waiting)] border-[color:var(--status-waiting)]/30",
    label: "Chờ",
  },
  progress: {
    icon: Activity,
    classes:
      "bg-[color:var(--status-progress)]/10 text-[color:var(--status-progress)] border-[color:var(--status-progress)]/30",
    label: "Đang xử lý",
  },
  done: {
    icon: CheckCircle,
    classes:
      "bg-[color:var(--status-done)]/10 text-[color:var(--status-done)] border-[color:var(--status-done)]/30",
    label: "Hoàn tất",
  },
  warning: {
    icon: AlertTriangle,
    classes:
      "bg-[color:var(--status-warning)]/10 text-[color:var(--status-warning)] border-[color:var(--status-warning)]/30",
    label: "Cảnh báo",
  },
  critical: {
    icon: AlertCircle,
    classes:
      "bg-[color:var(--status-critical)]/10 text-[color:var(--status-critical)] border-[color:var(--status-critical)]/30",
    label: "Nguy cấp",
  },
  insurance: {
    icon: Shield,
    classes:
      "bg-[color:var(--status-insurance)]/10 text-[color:var(--status-insurance)] border-[color:var(--status-insurance)]/30",
    label: "BHYT",
  },
};

export interface StatusBadgeProps {
  variant: HisStatusVariant;
  /** Nội dung tuỳ chỉnh; nếu không truyền sẽ dùng label mặc định của variant */
  children?: React.ReactNode;
  /** Ẩn icon (mặc định hiện) */
  hideIcon?: boolean;
  className?: string;
}

/**
 * Badge trạng thái nghiệp vụ HIS — LUÔN có icon + label (WCAG 1.4.1)
 *
 * @example
 * <StatusBadge variant="waiting">Chờ tiếp đón</StatusBadge>
 * <StatusBadge variant="done" />
 * <StatusBadge variant="insurance">BHYT còn hạn</StatusBadge>
 */
export function HisStatusBadge({
  variant,
  children,
  hideIcon = false,
  className,
}: StatusBadgeProps) {
  const config = VARIANT_CONFIG[variant];
  const Icon = config.icon;

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium leading-none",
        config.classes,
        className
      )}
      role="status"
      aria-label={typeof children === "string" ? children : config.label}
    >
      {!hideIcon && <Icon className="h-3 w-3 shrink-0" aria-hidden="true" />}
      <span>{children ?? config.label}</span>
    </span>
  );
}
