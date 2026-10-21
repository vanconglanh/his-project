"use client";

import { useState } from "react";
import { useRoomsAdmin, useCreateRoom, useUpdateRoom, useDeleteRoom } from "@/lib/hooks/use-rooms";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { DataTable } from "@/components/ui/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Plus, Pencil, Trash2, DoorOpen } from "lucide-react";
import type { RoomAdminResponse } from "@/lib/api/rooms";
import { RoomFormDialog, type RoomFormValues } from "./RoomFormDialog";

const PAGE_SIZE = 20;

export function RoomsPageClient() {
  const [page, setPage] = useState(1);
  const [formOpen, setFormOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<RoomAdminResponse | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<RoomAdminResponse | null>(null);

  const { has } = usePermissions();
  const canWrite = has("room.write");
  const canDelete = has("room.delete");

  const { data, isLoading, isError, refetch } = useRoomsAdmin({ page, page_size: PAGE_SIZE });
  const createMut = useCreateRoom();
  const updateMut = useUpdateRoom();
  const deleteMut = useDeleteRoom();

  const openCreate = () => {
    setEditTarget(null);
    setFormOpen(true);
  };
  const openEdit = (r: RoomAdminResponse) => {
    setEditTarget(r);
    setFormOpen(true);
  };

  const handleSubmit = async (v: RoomFormValues) => {
    try {
      if (editTarget) await updateMut.mutateAsync({ id: editTarget.id, body: v });
      else await createMut.mutateAsync(v);
      setFormOpen(false);
    } catch {
      /* toast đã hiện ở hook */
    }
  };

  const columns = [
    {
      key: "room_code",
      header: "Mã phòng",
      cell: (r: RoomAdminResponse) => <span className="font-mono text-xs">{r.room_code}</span>,
    },
    {
      key: "name",
      header: "Tên phòng",
      cell: (r: RoomAdminResponse) => <span className="font-medium text-sm">{r.name}</span>,
    },
    {
      key: "max_per_day",
      header: "Tối đa/ngày",
      cell: (r: RoomAdminResponse) => <span className="text-sm">{r.max_per_day}</span>,
    },
    {
      key: "is_active",
      header: "Trạng thái",
      cell: (r: RoomAdminResponse) => (
        <Badge
          variant="outline"
          className={
            r.is_active
              ? "bg-green-100 text-green-800 border-green-300"
              : "bg-gray-100 text-gray-700 border-gray-300"
          }
        >
          {r.is_active ? "Hoạt động" : "Ngừng"}
        </Badge>
      ),
    },
    {
      key: "actions",
      header: "",
      cell: (r: RoomAdminResponse) => (
        <div className="flex gap-1">
          {canWrite && (
            <Button
              variant="ghost"
              size="icon"
              className="h-11 w-11"
              aria-label={`Sửa ${r.name}`}
              onClick={() => openEdit(r)}
            >
              <Pencil className="h-4 w-4" />
            </Button>
          )}
          {canDelete && (
            <Button
              variant="ghost"
              size="icon"
              className="h-11 w-11 text-destructive"
              aria-label={`Xóa ${r.name}`}
              onClick={() => setDeleteTarget(r)}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          )}
        </div>
      ),
    },
  ];

  const rows = data?.data ?? [];

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        {canWrite && (
          <Button size="sm" className="h-11" onClick={openCreate}>
            <Plus className="h-4 w-4 mr-2" />
            Thêm phòng khám
          </Button>
        )}
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </div>
      ) : isError ? (
        <div className="rounded-md border p-6 text-center text-sm">
          <p className="mb-3 text-destructive">Không tải được danh sách phòng khám.</p>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Thử lại
          </Button>
        </div>
      ) : rows.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-md border border-dashed p-10 text-center">
          <DoorOpen className="h-10 w-10 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">Chưa có phòng khám nào. Hãy thêm phòng đầu tiên.</p>
          {canWrite && (
            <Button size="sm" onClick={openCreate}>
              <Plus className="h-4 w-4 mr-2" />
              Thêm phòng khám
            </Button>
          )}
        </div>
      ) : (
        <>
          <DataTable columns={columns} data={rows} />
          {data?.meta && data.meta.total > PAGE_SIZE && (
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <span>Tổng: {data.meta.total}</span>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
                  Trước
                </Button>
                <span className="flex items-center px-2">{page}</span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= Math.ceil(data.meta.total / PAGE_SIZE)}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Tiếp
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      <RoomFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        editTarget={editTarget}
        isSaving={createMut.isPending || updateMut.isPending}
        onSubmit={handleSubmit}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="Xóa phòng khám"
        description={`Bạn có chắc muốn xóa phòng "${deleteTarget?.name}"? Phòng sẽ không còn hiện ở form Tiếp đón.`}
        variant="destructive"
        confirmLabel="Xóa"
        isLoading={deleteMut.isPending}
        onConfirm={async () => {
          if (!deleteTarget) return;
          try {
            await deleteMut.mutateAsync(deleteTarget.id);
          } finally {
            setDeleteTarget(null);
          }
        }}
      />
    </div>
  );
}
