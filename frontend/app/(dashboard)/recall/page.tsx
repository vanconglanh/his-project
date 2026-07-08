"use client";

import { useState } from "react";
import { CalendarClock, PhoneCall, CalendarCheck, XCircle, Send } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Can } from "@/components/auth/Can";
import { useRecallList, useUpdateRecallStatus, useNotifyRecall } from "@/lib/hooks/use-recall";
import { formatDate, formatDateTime } from "@/lib/utils/format";
import type { RecallItem, RecallStatus } from "@/lib/api/types";

const STATUS_LABELS: Record<RecallStatus, string> = {
  PENDING: "Chờ liên hệ",
  CONTACTED: "Đã gọi",
  SCHEDULED: "Đã hẹn",
  DONE: "Hoàn tất",
  DISMISSED: "Đã bỏ qua",
};

const STATUS_BADGE_CLASS: Record<RecallStatus, string> = {
  PENDING: "bg-amber-100 text-amber-800 border-amber-300 dark:bg-amber-950/40 dark:text-amber-300",
  CONTACTED: "bg-blue-100 text-blue-800 border-blue-300 dark:bg-blue-950/40 dark:text-blue-300",
  SCHEDULED: "bg-purple-100 text-purple-800 border-purple-300 dark:bg-purple-950/40 dark:text-purple-300",
  DONE: "bg-green-100 text-green-800 border-green-300 dark:bg-green-950/40 dark:text-green-300",
  DISMISSED: "bg-gray-100 text-gray-700 border-gray-300 dark:bg-gray-800/40 dark:text-gray-300",
};

const RECALL_TYPE_LABELS: Record<string, string> = {
  HBA1C_DUE: "Định kỳ HbA1c",
  FOLLOW_UP: "Tái khám",
  EYE_EXAM: "Khám mắt",
  FOOT_EXAM: "Khám chân",
};

