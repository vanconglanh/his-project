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
import { useOpenShift } from "@/lib/hooks/use-cashier";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CashierShiftOpenDialog({ open, onOpenChange }: Props) {
  const { register, handleSubmit, reset } = useForm({ defaultValues: { opening_balance: 0, note: "" } });
  const openShift = useOpenShift();

  async function onSubmit(values: { opening_balance: number; note: string }) {
    try {
      await openShift.mutateAsync({ opening_balance: values.opening_balance, note: values.note || undefined });
      toast.success("Đã mở ca thu ngân");
      reset();
      onOpenChange(false);
    } catch {
      toast.error("Không thể mở ca");
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Mở ca thu ngân</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <Label htmlFor="opening_balance">Tiền mặt đầu ca (VND)</Label>
            <Input
              id="opening_balance"
              type="number"
              min={0}
              step={1000}
              {...register("opening_balance", { valueAsNumber: true })}
              className="mt-1"
            />
          </div>
          <div>
            <Label htmlFor="note">Ghi chú</Label>
            <Input id="note" {...register("note")} className="mt-1" />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Huỷ</Button>
            <Button type="submit" disabled={openShift.isPending}>
              {openShift.isPending ? "Đang mở..." : "Mở ca"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
