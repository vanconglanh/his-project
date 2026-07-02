/**
 * StickyActionBar — thanh action cố định dưới cùng form dài
 * Spec: docs/design/research-his-ui-patterns.md mục 3
 * Dùng CSS var --bg-elevated để nổi lên trên nội dung bên dưới
 */
import { cn } from "@/lib/utils";

export interface StickyActionBarProps {
  /** Slot trái — thường là hint text / phím tắt hint */
  left?: React.ReactNode;
  /** Slot phải — các nút action (Huỷ, Lưu, Tiếp nhận, ...) */
  children: React.ReactNode;
  className?: string;
}

/**
 * Wrapper sticky bottom cho action buttons trong form dài.
 * Tự động full-bleed trên chiều ngang (negative margin -mx-6).
 *
 * @example
 * <StickyActionBar left={<span className="text-xs text-muted-foreground">F2 Tiếp nhận • Esc Huỷ</span>}>
 *   <Button variant="ghost">Huỷ</Button>
 *   <Button>Lưu</Button>
 * </StickyActionBar>
 */
export function StickyActionBar({ left, children, className }: StickyActionBarProps) {
  return (
    <div
      className={cn(
        "sticky bottom-0 left-0 right-0 z-20",
        "-mx-6 px-6 py-3",
        "bg-[color:var(--bg-elevated)] border-t border-[color:var(--border-default)]",
        "flex items-center justify-end gap-2",
        className
      )}
      role="toolbar"
      aria-label="Thanh hành động"
    >
      {left && (
        <div className="mr-auto text-xs text-[color:var(--text-muted)]">{left}</div>
      )}
      {children}
    </div>
  );
}
