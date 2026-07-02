"use client";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { usePayments } from "@/lib/hooks/use-payments";
import { formatCurrency } from "@/lib/utils/format";
import { format } from "date-fns";

const METHOD_LABEL: Record<string, string> = {
  CASH: "Tiền mặt",
  BANK_TRANSFER: "Chuyển khoản",
  VISA: "Visa",
  MASTER: "Mastercard",
  QR_VIETQR: "VietQR",
  QR_MOMO: "MoMo",
  QR_VNPAY: "VNPay",
  OTHER: "Khác",
};

const STATUS_CONFIG: Record<string, { label: string; variant: "default" | "secondary" | "destructive" | "outline" }> = {
  COMPLETED: { label: "Hoàn thành", variant: "default" },
  PENDING: { label: "Đang xử lý", variant: "secondary" },
  FAILED: { label: "Thất bại", variant: "destructive" },
  REFUNDED: { label: "Hoàn tiền", variant: "outline" },
  VOID: { label: "Huỷ", variant: "destructive" },
};

export function PaymentHistoryTab() {
  const today = new Date().toISOString().slice(0, 10);
  const { data, isLoading } = usePayments({ from_date: today, page_size: 50 });
  const rows = data?.data ?? [];

  return (
    <div className="rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Thời gian</TableHead>
            <TableHead>Số HĐ</TableHead>
            <TableHead>Phương thức</TableHead>
            <TableHead className="text-right">Số tiền</TableHead>
            <TableHead>Trạng thái</TableHead>
            <TableHead>Tham chiếu</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {isLoading
            ? Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 6 }).map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                  ))}
                </TableRow>
              ))
            : rows.length === 0
            ? (
              <TableRow>
                <TableCell colSpan={6} className="h-32 text-center text-muted-foreground">
                  Chưa có giao dịch hôm nay
                </TableCell>
              </TableRow>
            )
            : rows.map((p) => {
              const statusCfg = STATUS_CONFIG[p.status] ?? { label: p.status, variant: "outline" as const };
              return (
                <TableRow key={p.id}>
                  <TableCell className="text-xs text-muted-foreground">
                    {p.paid_at ? format(new Date(p.paid_at), "HH:mm:ss") : "—"}
                  </TableCell>
                  <TableCell className="font-mono text-xs">{p.billing_id?.slice(0, 8)}...</TableCell>
                  <TableCell>{METHOD_LABEL[p.method] ?? p.method}</TableCell>
                  <TableCell className="text-right font-semibold">{formatCurrency(p.amount)}</TableCell>
                  <TableCell>
                    <Badge variant={statusCfg.variant}>{statusCfg.label}</Badge>
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground">{p.reference ?? p.provider_txn_id ?? "—"}</TableCell>
                </TableRow>
              );
            })}
        </TableBody>
      </Table>
    </div>
  );
}
