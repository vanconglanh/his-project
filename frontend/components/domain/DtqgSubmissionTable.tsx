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
import { Button } from "@/components/ui/button";
import { RefreshCw } from "lucide-react";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import type { DtqgSubmissionResponse } from "@/lib/api/dtqg";
import { useRetryDtqg } from "@/lib/hooks/use-dtqg";

const STATUS_VARIANT: Record<string, { label: string; cls: string }> = {
  PENDING: { label: "Đang chờ", cls: "bg-yellow-100 text-yellow-800 border-yellow-300" },
  SUBMITTED: { label: "Đã gửi", cls: "bg-blue-100 text-blue-800 border-blue-300" },
  ACCEPTED: { label: "Chấp nhận", cls: "bg-green-100 text-green-800 border-green-300" },
  REJECTED: { label: "Bị từ chối", cls: "bg-red-100 text-red-800 border-red-300" },
};

interface Props {
  submissions: DtqgSubmissionResponse[];
}

export function DtqgSubmissionTable({ submissions }: Props) {
  const retry = useRetryDtqg();

  if (submissions.length === 0) {
    return (
      <div className="flex flex-col items-center py-12 text-muted-foreground text-sm gap-2">
        <p>Chưa có dữ liệu gửi ĐTQG</p>
      </div>
    );
  }

  return (
    <div className="rounded-md border overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Mã đơn thuốc (ĐTQG)</TableHead>
            <TableHead>Trạng thái</TableHead>
            <TableHead>Lần thử</TableHead>
            <TableHead>Gửi lúc</TableHead>
            <TableHead>Lỗi</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {submissions.map((s) => {
            const cfg = STATUS_VARIANT[s.status] ?? { label: s.status, cls: "" };
            return (
              <TableRow key={s.id}>
                <TableCell className="font-mono text-sm">{s.ma_don_thuoc ?? "-"}</TableCell>
                <TableCell>
                  <Badge className={cfg.cls} variant="outline">{cfg.label}</Badge>
                </TableCell>
                <TableCell className="text-center">{s.retry_count}</TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {s.submitted_at
                    ? format(parseISO(s.submitted_at), "dd/MM HH:mm", { locale: vi })
                    : "-"}
                </TableCell>
                <TableCell className="text-xs text-destructive max-w-[200px] truncate">
                  {s.error_message ?? "-"}
                </TableCell>
                <TableCell>
                  {s.status === "REJECTED" && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => retry.mutate(s.prescription_id)}
                      disabled={retry.isPending}
                    >
                      <RefreshCw className="h-4 w-4 mr-1" />
                      Gửi lại
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}
