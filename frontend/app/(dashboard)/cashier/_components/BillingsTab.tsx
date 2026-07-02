"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { MoreHorizontal, Eye, CreditCard, Printer, Receipt } from "lucide-react";
import { useBillings } from "@/lib/hooks/use-billing";
import { BillingStatusBadge } from "@/components/domain/BillingStatusBadge";
import { PaymentDialog } from "@/components/domain/PaymentDialog";
import { formatCurrency } from "@/lib/utils/format";
import { printBillingPdf } from "@/lib/api/billing";
import { printReceiptPdf } from "@/lib/api/cashier";
import { listPayments } from "@/lib/api/payments";
import type { BillingStatus, BillingResponse } from "@/lib/api/billing";
import { format } from "date-fns";

interface Props {
  filterStatus?: BillingStatus[];
}

export function BillingsTab({ filterStatus }: Props) {
  const router = useRouter();
  const [search, setSearch] = useState("");
  const [payDialog, setPayDialog] = useState<{ billing: BillingResponse } | null>(null);

  const today = new Date().toISOString().slice(0, 10);
  const { data, isLoading } = useBillings({ from_date: today });

  const rows = (data?.data ?? []).filter((b) => {
    if (filterStatus && !filterStatus.includes(b.status)) return false;
    if (search) {
      const q = search.toLowerCase();
      return (
        b.bill_no?.toLowerCase().includes(q) ||
        b.patient_summary?.full_name?.toLowerCase().includes(q)
      );
    }
    return true;
  });

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <Input
          placeholder="Tìm số HĐ, tên bệnh nhân..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-sm"
        />
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Số HĐ</TableHead>
              <TableHead>Bệnh nhân</TableHead>
              <TableHead className="text-right">Tổng tiền</TableHead>
              <TableHead className="text-right">Đã thu</TableHead>
              <TableHead className="text-right">Còn lại</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Hạn TT</TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading
              ? Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 8 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                    ))}
                  </TableRow>
                ))
              : rows.length === 0
              ? (
                <TableRow>
                  <TableCell colSpan={8} className="h-32 text-center text-muted-foreground">
                    Không có hoá đơn
                  </TableCell>
                </TableRow>
              )
              : rows.map((bill) => (
                <TableRow
                  key={bill.id}
                  className="cursor-pointer hover:bg-muted/30"
                  onClick={() => router.push(`/billings/${bill.id}`)}
                >
                  <TableCell className="font-mono text-xs font-semibold">{bill.bill_no}</TableCell>
                  <TableCell>
                    <div>
                      <p className="font-medium text-sm">{bill.patient_summary?.full_name}</p>
                      <p className="text-xs text-muted-foreground">{bill.patient_summary?.phone}</p>
                    </div>
                  </TableCell>
                  <TableCell className="text-right font-medium">{formatCurrency(bill.patient_payable)}</TableCell>
                  <TableCell className="text-right text-green-600">{formatCurrency(bill.paid_amount)}</TableCell>
                  <TableCell className="text-right">
                    <span className={bill.balance > 0 ? "font-bold text-destructive" : "text-muted-foreground"}>
                      {formatCurrency(bill.balance)}
                    </span>
                  </TableCell>
                  <TableCell><BillingStatusBadge status={bill.status} /></TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {bill.payment_due_date ? format(new Date(bill.payment_due_date), "dd/MM/yyyy") : "—"}
                  </TableCell>
                  <TableCell onClick={(e) => e.stopPropagation()}>
                    <DropdownMenu>
                      <DropdownMenuTrigger className="inline-flex h-8 w-8 items-center justify-center rounded-lg hover:bg-muted" aria-label="Thao tác">
                        <MoreHorizontal className="h-4 w-4" />
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => router.push(`/billings/${bill.id}`)}>
                          <Eye className="mr-2 h-4 w-4" /> Xem chi tiết
                        </DropdownMenuItem>
                        {bill.balance > 0 && (
                          <DropdownMenuItem onClick={() => setPayDialog({ billing: bill })}>
                            <CreditCard className="mr-2 h-4 w-4" /> Thu tiền
                          </DropdownMenuItem>
                        )}
                        <DropdownMenuItem onClick={() => printBillingPdf(bill.id)}>
                          <Printer className="mr-2 h-4 w-4" /> In hoá đơn
                        </DropdownMenuItem>
                        <DropdownMenuItem onClick={async () => {
                          // In biên lai theo lần thanh toán COMPLETED mới nhất của hoá đơn;
                          // nếu chưa có thanh toán thì fallback in hoá đơn.
                          try {
                            const res = await listPayments({ billing_id: bill.id });
                            const completed = res.data.filter((p) => p.status === "COMPLETED");
                            const latest = completed[completed.length - 1];
                            if (latest) await printReceiptPdf(latest.id);
                            else printBillingPdf(bill.id);
                          } catch {
                            printBillingPdf(bill.id);
                          }
                        }}>
                          <Receipt className="mr-2 h-4 w-4" /> In phiếu thu
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
      </div>

      {payDialog && (
        <PaymentDialog
          open={true}
          onOpenChange={(o) => { if (!o) setPayDialog(null); }}
          billingId={payDialog.billing.id}
          balance={payDialog.billing.balance}
          onSuccess={() => setPayDialog(null)}
        />
      )}
    </div>
  );
}
