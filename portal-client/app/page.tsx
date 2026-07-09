"use client";

import { BigCard } from "@/components/BigCard";
import { HealthTrends } from "@/components/HealthTrends";
import { CalendarIcon, FileTextIcon, FlaskIcon, QueueIcon } from "@/components/icons";
import { useAppointments, useQueueInfo } from "@/lib/hooks";
import { formatDateTime } from "@/lib/utils";

export default function HomePage() {
  const { data: queue } = useQueueInfo();
  const { data: appointments } = useAppointments();

  const upcoming = appointments
    ?.filter((a) => a.status !== "cancelled" && a.status !== "done")
    .sort((a, b) => new Date(a.appointmentAt).getTime() - new Date(b.appointmentAt).getTime())[0];

  return (
    <div className="p-4">
      {upcoming && (
        <div className="mb-5 rounded-2xl border-2 border-teal-300 bg-teal-50 p-4">
          <p className="text-base font-semibold text-teal-900">Lịch hẹn sắp tới</p>
          <p className="mt-1 text-lg text-teal-900">
            {formatDateTime(upcoming.appointmentAt)} — {upcoming.doctorName}
          </p>
        </div>
      )}

      <HealthTrends />

      <section aria-label="Chức năng chính" className="grid grid-cols-2 gap-4">
        <BigCard
          href="/queue"
          icon={<QueueIcon className="h-9 w-9" />}
          title="Hàng đợi"
          subtitle={queue ? `Số ${queue.ticketNo}` : "Chưa lấy số"}
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
          icon={<FileTextIcon className="h-9 w-9" />}
          title="Hồ sơ"
          subtitle="Thông tin cá nhân"
        />
      </section>
    </div>
  );
}
