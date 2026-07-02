"use client";

import { useState } from "react";
import { Plus, MoreHorizontal, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { StatusBadge } from "@/components/ui/entity-status-badge";
import { TenantForm } from "@/components/domain/TenantForm";
import { TenantDetail } from "@/components/domain/TenantDetail";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import {
  useTenants,
  useCreateTenant,
  useUpdateTenant,
  useSuspendTenant,
  useActivateTenant,
  useDeleteTenant,
} from "@/lib/hooks/use-tenants";
import type { TenantResponse } from "@/lib/api/types";
import { formatDateTime } from "@/lib/utils/format";

export default function TenantsPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<string>("ALL");
  const [createOpen, setCreateOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<TenantResponse | null>(null);
  const [detailTarget, setDetailTarget] = useState<TenantResponse | null>(null);
  const [suspendTarget, setSuspendTarget] = useState<TenantResponse | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<TenantResponse | null>(null);

  const params = {
    page,
    page_size: 20,
    q: search || undefined,
    status: statusFilter !== "ALL" ? statusFilter : undefined,
  };

  const { data, isLoading } = useTenants(params);
  const createMutation = useCreateTenant();
  const updateMutation = useUpdateTenant();
  const suspendMutation = useSuspendTenant();
  const activateMutation = useActivateTenant();
  const deleteMutation = useDeleteTenant();

  const columns: Column<TenantResponse>[] = [
    {
      key: "code",
      header: "Mã",
      cell: (row) => <span className="font-mono text-sm">{row.code}</span>,
    },
    {
      key: "name",
      header: "Tên phòng khám",
      cell: (row) => (
        <div>
          <p className="font-medium">{row.name}</p>
          <p className="text-xs text-muted-foreground">{row.subdomain}.prodiab.vn</p>
        </div>
      ),
    },
    {
      key: "cskcb_code",
      header: "Mã CSKCB",
      cell: (row) => row.cskcb_code ?? <span className="text-muted-foreground">—</span>,
    },
    {
      key: "status",
      header: "Trạng thái",
      cell: (row) => <StatusBadge status={row.status} />,
    },
    {
      key: "storage_quota_gb",
      header: "Dung lượng",
      cell: (row) => (row.storage_quota_gb ? `${row.storage_quota_gb} GB` : "—"),
    },
    {
      key: "expires_at",
      header: "Hết hạn",
      cell: (row) =>
        row.expires_at ? (
          formatDateTime(row.expires_at)
        ) : (
          <span className="text-muted-foreground">Không giới hạn</span>
        ),
    },
    {
      key: "actions",
      header: "",
      cell: (row) => (
        <div className="flex items-center gap-2">
          {row.status === "ACTIVE" && (
            <Button
              variant="outline"
              size="sm"
              className="text-yellow-700 border-yellow-400 hover:bg-yellow-50 min-h-[36px]"
              onClick={(e) => {
                e.stopPropagation();
                setSuspendTarget(row);
              }}
            >
              Tạm ngưng
            </Button>
          )}
          {row.status === "SUSPENDED" && (
            <Button
              variant="outline"
              size="sm"
              className="text-green-700 border-green-400 hover:bg-green-50 min-h-[36px]"
              onClick={(e) => {
                e.stopPropagation();
                activateMutation.mutate(row.id);
              }}
            >
              Kích hoạt
            </Button>
          )}
          <DropdownMenu>
            <DropdownMenuTrigger
              render={
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8"
                  aria-label="Thao tác khác"
                />
              }
              onClick={(e) => e.stopPropagation()}
            >
              <MoreHorizontal className="h-4 w-4" />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                onClick={(e) => {
                  e.stopPropagation();
                  setEditTarget(row);
                }}
              >
                Sửa thông tin
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onClick={(e) => {
                  e.stopPropagation();
                  setDeleteTarget(row);
                }}
                className="text-destructive"
              >
                Chấm dứt
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ),
      className: "w-44",
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Quản lý phòng khám</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Danh sách toàn bộ phòng khám trong hệ thống
          </p>
        </div>
        <Button onClick={() => setCreateOpen(true)} className="min-h-[44px]">
          <Plus className="h-4 w-4 mr-2" />
          Tạo phòng khám mới
        </Button>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Tìm theo tên phòng khám..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            className="pl-9"
          />
        </div>
        <Select
          value={statusFilter}
          onValueChange={(v) => {
            if (v) {
              setStatusFilter(v);
              setPage(1);
            }
          }}
        >
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Trạng thái" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả</SelectItem>
            <SelectItem value="ACTIVE">Hoạt động</SelectItem>
            <SelectItem value="SUSPENDED">Tạm ngưng</SelectItem>
            <SelectItem value="TERMINATED">Chấm dứt</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      <DataTable
        columns={columns}
        data={data?.data ?? []}
        isLoading={isLoading}
        meta={data?.meta}
        onPageChange={setPage}
        onRowClick={(row) => setDetailTarget(row)}
      />

      {/* Create dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Tạo phòng khám mới</DialogTitle>
          </DialogHeader>
          <TenantForm
            onSubmit={async (values) => {
              await createMutation.mutateAsync(
                values as import("@/lib/api/types").CreateTenantRequest
              );
              setCreateOpen(false);
            }}
            isLoading={createMutation.isPending}
            onCancel={() => setCreateOpen(false)}
          />
        </DialogContent>
      </Dialog>

      {/* Edit dialog */}
      <Dialog open={!!editTarget} onOpenChange={(o) => !o && setEditTarget(null)}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Sửa thông tin phòng khám</DialogTitle>
          </DialogHeader>
          {editTarget && (
            <TenantForm
              initialValues={editTarget}
              isEdit
              onSubmit={async (values) => {
                await updateMutation.mutateAsync({
                  id: editTarget.id,
                  payload: values as import("@/lib/api/types").UpdateTenantRequest,
                });
                setEditTarget(null);
              }}
              isLoading={updateMutation.isPending}
              onCancel={() => setEditTarget(null)}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Detail sheet */}
      <Sheet open={!!detailTarget} onOpenChange={(o) => !o && setDetailTarget(null)}>
        <SheetContent className="w-[480px] sm:w-[480px] overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Chi tiết phòng khám</SheetTitle>
          </SheetHeader>
          {detailTarget && <TenantDetail tenant={detailTarget} />}
        </SheetContent>
      </Sheet>

      {/* Suspend confirm */}
      <ConfirmDialog
        open={!!suspendTarget}
        onOpenChange={(o) => !o && setSuspendTarget(null)}
        title="Tạm ngưng phòng khám"
        description={`Bạn có chắc muốn tạm ngưng "${suspendTarget?.name}"? Người dùng của phòng khám sẽ không thể đăng nhập.`}
        variant="warning"
        confirmLabel="Tạm ngưng"
        isLoading={suspendMutation.isPending}
        onConfirm={async () => {
          if (suspendTarget) {
            await suspendMutation.mutateAsync({ id: suspendTarget.id });
            setSuspendTarget(null);
          }
        }}
      />

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="Chấm dứt phòng khám"
        description={`Bạn có chắc muốn chấm dứt "${deleteTarget?.name}"? Hành động này không thể hoàn tác.`}
        variant="destructive"
        confirmLabel="Chấm dứt"
        isLoading={deleteMutation.isPending}
        onConfirm={async () => {
          if (deleteTarget) {
            await deleteMutation.mutateAsync(deleteTarget.id);
            setDeleteTarget(null);
          }
        }}
      />
    </div>
  );
}
