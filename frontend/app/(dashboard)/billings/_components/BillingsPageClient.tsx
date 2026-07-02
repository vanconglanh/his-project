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
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { MoreHorizontal, Eye, CreditCard, Printer } from "lucide-react";
import { useBillings } from "@/lib/hooks/use-billing";
import { BillingStatusBadge } from "@/components/domain/BillingStatusBadge";
import { PaymentDialog } from "@/components/domain/PaymentDialog";
import { formatCurrency } from "@/lib/utils/format";
import { printBillingPdf } from "@/lib/api/billing";
import { format } from "date-fns";
import type { BillingStatus, BillingResponse } from "@/lib/api/billing";

export function BillingsPageClient() {
  const router = useRouter();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<BillingStatus | "">("");
  const [payDialog, setPayDialog] = useState<BillingResponse | null>(null);
  const [page, setPage] = useState(1);

  const today = new Date().toISOString().slice(0, 10);
  const { data, isLoading } = useBillings({
    status: statusFilter || undefined,
    from_date: today,
    page,
    page_size: 20,
  });

  const rows = (data?.data ?? []).filter((b) => {
    if (!search) return true;
    const q = search.toLowerCase();
    return b.bill_no?.toLowerCase().includes(q) || b.patient_summary?.full_name?.toLowerCase().includes(q);
  });

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold tracking-tight">Hoá đơn</h2>
          <p className="text-sm text-muted-foreground">Quản lý hoá đơn khám bệnh</p>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <Input
          placeholder="Tìm số HĐ, tên BN..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-xs"
        />
        <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as BillingStatus | "")}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Tất cả trạng thái" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Tất cả</SelectItem>
            <SelectItem value="DRAFT">Nháp</SelectItem>
            <SelectItem value="FINALIZED">Đã xác nhận</SelectItem>
            <SelectItem value="PARTIAL_PAID">Thanh toán một phần</SelectItem>
            <SelectItem value="PAID">Đã thanh toán</SelectItem>
            <SelectItem value="VOID">Đã huỷ</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Số HĐ</TableHead>
              <TableHead>Bệnh nhân</TableHead>
              <TableHead className="text-right">Phải trả</TableHead>
              <TableHead className="text-right">Đã thu</TableHead>
              <TableHead className="text-right">Còn lại</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Ngày tạo</TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading
              ? Array.from({ length: 8 }).map((_, i) => (
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
                    {format(new Date(bill.created_at), "dd/MM/yyyy")}
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
                          <DropdownMenuItem onClick={() => setPayDialog(bill)}>
                            <CreditCard className="mr-2 h-4 w-4" /> Thu tiền
                          </DropdownMenuItem>
                        )}
                        <DropdownMenuItem onClick={() => printBillingPdf(bill.id)}>
                          <Printer className="mr-2 h-4 w-4" /> In hoá đơn
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
          billingId={payDialog.id}
          balance={payDialog.balance}
          onSuccess={() => setPayDialog(null)}
        />
      )}
    </div>
  );
}
