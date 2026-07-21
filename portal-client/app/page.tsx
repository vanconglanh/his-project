"use client";

import Link from "next/link";
import { BigCard } from "@/components/BigCard";
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

type Utility = { href: string; label: string; icon: ReactNode };

export default function HomePage() {
  const { data: queue, isLoading: queueLoading } = useQueueInfo();
  const { data: appointments } = useAppointments();

  const upcoming = appointments
    ?.filter((a) => {
      const s = (a.status ?? "").toUpperCase();
      return s !== "CANCELLED" && s !== "DONE";
    })
    .sort((a, b) => new Date(a.appointmentAt).getTime() - new Date(b.appointmentAt).getTime())[0];

  // 4 tính năng còn lại đưa vào panel Tiện ích
  const utilities: Utility[] = [
    { href: "/prescriptions", label: "Đơn thuốc", icon: <FileTextIcon className="h-7 w-7" /> },
    { href: "/encounters", label: "Lịch sử khám", icon: <CheckCircleIcon className="h-7 w-7" /> },
    { href: "/medications", label: "Nhắc thuốc", icon: <PillIcon className="h-7 w-7" /> },
    { href: "/health", label: "Sức khoẻ", icon: <HeartPulseIcon className="h-7 w-7" /> },
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

      {/* 4 cục lớn như cũ */}
      <section aria-label="Chức năng chính" className="mb-5 grid grid-cols-2 gap-4">
        <BigCard
          href="/queue"
          icon={<QueueIcon className="h-9 w-9" />}
          title="Hàng đợi"
          subtitle={queueLoading ? "Đang tải..." : queue ? `Số ${queue.ticketNo}` : "Chưa lấy số"}
        />
        <BigCard
          href="/appointments"
          icon={<CalendarIcon className="h-9 w-9" />}
          title="Đặt lịch"
          subtitle="Xem & đặt lịch khám"
        />
        <BigCard
          href="/lab-results"
          icon={<FlaskIcon className="h-9 w-9" />}
          title="Kết quả"
          subtitle="Xét nghiệm, CLS"
        />
        <BigCard
          href="/me"
          icon={<UserIcon className="h-9 w-9" />}
          title="Hồ sơ"
          subtitle="Thông tin cá nhân"
        />
      </section>

      {/* Panel tiện ích: các tính năng còn lại */}
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
              <span className="flex h-14 w-14 items-center justify-center rounded-full bg-teal-50 text-teal-700">
                {u.icon}
              </span>
              <span className="text-sm font-medium leading-tight text-slate-700">{u.label}</span>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
}
