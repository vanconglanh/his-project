import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

const ROLE_CONFIG: Record<string, { label: string; className: string }> = {
  SUPER_ADMIN: { label: "Siêu quản trị", className: "bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200" },
  ADMIN: { label: "Quản trị", className: "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200" },
  BACSI: { label: "Bác sĩ", className: "bg-teal-100 text-teal-800 dark:bg-teal-900 dark:text-teal-200" },
  DIEUDUONG: { label: "Điều dưỡng", className: "bg-cyan-100 text-cyan-800 dark:bg-cyan-900 dark:text-cyan-200" },
  LETAN: { label: "Lễ tân", className: "bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200" },
  DUOCSI: { label: "Dược sĩ", className: "bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200" },
  KETOAN: { label: "Kế toán", className: "bg-pink-100 text-pink-800 dark:bg-pink-900 dark:text-pink-200" },
  KYTHUATVIEN: { label: "Kỹ thuật viên", className: "bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200" },
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
