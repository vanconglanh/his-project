"use client";

import { useState } from "react";
import { PageHeader } from "@/components/ui/page-header";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Plus, ArrowLeftRight } from "lucide-react";
import { InternalReferralCreateDialog } from "@/components/domain/InternalReferralCreateDialog";
import {
  useIncomingInternalReferrals,
  useUpdateInternalReferralStatus,
} from "@/lib/hooks/use-internal-referrals";
import type {
  InternalReferralResponse,
  InternalReferralStatus,
} from "@/lib/api/internal-referrals";

const STATUS_CONFIG: Record<InternalReferralStatus, { label: string; className: string }> = {
  SENT: { label: "Đã gửi", className: "bg-blue-100 text-blue-800 border-blue-300" },
  ACCEPTED: { label: "Đã tiếp nhận", className: "bg-amber-100 text-amber-800 border-amber-300" },
  COMPLETED: { label: "Hoàn tất", className: "bg-green-100 text-green-800 border-green-300" },
  CANCELLED: { label: "Đã huỷ", className: "bg-gray-100 text-gray-700 border-gray-300" },
};

function formatDate(s: string) {
  return new Date(s).toLocaleDateString("vi-VN");
}

export default function InternalReferralsPage() {
  const [createOpen, setCreateOpen] = useState(false);
  const { data, isLoading } = useIncomingInternalReferrals();
  const updateStatusMutation = useUpdateInternalReferralStatus();

  const rows = data ?? [];

  function nextActions(row: InternalReferralResponse) {
    switch (row.status) {
      case "SENT":
        return (
          <div className="flex gap-1 justify-end">
            <Button
              size="sm"
              variant="outline"
              disabled={updateStatusMutation.isPending}
              onClick={() =>
                updateStatusMutation.mutate({ id: row.id, body: { status: "ACCEPTED" } })
              }
            >
              Tiếp nhận
            </Button>
            <Button
              size="sm"
              variant="ghost"
              className="text-destructive hover:text-destructive"
              disabled={updateStatusMutation.isPending}
              onClick={() =>
                updateStatusMutation.mutate({ id: row.id, body: { status: "CANCELLED" } })
              }
            >
              Huỷ
            </Button>
          </div>
        );
      case "ACCEPTED":
        return (
          <div className="flex justify-end">
            <Button
              size="sm"
              variant="outline"
              disabled={updateStatusMutation.isPending}
              onClick={() =>
                updateStatusMutation.mutate({ id: row.id, body: { status: "COMPLETED" } })
              }
            >
              Hoàn tất
            </Button>
          </div>
        );
      default:
        return null;
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Chuyển cơ sở nội bộ"
        description="Bệnh nhân được giới thiệu đến từ chi nhánh khác cùng tổ chức (BR-29)"
        actions={
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Giới thiệu sang cơ sở khác
          </Button>
        }
      />

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </div>
      ) : rows.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 gap-3">
          <div className="h-16 w-16 rounded-full bg-muted flex items-center justify-center">
            <ArrowLeftRight className="h-7 w-7 text-muted-foreground" />
          </div>
          <p className="text-sm font-medium">Chưa có bệnh nhân nào được giới thiệu đến</p>
          <p className="text-xs text-muted-foreground">
            Bấm "Giới thiệu sang cơ sở khác" để tạo giấy giới thiệu chuyển cơ sở nội bộ.
          </p>
        </div>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead>Chi nhánh nguồn</TableHead>
                <TableHead>Lý do</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Ngày tạo</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.id}>
                  <TableCell className="font-medium">
                    {row.patient_name ?? row.patient_id}
                  </TableCell>
                  <TableCell className="text-sm">{row.source_branch_name ?? "-"}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {row.reason ?? "-"}
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className={STATUS_CONFIG[row.status].className}>
                      {STATUS_CONFIG[row.status].label}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {formatDate(row.created_at)}
                  </TableCell>
                  <TableCell className="text-right">{nextActions(row)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <InternalReferralCreateDialog open={createOpen} onOpenChange={setCreateOpen} />
    </div>
  );
}
