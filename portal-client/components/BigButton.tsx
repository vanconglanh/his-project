"use client";

import type { ButtonHTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/utils";

type Variant = "primary" | "secondary" | "outline" | "danger" | "ghost";

interface BigButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  icon?: ReactNode;
  fullWidth?: boolean;
}

const variantClasses: Record<Variant, string> = {
  primary: "bg-[--color-primary] text-white hover:bg-[--color-primary-dark] active:scale-[0.98]",
  secondary: "bg-[--color-secondary] text-[--color-secondary-fg] hover:opacity-90 active:scale-[0.98]",
  outline: "border-2 border-[--color-primary] text-[--color-primary] bg-white hover:bg-[--color-primary-soft]",
  danger: "bg-[--color-danger] text-white hover:opacity-90 active:scale-[0.98]",
  ghost: "bg-transparent text-[--color-text] hover:bg-black/5",
};

export function BigButton({
  variant = "primary",
  icon,
  fullWidth = true,
  className,
  children,
  ...rest
}: BigButtonProps) {
  return (
    <button
      className={cn(
        "flex min-h-[56px] items-center justify-center gap-2 rounded-2xl px-6 text-lg font-semibold",
        "transition-all duration-150 focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus] focus-visible:ring-offset-2",
        "disabled:cursor-not-allowed disabled:opacity-50 disabled:active:scale-100",
        fullWidth && "w-full",
        variantClasses[variant],
        className
      )}
      {...rest}
    >
      {icon}
      <span>{children}</span>
    </button>
  );
}
