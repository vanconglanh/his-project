import { cn } from "@/lib/utils";
import type { ButtonHTMLAttributes } from "react";

interface BigButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger" | "outline";
  fullWidth?: boolean;
}

const variantClasses: Record<NonNullable<BigButtonProps["variant"]>, string> = {
  primary: "bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800",
  secondary: "bg-slate-100 text-slate-900 hover:bg-slate-200 active:bg-slate-300",
  danger: "bg-red-600 text-white hover:bg-red-700 active:bg-red-800",
  outline: "border-2 border-blue-600 text-blue-600 bg-white hover:bg-blue-50",
};

/** Nút bấm cỡ lớn (min-height 56px) thân thiện người lớn tuổi, luôn có label chữ rõ ràng */
export function BigButton({
  variant = "primary",
  fullWidth = true,
  className,
  disabled,
  children,
  ...rest
}: BigButtonProps) {
  return (
    <button
      className={cn(
        "min-h-14 rounded-2xl px-6 text-lg font-semibold transition-colors",
        "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-blue-500 focus-visible:ring-offset-2",
        "disabled:cursor-not-allowed disabled:opacity-50",
        fullWidth && "w-full",
        variantClasses[variant],
        className,
      )}
      disabled={disabled}
      {...rest}
    >
      {children}
    </button>
  );
}
