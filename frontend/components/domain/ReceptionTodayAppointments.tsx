"use client";

import { useState } from "react";
import { CalendarClock, LogIn, UserX } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { useTodayConfirmedAppointments } from "@/lib/hooks/use-reception";
import { useChangeAppointmentStatus } from "@/lib/hooks/use-appointments";
import type { AppointmentTodayResponse } from "@/lib/api/types";
import { formatTime } from "@/lib/utils/format";

interface ReceptionTodayAppointmentsProps {
  /** Được gọi khi lễ tân bấm "Check-in" trên một lịch hẹn — cha mở/prefill form tiếp đón. */
  onCheckIn: (appointment: AppointmentTodayResponse) => void;
}

/** BM-33: hiển thị lịch hẹn CONFIRMED hôm nay tại màn Tiếp đón, cho phép "Check-in" hoặc "Không đến". */
export function ReceptionTodayAppointments({ onCheckIn }: ReceptionTodayAppointmentsProps) {
  const { data: appointments, isLoading, isError } = useTodayConfirmedAppointments();
  const changeStatus = useChangeAppointmentStatus();
  const [noShowTarget, setNoShowTarget] = useState<AppointmentTodayResponse | null>(null);

  const confirmNoShow = () => {
    if (!noShowTarget) return;
    changeStatus.mutate(
      { id: noShowTarget.appointment_id, status: "NO_SHOW" },
      { onSuccess: () => setNoShowTarget(null) }
    );
  };

  if (isLoading) {
    return (
      <div className="space-y-2">
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-center text-sm text-destructive">
        Không tải được danh sách lịch hẹn hôm nay.
      </div>
    );
  }

  if (!appointments || appointments.length === 0) {
    return (
      <EmptyState
        variant="generic"
        title="Không có lịch hẹn hôm nay"
        description="Chưa có lịch hẹn nào được xác nhận cho hôm nay."
      />
    );
  }

  return (
    <>
      <ul className="divide-y rounded-lg border">
        {appointments.map((appt) => (
          <li
            key={appt.appointment_id}
            className="flex flex-wrap items-center justify-between gap-3 p-3"
          >
            <div className="flex items-start gap-3 min-w-0">
              <div className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                <CalendarClock className="h-4 w-4" />
              </div>
              <div className="min-w-0">
                <p className="font-medium truncate">
                  {appt.patient_name ?? "Chưa rõ bệnh nhân"}{" "}
                  <span className="font-normal text-muted-foreground">
                    · {formatTime(appt.appointment_at)}
                  </span>
                </p>
                <p className="text-xs text-muted-foreground truncate">
                  {appt.doctor_name ? `BS. ${appt.doctor_name}` : "Chưa chỉ định bác sĩ"}
                  {appt.patient_phone ? ` · ${appt.patient_phone}` : ""}
                </p>
                {appt.note && (
                  <p className="text-xs text-muted-foreground truncate">Ghi chú: {appt.note}</p>
                )}
              </div>
            </div>
            <div className="flex shrink-0 items-center gap-2">
              <Button size="sm" className="gap-1.5" onClick={() => onCheckIn(appt)}>
                <LogIn className="h-3.5 w-3.5" />
                Check-in
              </Button>
              <Button
                size="sm"
                variant="outline"
                className="gap-1.5 text-muted-foreground"
                onClick={() => setNoShowTarget(appt)}
              >
                <UserX className="h-3.5 w-3.5" />
                Không đến
              </Button>
            </div>
          </li>
        ))}
      </ul>

      <ConfirmDialog
        open={Boolean(noShowTarget)}
        onOpenChange={(o) => {
          if (!o) setNoShowTarget(null);
        }}
        title="Đánh dấu không đến"
        description={
          <>
            Đánh dấu <strong>{noShowTarget?.patient_name}</strong> không đến lịch hẹn lúc{" "}
            {noShowTarget ? formatTime(noShowTarget.appointment_at) : ""} hôm nay?
          </>
        }
        confirmLabel="Xác nhận"
        variant="destructive"
        isLoading={changeStatus.isPending}
        onConfirm={confirmNoShow}
      />
    </>
  );
}
