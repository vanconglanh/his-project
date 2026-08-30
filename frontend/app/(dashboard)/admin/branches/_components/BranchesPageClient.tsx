"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import {
  useBranches,
  useDeleteBranch,
  useSetDefaultBranch,
  useSetBranchStatus,
} from "@/lib/hooks/use-branches";
import { DataTable } from "@/components/ui/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { BranchCloneDialog } from "@/components/domain/BranchCloneDialog";
import { BranchReadinessDialog } from "@/components/domain/BranchReadinessDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Search, Plus, Pencil, Trash2, Star, Power, Copy, ListChecks } from "lucide-react";
import type { BranchResponse } from "@/lib/api/branches";

const BRANCH_STATUS_CONFIG: Record<string, { label: string; className: string }> = {
  DRAFT: { label: "Nháp", className: "bg-gray-100 text-gray-700 border-gray-300" },
  CONFIGURING: {
    label: "Đang cấu hình",
    className: "bg-blue-100 text-blue-800 border-blue-300",
  },
  READY_CHECK: {
    label: "Chờ kiểm tra",
    className: "bg-amber-100 text-amber-800 border-amber-300",
  },
  ACTIVE: { label: "Hoạt động", className: "bg-green-100 text-green-800 border-green-300" },
  SUSPENDED: { label: "Tạm ngừng", className: "bg-amber-100 text-amber-800 border-amber-300" },
  CLOSED: { label: "Đã đóng", className: "bg-red-100 text-red-800 border-red-300" },
};

function BranchStatusBadge({ status }: { status: string }) {
  const cfg = BRANCH_STATUS_CONFIG[status] ?? {
    label: status,
    className: "bg-gray-100 text-gray-700 border-gray-300",
  };
  return (
    <Badge variant="outline" className={cfg.className}>
      {cfg.label}
    </Badge>
  );
}

export function BranchesPageClient() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [deleteBranch, setDeleteBranch] = useState<BranchResponse | null>(null);
  const [cloneOpen, setCloneOpen] = useState(false);
  const [cloneSourceId, setCloneSourceId] = useState<number | undefined>(undefined);
  const [readinessBranch, setReadinessBranch] = useState<BranchResponse | null>(null);

  const { data, isLoading } = useBranches();
  const deleteMutation = useDeleteBranch();
  const setDefaultMutation = useSetDefaultBranch();
  const setStatusMutation = useSetBranchStatus();

  const branches = (data?.data ?? []).filter((b) =>
    q ? `${b.name} ${b.code}`.toLowerCase().includes(q.toLowerCase()) : true
  );

  const columns = [
    {
      key: "code",
      header: "Mã",
      cell: (row: BranchResponse) => <span className="font-mono text-xs">{row.code}</span>,
    },
    {
      key: "name",
      header: "Tên chi nhánh",
      cell: (row: BranchResponse) => (
        <div className="flex items-center gap-1.5">
          <span className="font-medium text-sm">{row.name}</span>
          {row.is_default && (
            <Badge variant="outline" className="text-[10px] gap-0.5 border-amber-300 text-amber-700 bg-amber-50">
              <Star className="h-3 w-3" /> Mặc định
            </Badge>
          )}
        </div>
      ),
    },
    {
      key: "phone",
      header: "Điện thoại",
      cell: (row: BranchResponse) => <span className="text-sm">{row.phone ?? "-"}</span>,
    },
    {
      key: "address",
      header: "Địa chỉ",
      cell: (row: BranchResponse) => <span className="text-sm">{row.address ?? "-"}</span>,
    },
    {
      key: "status",
      header: "Trạng thái",
      cell: (row: BranchResponse) => <BranchStatusBadge status={row.status} />,
    },
    {
      key: "actions",
      header: "",
      cell: (row: BranchResponse) => (
        <div className="flex gap-1" onDoubleClick={(e) => e.stopPropagation()}>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            title="Nhân bản chi nhánh"
            onClick={(e) => {
              e.stopPropagation();
              setCloneSourceId(row.id);
              setCloneOpen(true);
            }}
          >
            <Copy className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            title="Checklist go-live"
            onClick={(e) => {
              e.stopPropagation();
              setReadinessBranch(row);
            }}
          >
            <ListChecks className="h-4 w-4" />
          </Button>
          {!row.is_default && (
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              title="Đặt làm mặc định"
              disabled={setDefaultMutation.isPending}
              onClick={(e) => {
                e.stopPropagation();
                setDefaultMutation.mutate(row.id);
              }}
            >
              <Star className="h-4 w-4" />
            </Button>
          )}
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            title={row.is_active ? "Tắt chi nhánh" : "Bật chi nhánh"}
            disabled={setStatusMutation.isPending}
            onClick={(e) => {
              e.stopPropagation();
              setStatusMutation.mutate({ id: row.id, is_active: !row.is_active });
            }}
          >
            <Power className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            onClick={(e) => {
              e.stopPropagation();
              router.push(`/admin/branches/${row.id}/edit`);
            }}
          >
            <Pencil className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 text-destructive"
            onClick={(e) => {
              e.stopPropagation();
              setDeleteBranch(row);
            }}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <div className="flex gap-2 items-center">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Tìm chi nhánh..."
            className="pl-9"
          />
        </div>
        <Button
          size="sm"
          variant="outline"
          onClick={() => {
            setCloneSourceId(undefined);
            setCloneOpen(true);
          }}
        >
          <Copy className="h-4 w-4 mr-2" />
          Nhân bản
        </Button>
        <Button size="sm" onClick={() => router.push("/admin/branches/new")}>
          <Plus className="h-4 w-4 mr-2" />
          Tạo chi nhánh
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={branches}
          onRowDoubleClick={(row) => router.push(`/admin/branches/${row.id}/edit`)}
          emptyState={
            <div className="py-10 text-center text-sm text-muted-foreground">
              Chưa có chi nhánh nào. Bấm "Tạo chi nhánh" để thêm mới.
            </div>
          }
        />
      )}

      <ConfirmDialog
        open={!!deleteBranch}
        onOpenChange={(o) => !o && setDeleteBranch(null)}
        title="Xóa chi nhánh"
        description={`Bạn có chắc muốn xóa chi nhánh "${deleteBranch?.name}"?`}
        variant="destructive"
        confirmLabel="Xóa"
        isLoading={deleteMutation.isPending}
        onConfirm={async () => {
          if (!deleteBranch) return;
          await deleteMutation.mutateAsync(deleteBranch.id);
          setDeleteBranch(null);
        }}
      />

      <BranchCloneDialog
        open={cloneOpen}
        onOpenChange={setCloneOpen}
        branches={branches}
        defaultSourceId={cloneSourceId}
      />

      <BranchReadinessDialog
        open={!!readinessBranch}
        onOpenChange={(o) => !o && setReadinessBranch(null)}
        branch={readinessBranch}
      />
    </div>
  );
}
