"use client";

import { useState } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
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
import { Plus, ExternalLink, Trash2 } from "lucide-react";
import Link from "next/link";
import { BhytExportStatusBadge } from "@/components/domain/bhyt/BhytExportStatusBadge";
import { BhytExportForm } from "@/components/domain/bhyt/BhytExportForm";
import { BhytReconcileUploader } from "@/components/domain/bhyt/BhytReconcileUploader";
import { BhytReconcileTable } from "@/components/domain/bhyt/BhytReconcileTable";
import { BhytReconcileSummaryCard } from "@/components/domain/bhyt/BhytReconcileSummaryCard";
import { useBhytExports, useDeleteBhytExport } from "@/lib/hooks/use-bhyt-export";
import { useReconcileSummary } from "@/lib/hooks/use-bhyt-reconcile";
import type { BhytExportResponse } from "@/lib/api/bhyt-export";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";

function formatVnd(n: number) {
  return (
    new Intl.NumberFormat("vi-VN", { notation: "compact", maximumFractionDigits: 1 }).format(n) +
    " ₫"
  );
}

function formatDate(s: string | null) {
  if (!s) return "-";
  return new Date(s).toLocaleDateString("vi-VN");
}

// ─── Tab: Kỳ xuất ─────────────────────────────────────────────────────────────

