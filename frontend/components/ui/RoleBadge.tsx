import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

// Role code theo seed DB (9001-series): snake_case thường
const ROLE_CONFIG: Record<string, { label: string; className: string }> = {
  super_admin: { label: "Siêu quản trị", className: "bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200" },
  admin: { label: "Quản trị", className: "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200" },
  bac_si: { label: "Bác sĩ", className: "bg-teal-100 text-teal-800 dark:bg-teal-900 dark:text-teal-200" },
  dieu_duong: { label: "Điều dưỡng", className: "bg-cyan-100 text-cyan-800 dark:bg-cyan-900 dark:text-cyan-200" },
  le_tan: { label: "Lễ tân", className: "bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200" },
  duoc_si: { label: "Dược sĩ", className: "bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200" },
  ke_toan: { label: "Kế toán", className: "bg-pink-100 text-pink-800 dark:bg-pink-900 dark:text-pink-200" },
  ky_thuat_vien: { label: "Kỹ thuật viên", className: "bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200" },
};

interface RoleBadgeProps {
  code: string;
  name?: string;
  className?: string;
}

export function RoleBadge({ code, name, className }: RoleBadgeProps) {
  const config = ROLE_CONFIG[code];
  return (
    <Badge
      variant="secondary"
      className={cn("text-xs font-medium", config?.className ?? "", className)}
    >
      {config?.label ?? name ?? code}
    </Badge>
  );
}
