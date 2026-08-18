"use client";

import { useState } from "react";
import { CalendarClock, Loader2, Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { HisStatusBadge } from "@/components/ui/status-badge";
import { useAppointments, useCreateAppointment } from "@/lib/hooks/use-appointments";
import { getAppointmentSlipPdfUrl } from "@/lib/api/appointments";
import { formatVnDateTime } from "@/lib/utils/encounter-format";
import type { AppointmentStatus } from "@/lib/api/appointments";

interface Props {
  patientId: string;
  patientName: string;
  doctorId?: string | null;
  canEdit: boolean;
}

const STATUS_LABEL: Record<AppointmentStatus, string> = {
  PENDING: "Chờ xác nhận",
  CONFIRMED: "Đã xác nhận",
  CHECKED_IN: "Đã tiếp đón",
  CANCELLED: "Đã huỷ",
  NO_SHOW: "Không đến",
};

function statusVariant(status: AppointmentStatus) {
  if (status === "CONFIRMED" || status === "CHECKED_IN") return "done" as const;
  if (status === "CANCELLED" || status === "NO_SHOW") return "critical" as const;
  return "waiting" as const;
}

export function FollowUpTabPanel({ patientId, patientName, doctorId, canEdit }: Props) {
  const { data, isLoading } = useAppointments({ q: patientName, page_size: 10 });
  const createAppointment = useCreateAppointment();

  const [appointmentAt, setAppointmentAt] = useState("");
  const [note, setNote] = useState("");

  const appointments = (data?.data ?? []).filter((a) => a.patient_ref === patientId);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!appointmentAt) return;
    createAppointment.mutate(
      {
        patient_ref: patientId,
        doctor_ref: doctorId ?? undefined,
        appointment_at: new Date(appointmentAt).toISOString(),
        duration_minutes: 30,
        source: "WALK_IN",
        note: note.trim() || undefined,
      },
      {
        onSuccess: () => {
          setAppointmentAt("");
          setNote("");
        },
      }
    );
  }

  return (
    <div className="space-y-6">
      {canEdit && (
        <Card>
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-sm font-semibold flex items-center gap-1.5">
              <CalendarClock className="h-4 w-4" aria-hidden="true" />
              Đặt lịch tái khám
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-1">
                  <Label htmlFor="followup-at">Thời gian tái khám</Label>
                  <Input
                    id="followup-at"
                    type="datetime-local"
                    value={appointmentAt}
                    onChange={(e) => setAppointmentAt(e.target.value)}
                    className="min-h-[44px]"
                    required
                  />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="followup-note">Dặn dò bệnh nhân</Label>
                  <Textarea
                    id="followup-note"
                    value={note}
                    onChange={(e) => setNote(e.target.value)}
                    placeholder="Nhịn ăn trước khi xét nghiệm, mang theo sổ khám..."
                    rows={2}
                  />
                </div>
              </div>
              <Button
                type="submit"
                className="gap-2 min-h-[44px]"
                disabled={!appointmentAt || createAppointment.isPending}
              >
                {createAppointment.isPending && (
                  <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
                )}
                {createAppointment.isPending ? "Đang lưu…" : "Đặt lịch tái khám"}
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      <div className="space-y-2">
        <h3 className="text-lg font-semibold">Lịch hẹn của bệnh nhân</h3>
        {isLoading ? (
          <div className="space-y-2">
            {[1, 2].map((i) => (
              <Skeleton key={i} className="h-12 w-full" />
            ))}
          </div>
        ) : appointments.length === 0 ? (
          <EmptyState
            variant="encounters"
            title="Chưa hẹn tái khám"
            description="Đặt lịch tái khám để nhắc bệnh nhân qua SMS/Zalo."
          />
        ) : (
          <div className="space-y-2">
            {appointments.map((a) => (
              <div key={a.id} className="flex flex-wrap items-center gap-2 rounded-lg border p-3">
                <span className="text-sm font-medium tabular-nums">
                  {formatVnDateTime(a.appointment_at)}
                </span>
                <HisStatusBadge variant={statusVariant(a.status)}>
                  {STATUS_LABEL[a.status]}
                </HisStatusBadge>
                {a.doctor_name && (
                  <span className="text-xs text-muted-foreground">{a.doctor_name}</span>
                )}
                {a.note && <span className="text-xs text-muted-foreground">· {a.note}</span>}
                <Button
                  variant="ghost"
                  size="sm"
                  className="ml-auto gap-1 min-h-[44px]"
                  onClick={() => window.open(getAppointmentSlipPdfUrl(a.id), "_blank")}
                  aria-label="In giấy hẹn tái khám"
                >
                  <Printer className="h-4 w-4" aria-hidden="true" />
                  In giấy hẹn
                </Button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
