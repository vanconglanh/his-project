"use client";

import { useMemo, useState } from "react";
import { Plus, Eye, EyeOff, Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { Tooltip, TooltipContent, TooltipTrigger, TooltipProvider } from "@/components/ui/tooltip";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { cn } from "@/lib/utils";
import {
  useAdminCodeGroups,
  useAdminCodeDetails,
  useCreateAdminCodeDetail,
  useUpdateAdminCodeDetail,
  useSetAdminCodeDetailVisibility,
  useDeleteAdminCodeDetail,
} from "@/lib/hooks/use-admin-codes";
import type { AdminCodeDetail } from "@/lib/api/admin-codes";
import { CodeDetailFormDialog } from "./CodeDetailFormDialog";

export function MasterCodesPageClient() {
  const { data: groups = [], isLoading: isLoadingGroups } = useAdminCodeGroups();
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);

  const activeGroupId = selectedGroupId ?? groups[0]?.id ?? null;
  const activeGroup = groups.find((g) => g.id === activeGroupId) ?? null;

  const { data: details = [], isLoading: isLoadingDetails } = useAdminCodeDetails(
    activeGroupId ?? ""
  );

  const createMutation = useCreateAdminCodeDetail(activeGroupId ?? "");
  const updateMutation = useUpdateAdminCodeDetail(activeGroupId ?? "");
  const visibilityMutation = useSetAdminCodeDetailVisibility(activeGroupId ?? "");
  const deleteMutation = useDeleteAdminCodeDetail(activeGroupId ?? "");

  const [formOpen, setFormOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<AdminCodeDetail | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AdminCodeDetail | null>(null);

  const sortedDetails = useMemo(
    () => [...details].sort((a, b) => (a.sort_order ?? 0) - (b.sort_order ?? 0)),
    [details]
  );

  function openCreate() {
    setEditTarget(null);
    setFormOpen(true);
  }

  function openEdit(row: AdminCodeDetail) {
    setEditTarget(row);
    setFormOpen(true);
  }

  const columns: Column<AdminCodeDetail>[] = [
    {
      key: "code",
      header: "Mã",
      cell: (row) => <span className="font-mono text-sm">{row.code}</span>,
    },
    {
      key: "name",
      header: "Tên hiển thị",
      cell: (row) => (
        <div className="flex items-center gap-2">
          <span className={cn("font-medium", row.is_hidden && "text-muted-foreground line-through")}>
            {row.name}
          </span>
          {row.is_hidden && (
            <Badge variant="outline" className="text-muted-foreground">
              Đã ẩn
            </Badge>
          )}
        </div>
      ),
    },
    {
      key: "name_en",
      header: "Tên tiếng Anh",
      cell: (row) => <span className="text-sm text-muted-foreground">{row.name_en ?? "—"}</span>,
    },
    {
      key: "source",
      header: "Nguồn",
      cell: (row) => (
        <Badge variant={row.is_override ? "secondary" : "outline"}>
          {row.is_override ? "Riêng phòng khám" : "Hệ thống"}
        </Badge>
      ),
    },
    {
      key: "actions",
      header: "",
      className: "w-56 text-right",
      cell: (row) => (
        <TooltipProvider>
          <div className="flex items-center justify-end gap-1">
            <Tooltip>
              <TooltipTrigger
                className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-muted"
                aria-label={row.is_hidden ? "Hiện giá trị" : "Ẩn giá trị"}
                onClick={() =>
                  visibilityMutation.mutate({ code: row.code, isHidden: !row.is_hidden })
                }
              >
                {row.is_hidden ? <Eye className="h-4 w-4" /> : <EyeOff className="h-4 w-4" />}
              </TooltipTrigger>
              <TooltipContent>{row.is_hidden ? "Hiện giá trị" : "Ẩn giá trị"}</TooltipContent>
            </Tooltip>

            <Button variant="ghost" size="sm" onClick={() => openEdit(row)}>
              <Pencil className="h-4 w-4" />
            </Button>

            {row.is_system ? (
              <Tooltip>
                <TooltipTrigger
                  className="inline-flex h-8 w-8 cursor-not-allowed items-center justify-center rounded-md text-muted-foreground/50"
                  aria-label="Mã hệ thống chỉ có thể ẩn"
                >
                  <Trash2 className="h-4 w-4" />
                </TooltipTrigger>
                <TooltipContent>Mã hệ thống chỉ có thể ẩn</TooltipContent>
              </Tooltip>
            ) : (
              <Button
                variant="ghost"
                size="sm"
                className="text-destructive hover:text-destructive"
                onClick={() => setDeleteTarget(row)}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
          </div>
        </TooltipProvider>
      ),
    },
  ];

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-[260px_1fr]">
      {/* Cột trái: danh sách nhóm mã */}
      <div className="rounded-md border">
        <div className="border-b px-3 py-2 text-sm font-semibold text-muted-foreground">
          Nhóm mã
        </div>
        {isLoadingGroups ? (
          <div className="space-y-2 p-3">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="h-8 w-full" />
            ))}
          </div>
        ) : groups.length === 0 ? (
          <p className="p-3 text-sm text-muted-foreground">Chưa có nhóm mã nào.</p>
        ) : (
          <ul className="max-h-[70vh] overflow-y-auto py-1">
            {groups.map((g) => (
              <li key={g.id}>
                <button
                  type="button"
                  onClick={() => setSelectedGroupId(g.id)}
                  className={cn(
                    "flex min-h-11 w-full items-center justify-between gap-2 px-3 py-2 text-left text-sm transition-colors hover:bg-muted",
                    activeGroupId === g.id && "bg-muted font-medium"
                  )}
                >
                  <span className="truncate">{g.name}</span>
                  {!g.is_active && (
                    <Badge variant="outline" className="shrink-0 text-muted-foreground">
                      Tạm ẩn
                    </Badge>
                  )}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Cột phải: giá trị của nhóm đang chọn */}
      <div className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <h3 className="text-base font-semibold">{activeGroup?.name ?? "Chọn một nhóm mã"}</h3>
            <p className="text-xs text-muted-foreground">
              {activeGroup?.is_system
                ? "Nhóm mã hệ thống — mã hệ thống chỉ có thể ẩn/hiện, mã riêng phòng khám có thể sửa/xoá."
                : "Nhóm mã riêng"}
            </p>
          </div>
          <Button onClick={openCreate} disabled={!activeGroupId} className="min-h-[40px]">
            <Plus className="h-4 w-4 mr-2" />
            Thêm giá trị
          </Button>
        </div>

        <DataTable
          columns={columns}
          data={sortedDetails}
          isLoading={isLoadingDetails}
          emptyState={
            <div className="flex flex-col items-center gap-2 text-muted-foreground">
              <p className="text-sm">Nhóm mã này chưa có giá trị nào.</p>
            </div>
          }
        />
      </div>

      <CodeDetailFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        groupLabel={activeGroup?.name ?? ""}
        editTarget={editTarget}
        isSaving={createMutation.isPending || updateMutation.isPending}
        onCreate={(values) =>
          createMutation.mutate(values, { onSuccess: () => setFormOpen(false) })
        }
        onUpdate={(values) => {
          if (!editTarget) return;
          updateMutation.mutate(
            { id: editTarget.id, body: values },
            { onSuccess: () => setFormOpen(false) }
          );
        }}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="Xoá giá trị"
        description={
          deleteTarget ? (
            <>
              Bạn có chắc muốn xoá giá trị <b>{deleteTarget.name}</b> ({deleteTarget.code})? Hành
              động này không thể hoàn tác.
            </>
          ) : (
            ""
          )
        }
        variant="destructive"
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
