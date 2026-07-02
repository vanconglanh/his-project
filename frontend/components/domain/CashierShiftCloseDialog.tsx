"use client";

import { useForm } from "react-hook-form";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import { formatCurrency } from "@/lib/utils/format";
import { useCloseShift } from "@/lib/hooks/use-cashier";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  shiftId?: string;
  expectedCash?: number;
}

export function CashierShiftCloseDialog({ open, onOpenChange, shiftId, expectedCash }: Props) {
  const { register, handleSubmit, watch, reset } = useForm({
    defaultValues: { actual_cash: 0, note: "", accept_difference: false },
  });
  const closeShift = useCloseShift();
  const actualCash = watch("actual_cash") ?? 0;
  const difference = expectedCash != null ? actualCash - expectedCash : null;

  async function onSubmit(values: { actual_cash: number; note: string; accept_difference: boolean }) {
    try {
      await closeShift.mutateAsync({
        shift_id: shiftId,
        actual_cash: values.actual_cash,
        note: values.note || undefined,
        accept_difference: values.accept_difference,
      });
      toast.success("Đã đóng ca thu ngân");
      reset();
      onOpenChange(false);
    } catch {
      toast.error("Không thể đóng ca");
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Đóng ca thu ngân</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          {expectedCash != null && (
            <div className="rounded-lg bg-muted/50 p-3 text-sm space-y-1">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Tiền mặt kỳ vọng:</span>
                <span className="font-semibold">{formatCurrency(expectedCash)}</span>
              </div>
              {difference !== null && actualCash > 0 && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Chênh lệch:</span>
                  <span className={cn("font-semibold", difference >= 0 ? "text-green-600" : "text-destructive")}>
                    {difference >= 0 ? "+" : ""}{formatCurrency(difference)}
                  </span>
                </div>
              )}
            </div>
          )}

          <div>
            <Label htmlFor="actual_cash">Tiền mặt thực tế (VND)</Label>
            <Input
              id="actual_cash"
              type="number"
              min={0}
              step={1000}
              {...register("actual_cash", { valueAsNumber: true })}
              className="mt-1"
            />
          </div>

          <div>
            <Label htmlFor="note_close">Ghi chú</Label>
            <Input id="note_close" {...register("note")} className="mt-1" />
          </div>

          {difference != null && difference !== 0 && actualCash > 0 && (
            <label className="flex items-center gap-2 text-sm cursor-pointer">
              <input type="checkbox" {...register("accept_difference")} />
              Chấp nhận chênh lệch và đóng ca
            </label>
          )}

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Huỷ</Button>
            <Button type="submit" variant="destructive" disabled={closeShift.isPending}>
              {closeShift.isPending ? "Đang đóng..." : "Đóng ca"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
