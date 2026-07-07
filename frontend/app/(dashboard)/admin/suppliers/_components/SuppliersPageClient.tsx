"use client";

import { useState } from "react";
import { useSuppliers, useDeleteSupplier } from "@/lib/hooks/use-suppliers";
import { DataTable } from "@/components/ui/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { SupplierForm } from "@/components/domain/SupplierForm";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Search, Plus, Pencil, Trash2 } from "lucide-react";
import type { SupplierResponse } from "@/lib/api/suppliers";

export function SuppliersPageClient() {
  const [q, setQ] = useState("");
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);
  const [editSupplier, setEditSupplier] = useState<SupplierResponse | null>(null);
  const [deleteSupplier, setDeleteSupplier] = useState<SupplierResponse | null>(null);

  const { data, isLoading } = useSuppliers({ q: q || undefined, page, page_size: 20 });
  const deleteMutation = useDeleteSupplier();

  const columns = [
    {
      key: "code",
      header: "Mã",
      cell: (row: SupplierResponse) => <span className="font-mono text-xs">{row.code}</span>,
    },
    {
      key: "name",
      header: "Tên NCC",
      cell: (row: SupplierResponse) => <span className="font-medium text-sm">{row.name}</span>,
    },
    {
      key: "tax_code",
      header: "MST",
      cell: (row: SupplierResponse) => <span className="text-xs">{row.tax_code ?? "-"}</span>,
    },
    {
      key: "phone",
      header: "Điện thoại",
      cell: (row: SupplierResponse) => <span className="text-sm">{row.phone ?? "-"}</span>,
    },
    {
      key: "email",
      header: "Email",
      cell: (row: SupplierResponse) => <span className="text-sm">{row.email ?? "-"}</span>,
    },
    {
      key: "contact_person",
      header: "Người liên hệ",
      cell: (row: SupplierResponse) => <span className="text-sm">{row.contact_person ?? "-"}</span>,
    },
    {
      key: "status",
      header: "TT",
      cell: (row: SupplierResponse) => (
        <Badge
          className={row.status === "ACTIVE" ? "bg-green-100 text-green-800 border-green-300" : "bg-gray-100 text-gray-700 border-gray-300"}
          variant="outline"
        >
          {row.status === "ACTIVE" ? "Hoạt động" : "Ẩn"}
        </Badge>
      ),
    },
    {
      key: "actions",
      header: "",
      cell: (row: SupplierResponse) => (
        <div className="flex gap-1">
          <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => setEditSupplier(row)}>
            <Pencil className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive" onClick={() => setDeleteSupplier(row)}>
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
            onChange={(e) => { setQ(e.target.value); setPage(1); }}
            placeholder="Tìm nhà cung cấp..."
            className="pl-9"
          />
        </div>
        <Button size="sm" onClick={() => setCreateOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Tạo NCC
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
        </div>
      ) : (
        <>
          <DataTable columns={columns} data={data?.data ?? []} />
          {data?.meta && data.meta.total > 20 && (
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <span>Tổng: {data.meta.total}</span>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Trước</Button>
                <span className="flex items-center px-2">{page}</span>
                <Button variant="outline" size="sm" disabled={page >= Math.ceil(data.meta.total / 20)} onClick={() => setPage((p) => p + 1)}>Tiếp</Button>
              </div>
            </div>
          )}
        </>
      )}

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent fullScreen>
          <DialogHeader><DialogTitle>Tạo nhà cung cấp</DialogTitle></DialogHeader>
          <SupplierForm onSuccess={() => setCreateOpen(false)} onCancel={() => setCreateOpen(false)} />
        </DialogContent>
      </Dialog>

      <Dialog open={!!editSupplier} onOpenChange={(o) => !o && setEditSupplier(null)}>
        <DialogContent fullScreen>
          <DialogHeader><DialogTitle>Sửa nhà cung cấp</DialogTitle></DialogHeader>
          {editSupplier && (
            <SupplierForm supplier={editSupplier} onSuccess={() => setEditSupplier(null)} onCancel={() => setEditSupplier(null)} />
          )}
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={!!deleteSupplier}
        onOpenChange={(o) => !o && setDeleteSupplier(null)}
        title="Xóa nhà cung cấp"
        description={`Bạn có chắc muốn xóa "${deleteSupplier?.name}"?`}
        variant="destructive"
        confirmLabel="Xóa"
        isLoading={deleteMutation.isPending}
        onConfirm={async () => {
          if (!deleteSupplier) return;
          await deleteMutation.mutateAsync(deleteSupplier.id);
          setDeleteSupplier(null);
        }}
      />
    </div>
  );
}
