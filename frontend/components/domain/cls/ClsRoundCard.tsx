"use client";

import { useState } from "react";
import { ChevronDown, ChevronRight, Printer, Send, Trash2, Wallet, BadgePercent } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { ClsRoundPaymentBadge } from "./ClsRoundPaymentBadge";
import { ClsOrderItemTable, type ClsOrderItemRow } from "./ClsOrderItemTable";
import { formatVnd, formatVnDateTime } from "@/lib/utils/encounter-format";
import type { ClsRound } from "@/lib/api/cls-rounds";

export interface ClsRoundCardProps {
  round: ClsRound;
  defaultOpen?: boolean;
  canEdit: boolean;
  isPending?: boolean;
  onPrint: () => void;
  onSubmit?: () => void;
  onCancel?: () => void;
  onPay?: () => void;
  onWaive?: (reason: string) => void;
}

export function ClsRoundCard({
  round,
  defaultOpen = false,
  canEdit,
  isPending,
  onPrint,
  onSubmit,
  onCancel,
  onPay,
  onWaive,
}: ClsRoundCardProps) {
  const [open, setOpen] = useState(defaultOpen);
  const [confirmCancel, setConfirmCancel] = useState(false);
  const [confirmWaive, setConfirmWaive] = useState(false);

  const items: ClsOrderItemRow[] = [
    ...(round.lab_orders ?? []).map((o) => ({ ...o, kind: "LAB" as const })),
    ...(round.rad_orders ?? []).map((o) => ({ ...o, kind: "RAD" as const })),
  ];
  const roundLabel = `Đợt #${round.round_no}`;
  const isCancelled = round.status === "CANCELLED";
  const isPaid = round.payment_status === "PAID" || round.payment_status === "WAIVED";
  const canMutate = canEdit && !isCancelled && !isPaid && round.status === "OPEN";
  // Sau khi chốt đợt (SUBMITTED) mà chưa thanh toán -> cho phép thu tiền / miễn phí
  // để mở khoá bước nhập kết quả XN (gate CLS_ORDER_UNPAID).
  const canPay =
    canEdit && !isCancelled && round.payment_status === "UNPAID" && round.status === "SUBMITTED";

  return (
    <Card>
      <CardContent className="p-0">
        <div className="flex flex-wrap items-center gap-2 p-3">
          <button
            type="button"
            onClick={() => setOpen((v) => !v)}
            aria-expanded={open}
            className="flex min-h-[44px] flex-1 items-center gap-2 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[color:var(--focus-ring)]"
          >
            {open ? (
              <ChevronDown className="h-4 w-4 shrink-0" aria-hidden="true" />
            ) : (
              <ChevronRight className="h-4 w-4 shrink-0" aria-hidden="true" />
            )}
            <span className="text-sm font-semibold">{roundLabel}</span>
            <span className="text-xs text-muted-foreground">
              {formatVnDateTime(round.created_at)} · {items.length} dịch vụ
            </span>
          </button>

          <ClsRoundPaymentBadge status={round.payment_status} cancelled={isCancelled} />

          <span className="font-mono tabular-nums text-sm font-semibold">
            {formatVnd(round.total_amount)} <span aria-label="đồng">₫</span>
          </span>

          <Button
            variant="ghost"
            size="sm"
            className="min-h-[44px] gap-1"
            onClick={onPrint}
            aria-label={`In phiếu chỉ định ${roundLabel}`}
          >
            <Printer className="h-4 w-4" aria-hidden="true" />
            In phiếu
          </Button>

          {canMutate && onSubmit && (
            <Button
              variant="outline"
              size="sm"
              className="min-h-[44px] gap-1"
              onClick={onSubmit}
              disabled={isPending}
            >
              <Send className="h-4 w-4" aria-hidden="true" />
              Chốt đợt
            </Button>
          )}

          {canPay && onPay && (
            <Button
              variant="default"
              size="sm"
              className="min-h-[44px] gap-1"
              onClick={onPay}
              disabled={isPending}
            >
              <Wallet className="h-4 w-4" aria-hidden="true" />
              Thu tiền
            </Button>
          )}

          {canPay && onWaive && (
            <Button
              variant="outline"
              size="sm"
              className="min-h-[44px] gap-1"
              onClick={() => setConfirmWaive(true)}
              disabled={isPending}
            >
              <BadgePercent className="h-4 w-4" aria-hidden="true" />
              Miễn phí
            </Button>
          )}

          {canMutate && onCancel && (
            <Button
              variant="ghost"
              size="sm"
              className="min-h-[44px] text-destructive"
              onClick={() => setConfirmCancel(true)}
              disabled={isPending}
              aria-label={`Huỷ ${roundLabel}`}
            >
              <Trash2 className="h-4 w-4" aria-hidden="true" />
            </Button>
          )}
        </div>

        {open && (
          <div className="border-t border-border">
            {items.length === 0 ? (
              <p className="p-4 text-sm text-muted-foreground">Đợt này chưa có dịch vụ nào.</p>
            ) : (
              <ClsOrderItemTable items={items} roundLabel={roundLabel} />
            )}
          </div>
        )}
      </CardContent>

      <ConfirmDialog
        open={confirmCancel}
        onOpenChange={setConfirmCancel}
        title={`Huỷ đợt chỉ định #${round.round_no}?`}
        description={`Toàn bộ ${items.length} dịch vụ trong đợt này sẽ bị huỷ. Không thể hoàn tác.`}
        variant="destructive"
        confirmLabel="Huỷ đợt"
        cancelLabel="Giữ lại"
        isLoading={isPending}
        onConfirm={() => {
          setConfirmCancel(false);
          onCancel?.();
        }}
      />

      <ConfirmDialog
        open={confirmWaive}
        onOpenChange={setConfirmWaive}
        title={`Miễn phí đợt chỉ định #${round.round_no}?`}
        description={`Đợt ${formatVnd(round.total_amount)} ₫ sẽ được đánh dấu miễn phí và mở khoá bước nhập kết quả, không thu tiền bệnh nhân.`}
        confirmLabel="Miễn phí đợt"
        cancelLabel="Huỷ"
        isLoading={isPending}
        onConfirm={() => {
          setConfirmWaive(false);
          onWaive?.("Miễn phí theo chỉ định lâm sàng");
        }}
      />
    </Card>
  );
}
