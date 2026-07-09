import { cn } from "@/lib/utils";

export type BadgeTone = "pending" | "confirmed" | "done" | "cancelled" | "high" | "low" | "ok";

const STYLE: Record<BadgeTone, string> = {
  pending: "bg-amber-100 text-amber-800",
  confirmed: "bg-teal-100 text-[#01645A]",
  done: "bg-slate-100 text-slate-600",
  cancelled: "bg-red-100 text-red-700",
  high: "bg-red-100 text-red-700",
  low: "bg-amber-100 text-amber-800",
  ok: "bg-teal-100 text-[#01645A]",
};

/** Badge trạng thái dùng chung: màu theo tone + luôn kèm nhãn tiếng Việt (không truyền tin chỉ bằng màu) */
export function StatusBadge({
  tone,
  label,
  className,
}: {
  tone: BadgeTone;
  label: string;
  className?: string;
}) {
  return (
    <span className={cn("rounded-full px-3 py-1 text-sm font-semibold", STYLE[tone], className)}>
      {label}
    </span>
  );
}
