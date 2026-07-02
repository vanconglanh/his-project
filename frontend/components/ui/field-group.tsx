/**
 * FieldGroup — wrapper label-trên + slot input + hint + error inline
 * Spec: docs/design/research-his-ui-patterns.md mục 3
 * A11y: aria-describedby, aria-invalid, aria-required
 */
import { useId } from "react";
import { cn } from "@/lib/utils";

export interface FieldGroupProps {
  /** Nhãn hiển thị trên input */
  label: string;
  /** Trường bắt buộc — thêm dấu * đỏ + aria-required */
  required?: boolean;
  /** Gợi ý phụ (text-xs muted), hiện dưới input khi không có lỗi */
  hint?: string;
  /** Thông báo lỗi inline (text-xs critical) */
  error?: string;
  /** Input / Select / Textarea slot */
  children: React.ReactNode;
  className?: string;
  /** Override htmlFor nếu cần (mặc định auto-id) */
  htmlFor?: string;
}

/**
 * FieldGroup chuẩn hoá label trên + error inline cho mọi form HIS.
 *
 * @example
 * <FieldGroup label="Số thẻ BHYT" required hint="10 ký tự" error={errors.bhyt_card_no?.message}>
 *   <Input id={fieldId} {...register("bhyt_card_no")} />
 * </FieldGroup>
 */
export function FieldGroup({
  label,
  required,
  hint,
  error,
  children,
  className,
  htmlFor,
}: FieldGroupProps) {
  const autoId = useId();
  const descId = `${autoId}-desc`;
  const errId = `${autoId}-err`;
  const forId = htmlFor ?? autoId;

  return (
    <div className={cn("flex flex-col gap-1", className)}>
      {/* Label */}
      <label
        htmlFor={forId}
        className="text-sm font-medium text-[color:var(--text-primary)] leading-none"
      >
        {label}
        {required && (
          <span
            className="ml-0.5 text-[color:var(--status-critical)]"
            aria-hidden="true"
          >
            *
          </span>
        )}
      </label>

      {/* Input slot — truyền aria props xuống qua clone nếu cần */}
      <div
        aria-describedby={error ? errId : hint ? descId : undefined}
        aria-invalid={error ? "true" : undefined}
        aria-required={required ? "true" : undefined}
      >
        {children}
      </div>

      {/* Hint hoặc Error (error ưu tiên hơn hint) */}
      {error ? (
        <p
          id={errId}
          role="alert"
          className="text-xs text-[color:var(--status-critical)] leading-snug"
        >
          {error}
        </p>
      ) : hint ? (
        <p
          id={descId}
          className="text-xs text-[color:var(--text-muted)] leading-snug"
        >
          {hint}
        </p>
      ) : null}
    </div>
  );
}
