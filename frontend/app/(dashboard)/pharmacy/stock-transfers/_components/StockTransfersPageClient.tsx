"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Plus, ArrowLeftRight } from "lucide-react";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import { useStockTransfers } from "@/lib/hooks/use-stock-transfers";
import { useBranches } from "@/lib/hooks/use-branches";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { StockTransferStatusBadge } from "@/components/domain/StockTransferStatusBadge";
import { STOCK_TRANSFER_STATUSES, type StockTransferStatus } from "@/lib/api/stock-transfers";

const STATUS_LABEL: Record<StockTransferStatus, string> = {
  DRAFT: "Nháp",
  PENDING_APPROVAL: "Chờ duyệt",
  APPROVED: "Đã duyệt",
  REJECTED: "Từ chối",
  IN_TRANSIT: "Đang vận chuyển",
  RECEIVED: "Đã nhận đủ",
  PARTIALLY_RECEIVED: "Nhận thiếu",
  CLOSED: "Đã đóng",
  CANCELLED: "Đã huỷ",
};

export function StockTransfersPageClient() {
  const router = useRouter();
  const { has } = usePermissions();
  const [status, setStatus] = useState<StockTransferStatus | "ALL">("ALL");
  const [branchId, setBranchId] = useState<string>("ALL");

  const { data: branchesData } = useBranches();
  const branches = branchesData?.data ?? [];

  const { data, isLoading } = useStockTransfers({
    status: status === "ALL" ? undefined : status,
    branch_id: branchId === "ALL" ? undefined : Number(branchId),
    page: 1,
    page_size: 50,
  });

  const items = data?.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <Select value={status} onValueChange={(v) => setStatus((v as StockTransferStatus | "ALL") ?? "ALL")}>
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Trạng thái" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ALL">Tất cả trạng thái</SelectItem>
              {STOCK_TRANSFER_STATUSES.map((s) => (
                <SelectItem key={s} value={s}>{STATUS_LABEL[s]}</SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={branchId} onValueChange={(v) => setBranchId(v ?? "ALL")}>
            <SelectTrigger className="w-[200px]">
              <SelectValue placeholder="Chi nhánh" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ALL">Tất cả chi nhánh</SelectItem>
              {branches.map((b) => (
                <SelectItem key={b.id} value={String(b.id)}>{b.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {has("stock_transfer.create") && (
          <Button size="sm" onClick={() => router.push("/pharmacy/stock-transfers/new")}>
            <Plus className="h-4 w-4 mr-2" />
            Tạo phiếu điều chuyển
          </Button>
        )}
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
        </div>
      ) : items.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed py-16 text-center">
          <ArrowLeftRight className="h-10 w-10 text-muted-foreground" />
          <div>
            <p className="font-medium">Chưa có phiếu điều chuyển kho nào</p>
            <p className="text-sm text-muted-foreground">Tạo phiếu để chuyển thuốc/vật tư giữa các chi nhánh</p>
          </div>
          {has("stock_transfer.create") && (
            <Button size="sm" onClick={() => router.push("/pharmacy/stock-transfers/new")}>
              <Plus className="h-4 w-4 mr-2" />
              Tạo phiếu điều chuyển
            </Button>
          )}
        </div>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã phiếu</TableHead>
                <TableHead>Chi nhánh gửi</TableHead>
                <TableHead>Chi nhánh nhận</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead className="text-right">Giá trị</TableHead>
                <TableHead>Ngày tạo</TableHead>
                <TableHead>Người tạo</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((t) => (
                <TableRow key={t.id} className="cursor-pointer hover:bg-muted/50">
                  <TableCell className="font-mono text-sm">
                    <Link href={`/pharmacy/stock-transfers/${t.id}`} className="hover:underline">
                      {t.transfer_no}
                    </Link>
                  </TableCell>
                  <TableCell>{t.from_branch_name ?? `#${t.from_branch_id}`}</TableCell>
                  <TableCell>{t.to_branch_name ?? `#${t.to_branch_id}`}</TableCell>
                  <TableCell><StockTransferStatusBadge status={t.status} /></TableCell>
                  <TableCell className="text-right font-medium">
                    {t.total_value.toLocaleString("vi-VN")}đ
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {t.created_at ? format(parseISO(t.created_at), "dd/MM/yyyy HH:mm", { locale: vi }) : "—"}
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">{t.requested_by ?? "—"}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}
