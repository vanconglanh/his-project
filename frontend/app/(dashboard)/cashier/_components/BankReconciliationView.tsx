"use client";

import { useState } from "react";
import { PageHeader } from "@/components/ui/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Landmark, UploadCloud } from "lucide-react";
import { formatDateTime } from "@/lib/utils/format";
import { useBankStatements } from "@/lib/hooks/use-bank-reconciliation";
import { ImportStatementDialog } from "./ImportStatementDialog";
import { StatementLinesTable } from "./StatementLinesTable";

export function BankReconciliationView() {
  const [importOpen, setImportOpen] = useState(false);
  const [selectedStatementId, setSelectedStatementId] = useState<string | null>(null);

  const { data, isLoading, isError } = useBankStatements({ page: 1, page_size: 50 });
  const rows = data?.data ?? [];

  if (selectedStatementId) {
    return (
      <StatementLinesTable
        statementId={selectedStatementId}
        onBack={() => setSelectedStatementId(null)}
      />
    );
  }

  return (
    <div className="space-y-5">
      <PageHeader
        title="Đối soát ngân hàng"
        description="Tải lên sao kê ngân hàng/POS và đối chiếu tự động với khoản thu hệ thống"
        actions={
          <Button onClick={() => setImportOpen(true)}>
            <UploadCloud className="h-4 w-4 mr-1" />
            Tải lên sao kê
          </Button>
        }
      />

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
              Không tải được lịch sử đối soát. Vui lòng thử lại.
            </div>
          ) : rows.length === 0 ? (
            <div className="p-10 text-center">
              <Landmark className="h-10 w-10 mx-auto text-muted-foreground mb-2" />
              <p className="text-sm text-muted-foreground mb-3">
                Chưa có sao kê nào được tải lên.
              </p>
              <Button variant="outline" size="sm" onClick={() => setImportOpen(true)}>
                <UploadCloud className="h-4 w-4 mr-1" />
                Tải lên sao kê đầu tiên
              </Button>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Tên file</TableHead>
                  <TableHead>Ngân hàng</TableHead>
                  <TableHead>Kỳ sao kê</TableHead>
                  <TableHead className="text-right">Tổng dòng</TableHead>
                  <TableHead className="text-right">Đã khớp</TableHead>
                  <TableHead className="text-right">Chưa khớp</TableHead>
                  <TableHead>Thời gian tải</TableHead>
                  <TableHead>Người tải</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((s) => (
                  <TableRow
                    key={s.id}
                    className="cursor-pointer hover:bg-muted/50"
                    onClick={() => setSelectedStatementId(s.id)}
                  >
                    <TableCell className="font-medium">{s.file_name}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {s.bank_code ?? "—"}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {s.statement_date ?? "—"}
                    </TableCell>
                    <TableCell className="text-right">{s.total_lines}</TableCell>
                    <TableCell className="text-right">
                      <Badge
                        variant="outline"
                        className="text-xs bg-green-100 text-green-700 border-green-200 hover:bg-green-100"
                      >
                        {s.matched_lines}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">
                      {s.unmatched_lines > 0 ? (
                        <Badge
                          variant="outline"
                          className="text-xs bg-amber-100 text-amber-700 border-amber-200 hover:bg-amber-100"
                        >
                          {s.unmatched_lines}
                        </Badge>
                      ) : (
                        <span className="text-xs text-muted-foreground">0</span>
                      )}
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground">
                      {formatDateTime(s.uploaded_at)}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {s.uploaded_by_name ?? "—"}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <ImportStatementDialog open={importOpen} onOpenChange={setImportOpen} />
    </div>
  );
}
