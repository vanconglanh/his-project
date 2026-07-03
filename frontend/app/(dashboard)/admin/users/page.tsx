"use client";

import { useState } from "react";
import { Plus, MoreHorizontal, Search } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageHeader } from "@/components/ui/page-header";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { StatusBadge } from "@/components/ui/entity-status-badge";
import { RoleBadge } from "@/components/ui/RoleBadge";
import { InviteUserForm } from "@/components/domain/InviteUserForm";
import { UserDetail } from "@/components/domain/UserDetail";
import { AssignRolesForm } from "@/components/domain/AssignRolesForm";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { Can } from "@/components/auth/Can";
import {
  useUsers,
  useInviteUser,
  useDisableUser,
  useEnableUser,
  useDeleteUser,
} from "@/lib/hooks/use-users";
import type { UserResponse } from "@/lib/api/types";
import { formatDateTime } from "@/lib/utils/format";

export default function UsersPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState("ALL");
  const [statusFilter, setStatusFilter] = useState("ALL");
  const [inviteOpen, setInviteOpen] = useState(false);
  const [detailTarget, setDetailTarget] = useState<UserResponse | null>(null);
  const [assignRolesTarget, setAssignRolesTarget] = useState<UserResponse | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<UserResponse | null>(null);

  const params = {
    page,
    page_size: 20,
    q: search || undefined,
    role: roleFilter !== "ALL" ? roleFilter : undefined,
    status: statusFilter !== "ALL" ? statusFilter : undefined,
  };

  const { data, isLoading } = useUsers(params);
  const inviteMutation = useInviteUser();
  const disableMutation = useDisableUser();
  const enableMutation = useEnableUser();
  const deleteMutation = useDeleteUser();

  const columns: Column<UserResponse>[] = [
    {
      key: "user",
      header: "Người dùng",
      cell: (row) => (
        <div className="flex items-center gap-3">
          <Avatar className="h-8 w-8">
            <AvatarImage src={row.avatar_url ?? undefined} />
            <AvatarFallback className="text-xs">
              {(row.full_name ?? "?").charAt(0).toUpperCase()}
            </AvatarFallback>
          </Avatar>
          <div>
            <p className="font-medium text-sm">{row.full_name}</p>
            <p className="text-xs text-muted-foreground">{row.email}</p>
          </div>
        </div>
      ),
    },
    {
      key: "phone",
      header: "Điện thoại",
      cell: (row) => row.phone ?? <span className="text-muted-foreground">—</span>,
    },
    {
      key: "roles",
      header: "Vai trò",
      cell: (row) => (
        <div className="flex flex-wrap gap-1">
          {row.roles.map((r) => (
            <RoleBadge key={r.code} code={r.code} name={r.name} />
          ))}
        </div>
      ),
    },
    {
      key: "status",
      header: "Trạng thái",
      cell: (row) => <StatusBadge status={row.status} />,
    },
    {
      key: "last_login_at",
      header: "Đăng nhập cuối",
      cell: (row) =>
        row.last_login_at ? (
          formatDateTime(row.last_login_at)
        ) : (
          <span className="text-muted-foreground">Chưa đăng nhập</span>
        ),
    },
    {
      key: "actions",
      header: "",
      cell: (row) => (
        <DropdownMenu>
          <DropdownMenuTrigger
            className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-muted"
            aria-label="Thao tác"
            onClick={(e) => e.stopPropagation()}
          >
            <MoreHorizontal className="h-4 w-4" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={(e) => { e.stopPropagation(); setAssignRolesTarget(row); }}>
              Gán vai trò
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            {row.status === "ACTIVE" || row.status === "PENDING" ? (
              <DropdownMenuItem
                onClick={(e) => { e.stopPropagation(); disableMutation.mutate(row.id); }}
                className="text-yellow-600"
              >
                Khoá tài khoản
              </DropdownMenuItem>
            ) : (
              <DropdownMenuItem
                onClick={(e) => { e.stopPropagation(); enableMutation.mutate(row.id); }}
                className="text-green-600"
              >
                Mở khoá
              </DropdownMenuItem>
            )}
            <DropdownMenuSeparator />
            <DropdownMenuItem
              onClick={(e) => { e.stopPropagation(); setDeleteTarget(row); }}
              className="text-destructive"
            >
              Xoá người dùng
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      ),
      className: "w-12",
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Quản lý người dùng"
        description="Danh sách người dùng trong phòng khám"
        actions={
          <Button onClick={() => setInviteOpen(true)} className="min-h-[44px]">
            <Plus className="h-4 w-4 mr-2" />
            Mời người dùng
          </Button>
        }
      />

      <div className="flex gap-3 flex-wrap">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Tìm theo tên, email..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            className="pl-9"
          />
        </div>
        <Select value={roleFilter} onValueChange={(v) => { if (v) { setRoleFilter(v); setPage(1); } }}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Vai trò" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả vai trò</SelectItem>
            <SelectItem value="ADMIN">Quản trị</SelectItem>
            <SelectItem value="BACSI">Bác sĩ</SelectItem>
            <SelectItem value="DIEUDUONG">Điều dưỡng</SelectItem>
            <SelectItem value="LETAN">Lễ tân</SelectItem>
            <SelectItem value="DUOCSI">Dược sĩ</SelectItem>
            <SelectItem value="KETOAN">Kế toán</SelectItem>
            <SelectItem value="KYTHUATVIEN">Kỹ thuật viên</SelectItem>
          </SelectContent>
        </Select>
        <Select value={statusFilter} onValueChange={(v) => { if (v) { setStatusFilter(v); setPage(1); } }}>
          <SelectTrigger className="w-36">
            <SelectValue placeholder="Trạng thái" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả</SelectItem>
            <SelectItem value="PENDING">Chờ kích hoạt</SelectItem>
            <SelectItem value="ACTIVE">Hoạt động</SelectItem>
            <SelectItem value="LOCKED">Bị khoá</SelectItem>
            <SelectItem value="DISABLED">Vô hiệu</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={data?.data ?? []}
        isLoading={isLoading}
        meta={data?.meta}
        onPageChange={setPage}
        onRowClick={(row) => setDetailTarget(row)}
      />

      {/* Invite dialog */}
      <Dialog open={inviteOpen} onOpenChange={setInviteOpen}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>Mời người dùng mới</DialogTitle>
          </DialogHeader>
          <InviteUserForm
            onSubmit={async (values) => {
              await inviteMutation.mutateAsync(values);
              setInviteOpen(false);
            }}
            isLoading={inviteMutation.isPending}
            onCancel={() => setInviteOpen(false)}
          />
        </DialogContent>
      </Dialog>

      {/* Detail sheet */}
      <Sheet open={!!detailTarget} onOpenChange={(o) => !o && setDetailTarget(null)}>
        <SheetContent className="w-full sm:max-w-xl overflow-y-auto px-6 pb-6">
          <SheetHeader>
            <SheetTitle>Chi tiết người dùng</SheetTitle>
          </SheetHeader>
          {detailTarget && <UserDetail user={detailTarget} />}
        </SheetContent>
      </Sheet>

      {/* Assign roles dialog */}
      <Dialog open={!!assignRolesTarget} onOpenChange={(o) => !o && setAssignRolesTarget(null)}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>Gán vai trò — {assignRolesTarget?.full_name}</DialogTitle>
          </DialogHeader>
          {assignRolesTarget && (
            <AssignRolesForm
              user={assignRolesTarget}
              onClose={() => setAssignRolesTarget(null)}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="Xoá người dùng"
        description={`Bạn có chắc muốn xoá "${deleteTarget?.full_name}"? Hành động này không thể hoàn tác.`}
        variant="destructive"
        confirmLabel="Xoá"
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
