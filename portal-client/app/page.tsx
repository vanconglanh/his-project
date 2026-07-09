"use client";

import Link from "next/link";
import {
  CalendarIcon,
  CheckCircleIcon,
  FileTextIcon,
  FlaskIcon,
  HeartPulseIcon,
  PillIcon,
  QueueIcon,
  UserIcon,
} from "@/components/icons";
import { useAppointments, useQueueInfo } from "@/lib/hooks";
import { formatDateTime } from "@/lib/utils";
import type { ReactNode } from "react";

type Utility = { href: string; label: string; icon: ReactNode; badge?: string | number };

export default function HomePage() {
  const { data: queue } = useQueueInfo();
  const { data: appointments } = useAppointments();

  const upcoming = appointments
    ?.filter((a) => {
      const s = (a.status ?? "").toUpperCase();
      return s !== "CANCELLED" && s !== "DONE";
    })
    .sort((a, b) => new Date(a.appointmentAt).getTime() - new Date(b.appointmentAt).getTime())[0];

  const utilities: Utility[] = [
    { href: "/queue", label: "Hàng đợi", icon: <QueueIcon className="h-7 w-7" />, badge: queue?.ticketNo },
    { href: "/appointments", label: "Đặt lịch", icon: <CalendarIcon className="h-7 w-7" /> },
    { href: "/lab-results", label: "Kết quả", icon: <FlaskIcon className="h-7 w-7" /> },
    { href: "/prescriptions", label: "Đơn thuốc", icon: <FileTextIcon className="h-7 w-7" /> },
    { href: "/encounters", label: "Lịch sử khám", icon: <CheckCircleIcon className="h-7 w-7" /> },
    { href: "/medications", label: "Nhắc thuốc", icon: <PillIcon className="h-7 w-7" /> },
    { href: "/health", label: "Sức khoẻ", icon: <HeartPulseIcon className="h-7 w-7" /> },
    { href: "/me", label: "Hồ sơ", icon: <UserIcon className="h-7 w-7" /> },
  ];

  return (
    <div className="p-4">
      {upcoming && (
        <div className="mb-5 flex items-start gap-3 rounded-2xl bg-teal-50 p-4 shadow-[0_2px_10px_rgba(1,100,90,0.06)]">
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-white text-teal-700">
            <CalendarIcon className="h-6 w-6" />
          </span>
          <div>
            <p className="text-base font-semibold text-teal-900">Lịch hẹn sắp tới</p>
            <p className="mt-0.5 text-lg font-medium text-teal-900">
              {formatDateTime(upcoming.appointmentAt)} — {upcoming.doctorName}
            </p>
          </div>
        </div>
      )}

      {/* Panel tiện ích: 1 khung chứa lưới icon */}
      <section
        aria-label="Tiện ích"
        className="rounded-2xl border border-[var(--border-soft)] bg-white p-4 shadow-[var(--shadow-card)]"
      >
        <h2 className="mb-3 text-lg font-bold text-slate-900">Tiện ích</h2>
        <div className="grid grid-cols-4 gap-2">
          {utilities.map((u) => (
            <Link
              key={u.href}
              href={u.href}
              aria-label={u.label}
              className="flex min-h-[5.5rem] flex-col items-center justify-start gap-1.5 rounded-xl p-2 text-center transition-colors hover:bg-teal-50 focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-teal-600"
            >
              <span className="relative flex h-14 w-14 items-center justify-center rounded-full bg-teal-50 text-teal-700">
                {u.icon}
                {u.badge != null && (
                  <span className="absolute -right-1 -top-1 flex min-w-5 items-center justify-center rounded-full bg-red-600 px-1.5 py-0.5 text-xs font-bold text-white">
                    {u.badge}
                  </span>
                )}
              </span>
              <span className="text-sm font-medium leading-tight text-slate-700">{u.label}</span>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
}
