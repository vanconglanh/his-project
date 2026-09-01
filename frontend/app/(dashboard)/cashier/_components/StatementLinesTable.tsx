"use client";

import { useState } from "react";
import { isAxiosError } from "axios";
import { toast } from "sonner";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { ArrowLeft, Ban, FileSpreadsheet, Link2, Unlink } from "lucide-react";
import { formatCurrency, formatDateTime } from "@/lib/utils/format";
import {
  useBankStatementLines,
  useIgnoreBankStatementLine,
  useUnmatchBankStatementLine,
} from "@/lib/hooks/use-bank-reconciliation";
import type { BankStatementLineMatchStatus } from "@/lib/api/bank-reconciliation";
import { ManualMatchDialog } from "./ManualMatchDialog";

const STATUS_BADGE: Record<
  BankStatementLineMatchStatus,
  { label: string; className: string }
> = {
  MATCHED: { label: "Đã khớp", className: "bg-green-100 text-green-700 border-green-200 hover:bg-green-100" },
  MANUAL_MATCHED: { label: "Khớp thủ công", className: "bg-blue-100 text-blue-700 border-blue-200 hover:bg-blue-100" },
  UNMATCHED: { label: "Chưa khớp", className: "bg-amber-100 text-amber-700 border-amber-200 hover:bg-amber-100" },
  IGNORED: { label: "Bỏ qua", className: "bg-gray-100 text-gray-600 border-gray-200 hover:bg-gray-100" },
};

function MatchStatusBadge({ status }: { status: BankStatementLineMatchStatus }) {
  const cfg = STATUS_BADGE[status];
  return (
    <Badge variant="outline" className={`text-xs ${cfg.className}`}>
      {cfg.label}
    </Badge>
  );
}

export interface StatementLinesTableProps {
  statementId: string;
  onBack: () => void;
}

