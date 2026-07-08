"use client";

import { BigCard } from "@/components/BigCard";
import { Skeleton } from "@/components/Skeleton";
import { useAppointments, useMe, useQueueInfo } from "@/lib/queries";
import { formatDateTime } from "@/lib/format";

export default function HomePage() {
  const { data: me } = useMe();
  const { data: queue } = useQueueInfo();
  const { data: appointments, isLoading: appointmentsLoading } = useAppointments();

  const nextAppointment = appointments
    ?.filter((a) => a.status !== "cancelled" && new Date(a.appointmentAt).getTime() > Date.now())
    .sort((a, b) => new Date(a.appointmentAt).getTime() - new Date(b.appointmentAt).getTime())[0];

  return (
    <main className="mx-auto flex max-w-md flex-col gap-6 px-5 py-6">
      <header>
        <p className="text-base text-[--color-text-muted]">Xin chào,</p>
        <h1 className="text-2xl font-bold">{me?.fullName ?? "Bệnh nhân"}</h1>
      </header>

      {appointmentsLoading ? (
        <Skeleton className="h-20 w-full" />
      ) : nextAppointment ? (
        <div className="rounded-2xl bg-[--color-primary-soft] p-4">
          <p className="text-sm font-semibold text-[--color-primary]">Lịch hẹn sắp tới</p>
          <p className="mt-1 text-lg font-bold">{formatDateTime(nextAppointment.appointmentAt)}</p>
          <p className="text-base text-[--color-text-muted]">Bác sĩ {nextAppointment.doctorName}</p>
        </div>
      ) : null}

      {queue ? (
        <div className="rounded-2xl bg-[--color-success]/10 p-4">
          <p className="text-sm font-semibold text-[--color-success]">Bạn đang trong hàng đợi hôm nay</p>
          <p className="mt-1 text-lg font-bold">
            Số của bạn: {queue.ticketNo} — còn {queue.waitingAhead} người trước
          </p>
        </div>
      ) : null}

      <div className="flex flex-col gap-4">
        <BigCard href="/queue" icon="🎫" title="Hàng đợi" subtitle="Xem số thứ tự, vị trí chờ" />
        <BigCard href="/appointments" icon="📅" title="Đặt lịch khám" subtitle="Đặt lịch hoặc xem lịch hẹn" />
        <BigCard href="/results" icon="📋" title="Kết quả & Đơn thuốc" subtitle="Lịch sử khám, đơn thuốc, xét nghiệm" />
        <BigCard href="/me" icon="👤" title="Hồ sơ của tôi" subtitle="Thông tin cá nhân, BHYT" />
      </div>
    </main>
  );
}
