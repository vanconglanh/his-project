"use client";

import { useState } from "react";
import { toast } from "sonner";
import { PageHeader } from "@/components/ui/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useInterBranchDebts, useSettleInterBranchDebt } from "@/lib/hooks/use-inter-branch-debts";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { formatCurrency, formatDateTime } from "@/lib/utils/format";
import { Handshake } from "lucide-react";

const SOURCE_TYPE_LABEL: Record<string, string> = {
  CROSS_BRANCH_PAYMENT: "Thu hộ chi nhánh khác",
  STOCK_TRANSFER: "Điều chuyển kho",
};

const STATUS_LABEL: Record<string, string> = {
  OPEN: "Chưa đối soát",
  SETTLED: "Đã đối soát",
};

export function InterBranchDebtsClient() {
  const [status, setStatus] = useState<string>("OPEN");
  const { hasAny } = usePermissions();
  const canSettle = hasAny(["inter_branch_debt.settle"]);

  const { data, isLoading, isError } = useInterBranchDebts({
    status: status === "ALL" ? undefined : status,
    page: 1,
    page_size: 50,
  });
  const settleMutation = useSettleInterBranchDebt();

  const rows = data?.data ?? [];

  function handleSettle(id: string) {
    if (!window.confirm("Xác nhận đã đối soát khoản công nợ nội bộ này?")) return;
    settleMutation.mutate(
      { id },
      {
        onSuccess: () => toast.success("Đã đánh dấu đối soát công nợ nội bộ"),
        onError: () => toast.error("Không thể đánh dấu đối soát. Vui lòng thử lại."),
      }
    );
  }

  return (
    <div className="space-y-5">
      <PageHeader
        title="Công nợ nội bộ giữa các chi nhánh"
        description="Bút toán phát sinh khi thu hộ hoá đơn chi nhánh khác (BR-85) hoặc điều chuyển kho (BR-87)"
      />

      <div className="flex items-end gap-3">
        <div className="flex flex-col gap-1">
          <Label className="text-xs">Trạng thái</Label>
          <Select
            items={{ ALL: "Tất cả", OPEN: "Chưa đối soát", SETTLED: "Đã đối soát" }}
            value={status}
            onValueChange={(v) => v && setStatus(v)}
          >
            <SelectTrigger className="w-48 h-9">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ALL">Tất cả</SelectItem>
              <SelectItem value="OPEN">Chưa đối soát</SelectItem>
              <SelectItem value="SETTLED">Đã đối soát</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-4 space-y-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : isError ? (
            <div className="p-10 text-center text-sm text-muted-foreground">
              Không tải được danh sách công nợ nội bộ. Vui lòng thử lại.
            </div>
          ) : rows.length === 0 ? (
            <div className="p-10 text-center">
              <Handshake className="h-10 w-10 mx-auto text-muted-foreground mb-2" />
              <p className="text-sm text-muted-foreground">Chưa có công nợ nội bộ nào.</p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Chi nhánh nợ</TableHead>
                  <TableHead>Chi nhánh được nợ</TableHead>
                  <TableHead className="text-right">Số tiền</TableHead>
                  <TableHead>Nguồn</TableHead>
                  <TableHead>Mã chứng từ</TableHead>
                  <TableHead>Trạng thái</TableHead>
                  <TableHead>Thời gian tạo</TableHead>
                  {canSettle && <TableHead className="text-right">Thao tác</TableHead>}
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell className="font-medium">
                      {d.debtor_branch_name ?? `#${d.debtor_branch_id}`}
                    </TableCell>
                    <TableCell className="font-medium">
                      {d.creditor_branch_name ?? `#${d.creditor_branch_id}`}
                    </TableCell>
                    <TableCell className="text-right font-semibold">{formatCurrency(d.amount)}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {SOURCE_TYPE_LABEL[d.source_type] ?? d.source_type}
                    </TableCell>
                    <TableCell className="font-mono text-xs">{d.source_ref_code ?? "—"}</TableCell>
                    <TableCell>
                      <Badge
                        variant={d.status === "SETTLED" ? "secondary" : "outline"}
                        className="text-xs"
                      >
                        {STATUS_LABEL[d.status] ?? d.status}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground">
                      {formatDateTime(d.created_at)}
                    </TableCell>
                    {canSettle && (
                      <TableCell className="text-right">
                        {d.status === "OPEN" ? (
                          <Button
                            size="sm"
                            variant="outline"
                            className="min-h-[36px]"
                            disabled={settleMutation.isPending}
                            onClick={() => handleSettle(d.id)}
                          >
                            Đánh dấu đã đối soát
                          </Button>
                        ) : (
                          <span className="text-xs text-muted-foreground">
                            {d.settled_at ? formatDateTime(d.settled_at) : "—"}
                          </span>
                        )}
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
