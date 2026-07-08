"use client";

import { cn } from "@/lib/utils";

interface NumPadProps {
  value: string;
  maxLength: number;
  onChange: (value: string) => void;
  label?: string;
}

const KEYS = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "xoa", "0", "clear"] as const;

/** Bàn phím số cỡ lớn để nhập PIN / mã kích hoạt / OTP — thân thiện người lớn tuổi */
export function NumPad({ value, maxLength, onChange, label }: NumPadProps) {
  function handleKey(key: (typeof KEYS)[number]) {
    if (key === "xoa") {
      onChange(value.slice(0, -1));
      return;
    }
    if (key === "clear") {
      onChange("");
      return;
    }
    if (value.length < maxLength) {
      onChange(value + key);
    }
  }

  return (
    <div className="w-full">
      <div
        role="status"
        aria-label={label ?? "Mã đã nhập"}
        className="mb-5 flex items-center justify-center gap-3"
      >
        {Array.from({ length: maxLength }).map((_, index) => (
          <span
            key={index}
            className={cn(
              "flex h-14 w-11 items-center justify-center rounded-xl border-2 text-2xl font-bold",
              index < value.length
                ? "border-[--color-primary] bg-[--color-primary-soft] text-[--color-primary]"
                : "border-[--color-border] bg-white text-transparent"
            )}
          >
            {value[index] ? "●" : "0"}
          </span>
        ))}
      </div>
      <div className="grid grid-cols-3 gap-3">
        {KEYS.map((key) => {
          if (key === "clear") {
            return (
              <button
                key={key}
                type="button"
                onClick={() => handleKey(key)}
                aria-label="Xoá hết"
                className="flex min-h-[64px] items-center justify-center rounded-2xl bg-[--color-surface-alt] text-base font-semibold text-[--color-text-muted] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus]"
              >
                Xoá hết
              </button>
            );
          }
          if (key === "xoa") {
            return (
              <button
                key={key}
                type="button"
                onClick={() => handleKey(key)}
                aria-label="Xoá một ký tự"
                className="flex min-h-[64px] items-center justify-center rounded-2xl bg-[--color-surface-alt] text-2xl font-semibold text-[--color-text-muted] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus]"
              >
                ⌫
              </button>
            );
          }
          return (
            <button
              key={key}
              type="button"
              onClick={() => handleKey(key)}
              aria-label={`Số ${key}`}
              className="flex min-h-[64px] items-center justify-center rounded-2xl bg-white text-2xl font-bold text-[--color-text] shadow-sm active:scale-95 focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus]"
            >
              {key}
            </button>
          );
        })}
      </div>
    </div>
  );
}
