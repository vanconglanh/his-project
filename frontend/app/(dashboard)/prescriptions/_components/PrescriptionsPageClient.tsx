"use client";

import { useState } from "react";
import { usePrescriptions } from "@/lib/hooks/use-prescriptions";
import { DataTable } from "@/components/ui/DataTable";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/ui/page-header";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import Link from "next/link";
import type { PrescriptionResponse, PrescriptionStatus } from "@/lib/api/prescriptions";
import { Search, Filter, Plus, MoreHorizontal, Pill, Send, Eye } from "lucide-react";
import { useRouter } from "next/navigation";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

const STATUS_LABELS: Record<PrescriptionStatus, string> = {
  DRAFT: "Nháp",
  SIGNED: "Đã ký",
  SUBMITTED_DTQG: "Đã gửi ĐTQG",
  DISPENSED: "Đã phát",
  PARTIAL_DISPENSED: "Phát một phần",
  CANCELLED: "Đã hủy",
};

const STATUS_VARIANT: Record<PrescriptionStatus, string> = {
  DRAFT: "bg-gray-100 text-gray-700 border-gray-300",
  SIGNED: "bg-blue-100 text-blue-800 border-blue-300",
  SUBMITTED_DTQG: "bg-purple-100 text-purple-800 border-purple-300",
  DISPENSED: "bg-green-100 text-green-800 border-green-300",
  PARTIAL_DISPENSED: "bg-yellow-100 text-yellow-800 border-yellow-300",
  CANCELLED: "bg-red-100 text-red-800 border-red-300",
};

export function PrescriptionsPageClient() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [status, setStatus] = useState<PrescriptionStatus | "">("");
  const [page, setPage] = useState(1);

  const { data, isLoading } = usePrescriptions({
    page,
    page_size: 20,
    q: q || undefined,
    status: status || undefined,
  });

  const columns = [
    {
      key: "patient_summary.full_name",
      header: "Bệnh nhân",
      cell: (row: PrescriptionResponse) => (
        <div>
          <p className="font-medium">{row.patient_summary?.full_name}</p>
          {row.patient_summary?.bhyt_no && (
            <p className="text-xs text-muted-foreground">{row.patient_summary.bhyt_no}</p>
          )}
        </div>
      ),
    },
    {
      key: "doctor_name",
      header: "Bác sĩ",
      cell: (row: PrescriptionResponse) => <span className="text-sm">{row.doctor_name}</span>,
    },
    {
      key: "prescribed_at",
      header: "Ngày kê",
      cell: (row: PrescriptionResponse) => (
        <span className="text-sm text-muted-foreground">
          {row.prescribed_at
            ? format(parseISO(row.prescribed_at), "dd/MM/yyyy HH:mm", { locale: vi })
            : "—"}
        </span>
      ),
    },
    {
      key: "status",
      header: "Trạng thái",
      cell: (row: PrescriptionResponse) => (
        <Badge className={STATUS_VARIANT[row.status]} variant="outline">
          {STATUS_LABELS[row.status]}
        </Badge>
      ),
    },
    {
      key: "dtqg_code",
      header: "Mã ĐTQG",
      cell: (row: PrescriptionResponse) => (
        <span className="font-mono text-xs">{row.dtqg_code ?? "-"}</span>
      ),
    },
    {
      key: "items",
      header: "Số thuốc",
      cell: (row: PrescriptionResponse) => (
        <span className="text-sm text-center">{row.items?.length ?? 0}</span>
      ),
    },
    {
      key: "total_amount",
      header: "Tổng tiền",
      cell: (row: PrescriptionResponse) => (
        <span className="text-sm text-right font-medium">
          {(row.total_amount ?? 0).toLocaleString("vi-VN")}đ
        </span>
      ),
    },
    {
      key: "actions",
      header: "",
      cell: (row: PrescriptionResponse) => (
        <div className="flex items-center gap-1">
          {row.status === "DRAFT" && (
            <Button
              variant="ghost"
              size="sm"
              className="text-xs h-8"
              onClick={(e) => { e.stopPropagation(); router.push(`/prescriptions/${row.id}?action=add-drug`); }}
              onDoubleClick={(e) => e.stopPropagation()}
              aria-label="Thêm thuốc"
            >
              <Pill className="mr-1 h-3.5 w-3.5" />
              Thêm thuốc
            </Button>
          )}
          {row.status === "DRAFT" && (row.items?.length ?? 0) > 0 && (
            <Button
              variant="ghost"
              size="sm"
              className="text-xs h-8 text-primary"
              onClick={(e) => { e.stopPropagation(); router.push(`/prescriptions/${row.id}?action=submit`); }}
              onDoubleClick={(e) => e.stopPropagation()}
              aria-label="Gửi đơn"
            >
              <Send className="mr-1 h-3.5 w-3.5" />
              Gửi
            </Button>
          )}
          <DropdownMenu>
            <DropdownMenuTrigger
              className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-muted"
              onClick={(e) => e.stopPropagation()}
              onDoubleClick={(e) => e.stopPropagation()}
              aria-label="Thao tác"
            >
              <MoreHorizontal className="h-4 w-4" />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => router.push(`/prescriptions/${row.id}`)}>
                <Eye className="mr-2 h-4 w-4" />
                Xem chi tiết
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <PageHeader
        title="Kê đơn thuốc"
        description="Quản lý đơn thuốc bệnh nhân"
        actions={
          <Link href="/prescriptions/new">
            <Button size="sm" className="gap-2 min-h-[44px]">
              <Plus className="h-4 w-4" />
              Tạo đơn mới
            </Button>
          </Link>
        }
      />

      {/* Filters */}
      <div className="flex flex-wrap gap-2 items-center">
        <div className="relative flex-1 min-w-[200px]">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            value={q}
            onChange={(e) => { setQ(e.target.value); setPage(1); }}
            placeholder="Tìm theo tên BN, mã đơn..."
            className="pl-9"
            aria-label="Tìm kiếm đơn thuốc"
          />
        </div>

        <Select
          items={{ "": "Tất cả trạng thái", ...STATUS_LABELS }}
          value={status}
          onValueChange={(v) => { setStatus(v as PrescriptionStatus | ""); setPage(1); }}
        >
          <SelectTrigger className="w-44" aria-label="Lọc trạng thái">
            <Filter className="h-4 w-4 mr-2 text-muted-foreground" />
            <SelectValue placeholder="Tất cả TT" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Tất cả trạng thái</SelectItem>
            {(Object.keys(STATUS_LABELS) as PrescriptionStatus[]).map((s) => (
              <SelectItem key={s} value={s}>{STATUS_LABELS[s]}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-14 w-full rounded-md" />
          ))}
        </div>
      ) : (
        <>
          <DataTable
            columns={columns}
            data={data?.data ?? []}
            onRowDoubleClick={(row) => router.push(`/prescriptions/${row.id}`)}
          />

          {/* Pagination */}
          {data?.meta && data.meta.total > 20 && (
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <span>Tổng: {data.meta.total} đơn thuốc</span>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                >
                  Trước
                </Button>
                <span className="flex items-center px-2">
                  {page} / {Math.ceil(data.meta.total / 20)}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= Math.ceil(data.meta.total / 20)}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Tiếp
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
