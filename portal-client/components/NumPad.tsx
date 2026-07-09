"use client";

import { cn } from "@/lib/utils";

interface NumPadProps {
  value: string;
  onChange: (value: string) => void;
  maxLength?: number;
  label?: string;
}

const KEYS = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "xoa", "0", "xong"];

/** Bàn phím số cỡ lớn để nhập PIN/mã kích hoạt, thân thiện tablet và người lớn tuổi */
export function NumPad({ value, onChange, maxLength = 6, label }: NumPadProps) {
  function handleKey(key: string) {
    if (key === "xoa") {
      onChange(value.slice(0, -1));
      return;
    }
    if (key === "xong") {
      return;
    }
    if (value.length >= maxLength) return;
    onChange(value + key);
  }

  return (
    <div className="w-full">
      {label && (
        <p className="mb-2 text-center text-lg font-medium text-slate-700">{label}</p>
      )}
      <div className="mb-4 flex justify-center gap-3" aria-live="polite">
        {Array.from({ length: maxLength }).map((_, i) => (
          <span
            key={i}
            className={cn(
              "flex h-12 w-10 items-center justify-center rounded-xl border-2 text-2xl font-bold",
              value.length > i ? "border-teal-700 bg-teal-50" : "border-slate-300 bg-white",
            )}
          >
            {value[i] ? "•" : ""}
          </span>
        ))}
      </div>
      <div className="grid grid-cols-3 gap-3">
        {KEYS.map((key) => {
          if (key === "xoa") {
            return (
              <button
                key={key}
                type="button"
                onClick={() => handleKey(key)}
                aria-label="Xóa số"
                className="min-h-14 rounded-2xl bg-slate-100 text-lg font-semibold text-slate-700 hover:bg-slate-200 focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-teal-600"
              >
                Xóa
              </button>
            );
          }
          if (key === "xong") {
            return <div key={key} aria-hidden="true" />;
          }
          return (
            <button
              key={key}
              type="button"
              onClick={() => handleKey(key)}
              aria-label={`Số ${key}`}
              className="min-h-14 rounded-2xl bg-white text-2xl font-bold text-slate-900 shadow-sm ring-1 ring-slate-200 hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-teal-600"
            >
              {key}
            </button>
          );
        })}
      </div>
    </div>
  );
}
