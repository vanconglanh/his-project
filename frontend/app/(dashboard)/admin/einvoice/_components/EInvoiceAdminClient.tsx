"use client";

import { useState } from "react";
import { toast } from "sonner";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { FileText, XCircle, Download } from "lucide-react";
import { useEInvoices, useCancelEInvoice } from "@/lib/hooks/use-einvoice";
import { downloadEInvoiceXml } from "@/lib/api/einvoice";
import { formatCurrency } from "@/lib/utils/format";
import { format } from "date-fns";
import type { EInvoiceProvider, EInvoiceStatus } from "@/lib/api/einvoice";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";

const STATUS_CONFIG: Record<EInvoiceStatus, { label: string; variant: string }> = {
  DRAFT: { label: "Nháp", variant: "secondary" },
  ISSUED: { label: "Đã phát hành", variant: "success" },
  CANCELLED: { label: "Đã huỷ", variant: "destructive" },
  REPLACED: { label: "Thay thế", variant: "outline" },
};

export function EInvoiceAdminClient() {
  const [provider, setProvider] = useState<EInvoiceProvider | undefined>();
  const [status, setStatus] = useState<EInvoiceStatus | undefined>();
  const [cancelTarget, setCancelTarget] = useState<{ id: string; invoice_no: string } | null>(null);
  const [cancelReason, setCancelReason] = useState("");

  const { data, isLoading } = useEInvoices({ provider, status, page_size: 50 });
  const cancelEInvoice = useCancelEInvoice();
  const rows = data?.data ?? [];

  async function handleCancel() {
    if (!cancelTarget || cancelReason.length < 5) {
      toast.error("Lý do tối thiểu 5 ký tự");
      return;
    }
    try {
      await cancelEInvoice.mutateAsync({ id: cancelTarget.id, reason: cancelReason });
      toast.success("Đã huỷ HĐĐT");
      setCancelTarget(null);
      setCancelReason("");
    } catch {
      toast.error("Huỷ HĐĐT thất bại");
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Hoá đơn điện tử</h2>
        <p className="text-sm text-muted-foreground">Quản lý HĐĐT đã phát hành qua MISA / VNPT / EFY</p>
      </div>

      {/* Provider config card (mock) */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Cấu hình nhà cung cấp HĐĐT</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4 md:grid-cols-4">
          <div>
            <Label>Nhà cung cấp</Label>
            <Select defaultValue="MISA">
              <SelectTrigger className="mt-1">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="MISA">MISA</SelectItem>
                <SelectItem value="VNPT">VNPT</SelectItem>
                <SelectItem value="EFY">EFY</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label>Username</Label>
            <Input className="mt-1" placeholder="Tài khoản API" />
          </div>
          <div>
            <Label>API Key</Label>
            <Input type="password" className="mt-1" placeholder="••••••••" />
          </div>
          <div className="flex items-end">
            <Button variant="outline" className="w-full" onClick={() => toast.info("Dev mock: cấu hình đã lưu")}>
              Lưu cấu hình
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Filters */}
      <div className="flex items-center gap-3">
        <Select value={provider ?? "all"} onValueChange={(v) => setProvider(v === "all" ? undefined : v as EInvoiceProvider)}>
          <SelectTrigger className="w-36">
            <SelectValue placeholder="Nhà cung cấp" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả</SelectItem>
            <SelectItem value="MISA">MISA</SelectItem>
            <SelectItem value="VNPT">VNPT</SelectItem>
            <SelectItem value="EFY">EFY</SelectItem>
          </SelectContent>
        </Select>
        <Select value={status ?? "all"} onValueChange={(v) => setStatus(v === "all" ? undefined : v as EInvoiceStatus)}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Trạng thái" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả</SelectItem>
            <SelectItem value="ISSUED">Đã phát hành</SelectItem>
            <SelectItem value="CANCELLED">Đã huỷ</SelectItem>
            <SelectItem value="DRAFT">Nháp</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Số HĐ</TableHead>
              <TableHead>Mã CQT</TableHead>
              <TableHead>Nhà cung cấp</TableHead>
              <TableHead className="text-right">Tổng tiền</TableHead>
              <TableHead>Ngày phát hành</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead className="w-20" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading
              ? Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 7 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                    ))}
                  </TableRow>
                ))
              : rows.length === 0
              ? (
                <TableRow>
                  <TableCell colSpan={7} className="h-32 text-center text-muted-foreground">
                    Chưa có hoá đơn điện tử
                  </TableCell>
                </TableRow>
              )
              : rows.map((inv) => {
                const sc = STATUS_CONFIG[inv.status];
                return (
                  <TableRow key={inv.id}>
                    <TableCell className="font-mono text-sm font-semibold">{inv.invoice_no}</TableCell>
                    <TableCell className="font-mono text-xs">{inv.cqt_code}</TableCell>
                    <TableCell><Badge variant="outline">{inv.provider}</Badge></TableCell>
                    <TableCell className="text-right">{formatCurrency(inv.total_amount)}</TableCell>
                    <TableCell className="text-xs text-muted-foreground">
                      {format(new Date(inv.issue_date), "dd/MM/yyyy HH:mm")}
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant={inv.status === "ISSUED" ? "default" : inv.status === "CANCELLED" ? "destructive" : "secondary"}
                        className="text-xs"
                      >
                        {sc.label}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        {inv.pdf_url && (
                          <a
                            href={inv.pdf_url}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex h-7 w-7 items-center justify-center rounded-lg hover:bg-muted"
                          >
                            <FileText className="h-4 w-4" />
                          </a>
                        )}
                        <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => downloadEInvoiceXml(inv.id)}>
                          <Download className="h-4 w-4" />
                        </Button>
                        {inv.status === "ISSUED" && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-7 w-7 text-destructive"
                            onClick={() => setCancelTarget({ id: inv.id, invoice_no: inv.invoice_no })}
                          >
                            <XCircle className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })}
          </TableBody>
        </Table>
      </div>

      <ConfirmDialog
        open={Boolean(cancelTarget)}
        onOpenChange={(o) => { if (!o) { setCancelTarget(null); setCancelReason(""); } }}
        title="Huỷ hoá đơn điện tử"
        description={
          <div className="space-y-2">
            <p>Huỷ hoá đơn <strong>{cancelTarget?.invoice_no}</strong>. Nhập lý do:</p>
            <input
              className="w-full rounded-md border px-3 py-2 text-sm"
              placeholder="Lý do huỷ (tối thiểu 5 ký tự)"
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
            />
          </div>
        }
        confirmLabel="Huỷ HĐĐT"
        variant="destructive"
        onConfirm={handleCancel}
        isLoading={cancelEInvoice.isPending}
      />
    </div>
  );
}
