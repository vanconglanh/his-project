"use client";

import { useState } from "react";
import { Loader2, Plus, Search, Trash2, AlertTriangle } from "lucide-react";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { useClsCatalog } from "@/lib/hooks/use-cls-orders";
import { useDebounce } from "@/lib/hooks/use-debounce";
import { formatVnd } from "@/lib/utils/encounter-format";
import type { ClsCatalogItem, LabOrderRequest, RadOrderRequest, Modality } from "@/lib/api/types";
import type { ClsRound } from "@/lib/api/cls-rounds";

/**
 * BM-14 (Lark Bug-Feedback): thay nut "Xóa" (hủy cả đợt) trên toolbar chính bằng "Điều
 * chỉnh" — cho phép thêm/bớt TỪNG dịch vụ trong đợt, chỉ dùng được khi đợt "chưa thanh
 * toán" (canEdit=true tương đương canMutate ở ClsRoundCard). Việc hủy TOÀN BỘ đợt vẫn
 * còn (BM-15) nhưng chuyển vào đây như 1 hành động phụ, không còn là nút chính ở toolbar.
 */
export interface ClsRoundEditDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  round: ClsRound;
  isAdding?: boolean;
  isRemoving?: boolean;
  isCancelling?: boolean;
  onAddLab: (tests: LabOrderRequest[]) => void;
  onAddRad: (orders: RadOrderRequest[]) => void;
  onRemove: (id: string, kind: "LAB" | "RAD") => void;
  onCancelRound?: (reason: string) => void;
}

export function ClsRoundEditDialog({
  open,
  onOpenChange,
  round,
  isAdding,
  isRemoving,
  isCancelling,
  onAddLab,
  onAddRad,
  onRemove,
  onCancelRound,
}: ClsRoundEditDialogProps) {
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebounce(query, 300);
  const [confirmCancel, setConfirmCancel] = useState(false);
  const { data: catalog, isLoading } = useClsCatalog({ q: debouncedQuery, limit: 20 });

  const existingCodes = new Set([
    ...round.lab_orders.map((o) => o.code),
    ...round.rad_orders.map((o) => o.code),
  ]);

  function handleAdd(item: ClsCatalogItem) {
    if (existingCodes.has(item.code)) {
      toast.info(`"${item.name}" - Chỉ định đã được chọn`);
      return;
    }
    if (item.kind === "LAB") {
      onAddLab([{ test_code: item.code, sample_type: item.sample_type ?? undefined, priority: "NORMAL" }]);
    } else {
      onAddRad([{ modality: (item.modality as Modality) ?? "XRAY", procedure_code: item.code, priority: "NORMAL" }]);
    }
    setQuery("");
  }

  const allItems = [...round.lab_orders, ...round.rad_orders];

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>Điều chỉnh đợt #{round.round_no}</DialogTitle>
            <DialogDescription>
              Thêm hoặc bỏ dịch vụ trong đợt — chỉ áp dụng khi đợt chưa thanh toán.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 md:grid-cols-[1.1fr_1fr]">
            {/* Trái — tìm dịch vụ để thêm */}
            <div className="space-y-2">
              <Label htmlFor="cls-edit-search">Thêm dịch vụ</Label>
              <div className="relative">
                <Search
                  className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
                  aria-hidden="true"
                />
                <Input
                  id="cls-edit-search"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="Nhập mã hoặc tên dịch vụ..."
                  className="min-h-[44px] pl-9"
                />
              </div>
              <div className="max-h-72 space-y-1 overflow-y-auto rounded-lg border border-border p-1">
                {isLoading ? (
                  [1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full" />)
                ) : !catalog || catalog.length === 0 ? (
                  <p className="p-3 text-sm text-muted-foreground">
                    {debouncedQuery ? "Không tìm thấy dịch vụ phù hợp." : "Nhập từ khoá để tìm dịch vụ."}
                  </p>
                ) : (
                  catalog.map((item) => (
                    <button
                      key={`${item.kind}-${item.code}`}
                      type="button"
                      onClick={() => handleAdd(item)}
                      disabled={isAdding}
                      className="flex min-h-[44px] w-full items-center gap-2 rounded-md px-2 text-left hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[color:var(--focus-ring)] disabled:opacity-50"
                    >
                      <Plus className="h-4 w-4 shrink-0" aria-hidden="true" />
                      <span className="font-mono text-xs tabular-nums text-primary">{item.code}</span>
                      <span className="flex-1 line-clamp-2 text-sm">{item.name}</span>
                      <span className="font-mono text-xs tabular-nums">{formatVnd(item.default_price)} ₫</span>
                    </button>
                  ))
                )}
              </div>
            </div>

            {/* Phải — dịch vụ hiện có trong đợt */}
            <div className="space-y-2">
              <Label>Dịch vụ trong đợt ({allItems.length})</Label>
              <div className="max-h-72 space-y-1 overflow-y-auto rounded-lg border border-border p-1">
                {allItems.length === 0 ? (
                  <p className="p-3 text-sm text-muted-foreground">Đợt chưa có dịch vụ nào.</p>
                ) : (
                  allItems.map((item) => (
                    <div key={item.id} className="flex items-center gap-2 rounded-md px-2 py-1">
                      <span className="font-mono text-xs tabular-nums text-primary">{item.code}</span>
                      <span className="flex-1 line-clamp-2 text-sm">{item.name}</span>
                      <span
                        className={
                          "font-mono text-xs tabular-nums" +
                          (item.is_free ? " text-muted-foreground line-through" : "")
                        }
                      >
                        {formatVnd(item.unit_price)} ₫
                      </span>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-destructive"
                        onClick={() => onRemove(item.id, item.kind)}
                        disabled={isRemoving}
                        aria-label={`Bỏ dịch vụ ${item.name}`}
                      >
                        <Trash2 className="h-4 w-4" aria-hidden="true" />
                      </Button>
                    </div>
                  ))
                )}
              </div>
              <div className="flex items-center justify-between text-sm font-semibold">
                <span>Tổng tiền</span>
                <span className="font-mono tabular-nums">
                  {formatVnd(round.total_amount)} <span aria-label="đồng">₫</span>
                </span>
              </div>
            </div>
          </div>

          <DialogFooter className="sm:justify-between">
            {onCancelRound && (
              <Button
                variant="ghost"
                className="text-destructive gap-1.5"
                onClick={() => setConfirmCancel(true)}
                disabled={isCancelling}
              >
                <AlertTriangle className="h-4 w-4" aria-hidden="true" />
                Huỷ toàn bộ đợt
              </Button>
            )}
            <Button onClick={() => onOpenChange(false)} className="gap-2">
              {(isAdding || isRemoving) && <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />}
              Xong
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmCancel}
        onOpenChange={setConfirmCancel}
        title={`Huỷ toàn bộ đợt chỉ định #${round.round_no}?`}
        description={`Toàn bộ ${allItems.length} dịch vụ trong đợt này sẽ bị huỷ. Không thể hoàn tác.`}
        variant="destructive"
        confirmLabel="Huỷ đợt"
        cancelLabel="Giữ lại"
        isLoading={isCancelling}
        onConfirm={() => {
          setConfirmCancel(false);
          onOpenChange(false);
          onCancelRound?.("Huỷ theo yêu cầu bác sĩ");
        }}
      />
    </>
  );
}
