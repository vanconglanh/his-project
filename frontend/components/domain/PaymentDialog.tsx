"use client";

import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
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
import { useCreatePayment, useGenerateQr, useChargeCard } from "@/lib/hooks/use-payments";
import { QrPaymentModal } from "./QrPaymentModal";
import type { PaymentMethod } from "@/lib/api/payments";
import { printReceiptPdf } from "@/lib/api/cashier";

const METHODS: { id: PaymentMethod; label: string; icon: string; key: string }[] = [
  { id: "CASH", label: "Tiền mặt", icon: "💵", key: "1" },
  { id: "BANK_TRANSFER", label: "Chuyển khoản", icon: "🏦", key: "2" },
  { id: "VISA", label: "Visa", icon: "💳", key: "3" },
  { id: "MASTER", label: "Mastercard", icon: "💳", key: "4" },
  { id: "QR_VIETQR", label: "VietQR", icon: "📱", key: "5" },
  { id: "QR_MOMO", label: "MoMo", icon: "🟣", key: "6" },
  { id: "QR_VNPAY", label: "VNPay", icon: "🔵", key: "7" },
];

const schema = z.object({
  amount: z.number({ message: "Nhập số tiền" }).min(1000, "Tối thiểu 1.000đ"),
  reference: z.string().optional(),
  note: z.string().optional(),
  cash_received: z.number().optional(),
  card_token: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  billingId: string;
  balance: number;
  onSuccess?: () => void;
}

export function PaymentDialog({ open, onOpenChange, billingId, balance, onSuccess }: Props) {
  const [method, setMethod] = useState<PaymentMethod>("CASH");
  const [qrData, setQrData] = useState<{ id: string; provider: "VIETQR" | "MOMO" | "VNPAY"; qr_payload: string; amount: number; expires_at: string; transaction_ref: string } | null>(null);
  const amountRef = useRef<HTMLInputElement>(null);

  const createPayment = useCreatePayment();
  const generateQr = useGenerateQr();
  const chargeCard = useChargeCard();

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { amount: balance },
  });

  const amount = watch("amount") ?? 0;
  const cashReceived = watch("cash_received") ?? 0;
  const change = cashReceived - amount;

  // Reset khi mở
  useEffect(() => {
    if (open) {
      reset({ amount: balance });
      setMethod("CASH");
      setQrData(null);
      setTimeout(() => amountRef.current?.focus(), 100);
    }
  }, [open, balance, reset]);

  // Keyboard shortcuts 1-7 to pick method
  useEffect(() => {
    if (!open) return;
    const handler = (e: KeyboardEvent) => {
      const m = METHODS.find((x) => x.key === e.key);
      if (m) setMethod(m.id);
    };
    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [open]);

  const isQr = method.startsWith("QR_");
  const isCard = method === "VISA" || method === "MASTER";

  function getQrProvider(m: PaymentMethod): "VIETQR" | "MOMO" | "VNPAY" {
    if (m === "QR_MOMO") return "MOMO";
    if (m === "QR_VNPAY") return "VNPAY";
    return "VIETQR";
  }

  async function handleGenerateQr() {
    const amt = amount;
    if (!amt || amt < 1000) {
      toast.error("Số tiền không hợp lệ");
      return;
    }
    try {
      const qr = await generateQr.mutateAsync({
        billing_id: billingId,
        provider: getQrProvider(method),
        amount: amt,
      });
      setQrData({
        id: qr.id,
        provider: qr.provider,
        qr_payload: qr.qr_payload,
        amount: qr.amount,
        expires_at: qr.expires_at,
        transaction_ref: qr.transaction_ref,
      });
    } catch {
      toast.error("Không thể tạo mã QR");
    }
  }

  async function onSubmit(values: FormData) {
    try {
      let paymentId: string | undefined;
      if (isCard) {
        if (!values.card_token) {
          toast.error("Nhập card token");
          return;
        }
        const p = await chargeCard.mutateAsync({
          billing_id: billingId,
          amount: values.amount,
          card_token: values.card_token,
          provider: method as "VISA" | "MASTER",
        });
        paymentId = p.id;
      } else if (!isQr) {
        const p = await createPayment.mutateAsync({
          billing_id: billingId,
          amount: values.amount,
          method,
          reference: values.reference,
          note: values.note,
        });
        paymentId = p.id;
      }
      toast.success("Thu tiền thành công");
      onOpenChange(false);
      onSuccess?.();
      // Sau khi thu tiền thành công: tự động mở biên lai để in (không chặn nếu in lỗi)
      if (paymentId) {
        try {
          await printReceiptPdf(paymentId);
        } catch {
          /* Không chặn luồng thu tiền nếu in biên lai lỗi */
        }
      }
    } catch {
      toast.error("Thu tiền thất bại");
    }
  }

  function handleQrPaid() {
    setQrData(null);
    onOpenChange(false);
    onSuccess?.();
    toast.success("Thanh toán QR thành công");
  }

  const isPending = createPayment.isPending || chargeCard.isPending;

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent fullScreen>
          <DialogHeader>
            <DialogTitle>Thu tiền</DialogTitle>
          </DialogHeader>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {/* Method picker */}
            <div>
              <Label className="mb-2 block text-sm font-medium">
                Phương thức thanh toán{" "}
                <span className="text-muted-foreground text-xs">(phím 1-7)</span>
              </Label>
              <div className="grid grid-cols-4 gap-2">
                {METHODS.map((m) => (
                  <button
                    key={m.id}
                    type="button"
                    onClick={() => setMethod(m.id)}
                    className={cn(
                      "flex min-h-[52px] flex-col items-center justify-center rounded-lg border p-2 text-xs font-medium transition-all",
                      method === m.id
                        ? "border-primary bg-primary/10 text-primary ring-2 ring-primary"
                        : "border-border hover:bg-accent"
                    )}
                  >
                    <span className="text-lg">{m.icon}</span>
                    <span className="mt-0.5 text-[10px]">{m.label}</span>
                    <span className="text-[9px] text-muted-foreground">({m.key})</span>
                  </button>
                ))}
              </div>
            </div>

            {/* Amount */}
            <div>
              <Label htmlFor="amount">
                Số tiền (VND){" "}
                <span className="text-muted-foreground text-xs">Cần thu: {formatCurrency(balance)}</span>
              </Label>
              <Input
                id="amount"
                type="number"
                min={0}
                step={1000}
                {...register("amount", { valueAsNumber: true })}
                ref={(el) => {
                  register("amount").ref(el);
                  (amountRef as React.MutableRefObject<HTMLInputElement | null>).current = el;
                }}
                className="mt-1"
              />
              {errors.amount && (
                <p className="mt-1 text-xs text-destructive">{errors.amount.message}</p>
              )}
            </div>

            {/* Cash received */}
            {method === "CASH" && (
              <div>
                <Label htmlFor="cash_received">Khách đưa (VND)</Label>
                <Input
                  id="cash_received"
                  type="number"
                  min={0}
                  step={1000}
                  {...register("cash_received", { valueAsNumber: true })}
                  className="mt-1"
                />
                {cashReceived > 0 && (
                  <p className={cn("mt-1 text-sm font-medium", change >= 0 ? "text-green-600" : "text-destructive")}>
                    {change >= 0
                      ? `Tiền thừa: ${formatCurrency(change)}`
                      : `Thiếu: ${formatCurrency(Math.abs(change))}`}
                  </p>
                )}
              </div>
            )}

            {/* Card token */}
            {isCard && (
              <div>
                <Label htmlFor="card_token">Card Token (dev: tok_visa_xxx)</Label>
                <Input
                  id="card_token"
                  placeholder="tok_visa_success"
                  {...register("card_token")}
                  className="mt-1"
                />
              </div>
            )}

            {/* Reference */}
            {(method === "BANK_TRANSFER") && (
              <div>
                <Label htmlFor="reference">Mã giao dịch ngân hàng</Label>
                <Input id="reference" {...register("reference")} className="mt-1" />
              </div>
            )}

            {/* Note */}
            <div>
              <Label htmlFor="note">Ghi chú</Label>
              <Input id="note" {...register("note")} className="mt-1" />
            </div>

            <DialogFooter className="gap-2">
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                Huỷ
              </Button>

              {isQr ? (
                <Button
                  type="button"
                  onClick={handleGenerateQr}
                  disabled={generateQr.isPending}
                >
                  {generateQr.isPending ? "Đang tạo..." : "Tạo mã QR"}
                </Button>
              ) : (
                <Button type="submit" disabled={isPending}>
                  {isPending ? "Đang xử lý..." : "Xác nhận thu tiền (F4)"}
                </Button>
              )}
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {qrData && (
        <QrPaymentModal
          open={Boolean(qrData)}
          onClose={() => setQrData(null)}
          onPaid={handleQrPaid}
          qrId={qrData.id}
          qrPayload={qrData.qr_payload}
          amount={qrData.amount}
          provider={qrData.provider}
          expiresAt={qrData.expires_at}
          transactionRef={qrData.transaction_ref}
        />
      )}
    </>
  );
}
