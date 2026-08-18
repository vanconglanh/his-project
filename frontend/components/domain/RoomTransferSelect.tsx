"use client";

import { useMemo, useState } from "react";
import { DoorOpen } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { useRooms } from "@/lib/hooks/use-reception";

export interface RoomTransferSelectProps {
  patientName: string;
  currentRoomId?: string | null;
  currentRoomName?: string | null;
  disabled?: boolean;
  isPending?: boolean;
  onTransfer: (roomId: string, roomName: string) => void;
}

export function RoomTransferSelect({
  patientName,
  currentRoomId,
  currentRoomName,
  disabled,
  isPending,
  onTransfer,
}: RoomTransferSelectProps) {
  const { data: rooms } = useRooms();
  const [pendingRoomId, setPendingRoomId] = useState<string | null>(null);

  const items = useMemo(() => {
    const map: Record<string, string> = {};
    (rooms ?? []).forEach((r) => {
      map[r.id] = r.name;
    });
    return map;
  }, [rooms]);

  const targetRoom = (rooms ?? []).find((r) => r.id === pendingRoomId);

  return (
    <>
      <Select
        items={items}
        value={currentRoomId ?? ""}
        onValueChange={(v) => {
          const next = String(v ?? "");
          if (next && next !== currentRoomId) setPendingRoomId(next);
        }}
        disabled={disabled || isPending}
      >
        <SelectTrigger
          size="sm"
          className="min-h-[44px] w-[190px]"
          aria-label="Chuyển phòng khám"
        >
          <DoorOpen className="h-4 w-4 shrink-0" aria-hidden="true" />
          <SelectValue placeholder="Chọn phòng khám" />
        </SelectTrigger>
        <SelectContent>
          {(rooms ?? []).map((r) => (
            <SelectItem key={r.id} value={r.id}>
              {r.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <ConfirmDialog
        open={!!pendingRoomId}
        onOpenChange={(v) => {
          if (!v) setPendingRoomId(null);
        }}
        title="Chuyển bệnh nhân sang phòng khác?"
        description={`Bệnh nhân ${patientName} sẽ được chuyển từ ${
          currentRoomName ?? "phòng hiện tại"
        } sang ${targetRoom?.name ?? "phòng mới"}. Bác sĩ phòng mới sẽ tiếp nhận lượt khám này.`}
        confirmLabel="Xác nhận chuyển"
        cancelLabel="Huỷ"
        isLoading={isPending}
        onConfirm={() => {
          if (pendingRoomId) onTransfer(pendingRoomId, targetRoom?.name ?? "");
          setPendingRoomId(null);
        }}
      />
    </>
  );
}
