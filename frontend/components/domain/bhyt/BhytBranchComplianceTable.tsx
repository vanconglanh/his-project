"use client";

import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { CheckCircle2, XCircle } from "lucide-react";
import { cn } from "@/lib/utils";
import { useBranchBhytCompliance } from "@/lib/hooks/use-branches";
import type { BranchBhytComplianceResponse } from "@/lib/api/branches";

function ComplianceIcon({ ok }: { ok: boolean }) {
  return ok ? (
    <CheckCircle2 className="h-4 w-4 text-green-600 mx-auto" aria-label="Đạt" />
  ) : (
    <XCircle className="h-4 w-4 text-red-600 mx-auto" aria-label="Chưa đạt" />
  );
}

function rowHasWarning(row: BranchBhytComplianceResponse): boolean {
  return (
    !row.has_cskcb ||
    (row.bhyt_enabled && !row.bhyt_contract_valid) ||
    (row.bhyt_enabled && !row.dtqg_connected) ||
    (row.dtqg_connected && !row.dtqg_token_valid)
  );
}

export function BhytBranchComplianceTable() {
  const { data, isLoading } = useBranchBhytCompliance();
  const rows = data ?? [];

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (rows.length === 0) {
    return (
      <div className="py-10 text-center text-sm text-muted-foreground">
        Chưa có dữ liệu tuân thủ BHYT cho chi nhánh nào.
      </div>
    );
  }

  return (
    <div className="rounded-md border overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Chi nhánh</TableHead>
            <TableHead className="text-center">Mã CSKCB</TableHead>
            <TableHead className="text-center">Khám BHYT</TableHead>
            <TableHead className="text-center">Hợp đồng còn hiệu lực</TableHead>
            <TableHead className="text-center">ĐTQG kết nối</TableHead>
            <TableHead className="text-center">Token còn hạn</TableHead>
            <TableHead>Kỳ giám định gần nhất</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => (
            <TableRow
              key={row.branch_id}
              className={cn(rowHasWarning(row) && "bg-amber-50")}
            >
              <TableCell className="font-medium">{row.name}</TableCell>
              <TableCell className="text-center">
                <ComplianceIcon ok={row.has_cskcb} />
              </TableCell>
              <TableCell className="text-center">
                <ComplianceIcon ok={row.bhyt_enabled} />
              </TableCell>
              <TableCell className="text-center">
                <ComplianceIcon ok={row.bhyt_contract_valid} />
              </TableCell>
              <TableCell className="text-center">
                <ComplianceIcon ok={row.dtqg_connected} />
              </TableCell>
              <TableCell className="text-center">
                <ComplianceIcon ok={row.dtqg_token_valid} />
              </TableCell>
              <TableCell className="text-sm text-muted-foreground">
                {row.last_bhyt_export_period ?? "Chưa gửi"}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
