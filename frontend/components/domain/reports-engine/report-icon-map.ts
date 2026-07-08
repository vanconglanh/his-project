import type { ComponentType } from "react";
import * as LucideIcons from "lucide-react";
import { FileText } from "lucide-react";

type IconComponent = ComponentType<{ className?: string }>;

function kebabToPascal(value: string): string {
  return value
    .split(/[-_\s]+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join("");
}

/**
 * Map icon key (kebab-case, vd "rotate-ccw") từ catalog[].icon sang component lucide-react.
 * Fallback an toàn về FileText khi tên icon không khớp bất kỳ export nào của lucide-react.
 */
export function getReportIcon(icon?: string | null): IconComponent {
  if (!icon) return FileText;
  const name = kebabToPascal(icon);
  const icons = LucideIcons as unknown as Record<string, IconComponent | undefined>;
  return icons[name] ?? FileText;
}

/** Nhãn tiếng Việt cho nhóm báo cáo (group trả về từ /reports/catalog). */
export const REPORT_GROUP_LABELS: Record<string, string> = {
  Financial: "Tài chính",
  Clinical: "Khám bệnh/Sổ",
  Statistics: "Thống kê",
  Pharmacy: "Kho dược",
  Bhyt: "BHYT",
};

export function getReportGroupLabel(group: string): string {
  return REPORT_GROUP_LABELS[group] ?? group;
}
