"use client";

import { useState } from "react";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
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
import { EmptyState } from "@/components/ui/EmptyState";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  usePendingClsOrders,
  useUpdateLabOrderStatusInQueue,
  useUpdateRadOrderStatusInQueue,
} from "@/lib/hooks/use-cls-orders";
import type { PendingClsOrderResponse } from "@/lib/api/types";

// BM-40 (Lark Bug-Feedback): man hinh "hang cho" cho KTV cap nhat trang thai chi dinh XN/CDHA.
// Buoc chuyen trang thai (khop dung LabOrderStatus.CanTransition/RadOrderStatus.CanTransition
// o BE) - chi hien 1 nut "buoc tiep theo" duy nhat, khong cho nhay tat.
const LAB_NEXT: Record<string, { status: string; label: string } | undefined> = {
  ordered: { status: "sample_taken", label: "Đã lấy mẫu" },
  sample_taken: { status: "processing", label: "Bắt đầu xử lý" },
  processing: { status: "done", label: "Hoàn tất" },
};

const RAD_NEXT: Record<string, { status: string; label: string } | undefined> = {
  ordered: { status: "scheduled", label: "Đã lên lịch" },
  scheduled: { status: "in_progress", label: "Bắt đầu thực hiện" },
  in_progress: { status: "done", label: "Hoàn tất" },
};

const STATUS_LABELS: Record<string, string> = {
  ordered: "Đã chỉ định",
  sample_taken: "Đã lấy mẫu",
  processing: "Đang xử lý",
  scheduled: "Đã lên lịch",
  in_progress: "Đang thực hiện",
  done: "Hoàn tất",
  cancelled: "Đã huỷ",
};

const PRIORITY_LABELS: Record<string, string> = {
  NORMAL: "Thường",
  URGENT: "Khẩn",
  STAT: "Tối khẩn",
};

export function ClsOrderQueueTab() {
  const [kindFilter, setKindFilter] = useState<"ALL" | "LAB" | "RAD">("ALL");
  const { data, isLoading } = usePendingClsOrders(kindFilter === "ALL" ? undefined : kindFilter);
  const updateLab = useUpdateLabOrderStatusInQueue();
  const updateRad = useUpdateRadOrderStatusInQueue();

  const items = data ?? [];

  function handleAdvance(item: PendingClsOrderResponse) {
    const next = item.kind === "LAB" ? LAB_NEXT[item.status] : RAD_NEXT[item.status];
    if (!next) return;
    if (item.kind === "LAB") {
      updateLab.mutate({ id: item.id, status: next.status as never });
    } else {
      updateRad.mutate({ id: item.id, status: next.status as never });
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <Select
          items={{ ALL: "Tất cả", LAB: "Xét nghiệm", RAD: "Chẩn đoán hình ảnh" }}
          value={kindFilter}
          onValueChange={(v) => setKindFilter(v as "ALL" | "LAB" | "RAD")}
        >
          <SelectTrigger className="w-52" aria-label="Lọc theo loại chỉ định">
            <SelectValue placeholder="Loại chỉ định" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả</SelectItem>
            <SelectItem value="LAB">Xét nghiệm</SelectItem>
            <SelectItem value="RAD">Chẩn đoán hình ảnh</SelectItem>
          </SelectContent>
        </Select>
        <p className="text-sm text-muted-foreground">
          {items.length} chỉ định đang chờ xử lý
        </p>
      </div>

      {isLoading ? (
        <Skeleton className="h-64 w-full rounded-xl" />
      ) : items.length === 0 ? (
        <EmptyState
          variant="labrad"
          title="Không có chỉ định nào đang chờ"
          description="Mọi chỉ định XN/CĐHA đã được xử lý hoặc huỷ."
        />
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Bệnh nhân</TableHead>
              <TableHead>Loại</TableHead>
              <TableHead>Chỉ định</TableHead>
              <TableHead>Ưu tiên</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Ngày chỉ định</TableHead>
              <TableHead className="text-right">Hành động</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {items.map((item) => {
              const next = item.kind === "LAB" ? LAB_NEXT[item.status] : RAD_NEXT[item.status];
              const isPending = item.kind === "LAB" ? updateLab.isPending : updateRad.isPending;
              return (
                <TableRow key={item.id}>
                  <TableCell className="font-medium">{item.patient_name}</TableCell>
                  <TableCell>{item.kind === "LAB" ? "XN" : "CĐHA"}</TableCell>
                  <TableCell>{item.item_name}</TableCell>
                  <TableCell>
                    {item.priority !== "NORMAL" && (
                      <Badge variant={item.priority === "STAT" ? "destructive" : "outline"}>
                        {PRIORITY_LABELS[item.priority] ?? item.priority}
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    <Badge variant="secondary">{STATUS_LABELS[item.status] ?? item.status}</Badge>
                  </TableCell>
                  <TableCell>
                    {format(new Date(item.ordered_at), "HH:mm dd/MM/yyyy", { locale: vi })}
                  </TableCell>
                  <TableCell className="text-right">
                    {next && (
                      <Button
                        size="sm"
                        className="min-h-[36px]"
                        disabled={isPending}
                        onClick={() => handleAdvance(item)}
                      >
                        {next.label}
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
