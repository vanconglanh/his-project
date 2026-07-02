"use client";

import { useState } from "react";
import { Plus, Shield } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { RoleForm } from "@/components/domain/RoleForm";
import { PermissionMatrix } from "@/components/domain/PermissionMatrix";
import { Can } from "@/components/auth/Can";
import { useRoles, useCreateRole, useUpdateRole, useDeleteRole } from "@/lib/hooks/use-roles";
import type { RoleResponse } from "@/lib/api/types";

export default function RolesPage() {
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedRole, setSelectedRole] = useState<RoleResponse | null>(null);
  const [editRole, setEditRole] = useState<RoleResponse | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<RoleResponse | null>(null);

  const { data: roles = [], isLoading } = useRoles();
  const createMutation = useCreateRole();
  const updateMutation = useUpdateRole();
  const deleteMutation = useDeleteRole();

  const columns: Column<RoleResponse>[] = [
    {
      key: "code",
      header: "Mã vai trò",
      cell: (row) => (
        <div className="flex items-center gap-2">
          <Shield className="h-4 w-4 text-muted-foreground" />
          <span className="font-mono text-sm">{row.code}</span>
        </div>
      ),
    },
    {
      key: "name",
      header: "Tên vai trò",
      cell: (row) => <span className="font-medium">{row.name}</span>,
    },
    {
      key: "type",
      header: "Loại",
      cell: (row) => (
        <Badge variant={row.role_type === "SYSTEM" ? "secondary" : "outline"}>
          {row.role_type === "SYSTEM" ? "Hệ thống" : "Tuỳ chỉnh"}
        </Badge>
      ),
    },
    {
      key: "permissions",
      header: "Số quyền",
      cell: (row) => (
        <span className="text-sm text-muted-foreground">
          {(row.permission_codes ?? []).length} quyền
        </span>
      ),
    },
    {
      key: "description",
      header: "Mô tả",
      cell: (row) => (
        <span className="text-sm text-muted-foreground">{row.description ?? "—"}</span>
      ),
    },
    {
      key: "actions",
      header: "",
      cell: (row) => (
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={(e) => {
              e.stopPropagation();
              setSelectedRole(row);
            }}
          >
            Sửa quyền
          </Button>
          {row.role_type === "CUSTOM" && (
            <>
              <Button
                variant="ghost"
                size="sm"
                onClick={(e) => {
                  e.stopPropagation();
                  setEditRole(row);
                }}
              >
                Sửa
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="text-destructive hover:text-destructive"
                onClick={(e) => {
                  e.stopPropagation();
                  setDeleteTarget(row);
                }}
              >
                Xoá
              </Button>
            </>
          )}
        </div>
      ),
      className: "w-48",
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold tracking-tight">Vai trò & Quyền hạn</h1>
          <p className="text-muted-foreground text-sm mt-1">Quản lý vai trò và ma trận quyền hạn</p>
        </div>
        <Can permission="role.write">
          <Button onClick={() => setCreateOpen(true)} className="min-h-[44px]">
            <Plus className="h-4 w-4 mr-2" />
            Tạo vai trò mới
          </Button>
        </Can>
      </div>

      <DataTable
        columns={columns}
        data={roles}
        isLoading={isLoading}
        onRowClick={(row) => setSelectedRole(row)}
      />

      {/* Permission matrix sheet */}
      <Sheet open={!!selectedRole} onOpenChange={(o) => !o && setSelectedRole(null)}>
        <SheetContent className="w-full sm:max-w-xl overflow-y-auto px-6 pb-6">
          <SheetHeader>
            <SheetTitle>Ma trận quyền — {selectedRole?.name}</SheetTitle>
          </SheetHeader>
          {selectedRole && <PermissionMatrix role={selectedRole} />}
        </SheetContent>
      </Sheet>

      {/* Create dialog */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Tạo vai trò mới</DialogTitle>
          </DialogHeader>
          <RoleForm
            onSubmit={async (values) => {
              await createMutation.mutateAsync(
                values as import("@/lib/api/types").CreateRoleRequest
              );
              setCreateOpen(false);
            }}
            isLoading={createMutation.isPending}
            onCancel={() => setCreateOpen(false)}
          />
        </DialogContent>
      </Dialog>

      {/* Edit dialog */}
      <Dialog open={!!editRole} onOpenChange={(o) => !o && setEditRole(null)}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Sửa vai trò — {editRole?.name}</DialogTitle>
          </DialogHeader>
          {editRole && (
            <RoleForm
              initialValues={editRole}
              isEdit
              onSubmit={async (values) => {
                await updateMutation.mutateAsync({
                  code: editRole.code,
                  payload: values as import("@/lib/api/types").UpdateRoleRequest,
                });
                setEditRole(null);
              }}
              isLoading={updateMutation.isPending}
              onCancel={() => setEditRole(null)}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="Xoá vai trò"
        description={`Bạn có chắc muốn xoá vai trò "${deleteTarget?.name}"? Người dùng đang có vai trò này sẽ mất quyền.`}
        variant="destructive"
        confirmLabel="Xoá"
        isLoading={deleteMutation.isPending}
        onConfirm={async () => {
          if (deleteTarget) {
            await deleteMutation.mutateAsync(deleteTarget.code);
            setDeleteTarget(null);
          }
        }}
      />
    </div>
  );
}
