"use client";

import { useState } from "react";
import { Calendar, Plus, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { PortalLayout } from "@/components/domain/PortalLayout";
import { PortalAppointmentBookForm } from "@/components/domain/PortalAppointmentBookForm";
import {
  usePortalAppointments,
  useCreatePortalAppointment,
  useCancelPortalAppointment,
} from "@/lib/hooks/use-portal";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import type { PortalAppointmentResponse } from "@/lib/api/portal";

const STATUS_LABELS: Record<string, { label: string; variant: "default" | "secondary" | "destructive" | "outline" }> = {
  BOOKED: { label: "Đã đặt", variant: "default" },
  CONFIRMED: { label: "Đã xác nhận", variant: "default" },
  CANCELLED: { label: "Đã huỷ", variant: "destructive" },
  DONE: { label: "Hoàn thành", variant: "secondary" },
};

export default function PortalAppointmentsPage() {
  const [showBook, setShowBook] = useState(false);
  const [cancelTarget, setCancelTarget] = useState<PortalAppointmentResponse | null>(null);

  const { data, isLoading } = usePortalAppointments();
  const createMutation = useCreatePortalAppointment();
  const cancelMutation = useCancelPortalAppointment();

  const appointments = data?.data ?? [];

  return (
    <PortalLayout>
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-xl font-bold">Lịch hẹn</h2>
            <p className="text-sm text-muted-foreground">{appointments.length} lịch hẹn</p>
          </div>
          <Button onClick={() => setShowBook(true)}>
            <Plus className="mr-2 h-4 w-4" />
            Đặt lịch mới
          </Button>
        </div>

        {isLoading ? (
          <div className="space-y-3">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-24 animate-pulse rounded-lg bg-muted" />
            ))}
          </div>
        ) : appointments.length === 0 ? (
          <div className="flex flex-col items-center py-16 text-center">
            <Calendar className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="font-medium">Chưa có lịch hẹn</p>
            <p className="text-sm text-muted-foreground mt-1">
              Nhấn "Đặt lịch mới" để đặt lịch khám
            </p>
          </div>
        ) : (
          <div className="divide-y rounded-lg border">
            {appointments.map((appt) => {
              const statusInfo = STATUS_LABELS[appt.status] ?? { label: appt.status, variant: "outline" as const };
              const canCancel = appt.status === "BOOKED" || appt.status === "CONFIRMED";

              return (
                <div key={appt.id} className="flex items-start gap-4 p-4">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <Badge variant="outline" className="text-xs font-mono">
                        {appt.appointment_code}
                      </Badge>
                      <Badge variant={statusInfo.variant} className="text-xs">
                        {statusInfo.label}
                      </Badge>
                    </div>
                    <p className="font-medium text-sm mt-1.5">
                      {format(parseISO(appt.appointment_at), "EEEE, dd/MM/yyyy - HH:mm", {
                        locale: vi,
                      })}
                    </p>
                    {appt.doctor_name && (
                      <p className="text-xs text-muted-foreground mt-0.5">
                        Bác sĩ: {appt.doctor_name}
                      </p>
                    )}
                    {appt.note && (
                      <p className="text-xs text-muted-foreground mt-1">{appt.note}</p>
                    )}
                  </div>

                  {canCancel && (
                    <Button
                      variant="ghost"
                      size="icon"
                      className="shrink-0 text-destructive hover:text-destructive"
                      onClick={() => setCancelTarget(appt)}
                      title="Huỷ lịch hẹn"
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Book dialog */}
      <Dialog open={showBook} onOpenChange={setShowBook}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Đặt lịch hẹn mới</DialogTitle>
          </DialogHeader>
          <PortalAppointmentBookForm
            onSubmit={(data) =>
              createMutation.mutate(data, {
                onSuccess: () => setShowBook(false),
              })
            }
            isLoading={createMutation.isPending}
            onCancel={() => setShowBook(false)}
          />
        </DialogContent>
      </Dialog>

      {/* Cancel confirm */}
      <AlertDialog
        open={!!cancelTarget}
        onOpenChange={(open) => !open && setCancelTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Huỷ lịch hẹn?</AlertDialogTitle>
            <AlertDialogDescription>
              Lịch hẹn ngày{" "}
              {cancelTarget &&
                format(parseISO(cancelTarget.appointment_at), "dd/MM/yyyy HH:mm", { locale: vi })}{" "}
              sẽ bị huỷ. Bạn có thể đặt lại bất kỳ lúc nào.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Không huỷ</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (cancelTarget) {
                  cancelMutation.mutate(cancelTarget.id, {
                    onSuccess: () => setCancelTarget(null),
                  });
                }
              }}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Huỷ lịch hẹn
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </PortalLayout>
  );
}
