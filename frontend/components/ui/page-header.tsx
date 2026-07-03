/**
 * PageHeader — tiêu đề trang chuẩn cho các màn hình dashboard (list/detail).
 * Một nguồn chân lý cho pattern "title + mô tả + actions phải" → tránh lệch
 * cỡ chữ/khoảng cách giữa các trang (finding F-09).
 * Chuẩn: docs/design/design-system-standards.md mục 2 — page title `text-xl font-bold`.
 *
 * Component thuần trình bày — KHÔNG chứa logic nghiệp vụ. Trang cha tự quyết
 * định `actions` (thường là 1 Button primary + tuỳ chọn 1-2 nút phụ).
 */
import { cn } from "@/lib/utils";

export interface PageHeaderProps {
  /** Tiêu đề trang — luôn render `text-xl font-bold` theo design system */
  title: string;
  /** Mô tả ngắn dưới tiêu đề */
  description?: string;
  /** Slot nút hành động, căn phải (vd: nút "Thêm mới") */
  actions?: React.ReactNode;
  className?: string;
}

/**
 * @example
 * <PageHeader
 *   title="Bệnh nhân"
 *   description="Quản lý hồ sơ bệnh nhân trong hệ thống"
 *   actions={<Button onClick={onCreate}><Plus className="h-4 w-4 mr-1" />Thêm bệnh nhân</Button>}
 * />
 */
export function PageHeader({ title, description, actions, className }: PageHeaderProps) {
  return (
    <div className={cn("flex flex-wrap items-start justify-between gap-3", className)}>
      <div className="min-w-0">
        <h1 className="text-xl font-bold tracking-tight truncate">{title}</h1>
        {description && (
          <p className="text-sm text-muted-foreground mt-1">{description}</p>
        )}
      </div>
      {actions && <div className="flex items-center gap-2 shrink-0">{actions}</div>}
    </div>
  );
}
