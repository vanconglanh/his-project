"use client";

import { useState } from "react";
import Link from "next/link";
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
import { Separator } from "@/components/ui/separator";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  ChevronLeft,
  CreditCard,
  FileText,
  Printer,
  CheckCircle,
  XCircle,
  ShieldCheck,
} from "lucide-react";
import { useBilling, useFinalizeBilling, useVoidBilling, useApplyBhyt } from "@/lib/hooks/use-billing";
import { usePayments } from "@/lib/hooks/use-payments";
import { useEInvoices } from "@/lib/hooks/use-einvoice";
import { BillingStatusBadge } from "@/components/domain/BillingStatusBadge";
import { PaymentDialog } from "@/components/domain/PaymentDialog";
import { EInvoiceIssueDialog } from "@/components/domain/EInvoiceIssueDialog";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { formatCurrency } from "@/lib/utils/format";
import { printBillingPdf } from "@/lib/api/billing";
import { downloadEInvoiceXml as downloadXml } from "@/lib/api/einvoice";
import { format } from "date-fns";
import { cn } from "@/lib/utils";

interface Props {
  id: string;
}

export function BillingDetailClient({ id }: Props) {
  const [payOpen, setPayOpen] = useState(false);
  const [einvoiceOpen, setEinvoiceOpen] = useState(false);
  const [voidOpen, setVoidOpen] = useState(false);
  const [voidReason, setVoidReason] = useState("");
  const [bhytOpen, setBhytOpen] = useState(false);

  const { data: billing, isLoading } = useBilling(id);
  const { data: paymentsData } = usePayments({ billing_id: id });
  const { data: einvoiceData } = useEInvoices({ billing_id: id });

  const finalize = useFinalizeBilling();
  const voidBilling = useVoidBilling();
  const applyBhyt = useApplyBhyt();

  const payments = paymentsData?.data ?? [];
  const einvoice = einvoiceData?.data?.[0];

  async function handleFinalize() {
    try {
      await finalize.mutateAsync(id);
      toast.success("Đã xác nhận hoá đơn");
    } catch {
      toast.error("Không thể xác nhận hoá đơn");
    }
  }

  async function handleVoid(reason: string) {
    try {
      await voidBilling.mutateAsync({ id, reason });
      toast.success("Đã huỷ hoá đơn");
    } catch {
      toast.error("Không thể huỷ hoá đơn");
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-48 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (!billing) {
    return (
      <div className="flex h-64 items-center justify-center text-muted-foreground">
        Không tìm thấy hoá đơn
      </div>
    );
  }

  const canPay = (billing.status === "FINALIZED" || billing.status === "PARTIAL_PAID") && billing.balance > 0;
  const canFinalize = billing.status === "DRAFT";
  const canVoid = billing.status !== "VOID" && billing.status !== "PAID";
  const einvoiceIssued = einvoice?.status === "ISSUED";

  return (
    <div className="space-y-6">
      {/* Back + header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <Link href="/cashier" className="inline-flex h-8 w-8 items-center justify-center rounded-lg hover:bg-muted">
            <ChevronLeft className="h-5 w-5" />
          </Link>
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl font-bold">{billing.bill_no}</h2>
              <BillingStatusBadge status={billing.status} />
            </div>
            <p className="text-sm text-muted-foreground">
              {billing.patient_summary?.full_name} — {format(new Date(billing.created_at), "dd/MM/yyyy HH:mm")}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {canFinalize && (
            <Button variant="outline" size="sm" onClick={handleFinalize} disabled={finalize.isPending}>
              <CheckCircle className="mr-2 h-4 w-4" />
              Xác nhận hoá đơn
            </Button>
          )}
          {canPay && (
            <Button size="sm" onClick={() => setPayOpen(true)}>
              <CreditCard className="mr-2 h-4 w-4" />
              Thu tiền
            </Button>
          )}
          <Button variant="outline" size="sm" onClick={() => printBillingPdf(id)}>
            <Printer className="mr-2 h-4 w-4" />
            In hoá đơn
          </Button>
          {canVoid && (
            <Button variant="outline" size="sm" className="text-destructive border-destructive hover:bg-destructive/10" onClick={() => setVoidOpen(true)}>
              <XCircle className="mr-2 h-4 w-4" />
              Huỷ hoá đơn
            </Button>
          )}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Left: items + summary */}
        <div className="space-y-6 lg:col-span-2">
          {/* Items table */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Mục hoá đơn</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nội dung</TableHead>
                    <TableHead className="text-right">SL</TableHead>
                    <TableHead className="text-right">Đơn giá</TableHead>
                    <TableHead className="text-right">VAT</TableHead>
                    <TableHead className="text-right">Thành tiền</TableHead>
                    <TableHead>BHYT</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {billing.items.map((item) => (
                    <TableRow key={item.id}>
                      <TableCell>
                        <div>
                          <p className="font-medium text-sm">{item.name}</p>
                          {item.code && <p className="text-xs text-muted-foreground">{item.code}</p>}
                        </div>
                      </TableCell>
                      <TableCell className="text-right">{item.quantity}</TableCell>
                      <TableCell className="text-right">{formatCurrency(item.unit_price)}</TableCell>
                      <TableCell className="text-right">{item.vat_rate}%</TableCell>
                      <TableCell className="text-right font-medium">{formatCurrency(item.line_total)}</TableCell>
                      <TableCell>
                        {item.bhyt_applicable && (
                          <Badge variant="outline" className="text-xs bg-blue-50 text-blue-700 border-blue-200">
                            BHYT
                          </Badge>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>

          {/* Payments */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Lịch sử thanh toán</CardTitle>
            </CardHeader>
            <CardContent>
              {payments.length === 0 ? (
                <p className="text-sm text-muted-foreground">Chưa có thanh toán</p>
              ) : (
                <div className="space-y-2">
                  {payments.map((p) => (
                    <div key={p.id} className="flex items-center justify-between rounded-lg bg-muted/30 px-3 py-2 text-sm">
                      <div>
                        <span className="font-medium">{p.method.replace("QR_", "")}</span>
                        {p.reference && <span className="ml-2 text-muted-foreground text-xs">#{p.reference}</span>}
                      </div>
                      <div className="flex items-center gap-3">
                        <span>{p.paid_at ? format(new Date(p.paid_at), "dd/MM HH:mm") : "—"}</span>
                        <span className="font-semibold text-green-600">{formatCurrency(p.amount)}</span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Right: summary + BHYT + eInvoice */}
        <div className="space-y-4">
          {/* Summary */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Tổng hợp</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <SummaryRow label="Tạm tính" value={formatCurrency(billing.subtotal)} />
              <SummaryRow label="VAT" value={formatCurrency(billing.vat_total)} />
              {billing.discount_amount > 0 && (
                <SummaryRow label="Giảm giá" value={`-${formatCurrency(billing.discount_amount)}`} className="text-green-600" />
              )}
              {billing.bhyt_amount > 0 && (
                <SummaryRow label="BHYT thanh toán" value={`-${formatCurrency(billing.bhyt_amount)}`} className="text-blue-600" />
              )}
              <Separator />
              <SummaryRow label="Bệnh nhân phải trả" value={formatCurrency(billing.patient_payable)} className="font-bold text-base" />
              <SummaryRow label="Đã thu" value={formatCurrency(billing.paid_amount)} className="text-green-600" />
              <SummaryRow
                label="Còn lại"
                value={formatCurrency(billing.balance)}
                className={billing.balance > 0 ? "font-bold text-destructive" : "text-muted-foreground"}
              />
            </CardContent>
          </Card>

          {/* BHYT */}
          {(billing.payer === "BHYT" || billing.payer === "MIXED") && billing.bhyt_amount === 0 && (
            <Card>
              <CardContent className="pt-4">
                <Button
                  variant="outline"
                  className="w-full border-blue-300 text-blue-700 hover:bg-blue-50"
                  onClick={() => setBhytOpen(true)}
                >
                  <ShieldCheck className="mr-2 h-4 w-4" />
                  Áp dụng BHYT
                </Button>
              </CardContent>
            </Card>
          )}

          {/* eInvoice */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Hoá đơn điện tử</CardTitle>
            </CardHeader>
            <CardContent>
              {einvoice ? (
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Số HĐ:</span>
                    <span className="font-mono font-semibold">{einvoice.invoice_no}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Mã CQT:</span>
                    <span className="font-mono text-xs">{einvoice.cqt_code}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Nhà cung cấp:</span>
                    <Badge variant="outline">{einvoice.provider}</Badge>
                  </div>
                  <div className="flex gap-2 mt-3">
                    {einvoice.pdf_url && (
                      <a
                        href={einvoice.pdf_url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex flex-1 items-center justify-center gap-1 rounded-lg border border-border px-2.5 py-1 text-[0.8rem] font-medium hover:bg-muted"
                      >
                        <FileText className="h-4 w-4" /> PDF
                      </a>
                    )}
                    <Button size="sm" variant="outline" className="flex-1" onClick={() => downloadXml(einvoice.id)}>
                      <FileText className="mr-1 h-4 w-4" /> XML
                    </Button>
                  </div>
                </div>
              ) : (
                <Button
                  variant="outline"
                  className="w-full"
                  onClick={() => setEinvoiceOpen(true)}
                  disabled={billing.status !== "PAID" && billing.status !== "FINALIZED"}
                >
                  <FileText className="mr-2 h-4 w-4" />
                  Phát hành HĐĐT
                </Button>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Dialogs */}
      <PaymentDialog
        open={payOpen}
        onOpenChange={setPayOpen}
        billingId={id}
        balance={billing.balance}
      />

      <EInvoiceIssueDialog
        open={einvoiceOpen}
        onOpenChange={setEinvoiceOpen}
        billingId={id}
      />

      <ConfirmDialog
        open={voidOpen}
        onOpenChange={(o) => { setVoidOpen(o); if (!o) setVoidReason(""); }}
        title="Huỷ hoá đơn"
        description={
          <div className="space-y-2">
            <p>Vui lòng nhập lý do huỷ hoá đơn (tối thiểu 5 ký tự)</p>
            <input
              className="w-full rounded-md border px-3 py-2 text-sm"
              placeholder="Lý do huỷ..."
              value={voidReason}
              onChange={(e) => setVoidReason(e.target.value)}
            />
          </div>
        }
        confirmLabel="Huỷ hoá đơn"
        variant="destructive"
        onConfirm={() => { if (voidReason.length >= 5) handleVoid(voidReason); else toast.error("Lý do tối thiểu 5 ký tự"); }}
      />
    </div>
  );
}

function SummaryRow({ label, value, className }: { label: string; value: string; className?: string }) {
  return (
    <div className="flex items-center justify-between">
      <span className="text-muted-foreground">{label}</span>
      <span className={className}>{value}</span>
    </div>
  );
}
