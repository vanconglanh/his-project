"use client";

import Link from "next/link";
import { useMe } from "@/lib/hooks";
import { BellIcon } from "@/components/icons";

/**
 * Header thương hiệu diaB — thanh gradient teal ở đầu app:
 * avatar + lời chào + tên bệnh nhân + mã BN, góc phải là chuông thông báo.
 */
export function Header() {
  const { data: me } = useMe();
  const name = me?.fullName ?? "Bệnh nhân";
  const initial = name.trim().split(/\s+/).pop()?.[0]?.toUpperCase() ?? "B";

  return (
    <header
      className="rounded-b-3xl bg-gradient-to-b from-[#0a8578] to-[#01645A] px-5 pb-5 text-white shadow-md"
      style={{ paddingTop: "calc(env(safe-area-inset-top) + 1rem)" }}
    >
      <div className="flex items-center gap-3">
        <div
          aria-hidden="true"
          className="flex h-12 w-12 shrink-0 items-center justify-center rounded-full bg-white/20 text-xl font-bold"
        >
          {initial}
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-sm text-white/80">Xin chào,</p>
          <p className="truncate text-xl font-bold leading-tight">{name}</p>
        </div>
        <Link
          href="/settings/notifications"
          aria-label="Thông báo"
          className="flex h-11 w-11 items-center justify-center rounded-full bg-white/15 hover:bg-white/25 focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-white/60"
        >
          <BellIcon className="h-6 w-6" />
        </Link>
      </div>

      <div className="mt-3 flex items-center gap-2 text-sm text-white/85">
        <span className="rounded-full bg-white/15 px-3 py-1 font-medium">
          {me?.patientCode ? `Mã BN: ${me.patientCode}` : "Cổng bệnh nhân"}
        </span>
        <span className="text-white/70">Sống khoẻ mỗi ngày</span>
      </div>
    </header>
  );
}
