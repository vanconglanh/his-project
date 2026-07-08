"use client";

import { useState } from "react";
import { useDrugs, useDeleteDrug, useSyncCucQld } from "@/lib/hooks/use-drugs";
import { DataTable } from "@/components/ui/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { DrugForm } from "@/components/domain/DrugForm";
import { DrugImportDropzone } from "@/components/domain/DrugImportDropzone";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { Skeleton } from "@/components/ui/skeleton";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Search, Plus, Upload, RefreshCw, Trash2, Pencil } from "lucide-react";
import type { DrugMasterResponse } from "@/lib/api/drugs";

export function DrugsPageClient() {
  const [q, setQ] = useState("");
  const [status, setStatus] = useState<"ACTIVE" | "INACTIVE" | "">("");
  const [requiresPrescription, setRequiresPrescription] = useState<boolean | undefined>();
  const [page, setPage] = useState(1);

  const [createOpen, setCreateOpen] = useState(false);
  const [importOpen, setImportOpen] = useState(false);
  const [editDrug, setEditDrug] = useState<DrugMasterResponse | null>(null);
  const [deleteDrug, setDeleteDrug] = useState<DrugMasterResponse | null>(null);

  const { data, isLoading } = useDrugs({
    q: q || undefined,
    status: status || undefined,
    requires_prescription: requiresPrescription,
    page,
    page_size: 20,
  });

  const deleteMutation = useDeleteDrug();
  const syncMutation = useSyncCucQld();

  const columns = [
    {
      key: "code",
      header: "Mã",
      cell: (row: DrugMasterResponse) => <span className="font-mono text-xs">{row.code}</span>,
    },
    {
      key: "name_vi",
      header: "Tên thuốc",
      cell: (row: DrugMasterResponse) => (
        <div>
          <p className="font-medium text-sm">{row.name_vi}</p>
          {row.generic_name && <p className="text-xs text-muted-foreground">{row.generic_name}</p>}
        </div>
      ),
    },
    {
      key: "atc_code",
      header: "ATC",
      cell: (row: DrugMasterResponse) => <span className="text-xs font-mono">{row.atc_code ?? "-"}</span>,
    },
    {
      key: "form",
      header: "Dạng",
      cell: (row: DrugMasterResponse) => <span className="text-xs">{row.form}</span>,
    },
    {
      key: "strength",
      header: "Hàm lượng",
      cell: (row: DrugMasterResponse) => <span className="text-xs">{row.strength ?? "-"}</span>,
    },
    {
      key: "manufacturer",
      header: "NSX",
      cell: (row: DrugMasterResponse) => <span className="text-xs truncate max-w-[120px]">{row.manufacturer ?? "-"}</span>,
    },
    {
      key: "flags",
      header: "Loại",
      cell: (row: DrugMasterResponse) => (
        <div className="flex gap-1 flex-wrap">
          {row.requires_prescription && (
            <Badge variant="outline" className="text-xs px-1">Kê đơn</Badge>
          )}
          {row.is_psychotropic && (
            <Badge variant="destructive" className="text-xs px-1">Hướng thần</Badge>
          )}
          {row.is_narcotic && (
            <Badge variant="destructive" className="text-xs px-1">Gây nghiện</Badge>
          )}
        </div>
      ),
    },
    {
      key: "price",
      header: "Giá",
      cell: (row: DrugMasterResponse) => (
        <span className="text-sm text-right">{row.price ? row.price.toLocaleString("vi-VN") + "đ" : "-"}</span>
      ),
    },
    {
      key: "status",
      header: "TT",
      cell: (row: DrugMasterResponse) => (
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
      cell: (row: DrugMasterResponse) => (
        <div className="flex gap-1">
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            onClick={(e) => { e.stopPropagation(); setEditDrug(row); }}
            onDoubleClick={(e) => e.stopPropagation()}
          >
            <Pencil className="h-4 w-4" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 text-destructive"
            onClick={(e) => { e.stopPropagation(); setDeleteDrug(row); }}
            onDoubleClick={(e) => e.stopPropagation()}
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-wrap gap-2 items-center justify-between">
        <div className="flex flex-wrap gap-2 items-center flex-1">
          <div className="relative min-w-[200px] flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              value={q}
              onChange={(e) => { setQ(e.target.value); setPage(1); }}
              placeholder="Tìm tên, mã, hoạt chất..."
              className="pl-9"
            />
          </div>
          <Select
            items={{ "": "Tất cả", ACTIVE: "Hoạt động", INACTIVE: "Ẩn" }}
            value={status}
            onValueChange={(v) => { setStatus(v as "ACTIVE" | "INACTIVE" | ""); setPage(1); }}
          >
            <SelectTrigger className="w-36">
              <SelectValue placeholder="Tất cả TT" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Tất cả</SelectItem>
              <SelectItem value="ACTIVE">Hoạt động</SelectItem>
              <SelectItem value="INACTIVE">Ẩn</SelectItem>
            </SelectContent>
          </Select>
          <Select
            items={{ "": "Tất cả", true: "Kê đơn bắt buộc", false: "OTC" }}
            value={requiresPrescription === undefined ? "" : String(requiresPrescription)}
            onValueChange={(v) => setRequiresPrescription(v === "" ? undefined : v === "true")}
          >
            <SelectTrigger className="w-36">
              <SelectValue placeholder="Kê đơn" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Tất cả</SelectItem>
              <SelectItem value="true">Kê đơn bắt buộc</SelectItem>
              <SelectItem value="false">OTC</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => syncMutation.mutate({ mode: "INCREMENTAL" })}
            disabled={syncMutation.isPending}
          >
            <RefreshCw className="h-4 w-4 mr-2" />
            Sync Cục QLD
          </Button>
          <Button variant="outline" size="sm" onClick={() => setImportOpen(true)}>
            <Upload className="h-4 w-4 mr-2" />
            Import Excel
          </Button>
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Tạo thuốc
          </Button>
        </div>
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-14 w-full" />)}
        </div>
      ) : (
        <>
          <DataTable
            columns={columns}
            data={data?.data ?? []}
            onRowDoubleClick={(row) => setEditDrug(row)}
          />
          {data?.meta && data.meta.total > 20 && (
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <span>Tổng: {data.meta.total} thuốc</span>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Trước</Button>
                <span className="flex items-center px-2">{page} / {Math.ceil(data.meta.total / 20)}</span>
                <Button variant="outline" size="sm" disabled={page >= Math.ceil(data.meta.total / 20)} onClick={() => setPage((p) => p + 1)}>Tiếp</Button>
              </div>
            </div>
          )}
        </>
      )}

      {/* Create sheet */}
      <Sheet open={createOpen} onOpenChange={setCreateOpen}>
        <SheetContent className="w-full sm:max-w-2xl overflow-y-auto px-6 pb-6">
          <SheetHeader className="px-0"><SheetTitle>Tạo thuốc mới</SheetTitle></SheetHeader>
          <div className="mt-4">
            <DrugForm onSuccess={() => setCreateOpen(false)} onCancel={() => setCreateOpen(false)} />
          </div>
        </SheetContent>
      </Sheet>

      {/* Edit sheet */}
      <Sheet open={!!editDrug} onOpenChange={(o) => !o && setEditDrug(null)}>
        <SheetContent className="w-full sm:max-w-2xl overflow-y-auto px-6 pb-6">
          <SheetHeader className="px-0"><SheetTitle>Sửa thuốc</SheetTitle></SheetHeader>
          {editDrug && (
            <div className="mt-4">
              <DrugForm drug={editDrug} onSuccess={() => setEditDrug(null)} onCancel={() => setEditDrug(null)} />
            </div>
          )}
        </SheetContent>
      </Sheet>

      {/* Import dialog */}
      <Dialog open={importOpen} onOpenChange={setImportOpen}>
        <DialogContent fullScreen>
          <DialogHeader><DialogTitle>Import thuốc từ Excel</DialogTitle></DialogHeader>
          <DrugImportDropzone onSuccess={() => setImportOpen(false)} />
        </DialogContent>
      </Dialog>

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteDrug}
        onOpenChange={(o) => !o && setDeleteDrug(null)}
        title="Xóa thuốc"
        description={`Bạn có chắc muốn xóa thuốc "${deleteDrug?.name_vi}"? Hành động này không thể hoàn tác.`}
        variant="destructive"
        confirmLabel="Xóa"
        isLoading={deleteMutation.isPending}
        onConfirm={async () => {
          if (!deleteDrug) return;
          await deleteMutation.mutateAsync(deleteDrug.id);
          setDeleteDrug(null);
        }}
      />
    </div>
  );
}
