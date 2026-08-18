"use client";

import { useMemo, useState } from "react";
import { AlertTriangle, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { ClsRoundCard } from "@/components/domain/cls/ClsRoundCard";
import { ClsRoundCreateDialog } from "@/components/domain/cls/ClsRoundCreateDialog";
import { ClsRoundPaymentBadge } from "@/components/domain/cls/ClsRoundPaymentBadge";
import {
  ClsOrderItemTable,
  type ClsOrderItemRow,
} from "@/components/domain/cls/ClsOrderItemTable";
import {
  useClsRounds,
  useCreateClsRound,
  useCancelClsRound,
  useSubmitClsRound,
} from "@/lib/hooks/use-cls-rounds";
import { useLabOrders, useRadOrders } from "@/lib/hooks/use-cls-orders";
import { printLabOrdersPdf } from "@/lib/api/cls-orders";

interface Props {
  encounterId: string;
  canEdit: boolean;
}

export function ClsOrderTabPanel({ encounterId, canEdit }: Props) {
  const { data, isLoading, isError, refetch } = useClsRounds(encounterId);
  const { data: labOrders } = useLabOrders(encounterId);
  const { data: radOrders } = useRadOrders(encounterId);

  const createRound = useCreateClsRound(encounterId);
  const submitRound = useSubmitClsRound(encounterId);
  const cancelRound = useCancelClsRound(encounterId);

  const [dialogOpen, setDialogOpen] = useState(false);

  const rounds = useMemo(
    () => [...(data?.rounds ?? [])].sort((a, b) => b.round_no - a.round_no),
    [data?.rounds]
  );

  /** Chỉ định cũ không thuộc đợt nào (round_id = NULL) — gom vào nhóm riêng, KHÔNG fake payment_status */
  const legacyItems: ClsOrderItemRow[] = useMemo(() => {
    const inRounds = new Set<string>();
    (data?.rounds ?? []).forEach((r) => {
      (r.lab_orders ?? []).forEach((o) => inRounds.add(o.id));
      (r.rad_orders ?? []).forEach((o) => inRounds.add(o.id));
    });
    const lab = (labOrders ?? [])
      .filter((o) => !inRounds.has(o.id))
      .map((o) => ({
        id: o.id,
        kind: "LAB" as const,
        code: o.test_code,
        name: o.test_name,
        status: o.status,
        unit_price: null,
      }));
    const rad = (radOrders ?? [])
      .filter((o) => !inRounds.has(o.id))
      .map((o) => ({
        id: o.id,
        kind: "RAD" as const,
        code: o.procedure_code,
        name: o.procedure_name,
        status: o.status,
        unit_price: null,
      }));
    return [...lab, ...rad];
  }, [data?.rounds, labOrders, radOrders]);

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[1, 2].map((i) => (
          <Skeleton key={i} className="h-24 w-full" />
        ))}
      </div>
    );
  }

  if (isError) {
    return (
      <Alert className="border-[color:var(--status-critical)]/30 bg-[color:var(--status-critical)]/10 text-[color:var(--status-critical)]">
        <AlertTriangle className="h-4 w-4" aria-hidden="true" />
        <AlertDescription className="flex items-center gap-3">
          Không tải được danh sách đợt chỉ định cận lâm sàng.
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Thử lại
          </Button>
        </AlertDescription>
      </Alert>
    );
  }

  const isEmpty = rounds.length === 0 && legacyItems.length === 0;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-lg font-semibold">Đợt chỉ định cận lâm sàng</h3>
        {canEdit && (
          <Button className="min-h-[44px] gap-2" onClick={() => setDialogOpen(true)}>
            <Plus className="h-4 w-4" aria-hidden="true" />
            Tạo đợt chỉ định mới
          </Button>
        )}
      </div>

      {isEmpty ? (
        <EmptyState
          variant="labrad"
          title="Chưa có chỉ định cận lâm sàng"
          description="Tạo đợt chỉ định để gửi yêu cầu XN/CĐHA cho bệnh nhân."
          action={
            canEdit ? { label: "Tạo đợt chỉ định mới", onClick: () => setDialogOpen(true) } : undefined
          }
        />
      ) : (
        <div className="space-y-3">
          {rounds.map((round, index) => (
            <ClsRoundCard
              key={round.id}
              round={round}
              defaultOpen={index === 0}
              canEdit={canEdit}
              isPending={submitRound.isPending || cancelRound.isPending}
              onPrint={() => void printLabOrdersPdf(encounterId)}
              onSubmit={() => submitRound.mutate(round.id)}
              onCancel={() => cancelRound.mutate({ roundId: round.id })}
            />
          ))}

          {legacyItems.length > 0 && (
            <Card>
              <CardContent className="p-0">
                <div className="flex flex-wrap items-center gap-2 p-3">
                  <span className="text-sm font-semibold">Chỉ định chưa gom đợt</span>
                  <span className="text-xs text-muted-foreground">
                    {legacyItems.length} dịch vụ tạo trước khi áp dụng đợt chỉ định
                  </span>
                  <ClsRoundPaymentBadge status={null} />
                </div>
                <div className="border-t border-border">
                  <ClsOrderItemTable
                    items={legacyItems}
                    roundLabel="chưa gom đợt"
                    showPrice={false}
                  />
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      )}

      <ClsRoundCreateDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        isPending={createRound.isPending}
        onSubmit={(body) =>
          createRound.mutate(body, { onSuccess: () => setDialogOpen(false) })
        }
      />
    </div>
  );
}