function TabKyXuat() {
  const [createOpen, setCreateOpen] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<BhytExportResponse | null>(null);
  const { data, isLoading } = useBhytExports();
  const deleteMutation = useDeleteBhytExport();

  const rows = data?.data ?? [];

  function handleDelete() {
    if (!deleteTarget) return;
    deleteMutation.mutate(deleteTarget.id, {
      onSuccess: () => {
        toast.success("Đã xoá kỳ export");
        setDeleteTarget(null);
      },
      onError: () => toast.error("Không thể xoá kỳ này"),
    });
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm text-muted-foreground">Danh sách kỳ export BHYT theo tháng</p>
        <Button size="sm" onClick={() => setCreateOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Tạo kỳ mới
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </div>
      ) : rows.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 gap-3">
          <div className="h-16 w-16 rounded-full bg-muted flex items-center justify-center">
            <Plus className="h-7 w-7 text-muted-foreground" />
          </div>
          <p className="text-sm font-medium">Chưa có kỳ export nào</p>
          <p className="text-xs text-muted-foreground">Tạo kỳ mới để bắt đầu xuất dữ liệu BHYT</p>
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            Tạo kỳ đầu tiên
          </Button>
        </div>
      ) : (
        <div className="rounded-md border overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Kỳ</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead className="text-right">Số lượt</TableHead>
                <TableHead className="text-right">Yêu cầu</TableHead>
                <TableHead className="text-right">Được duyệt</TableHead>
                <TableHead>Sinh XML lúc</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.id} className="cursor-pointer hover:bg-muted/50">
                  <TableCell className="font-medium">{row.period_month}</TableCell>
                  <TableCell>
                    <BhytExportStatusBadge status={row.status} />
                  </TableCell>
                  <TableCell className="text-right">
                    {row.encounter_count.toLocaleString()}
                  </TableCell>
                  <TableCell className="text-right">
                    {formatVnd(row.total_requested_amount)}
                  </TableCell>
                  <TableCell className="text-right">
                    {formatVnd(row.total_approved_amount)}
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {formatDate(row.generated_at)}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-1">
                      <Button
                        size="sm"
                        variant="ghost"
                        className="gap-1.5"
                        render={<Link href={`/bhyt/exports/${row.id}`} />}
                      >
                        <ExternalLink className="h-4 w-4" />
                        Chi tiết
                      </Button>
                      {row.status === "DRAFT" && (
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-8 w-8 text-destructive hover:text-destructive"
                          onClick={() => setDeleteTarget(row)}
                          aria-label="Xoá"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <BhytExportForm open={createOpen} onOpenChange={setCreateOpen} />

      <Dialog open={!!deleteTarget} onOpenChange={(v) => !v && setDeleteTarget(null)}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Xoá kỳ export?</DialogTitle>
            <DialogDescription>
              Kỳ <strong>{deleteTarget?.period_month}</strong> sẽ bị xoá vĩnh viễn. Hành động này
              không thể hoàn tác.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)}>
              Huỷ
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? "Đang xoá..." : "Xoá"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

// ─── Tab: Đối soát ────────────────────────────────────────────────────────────

function TabDoiSoat() {
  const { data: exportsData } = useBhytExports({ status: "SUBMITTED" });
  const [selectedExportId, setSelectedExportId] = useState<string>("");

  const submittedExports = exportsData?.data ?? [];
  const effectiveId = selectedExportId || submittedExports[0]?.id || "";

  const { data: summaryData } = useReconcileSummary(effectiveId);

  return (
    <div className="space-y-6">
      {submittedExports.length > 0 && (
        <div className="flex items-center gap-3">
          <label className="text-sm font-medium whitespace-nowrap">Kỳ export:</label>
          <select
            className="h-9 rounded-md border bg-background px-3 text-sm"
            value={effectiveId}
            onChange={(e) => setSelectedExportId(e.target.value)}
            aria-label="Chọn kỳ export"
          >
            {submittedExports.map((e) => (
              <option key={e.id} value={e.id}>
                {e.period_month} — {e.status}
              </option>
            ))}
          </select>
        </div>
      )}

      <div>
        <h3 className="text-sm font-medium mb-3">Tải lên kết quả giám định</h3>
        {effectiveId ? (
          <BhytReconcileUploader exportId={effectiveId} />
        ) : (
          <p className="text-sm text-muted-foreground">Chưa có kỳ nào ở trạng thái Đã gửi</p>
        )}
      </div>

      {summaryData && (
        <div>
          <h3 className="text-sm font-medium mb-3">Tổng kết đối soát</h3>
          <BhytReconcileSummaryCard summary={summaryData} />
        </div>
      )}

      {effectiveId && (
        <div>
          <h3 className="text-sm font-medium mb-3">Chi tiết từng dòng</h3>
          <BhytReconcileTable exportId={effectiveId} />
        </div>
      )}
    </div>
  );
}

// ─── Tab: Cấu hình ────────────────────────────────────────────────────────────

function TabCauHinh() {
  return (
    <div className="space-y-4">
      <div className="rounded-lg border bg-muted/50 p-6 max-w-xl">
        <h3 className="font-medium mb-1">Thông tin CSKCB</h3>
        <p className="text-sm text-muted-foreground mb-4">
          Thông tin cơ sở khám chữa bệnh dùng trong hồ sơ BHYT được quản lý tại cấu hình phòng khám.
        </p>
        <Button variant="outline" size="sm" render={<Link href="/admin/tenants" />}>
          Vào cấu hình phòng khám
        </Button>
      </div>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function BhytPage() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">BHYT</h2>
        <p className="text-sm text-muted-foreground">
          Export XML giám định theo QĐ 4750/QĐ-BYT, đối soát kết quả
        </p>
      </div>

      <Tabs defaultValue="ky-xuat">
        <TabsList>
          <TabsTrigger value="ky-xuat">Kỳ xuất</TabsTrigger>
          <TabsTrigger value="doi-soat">Đối soát giám định</TabsTrigger>
          <TabsTrigger value="cau-hinh">Cấu hình</TabsTrigger>
        </TabsList>

        <TabsContent value="ky-xuat" className="mt-4">
          <TabKyXuat />
        </TabsContent>

        <TabsContent value="doi-soat" className="mt-4">
          <TabDoiSoat />
        </TabsContent>

        <TabsContent value="cau-hinh" className="mt-4">
          <TabCauHinh />
        </TabsContent>
      </Tabs>
    </div>
  );
}