function RecallContent() {
  const [status, setStatus] = useState<string>("PENDING");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useRecallList({
    status: status === "ALL" ? undefined : (status as RecallStatus),
    page,
    pageSize,
  });
  const updateStatus = useUpdateRecallStatus();
  const notify = useNotifyRecall();

  const items = data?.data ?? [];
  const meta = data?.meta
    ? {
        page: data.meta.page,
        page_size: data.meta.page_size,
        total: data.meta.total,
        total_pages: Math.max(1, Math.ceil(data.meta.total / (data.meta.page_size || pageSize))),
      }
    : undefined;

  const columns: Column<RecallItem>[] = [
    {
      key: "patient",
      header: "Bệnh nhân",
      cell: (row) => (
        <div className="min-w-[180px]">
          <p className="font-medium text-sm">{row.patient_full_name}</p>
          <p className="text-xs text-muted-foreground font-mono">{row.patient_code}</p>
        </div>
      ),
    },
    {
      key: "type",
      header: "Loại nhắc",
      cell: (row) => (
        <Badge variant="outline" className="text-xs">
          {RECALL_TYPE_LABELS[row.recall_type] ?? row.recall_type}
        </Badge>
      ),
    },
    {
      key: "priority",
      header: "Ưu tiên",
      cell: (row) => (
        <span className="text-xs">
          {row.priority === "URGENT" ? "Khẩn cấp" : row.priority === "HIGH" ? "Cao" : "Bình thường"}
        </span>
      ),
    },
    {
      key: "due_date",
      header: "Hạn",
      cell: (row) => <span className="text-sm">{row.due_date ? formatDate(row.due_date) : "—"}</span>,
    },
    {
      key: "phone",
      header: "SĐT",
      cell: (row) => <span className="text-sm tabular-nums">{row.phone ?? "—"}</span>,
    },
    {
      key: "status",
      header: "Trạng thái",
      cell: (row) => (
        <Badge variant="outline" className={STATUS_BADGE_CLASS[row.status]}>
          {STATUS_LABELS[row.status] ?? row.status}
        </Badge>
      ),
    },
    {
      key: "contacted_at",
      header: "Liên hệ lúc",
      cell: (row) => <span className="text-xs text-muted-foreground">{row.contacted_at ? formatDateTime(row.contacted_at) : "—"}</span>,
    },
    {
      key: "actions",
      header: "",
      className: "min-w-[280px]",
      cell: (row) => (
        <div className="flex flex-wrap gap-1.5" onClick={(e) => e.stopPropagation()}>
          <Can permission="recall.manage">
            <Button
              size="sm"
              variant="outline"
              className="h-8 gap-1"
              disabled={updateStatus.isPending}
              onClick={() => updateStatus.mutate({ id: row.id, body: { status: "CONTACTED" } })}
            >
              <PhoneCall className="h-3.5 w-3.5" />
              Đã gọi
            </Button>
            <Button
              size="sm"
              variant="outline"
              className="h-8 gap-1"
              disabled={updateStatus.isPending}
              onClick={() => updateStatus.mutate({ id: row.id, body: { status: "SCHEDULED" } })}
            >
              <CalendarCheck className="h-3.5 w-3.5" />
              Đã hẹn
            </Button>
            <Button
              size="sm"
              variant="ghost"
              className="h-8 gap-1 text-destructive hover:text-destructive"
              disabled={updateStatus.isPending}
              onClick={() => updateStatus.mutate({ id: row.id, body: { status: "DISMISSED" } })}
            >
              <XCircle className="h-3.5 w-3.5" />
              Bỏ qua
            </Button>
            <Button
              size="sm"
              variant="outline"
              className="h-8 gap-1"
              disabled={notify.isPending || !row.phone}
              onClick={() => notify.mutate({ id: row.id })}
            >
              <Send className="h-3.5 w-3.5" />
              Gửi SMS
            </Button>
          </Can>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Nhắc tái khám"
        description="Danh sách bệnh nhân đến hạn tái khám / xét nghiệm định kỳ HbA1c"
      />

      <div className="flex items-center gap-2">
        <Select
          items={{
            ALL: "Tất cả",
            PENDING: "Chờ liên hệ",
            CONTACTED: "Đã gọi",
            SCHEDULED: "Đã hẹn",
            DONE: "Hoàn tất",
            DISMISSED: "Đã bỏ qua",
          }}
          value={status}
          onValueChange={(v) => {
            setStatus(v ?? "PENDING");
            setPage(1);
          }}
        >
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Trạng thái" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả</SelectItem>
            <SelectItem value="PENDING">Chờ liên hệ</SelectItem>
            <SelectItem value="CONTACTED">Đã gọi</SelectItem>
            <SelectItem value="SCHEDULED">Đã hẹn</SelectItem>
            <SelectItem value="DONE">Hoàn tất</SelectItem>
            <SelectItem value="DISMISSED">Đã bỏ qua</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={items}
        isLoading={isLoading}
        meta={meta}
        onPageChange={setPage}
        skeletonRows={8}
        emptyState={
          <div className="flex flex-col items-center gap-3 py-6">
            <div className="rounded-full bg-muted p-4">
              <CalendarClock className="h-8 w-8 text-muted-foreground" />
            </div>
            <div className="text-center">
              <p className="font-medium">Không có nhắc tái khám nào</p>
              <p className="text-sm text-muted-foreground mt-1">Thử đổi bộ lọc trạng thái</p>
            </div>
          </div>
        }
      />
    </div>
  );
}

export default function RecallPage() {
  return (
    <Can
      permission="recall.read"
      fallback={
        <div className="flex flex-col items-center justify-center h-64 gap-2 text-muted-foreground">
          <CalendarClock className="h-10 w-10" />
          <p>Bạn không có quyền xem danh sách nhắc tái khám</p>
        </div>
      }
    >
      <RecallContent />
    </Can>
  );
}
