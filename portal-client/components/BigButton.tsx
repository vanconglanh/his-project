import { cn } from "@/lib/utils";
import type { ButtonHTMLAttributes } from "react";

interface BigButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger" | "outline";
  fullWidth?: boolean;
}

const variantClasses: Record<NonNullable<BigButtonProps["variant"]>, string> = {
  primary: "bg-gradient-to-b from-[#0a8578] to-[#01645A] text-white shadow-md hover:brightness-105 active:brightness-95",
  secondary: "bg-slate-100 text-slate-900 hover:bg-slate-200 active:bg-slate-300",
  danger: "bg-red-600 text-white hover:bg-red-700 active:bg-red-800",
  outline: "border-2 border-[#01645A] text-[#01645A] bg-white hover:bg-teal-50",
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
        "min-h-14 rounded-full px-6 text-lg font-semibold transition-all",
        "focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-teal-600 focus-visible:ring-offset-2",
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
