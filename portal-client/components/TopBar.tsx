"use client";

import { useRouter } from "next/navigation";

interface TopBarProps {
  title: string;
  showBack?: boolean;
}

export function TopBar({ title, showBack = false }: TopBarProps) {
  const router = useRouter();

  return (
    <header className="sticky top-0 z-30 flex min-h-[64px] items-center gap-2 border-b border-[--color-border] bg-white/95 px-4 backdrop-blur">
      {showBack ? (
        <button
          type="button"
          onClick={() => router.back()}
          aria-label="Quay lại"
          className="flex h-11 w-11 items-center justify-center rounded-full text-2xl focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus]"
        >
          ←
        </button>
      ) : null}
      <h1 className="text-xl font-bold text-[--color-text]">{title}</h1>
    </header>
  );
}