export function StatementLinesTable({ statementId, onBack }: StatementLinesTableProps) {
  const [manualMatchLineId, setManualMatchLineId] = useState<string | null>(null);
  const { data, isLoading, isError } = useBankStatementLines(statementId);
  const ignoreMutation = useIgnoreBankStatementLine(statementId);
  const unmatchMutation = useUnmatchBankStatementLine(statementId);

  const statement = data?.statement;
  const lines = data?.lines ?? [];

  function handleIgnore(lineId: string) {
    if (!window.confirm("Xác nhận bỏ qua dòng sao kê này (không đối chiếu)?")) return;
    ignoreMutation.mutate(lineId, {
      onSuccess: () => toast.success("Đã bỏ qua dòng sao kê"),
      onError: (error) => {
        const message =
          (isAxiosError(error) && error.response?.data?.error?.message) ||
          "Không thể bỏ qua dòng sao kê. Vui lòng thử lại.";
        toast.error(message);
      },
    });
  }

  function handleUnmatch(lineId: string) {
    if (!window.confirm("Xác nhận gỡ khớp dòng sao kê này?")) return;
    unmatchMutation.mutate(lineId, {
      onSuccess: () => toast.success("Đã gỡ khớp dòng sao kê"),
      onError: (error) => {
        const message =
          (isAxiosError(error) && error.response?.data?.error?.message) ||
          "Không thể gỡ khớp. Vui lòng thử lại.";
        toast.error(message);
      },
    });
  }

  return (
    <div className="space-y-4">
      <Button variant="outline" size="sm" className="min-h-[36px]" onClick={onBack}>
        <ArrowLeft className="h-4 w-4 mr-1" />
        Quay lại danh sách
      </Button>

      <Card>
        <CardHeader className="pb-2">
          {isLoading ? (
            <Skeleton className="h-6 w-64" />
          ) : statement ? (
            <div className="flex flex-wrap items-center gap-x-6 gap-y-1">
              <div className="flex items-center gap-2 font-semibold">
                <FileSpreadsheet className="h-4 w-4 text-muted-foreground" />
                {statement.file_name}
              </div>
              <div className="text-sm text-muted-foreground">
                Ngân hàng: {statement.bank_code ?? "—"}
              </div>
              <div className="text-sm text-muted-foreground">
                Kỳ: {statement.statement_date ?? "—"}
              </div>
              <div className="text-sm text-muted-foreground">
                Tổng dòng: {statement.total_lines}
              </div>
              <div className="text-sm text-green-600">Đã khớp: {statement.matched_lines}</div>
              <div className="text-sm text-amber-600">Chưa khớp: {statement.unmatched_lines}</div>
              <div className="text-sm text-muted-foreground">
                Tải lên: {formatDateTime(statement.uploaded_at)}
              </div>
            </div>
          ) : null}
        </CardHeader>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-4 space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : isError ? (
            <div className="p-10 text-center text-sm text-muted-foreground">
              Không tải được chi tiết sao kê. Vui lòng thử lại.
            </div>
          ) : lines.length === 0 ? (
            <div className="p-10 text-center">
              <FileSpreadsheet className="h-10 w-10 mx-auto text-muted-foreground mb-2" />
              <p className="text-sm text-muted-foreground">Sao kê không có dòng giao dịch nào.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Ngày GD</TableHead>
                  <TableHead className="text-right">Số tiền</TableHead>
                  <TableHead>Mã tham chiếu</TableHead>
                  <TableHead>Diễn giải</TableHead>
                  <TableHead>Trạng thái</TableHead>
                  <TableHead>Khoản thu khớp</TableHead>
                  <TableHead className="text-right">Thao tác</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {lines.map((line) => (
                  <TableRow key={line.id}>
                    <TableCell className="text-sm">{formatDateTime(line.transaction_date)}</TableCell>
                    <TableCell className="text-right font-semibold">
                      {formatCurrency(line.amount)}
                    </TableCell>
                    <TableCell className="font-mono text-xs">{line.reference_no ?? "—"}</TableCell>
                    <TableCell className="text-sm text-muted-foreground max-w-[280px] truncate">
                      {line.description ?? "—"}
                    </TableCell>
                    <TableCell>
                      <MatchStatusBadge status={line.match_status} />
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground">
                      {line.matched_payment ? (
                        <div className="space-y-0.5">
                          <div className="font-mono">{line.matched_payment.reference}</div>
                          <div>
                            {line.matched_payment.method} — {formatCurrency(line.matched_payment.amount)}
                          </div>
                        </div>
                      ) : (
                        "—"
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      {line.match_status === "UNMATCHED" ? (
                        <div className="flex justify-end gap-2">
                          <Button
                            size="sm"
                            className="min-h-[36px]"
                            onClick={() => setManualMatchLineId(line.id)}
                          >
                            <Link2 className="h-4 w-4 mr-1" />
                            Khớp thủ công
                          </Button>
                          <Button
                            size="sm"
                            variant="outline"
                            className="min-h-[36px]"
                            disabled={ignoreMutation.isPending}
                            onClick={() => handleIgnore(line.id)}
                          >
                            <Ban className="h-4 w-4 mr-1" />
                            Bỏ qua
                          </Button>
                        </div>
                      ) : line.match_status === "MATCHED" || line.match_status === "MANUAL_MATCHED" ? (
                        <Button
                          size="sm"
                          variant="outline"
                          className="min-h-[36px]"
                          disabled={unmatchMutation.isPending}
                          onClick={() => handleUnmatch(line.id)}
                        >
                          <Unlink className="h-4 w-4 mr-1" />
                          Gỡ khớp
                        </Button>
                      ) : (
                        <span className="text-xs text-muted-foreground">—</span>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <ManualMatchDialog
        statementId={statementId}
        lineId={manualMatchLineId}
        onOpenChange={(open) => !open && setManualMatchLineId(null)}
      />
    </div>
  );
}
