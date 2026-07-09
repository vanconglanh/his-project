"use client";

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

export default function HomePage() {
  const { data: queue } = useQueueInfo();
  const { data: appointments } = useAppointments();

  const upcoming = appointments
    ?.filter((a) => {
      const s = (a.status ?? "").toUpperCase();
      return s !== "CANCELLED" && s !== "DONE";
    })
    .sort((a, b) => new Date(a.appointmentAt).getTime() - new Date(b.appointmentAt).getTime())[0];

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

      <section aria-label="Chức năng" className="grid grid-cols-2 gap-4">
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
          href="/prescriptions"
          icon={<FileTextIcon className="h-9 w-9" />}
          title="Đơn thuốc"
          subtitle="Toa thuốc của bạn"
        />
        <BigCard
          href="/encounters"
          icon={<CheckCircleIcon className="h-9 w-9" />}
          title="Lịch sử khám"
          subtitle="Các lần khám bệnh"
        />
        <BigCard
          href="/medications"
          icon={<PillIcon className="h-9 w-9" />}
          title="Nhắc uống thuốc"
          subtitle="Lịch uống thuốc"
        />
        <BigCard
          href="/health"
          icon={<HeartPulseIcon className="h-9 w-9" />}
          title="Sức khoẻ"
          subtitle="Chỉ số theo dõi"
        />
        <BigCard
          href="/me"
          icon={<UserIcon className="h-9 w-9" />}
          title="Hồ sơ"
          subtitle="Thông tin & cài đặt"
        />
      </section>
    </div>
  );
}
