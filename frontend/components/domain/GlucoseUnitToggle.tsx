"use client";

import { cn } from "@/lib/utils";
import { useGlucoseUnitStore } from "@/lib/stores/glucose-unit-store";
import type { GlucoseUnit } from "@/lib/utils/glucose";

const UNITS: GlucoseUnit[] = ["mmol/L", "mg/dL"];

interface Props {
  className?: string;
  /** Nhãn phụ trước bộ chọn (vd "Đơn vị đường huyết"). */
  label?: string;
}

/**
 * BM-69: Bộ chọn đơn vị đường huyết dùng chung. Thay đổi ở bất kỳ màn nào sẽ
 * đồng bộ sang mọi màn khác (store dùng chung), đảm bảo hiển thị nhất quán.
 */
export function GlucoseUnitToggle({ className, label }: Props) {
  const unit = useGlucoseUnitStore((s) => s.unit);
  const setUnit = useGlucoseUnitStore((s) => s.setUnit);

  return (
    <div className={cn("flex items-center gap-1.5", className)}>
      {label && <span className="text-xs text-muted-foreground">{label}</span>}
      <div
        role="group"
        aria-label="Chọn đơn vị đường huyết"
        className="inline-flex rounded-md border bg-muted/40 p-0.5"
      >
        {UNITS.map((u) => (
          <button
            key={u}
            type="button"
            aria-pressed={unit === u}
            onClick={() => setUnit(u)}
            className={cn(
              "rounded px-2 py-0.5 text-xs font-medium transition-colors",
              unit === u
                ? "bg-background text-foreground shadow-sm"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            {u}
          </button>
        ))}
      </div>
    </div>
  );
}
