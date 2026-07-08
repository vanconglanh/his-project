"use client";

import { useState } from "react";
import { Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Badge } from "@/components/ui/badge";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { useAuditLogs } from "@/lib/hooks/use-roles";
import type { AuditLogResponse, AuditAction } from "@/lib/api/types";
import { formatDateTime } from "@/lib/utils/format";

const ACTION_COLORS: Record<AuditAction, string> = {
  CREATE: "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200",
  UPDATE: "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200",
  DELETE: "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200",
  LOGIN: "bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300",
  LOGOUT: "bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400",
  EXPORT: "bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200",
  SIGN: "bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-200",
};

const ACTION_LABELS: Record<AuditAction, string> = {
  CREATE: "Tạo mới",
  UPDATE: "Cập nhật",
  DELETE: "Xoá",
  LOGIN: "Đăng nhập",
  LOGOUT: "Đăng xuất",
  EXPORT: "Xuất file",
  SIGN: "Ký xác nhận",
};

export default function AuditPage() {
  const [page, setPage] = useState(1);
  const [actionFilter, setActionFilter] = useState("ALL");
  const [resourceFilter, setResourceFilter] = useState("");
  const [selectedLog, setSelectedLog] = useState<AuditLogResponse | null>(null);

  const params = {
    page,
    page_size: 20,
    action: actionFilter !== "ALL" ? actionFilter : undefined,
    resource_type: resourceFilter || undefined,
    sort: "-created_at",
  };

  const { data, isLoading } = useAuditLogs(params);

  const columns: Column<AuditLogResponse>[] = [
    {
      key: "created_at",
      header: "Thời gian",
      cell: (row) => (
        <span className="text-sm whitespace-nowrap">{formatDateTime(row.created_at)}</span>
      ),
    },
    {
      key: "user",
      header: "Người dùng",
      cell: (row) => (
        <span className="text-sm">{row.user_email ?? <span className="text-muted-foreground">—</span>}</span>
      ),
    },
    {
      key: "action",
      header: "Hành động",
      cell: (row) => (
        <Badge
          variant="secondary"
          className={`text-xs ${ACTION_COLORS[row.action] ?? ""}`}
        >
          {ACTION_LABELS[row.action] ?? row.action}
        </Badge>
      ),
    },
    {
      key: "resource",
      header: "Đối tượng",
      cell: (row) => (
        <div>
          <span className="text-sm">{row.resource_type ?? "—"}</span>
          {row.resource_id && (
            <p className="text-xs text-muted-foreground font-mono">{row.resource_id.slice(0, 8)}...</p>
          )}
        </div>
      ),
    },
    {
      key: "ip",
      header: "IP",
      cell: (row) => (
        <span className="text-sm font-mono">{row.ip_address ?? "—"}</span>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-bold tracking-tight">Nhật ký thao tác</h1>
        <p className="text-muted-foreground text-sm mt-1">
          Theo dõi mọi thao tác trên hệ thống
        </p>
      </div>

      <div className="flex gap-3 flex-wrap">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Lọc theo loại đối tượng..."
            value={resourceFilter}
            onChange={(e) => { setResourceFilter(e.target.value); setPage(1); }}
            className="pl-9"
          />
        </div>
        <Select
          items={{ ALL: "Tất cả hành động", ...ACTION_LABELS }}
          value={actionFilter}
          onValueChange={(v) => { if (v) { setActionFilter(v); setPage(1); } }}
        >
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Hành động" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả hành động</SelectItem>
            <SelectItem value="CREATE">Tạo mới</SelectItem>
            <SelectItem value="UPDATE">Cập nhật</SelectItem>
            <SelectItem value="DELETE">Xoá</SelectItem>
            <SelectItem value="LOGIN">Đăng nhập</SelectItem>
            <SelectItem value="LOGOUT">Đăng xuất</SelectItem>
            <SelectItem value="EXPORT">Xuất file</SelectItem>
            <SelectItem value="SIGN">Ký xác nhận</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={data?.data ?? []}
        isLoading={isLoading}
        meta={data?.meta}
        onPageChange={setPage}
        onRowClick={(row) => setSelectedLog(row)}
        onRowDoubleClick={(row) => setSelectedLog(row)}
      />

      {/* Detail sheet */}
      <Sheet open={!!selectedLog} onOpenChange={(o) => !o && setSelectedLog(null)}>
        <SheetContent className="w-full sm:max-w-xl overflow-y-auto px-6 pb-6">
          <SheetHeader>
            <SheetTitle>Chi tiết nhật ký</SheetTitle>
          </SheetHeader>
          {selectedLog && (
            <div className="mt-4 space-y-3 text-sm">
              <div className="grid grid-cols-[120px_1fr] gap-2 border-b pb-2">
                <span className="text-muted-foreground">Thời gian</span>
                <span>{formatDateTime(selectedLog.created_at)}</span>
              </div>
              <div className="grid grid-cols-[120px_1fr] gap-2 border-b pb-2">
                <span className="text-muted-foreground">Người dùng</span>
                <span>{selectedLog.user_email ?? "—"}</span>
              </div>
              <div className="grid grid-cols-[120px_1fr] gap-2 border-b pb-2">
                <span className="text-muted-foreground">Hành động</span>
                <Badge variant="secondary" className={ACTION_COLORS[selectedLog.action] ?? ""}>
                  {ACTION_LABELS[selectedLog.action] ?? selectedLog.action}
                </Badge>
              </div>
              <div className="grid grid-cols-[120px_1fr] gap-2 border-b pb-2">
                <span className="text-muted-foreground">Đối tượng</span>
                <span>{selectedLog.resource_type ?? "—"}</span>
              </div>
              <div className="grid grid-cols-[120px_1fr] gap-2 border-b pb-2">
                <span className="text-muted-foreground">ID đối tượng</span>
                <span className="font-mono text-xs">{selectedLog.resource_id ?? "—"}</span>
              </div>
              <div className="grid grid-cols-[120px_1fr] gap-2 border-b pb-2">
                <span className="text-muted-foreground">IP</span>
                <span className="font-mono">{selectedLog.ip_address ?? "—"}</span>
              </div>
              <div>
                <p className="text-muted-foreground mb-2">Chi tiết thay đổi</p>
                <pre className="bg-muted rounded p-3 text-xs overflow-auto max-h-64 whitespace-pre-wrap">
                  {selectedLog.details
                    ? JSON.stringify(selectedLog.details, null, 2)
                    : "Không có chi tiết"}
                </pre>
              </div>
            </div>
          )}
        </SheetContent>
      </Sheet>
    </div>
  );
}
