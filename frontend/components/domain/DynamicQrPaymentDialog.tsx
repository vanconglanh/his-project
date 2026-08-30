"use client";

import { useEffect, useState } from "react";
import { AxiosError } from "axios";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { AlertTriangle, CheckCircle2, Loader2 } from "lucide-react";
import { useGenerateDynamicBillingQr } from "@/lib/hooks/use-billing";
import { useCreatePayment } from "@/lib/hooks/use-payments";
import { formatCurrency } from "@/lib/utils/format";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  billingId: string;
}

/**
 * FR-911 H-9 — QR thanh toán ĐỘNG theo số tiền phải thu thật sự của hoá đơn.
 * GHI CHÚ: backend hiện chưa có webhook/API kiểm tra trạng thái thanh toán cho
 * QR động này (khác với flow /payments/qr/generate + /payments/qr/{id}/status
 * đã có sẵn ở QrPaymentModal). Vì vậy màn này dùng nút "Xác nhận đã thanh toán"
 * THỦ CÔNG cho thu ngân — GIẢI PHÁP TẠM cho tới khi có webhook ngân hàng thật.
 */
export function DynamicQrPaymentDialog({ open, onOpenChange, billingId }: Props) {
  const generateQr = useGenerateDynamicBillingQr();
  const createPayment = useCreatePayment();
  const [confirmed, setConfirmed] = useState(false);

  useEffect(() => {
    if (open) {
      setConfirmed(false);
      generateQr.mutate(billingId);
    } else {
      generateQr.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, billingId]);

  const qr = generateQr.data;
  const error = generateQr.error as AxiosError<{ error?: { code?: string; message?: string } }> | null;
  const errorCode = error?.response?.data?.error?.code;
  const errorMessage = error?.response?.data?.error?.message ?? "Không thể tạo mã QR thanh toán";

  async function handleConfirmPaid() {
    if (!qr) return;
    try {
      await createPayment.mutateAsync({
        billing_id: qr.billing_id,
        amount: qr.amount,
        method: "QR_VIETQR",
        reference: qr.transaction_ref,
        note: "Xác nhận thủ công bởi thu ngân — chờ tích hợp webhook ngân hàng",
      });
      setConfirmed(true);
      toast.success("Đã ghi nhận thanh toán");
      setTimeout(() => onOpenChange(false), 1000);
    } catch {
      toast.error("Ghi nhận thanh toán thất bại");
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-sm text-center">
        <DialogHeader>
          <DialogTitle>Quét mã VietQR để thanh toán</DialogTitle>
          <DialogDescription>Số tiền QR được hệ thống tự tính theo số dư còn lại của hoá đơn</DialogDescription>
        </DialogHeader>

        <div className="flex flex-col items-center gap-4 py-2">
          {confirmed ? (
            <div className="flex flex-col items-center gap-3 py-4">
              <CheckCircle2 className="h-16 w-16 text-green-600" />
              <p className="text-lg font-semibold text-green-700">Đã ghi nhận thanh toán!</p>
            </div>
          ) : generateQr.isPending ? (
            <div className="flex flex-col items-center gap-3 py-6">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
              <p className="text-sm text-muted-foreground">Đang tạo mã QR...</p>
            </div>
          ) : generateQr.isError ? (
            <div className="flex flex-col items-center gap-3 py-6 text-center">
              <AlertTriangle className="h-10 w-10 text-destructive" />
              <p className="text-sm font-medium text-destructive">{errorMessage}</p>
              {errorCode === "BANK_ACCOUNT_NOT_CONFIGURED" && (
                <p className="text-xs text-muted-foreground">
                  Vui lòng cấu hình tài khoản nhận thanh toán trong phần Cài đặt trước khi dùng QR động.
                </p>
              )}
              <Button size="sm" variant="outline" onClick={() => generateQr.mutate(billingId)}>
                Thử lại
              </Button>
            </div>
          ) : qr ? (
            <>
              <div className="rounded-xl border-2 border-primary p-2">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={`data:image/png;base64,${qr.qr_payload_image_base64}`}
                  alt="QR Code thanh toán VietQR"
                  className="h-48 w-48 object-contain"
                />
              </div>

              <div className="space-y-1">
                <p className="text-2xl font-bold text-primary">{formatCurrency(qr.amount)}</p>
                <Badge variant="outline" className="text-xs">{qr.transaction_ref}</Badge>
              </div>

              <p className="text-xs text-muted-foreground">
                Sau khi bệnh nhân chuyển khoản thành công, thu ngân bấm "Xác nhận đã thanh toán".
              </p>
            </>
          ) : null}
        </div>

        {!confirmed && qr && (
          <div className="flex flex-col gap-2">
            <Button onClick={handleConfirmPaid} disabled={createPayment.isPending}>
              {createPayment.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Xác nhận đã thanh toán
            </Button>
            <Button variant="outline" onClick={() => onOpenChange(false)} disabled={createPayment.isPending}>
              Đóng
            </Button>
          </div>
        )}
        {!confirmed && !qr && !generateQr.isPending && (
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        )}
      </DialogContent>
    </Dialog>
  );
}
