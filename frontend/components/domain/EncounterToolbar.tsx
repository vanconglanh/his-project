"use client";

import { useState } from "react";
import {
  CheckCircle,
  ChevronDown,
  FileText,
  Loader2,
  PauseCircle,
  PenTool,
  Play,
  Printer,
  Receipt,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { EncounterStatusBadge } from "@/components/domain/EncounterStatusBadge";
import { HisStatusBadge } from "@/components/ui/status-badge";
import { RoomTransferSelect } from "@/components/domain/RoomTransferSelect";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import type { EncounterStatus } from "@/lib/api/types";

export interface EncounterToolbarProps {
  status: EncounterStatus;
  patientName: string;
  roomId?: string | null;
  roomName?: string | null;
  /** Vé tiếp đón đang ở trạng thái chờ kết quả CLS */
  isWaitingCls: boolean;
  /** Có tìm được vé tiếp đón tương ứng không (cần cho chuyển phòng / chờ CLS) */
  hasTicket: boolean;
  isEmrSigned: boolean;
  diagnosisCount: number;
  canEdit: boolean;
  isPending?: boolean;
  onStart: () => void;
  onSignEmr: () => void;
  onWaitForCls: () => void;
  onResume: () => void;
  onClose: () => void;
  onTransferRoom: (roomId: string) => void;
  onPrintEncounter: () => void;
  onPrintCls: () => void;
  /** Item 5 — nút Lập hoá đơn / Xem hoá đơn (chỉ hiện nếu FE có quyền billing.write). */
  canManageBilling?: boolean;
  hasBilling?: boolean;
  isBillingLoading?: boolean;
  isCreatingBilling?: boolean;
  onCreateBilling?: () => void;
  onViewBilling?: () => void;
}

export function EncounterToolbar({
  status,
  patientName,
  roomId,
  roomName,
  isWaitingCls,
  hasTicket,
  isEmrSigned,
  diagnosisCount,
  canEdit,
  isPending,
  onStart,
  onSignEmr,
  onWaitForCls,
  onResume,
  onClose,
  onTransferRoom,
  onPrintEncounter,
  onPrintCls,
  canManageBilling,
  hasBilling,
  isBillingLoading,
  isCreatingBilling,
  onCreateBilling,
  onViewBilling,
}: EncounterToolbarProps) {
  const [confirmClose, setConfirmClose] = useState(false);
  const isWaiting = status === "WAITING";
  const isInProgress = status === "IN_PROGRESS";

  return (
    <div className="sticky top-0 z-20 -mx-4 flex min-h-14 flex-wrap items-center gap-2 border-b border-border bg-card/95 px-4 py-2 backdrop-blur xl:-mx-6 xl:px-6">
      {isWaitingCls ? (
        <HisStatusBadge variant="waiting">Chờ kết quả CLS</HisStatusBadge>
      ) : (
        <EncounterStatusBadge status={status} />
      )}

      {(isWaiting || isInProgress) && (
        <RoomTransferSelect
          patientName={patientName}
          currentRoomId={roomId}
          currentRoomName={roomName}
          disabled={!hasTicket || isPending}
          isPending={isPending}
          onTransfer={(id) => onTransferRoom(id)}
        />
      )}

      <div className="ml-auto flex flex-wrap items-center gap-2">
        {isWaiting && (
          <Button
            className="min-h-[44px] gap-2"
            onClick={onStart}
            disabled={isPending}
            data-tour="enc-start"
          >
            {isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : (
              <Play className="h-4 w-4" aria-hidden="true" />
            )}
            Bắt đầu khám
          </Button>
        )}

        {isInProgress && !isWaitingCls && (
          <Button
            variant="outline"
            className="min-h-[44px] gap-2"
            onClick={onWaitForCls}
            disabled={!hasTicket || isPending}
            title="Tạm dừng ca khám, gọi bệnh nhân tiếp theo"
          >
            <PauseCircle className="h-4 w-4" aria-hidden="true" />
            Chờ kết quả CLS
          </Button>
        )}

        {isWaitingCls && (
          <Button
            variant="outline"
            className="min-h-[44px] gap-2"
            onClick={onResume}
            disabled={!hasTicket || isPending}
          >
            <Play className="h-4 w-4" aria-hidden="true" />
            Tiếp tục khám
          </Button>
        )}

        {isInProgress && (
          <Button
            variant="outline"
            className="min-h-[44px] gap-2"
            onClick={onSignEmr}
            disabled={isEmrSigned}
            data-tour="enc-sign"
          >
            <PenTool className="h-4 w-4" aria-hidden="true" />
            {isEmrSigned ? "Đã ký số bệnh án" : "Ký số bệnh án"}
          </Button>
        )}

        {canManageBilling && (
          <Button
            variant={hasBilling ? "outline" : "default"}
            className="min-h-[44px] gap-2"
            onClick={hasBilling ? onViewBilling : onCreateBilling}
            disabled={isBillingLoading || isCreatingBilling}
            title={hasBilling ? "Lượt khám đã có hoá đơn" : "Lập hoá đơn cho lượt khám này"}
          >
            {isCreatingBilling ? (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : hasBilling ? (
              <FileText className="h-4 w-4" aria-hidden="true" />
            ) : (
              <Receipt className="h-4 w-4" aria-hidden="true" />
            )}
            {hasBilling ? "Xem hoá đơn" : "Lập hoá đơn"}
          </Button>
        )}

        <DropdownMenu>
          <DropdownMenuTrigger
            render={<Button variant="outline" className="min-h-[44px] gap-2" aria-label="In tài liệu" />}
          >
            <Printer className="h-4 w-4" aria-hidden="true" />
            In
            <ChevronDown className="h-4 w-4" aria-hidden="true" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={onPrintEncounter}>In phiếu khám</DropdownMenuItem>
            <DropdownMenuItem onClick={onPrintCls}>In phiếu chỉ định CLS</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>

        {isInProgress && (
          <Button
            className="min-h-[44px] gap-2"
            onClick={() => setConfirmClose(true)}
            disabled={!canEdit || isPending}
            data-tour="enc-finish"
          >
            {isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
            ) : (
              <CheckCircle className="h-4 w-4" aria-hidden="true" />
            )}
            Kết thúc khám
          </Button>
        )}
      </div>

      <ConfirmDialog
        open={confirmClose}
        onOpenChange={setConfirmClose}
        title="Kết thúc lượt khám?"
        description={
          <div className="space-y-2">
            <p>
              Sau khi kết thúc, bệnh án sẽ bị khoá và chỉ có thể sửa bằng bản đính chính.
            </p>
            {diagnosisCount === 0 && (
              <p className="text-[color:var(--status-warning)]">
                Chưa có chẩn đoán ICD-10. Bệnh án thiếu chẩn đoán sẽ không xuất được XML giám định
                BHYT.
              </p>
            )}
            {!isEmrSigned && (
              <p className="text-[color:var(--status-warning)]">Bệnh án chưa được ký số.</p>
            )}
          </div>
        }
        confirmLabel="Kết thúc khám"
        cancelLabel="Xem lại"
        isLoading={isPending}
        onConfirm={() => {
          setConfirmClose(false);
          onClose();
        }}
      />
    </div>
  );
}
