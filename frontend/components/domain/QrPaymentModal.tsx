"use client";

import { useEffect, useRef } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { formatCurrency } from "@/lib/utils/format";
import { useQrStatusPoll } from "@/lib/hooks/use-payments";

interface Props {
  open: boolean;
  onClose: () => void;
  onPaid: () => void;
  qrId: string;
  qrPayload: string;
  amount: number;
  provider: "VIETQR" | "MOMO" | "VNPAY";
  expiresAt: string;
  transactionRef: string;
}

const PROVIDER_LABEL: Record<string, string> = {
  VIETQR: "VietQR",
  MOMO: "MoMo",
  VNPAY: "VNPay",
};

function useCountdown(expiresAt: string) {
  const [seconds, setSeconds] = useStateLocal(0);
  useEffect(() => {
    const calc = () => {
      const diff = Math.max(0, Math.floor((new Date(expiresAt).getTime() - Date.now()) / 1000));
      setSeconds(diff);
    };
    calc();
    const id = setInterval(calc, 1000);
    return () => clearInterval(id);
  }, [expiresAt]);
  return seconds;
}

// Simple local useState to avoid circular imports
import { useState as useStateLocal } from "react";

const BEEP_URL = "data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivsJBnaGqFl62qjHFgZX+YsLCWgmpqcoeZtbiahnB1eI2ir76rl4d9gJSnwMGvmpCGhI+ht8W4qZuRiI6fo8LCuKygkoqOm62/xLeqnZSOkJmtv8m+s6mfmpufsMDLwrqzraaipLTCzcLAt66oqK27y9G+wLeurK2wuc3Tw8O9tLCwr7jJ0tDAv7izsbO6y9PSwMC8t7e5vsvV08HCvru5u7/Mz9LFwb+9vL6/yM7Ux8PAv76/v8PKz9bLx8fFxMXGycvQ1s3Kyc3Oz87P0dTY0s3LzM7P0dPS1NbW1NTV1dTU1dXW2NnY2Nna2tnY19fY2NnZ2dvb29vc3Nzb2tvb29zc3d3d3d3e3t3d3t7f39/f3+Dg4ODg4OHh4eHi4uLi4uLj4+Pj4+Pk5OTk5OTl5eXl5ebm5ubm5ubm5+fn5+fn6Ojo6Ojo6Onp6enp6enq6urq6urq6+vr6+vr6+zs7Ozs7Ozs7e3t7e3t7u7u7u7u7u/v7+/v7+/v8PDw8PDw8PDx8fHx8fHx8vLy8vLy8vLy8/Pz8/Pz8/P09PT09PT09PT19fX19fX19fX29vb29vb29vb39/f39/f39/f4+Pj4+Pj4+Pj5+fn5+fn5+fn6+vr6+vr6+vr7+/v7+/v7+/v8/Pz8/Pz8/Pz9/f39/f39/f3+Pj4+Pj4+Pj4";

export function QrPaymentModal({
  open,
  onClose,
  onPaid,
  qrId,
  qrPayload,
  amount,
  provider,
  expiresAt,
  transactionRef,
}: Props) {
  const { data: qrStatus } = useQrStatusPoll(qrId, open);
  const countdown = useCountdown(expiresAt);
  const paidRef = useRef(false);

  useEffect(() => {
    if (qrStatus?.status === "PAID" && !paidRef.current) {
      paidRef.current = true;
      // Play sound
      try {
        const audio = new Audio(BEEP_URL);
        audio.play().catch(() => {});
      } catch {}
      setTimeout(() => onPaid(), 1200);
    }
  }, [qrStatus?.status, onPaid]);

  const isPaid = qrStatus?.status === "PAID";
  const isExpired = qrStatus?.status === "EXPIRED" || countdown === 0;

  const minutes = Math.floor(countdown / 60);
  const secs = countdown % 60;

  return (
    <Dialog open={open} onOpenChange={(o) => { if (!o) onClose(); }}>
      <DialogContent className="max-w-sm text-center">
        <DialogHeader>
          <DialogTitle>Quét mã {PROVIDER_LABEL[provider]} để thanh toán</DialogTitle>
        </DialogHeader>

        <div className="flex flex-col items-center gap-4 py-2">
          {isPaid ? (
            <div className="flex flex-col items-center gap-3 py-4">
              <div className={cn(
                "flex h-20 w-20 items-center justify-center rounded-full bg-green-100 text-5xl",
                "animate-bounce"
              )}>
                ✓
              </div>
              <p className="text-lg font-semibold text-green-700">Thanh toán thành công!</p>
            </div>
          ) : (
            <>
              {/* QR image */}
              <div className={cn(
                "relative rounded-xl border-2 p-2",
                isExpired ? "opacity-40 grayscale" : "border-primary"
              )}>
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={`data:image/png;base64,${qrPayload}`}
                  alt="QR Code thanh toán"
                  className="h-48 w-48 object-contain"
                />
                {isExpired && (
                  <div className="absolute inset-0 flex items-center justify-center rounded-xl bg-background/60">
                    <span className="rounded bg-destructive px-3 py-1 text-sm text-destructive-foreground">
                      Đã hết hạn
                    </span>
                  </div>
                )}
              </div>

              <div className="space-y-1">
                <p className="text-2xl font-bold text-primary">{formatCurrency(amount)}</p>
                <Badge variant="outline" className="text-xs">{transactionRef}</Badge>
              </div>

              {!isExpired && (
                <div className={cn(
                  "flex items-center gap-1 text-sm",
                  countdown < 60 ? "text-destructive" : "text-muted-foreground"
                )}>
                  <span>Hết hạn sau:</span>
                  <span className="font-mono font-semibold">
                    {minutes}:{String(secs).padStart(2, "0")}
                  </span>
                </div>
              )}

              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <span className="inline-block h-2 w-2 animate-pulse rounded-full bg-green-500" />
                Đang chờ thanh toán...
              </div>
            </>
          )}
        </div>

        {!isPaid && (
          <Button variant="outline" onClick={onClose} className="w-full">
            Huỷ QR
          </Button>
        )}
      </DialogContent>
    </Dialog>
  );
}
