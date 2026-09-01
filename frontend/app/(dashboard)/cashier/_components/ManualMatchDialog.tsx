"use client";

import { isAxiosError } from "axios";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Link2 } from "lucide-react";
import { formatCurrency, formatDateTime } from "@/lib/utils/format";
import {
  useBankStatementLineCandidates,
  useManualMatchBankStatementLine,
} from "@/lib/hooks/use-bank-reconciliation";

export interface ManualMatchDialogProps {
  statementId: string | null;
  lineId: string | null;
  onOpenChange: (open: boolean) => void;
}

export function ManualMatchDialog({ statementId, lineId, onOpenChange }: ManualMatchDialogProps) {
  const open = !!lineId;
  const { data: candidates, isLoading } = useBankStatementLineCandidates(lineId);
  const matchMutation = useManualMatchBankStatementLine(statementId);

  function handleMatch(paymentId: string) {
    if (!lineId) return;
    matchMutation.mutate(
      { lineId, paymentId },
      {
        onSuccess: () => {
          toast.success("Đã khớp thủ công dòng sao kê với khoản thu");
          onOpenChange(false);
        },
        onError: (error) => {
          const code = isAxiosError(error) ? error.response?.data?.error?.code : undefined;
          const message =
            code === "PAYMENT_ALREADY_MATCHED"
              ? "Khoản thu này đã được khớp với dòng sao kê khác"
              : (isAxiosError(error) && error.response?.data?.error?.message) ||
                "Không thể khớp thủ công. Vui lòng thử lại.";
          toast.error(message);
        },
      }
    );
  }

  return (
    <Dialog open={open} onOpenChange={(nextOpen) => !nextOpen && onOpenChange(false)}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>Khớp thủ công dòng sao kê</DialogTitle>
          <DialogDescription>
            Chọn khoản thu tương ứng để khớp thủ công với dòng sao kê này.
          </DialogDescription>
        </DialogHeader>

        <div className="rounded-lg border max-h-[420px] overflow-y-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã tham chiếu</TableHead>
                <TableHead>Phương thức</TableHead>
                <TableHead className="text-right">Số tiền</TableHead>
                <TableHead>Thời gian TT</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 5 }).map((_, j) => (
                      <TableCell key={j}>
                        <Skeleton className="h-5 w-full" />
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : !candidates || candidates.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    Không tìm thấy khoản thu phù hợp để khớp
                  </TableCell>
                </TableRow>
              ) : (
                candidates.map((c) => (
                  <TableRow key={c.id}>
                    <TableCell className="font-mono text-xs">{c.reference}</TableCell>
                    <TableCell className="text-sm">{c.method}</TableCell>
                    <TableCell className="text-right font-semibold">
                      {formatCurrency(c.amount)}
                    </TableCell>
                    <TableCell className="text-xs text-muted-foreground">
                      {formatDateTime(c.paid_at)}
                    </TableCell>
                    <TableCell className="text-right">
                      <Button
                        size="sm"
                        className="min-h-[36px]"
                        disabled={matchMutation.isPending}
                        onClick={() => handleMatch(c.id)}
                      >
                        <Link2 className="h-4 w-4 mr-1" />
                        Khớp
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
