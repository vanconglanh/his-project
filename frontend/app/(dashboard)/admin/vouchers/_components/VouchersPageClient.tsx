"use client";

import { useState } from "react";
import { useVouchers, useCreateVoucher, useUpdateVoucher, useDeleteVoucher } from "@/lib/hooks/use-vouchers";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { DataTable } from "@/components/ui/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Plus, Pencil, Trash2, Ticket } from "lucide-react";
import { VOUCHER_APPLIES_TO_LABEL, type VoucherResponse } from "@/lib/api/vouchers";
import { VoucherFormDialog, type VoucherFormValues } from "./VoucherFormDialog";

const PAGE_SIZE = 20;

function fmtDate(d: string | null): string {
  if (!d) return "—";
  const [y, m, day] = d.slice(0, 10).split("-");
  return `${day}/${m}/${y}`;
}

export function VouchersPageClient() {
  const [page, setPage] = useState(1);
  const [formOpen, setFormOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<VoucherResponse | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<VoucherResponse | null>(null);

  const { has } = usePermissions();
  const canWrite = has("voucher.write");
  const canDelete = has("voucher.delete");

  const { data, isLoading, isError, refetch } = useVouchers({ page, page_size: PAGE_SIZE });
  const createMut = useCreateVoucher();
  const updateMut = useUpdateVoucher();
  const deleteMut = useDeleteVoucher();

  const openCreate = () => {
    setEditTarget(null);
    setFormOpen(true);
  };
  const openEdit = (v: VoucherResponse) => {
    setEditTarget(v);
    setFormOpen(true);
  };

  const handleSubmit = async (v: VoucherFormValues) => {
    try {
      if (editTarget) {
        // Backend không cho đổi code -> bỏ code khỏi body PUT.
        const body = { ...v } as Partial<VoucherFormValues>;
        delete body.code;
        await updateMut.mutateAsync({ id: editTarget.id, body: body as Omit<VoucherFormValues, "code"> });
      } else {
        await createMut.mutateAsync(v);
      }
      setFormOpen(false);
    } catch {
      /* toast đã hiện ở hook */
    }
  };

  const columns = [
    {
      key: "code",
      header: "Mã voucher",
      cell: (v: VoucherResponse) => <span className="font-mono text-xs font-semibold">{v.code}</span>,
    },
    {
      key: "description",
      header: "Mô tả",
      cell: (v: VoucherResponse) => <span className="text-sm">{v.description || "—"}</span>,
    },
    {
      key: "discount_percent",
      header: "Giảm giá",
      cell: (v: VoucherResponse) => <span className="text-sm font-medium">{v.discount_percent}%</span>,
    },
    {
      key: "applies_to",
      header: "Áp dụng cho",
      cell: (v: VoucherResponse) => (
        <span className="text-sm">{VOUCHER_APPLIES_TO_LABEL[v.applies_to] ?? v.applies_to}</span>
      ),
    },
    {
      key: "valid",
      header: "Hiệu lực",
      cell: (v: VoucherResponse) => (
        <span className="text-sm">
          {fmtDate(v.valid_from)} - {fmtDate(v.valid_to)}
        </span>
      ),
    },
    {
      key: "max_uses",
      header: "Lượt dùng",
      cell: (v: VoucherResponse) => (
        <span className="text-sm">
          {v.used_count ?? 0}/{v.max_uses ?? "∞"}
        </span>
      ),
    },
    {
      key: "is_active",
      header: "Trạng thái",
      cell: (v: VoucherResponse) => (
        <Badge
          variant="outline"
          className={
            v.is_active
              ? "bg-green-100 text-green-800 border-green-300"
              : "bg-gray-100 text-gray-700 border-gray-300"
          }
        >
          {v.is_active ? "Hoạt động" : "Ngừng"}
        </Badge>
      ),
    },
    {
      key: "actions",
      header: "",
      cell: (v: VoucherResponse) => (
        <div className="flex gap-1">
          {canWrite && (
            <Button variant="ghost" size="icon" className="h-11 w-11" aria-label={`Sửa ${v.code}`} onClick={() => openEdit(v)}>
              <Pencil className="h-4 w-4" />
            </Button>
          )}
          {canDelete && (
            <Button
              variant="ghost"
              size="icon"
              className="h-11 w-11 text-destructive"
              aria-label={`Xóa ${v.code}`}
              onClick={() => setDeleteTarget(v)}
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
            Thêm voucher
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
          <p className="mb-3 text-destructive">Không tải được danh sách voucher.</p>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Thử lại
          </Button>
        </div>
      ) : rows.length === 0 ? (
        <div className="flex flex-col items-center gap-3 rounded-md border border-dashed p-10 text-center">
          <Ticket className="h-10 w-10 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">Chưa có voucher nào.</p>
          {canWrite && (
            <Button size="sm" onClick={openCreate}>
              <Plus className="h-4 w-4 mr-2" />
              Thêm voucher
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

      <VoucherFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        editTarget={editTarget}
        isSaving={createMut.isPending || updateMut.isPending}
        onSubmit={handleSubmit}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="Xóa voucher"
        description={`Bạn có chắc muốn xóa voucher "${deleteTarget?.code}"? Mã sẽ không còn dùng được cho hoá đơn mới.`}
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
