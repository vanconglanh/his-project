"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { RefreshCw, Activity } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { TicketCard } from "@/components/domain/TicketCard";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { VitalSignsForm } from "@/components/domain/VitalSignsForm";
import {
  useReceptionQueue,
  useCallTicket,
  useSkipTicket,
  useCancelTicket,
  useAdmitTicket,
} from "@/lib/hooks/use-reception";
import { useRooms } from "@/lib/hooks/use-reception";
import { useCreateVitalSigns } from "@/lib/hooks/use-vital-signs";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { isNursingRole } from "@/lib/utils/roles";
import type { ReceptionTicketResponse, VitalSignsRequest } from "@/lib/api/types";
import { Input } from "@/components/ui/input";

interface CancelState {
  ticketId: string;
  reason: string;
}

interface VitalSignsStepState {
  encounterId: string;
  patientName?: string;
}

export function ReceptionQueueBoard() {
  const [cancelState, setCancelState] = useState<CancelState | null>(null);
  // BM-05: Dieu duong/KTV sau khi "Dua vao kham" phai ghi sinh hieu ngay tai cho truoc
  // khi sang man "Kham benh", thay vi dieu huong thang nhu Le tan/Bac si.
  const [vitalStep, setVitalStep] = useState<VitalSignsStepState | null>(null);
  const router = useRouter();
  const { roles } = usePermissions();
  const isNursing = isNursingRole(roles);

  const { data: tickets, isLoading, refetch, isFetching } = useReceptionQueue();
  const { data: rooms } = useRooms();
  const callMutation = useCallTicket();
  const skipMutation = useSkipTicket();
  const cancelMutation = useCancelTicket();
  const admitMutation = useAdmitTicket();
  const createVital = useCreateVitalSigns(vitalStep?.encounterId ?? "");

  // Đưa bệnh nhân vào khám: tạo/lấy lượt khám. Dieu duong/KTV -> mo panel "Ghi sinh hieu"
  // ngay tai cho; cac role khac dieu huong thang sang man Kham benh nhu truoc.
  const handleAdmit = (ticket: ReceptionTicketResponse) =>
    admitMutation.mutate(ticket.id, {
      onSuccess: (res) => {
        if (isNursing) {
          setVitalStep({ encounterId: res.encounter_id, patientName: ticket.patient_summary?.full_name });
        } else {
          router.push(`/encounters/${res.encounter_id}`);
        }
      },
    });

  async function handleVitalSubmit(data: VitalSignsRequest) {
    if (!vitalStep) return;
    await createVital.mutateAsync(data);
    const encounterId = vitalStep.encounterId;
    setVitalStep(null);
    router.push(`/encounters/${encounterId}`);
  }

  const activeTickets = (tickets ?? []).filter(
    (t) => !["DONE", "CANCELLED"].includes(t.status)
  );

  // Group by room
  const byRoom = (rooms ?? []).reduce<Record<string, ReceptionTicketResponse[]>>(
    (acc, room) => {
      acc[room.id] = activeTickets.filter((t) => t.room_id === room.id);
      return acc;
    },
    {}
  );

  // Unassigned tickets (room not in rooms list)
  const roomIds = new Set((rooms ?? []).map((r) => r.id));
  const unassigned = activeTickets.filter((t) => !roomIds.has(t.room_id));

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="space-y-2">
            <Skeleton className="h-8 w-32" />
            {[1, 2, 3].map((j) => <Skeleton key={j} className="h-28 w-full" />)}
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          Tự động làm mới mỗi 5 giây
        </p>
        <Button
          size="sm"
          variant="outline"
          onClick={() => refetch()}
          disabled={isFetching}
          className="gap-1.5 h-8"
        >
          <RefreshCw className={`h-3.5 w-3.5 ${isFetching ? "animate-spin" : ""}`} />
          Tải lại
        </Button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {(rooms ?? []).map((room) => {
          const roomTickets = byRoom[room.id] ?? [];
          return (
            <div key={room.id} className="space-y-2">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="font-semibold text-sm">{room.name}</h3>
                  {room.on_duty_doctor && (
                    <p className="text-xs text-muted-foreground">{room.on_duty_doctor.full_name}</p>
                  )}
                </div>
                <span className="text-xs bg-muted rounded-full px-2 py-0.5">
                  {roomTickets.length} BN
                </span>
              </div>
              <div className="space-y-2 min-h-[4rem]">
                {roomTickets.length === 0 ? (
                  <div className="border-2 border-dashed rounded-lg h-16 flex items-center justify-center text-xs text-muted-foreground">
                    Không có bệnh nhân
                  </div>
                ) : (
                  roomTickets.map((ticket) => (
                    <TicketCard
                      key={ticket.id}
                      ticket={ticket}
                      onCall={(id) => callMutation.mutate(id)}
                      onSkip={(id) => skipMutation.mutate(id)}
                      onCancel={(id) => setCancelState({ ticketId: id, reason: "" })}
                      onAdmit={() => handleAdmit(ticket)}
                      isCallLoading={callMutation.isPending}
                      isSkipLoading={skipMutation.isPending}
                      isAdmitLoading={admitMutation.isPending}
                    />
                  ))
                )}
              </div>
            </div>
          );
        })}

        {unassigned.length > 0 && (
          <div className="space-y-2">
            <h3 className="font-semibold text-sm text-muted-foreground">Chưa phân phòng</h3>
            {unassigned.map((ticket) => (
              <TicketCard
                key={ticket.id}
                ticket={ticket}
                onCall={(id) => callMutation.mutate(id)}
                onSkip={(id) => skipMutation.mutate(id)}
                onCancel={(id) => setCancelState({ ticketId: id, reason: "" })}
                onAdmit={() => handleAdmit(ticket)}
                isCallLoading={callMutation.isPending}
                isSkipLoading={skipMutation.isPending}
                isAdmitLoading={admitMutation.isPending}
              />
            ))}
          </div>
        )}
      </div>

      <ConfirmDialog
        open={!!cancelState}
        onOpenChange={(open) => { if (!open) setCancelState(null); }}
        title="Huỷ tiếp đón"
        description={
          <div className="space-y-2">
            <p>Nhập lý do huỷ (tuỳ chọn):</p>
            <Input
              placeholder="Lý do huỷ..."
              value={cancelState?.reason ?? ""}
              onChange={(e) =>
                setCancelState((prev) => prev ? { ...prev, reason: e.target.value } : null)
              }
            />
          </div>
        }
        confirmLabel="Xác nhận huỷ"
        variant="destructive"
        isLoading={cancelMutation.isPending}
        onConfirm={() => {
          if (cancelState) {
            cancelMutation.mutate({ ticketId: cancelState.ticketId, reason: cancelState.reason });
            setCancelState(null);
          }
        }}
      />

      {/* BM-05: Dieu duong/KTV ghi sinh hieu ngay sau khi dua benh nhan vao kham,
          truoc khi sang man "Kham benh". */}
      <Sheet open={!!vitalStep} onOpenChange={(open) => { if (!open) setVitalStep(null); }}>
        <SheetContent side="right" className="w-full sm:max-w-xl overflow-y-auto px-6 pb-6">
          <SheetHeader>
            <SheetTitle className="flex items-center gap-2">
              <Activity className="h-5 w-5 text-primary" />
              Ghi sinh hiệu
              {vitalStep?.patientName && (
                <span className="text-base font-normal text-muted-foreground">
                  — {vitalStep.patientName}
                </span>
              )}
            </SheetTitle>
          </SheetHeader>
          <div className="mt-4">
            {vitalStep && (
              <VitalSignsForm
                key={vitalStep.encounterId}
                isLoading={createVital.isPending}
                onSubmit={handleVitalSubmit}
                onSubmitAndNext={handleVitalSubmit}
              />
            )}
          </div>
        </SheetContent>
      </Sheet>
    </div>
  );
}
