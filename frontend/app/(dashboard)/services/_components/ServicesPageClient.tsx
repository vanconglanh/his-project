"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Plus, MoreHorizontal, Upload, Pencil, Trash2 } from "lucide-react";
import {
  useServices,
  useDeleteService,
  useImportServices,
} from "@/lib/hooks/use-services";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { formatCurrency } from "@/lib/utils/format";
import type { ServiceResponse } from "@/lib/api/services";

const CATEGORY_LABEL: Record<string, { label: string; className: string }> = {
  CONSULTATION: { label: "Khám bệnh", className: "bg-purple-100 text-purple-700 border-purple-200" },
  PROCEDURE: { label: "Thủ thuật", className: "bg-blue-100 text-blue-700 border-blue-200" },
  LAB: { label: "Xét nghiệm", className: "bg-amber-100 text-amber-700 border-amber-200" },
  RAD: { label: "CĐHA", className: "bg-cyan-100 text-cyan-700 border-cyan-200" },
  PHARMACY: { label: "Dược", className: "bg-green-100 text-green-700 border-green-200" },
  OTHER: { label: "Khác", className: "bg-gray-100 text-gray-700 border-gray-200" },
};

export function ServicesPageClient() {
  const router = useRouter();
  const [search, setSearch] = useState("");
  const [deleteTarget, setDeleteTarget] = useState<ServiceResponse | null>(null);

  const { data, isLoading } = useServices({ q: search || undefined, page_size: 100 });
  const deleteService = useDeleteService();
  const importServices = useImportServices();

  const rows = data?.data ?? [];

  function handleImport() {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = ".xlsx,.xls,.csv";
    input.onchange = async () => {
      const file = input.files?.[0];
      if (!file) return;
      try {
        const result = await importServices.mutateAsync(file);
        toast.success(`Import xong: ${result.inserted} mới, ${result.updated} cập nhật`);
      } catch {
        toast.error("Import thất bại");
      }
    };
    input.click();
  }

  async function handleDelete() {
    if (!deleteTarget) return;
    try {
      await deleteService.mutateAsync(deleteTarget.id);
      toast.success("Đã xoá dịch vụ");
      setDeleteTarget(null);
    } catch {
      toast.error("Xoá thất bại");
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold tracking-tight">Danh mục dịch vụ</h2>
          <p className="text-sm text-muted-foreground">Bảng giá dịch vụ phòng khám</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={handleImport} disabled={importServices.isPending}>
            <Upload className="mr-2 h-4 w-4" />
            Import Excel
          </Button>
          <Button size="sm" onClick={() => router.push("/services/new")}>
            <Plus className="mr-2 h-4 w-4" />
            Tạo dịch vụ
          </Button>
        </div>
      </div>

      <Input
        placeholder="Tìm theo mã, tên dịch vụ..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="max-w-sm"
      />

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Mã</TableHead>
              <TableHead>Tên dịch vụ</TableHead>
              <TableHead>Nhóm</TableHead>
              <TableHead className="text-right">Giá</TableHead>
              <TableHead className="text-right">VAT</TableHead>
              <TableHead>Mã BHYT</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading
              ? Array.from({ length: 6 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 8 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                    ))}
                  </TableRow>
                ))
              : rows.length === 0
              ? (
                <TableRow>
                  <TableCell colSpan={8} className="h-32 text-center text-muted-foreground">
                    Chưa có dịch vụ. Tạo mới hoặc import Excel.
                  </TableCell>
                </TableRow>
              )
              : rows.map((svc) => {
                const cat = CATEGORY_LABEL[svc.category] ?? { label: svc.category, className: "" };
                return (
                  <TableRow key={svc.id}>
                    <TableCell className="font-mono text-xs font-semibold">{svc.code}</TableCell>
                    <TableCell className="font-medium">{svc.name}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className={`text-xs ${cat.className}`}>{cat.label}</Badge>
                    </TableCell>
                    <TableCell className="text-right">{formatCurrency(svc.price)}</TableCell>
                    <TableCell className="text-right">{svc.vat_rate}%</TableCell>
                    <TableCell className="text-xs font-mono">{svc.bhyt_code ?? "—"}</TableCell>
                    <TableCell>
                      <Badge variant={svc.is_active ? "default" : "secondary"} className="text-xs">
                        {svc.is_active ? "Hoạt động" : "Tạm ngưng"}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <DropdownMenu>
                        <DropdownMenuTrigger className="inline-flex h-8 w-8 items-center justify-center rounded-lg hover:bg-muted">
                          <MoreHorizontal className="h-4 w-4" />
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem onClick={() => router.push(`/services/${svc.id}/edit`)}>
                            <Pencil className="mr-2 h-4 w-4" /> Sửa
                          </DropdownMenuItem>
                          <DropdownMenuItem
                            className="text-destructive"
                            onClick={() => setDeleteTarget(svc)}
                          >
                            <Trash2 className="mr-2 h-4 w-4" /> Xoá
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                  </TableRow>
                );
              })}
          </TableBody>
        </Table>
      </div>

      <ConfirmDialog
        open={Boolean(deleteTarget)}
        onOpenChange={(o) => { if (!o) setDeleteTarget(null); }}
        title="Xoá dịch vụ"
        description={`Bạn có chắc muốn xoá dịch vụ "${deleteTarget?.name}"?`}
        confirmLabel="Xoá"
        variant="destructive"
        onConfirm={handleDelete}
        isLoading={deleteService.isPending}
      />
    </div>
  );
}
