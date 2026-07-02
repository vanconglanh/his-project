"use client";

import { useState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  Search,
  Plus,
  MoreHorizontal,
  Eye,
  Edit2,
  Trash2,
  UserPlus,
  Filter,
} from "lucide-react";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { DataTable } from "@/components/ui/DataTable";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import {
  usePatients,
  usePatientSearch,
  useDeletePatient,
} from "@/lib/hooks/use-patients";
import { formatDate } from "@/lib/utils/format";
import type { PatientResponse, Gender } from "@/lib/api/types";
import { cn } from "@/lib/utils";

const GENDER_LABELS: Record<Gender, string> = { MALE: "Nam", FEMALE: "Nữ", OTHER: "Khác" };

function getInitials(name: string | null | undefined): string {
  if (!name) return "?";
  return name.split(" ").slice(-2).map((w) => w[0] ?? "").join("").toUpperCase() || "?";
}

export default function PatientsPage() {
  const router = useRouter();
  const [searchQ, setSearchQ] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [page, setPage] = useState(1);
  const [gender, setGender] = useState<string>("all");
  const [status, setStatus] = useState<string>("all");
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);

  // Debounce
  useEffect(() => {
    const t = setTimeout(() => { setDebouncedQ(searchQ); setPage(1); }, 300);
    return () => clearTimeout(t);
  }, [searchQ]);

  // Keyboard shortcut Ctrl+K, F2 → navigate
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === "k") {
        e.preventDefault();
        searchInputRef.current?.focus();
      }
      if (e.key === "F2") {
        e.preventDefault();
        router.push("/patients/new");
      }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [router]);

  const isSearching = debouncedQ.length >= 1;

  const listParams = {
    page,
    page_size: 20,
    gender: gender !== "all" ? gender : undefined,
    status: status !== "all" ? status : undefined,
  };

  const { data: listData, isLoading: listLoading } = usePatients(isSearching ? undefined : listParams);
  const { data: searchData, isLoading: searchLoading } = usePatientSearch(
    { q: debouncedQ, page_size: 20 },
    isSearching
  );

  const deleteMutation = useDeletePatient();

  const patients = isSearching ? (searchData?.data ?? []) : (listData?.data ?? []);
  const meta = isSearching ? searchData?.meta : listData?.meta;
  const isLoading = isSearching ? searchLoading : listLoading;

  const columns = [
    {
      key: "patient",
      header: "Bệnh nhân",
      cell: (row: PatientResponse) => (
        <div className="flex items-center gap-3 min-w-[200px]">
          <Avatar className="h-9 w-9 shrink-0">
            <AvatarImage src={row.avatar_url ?? undefined} alt={row.full_name} />
            <AvatarFallback className="text-xs">{getInitials(row.full_name)}</AvatarFallback>
          </Avatar>
          <div>
            <p className="font-medium text-sm">{row.full_name}</p>
            <p className="text-xs text-muted-foreground font-mono">{row.code}</p>
          </div>
        </div>
      ),
    },
    {
      key: "gender",
      header: "Giới tính",
      cell: (row: PatientResponse) => (
        <span className="text-sm">{row.gender ? GENDER_LABELS[row.gender] : "—"}</span>
      ),
    },
    {
      key: "dob",
      header: "Ngày sinh / Tuổi",
      cell: (row: PatientResponse) => (
        <div className="text-sm">
          <p>{formatDate(row.date_of_birth)}</p>
          {row.age !== undefined && <p className="text-xs text-muted-foreground">{row.age} tuổi</p>}
        </div>
      ),
    },
    {
      key: "phone",
      header: "Điện thoại",
      cell: (row: PatientResponse) => <span className="text-sm tabular-nums">{row.phone ?? "—"}</span>,
    },
    {
      key: "bhyt",
      header: "BHYT",
      cell: (row: PatientResponse) => {
        if (!row.bhyt_card_no) return <span className="text-xs text-muted-foreground">—</span>;
        const isActive = row.bhyt_valid_to ? new Date(row.bhyt_valid_to) >= new Date() : false;
        return (
          <Badge variant={isActive ? "default" : "secondary"} className="text-xs">
            {isActive ? "Còn hạn" : "Hết hạn"}
          </Badge>
        );
      },
    },
    {
      key: "reception_note",
      header: "Ghi chú tiếp đón",
      cell: (row: PatientResponse) =>
        row.reception_note ? (
          <Badge variant="outline" className="text-xs max-w-[150px] truncate block">
            {row.reception_note}
          </Badge>
        ) : null,
    },
    {
      key: "actions",
      header: "",
      className: "w-10",
      cell: (row: PatientResponse) => (
        <DropdownMenu>
          <DropdownMenuTrigger
            className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-accent"
            onClick={(e: React.MouseEvent) => e.stopPropagation()}
            aria-label="Thao tác"
          >
            <MoreHorizontal className="h-4 w-4" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => router.push(`/patients/${row.id}`)}>
              <Eye className="mr-2 h-4 w-4" />
              Xem chi tiết
            </DropdownMenuItem>
            <DropdownMenuItem onClick={() => router.push(`/patients/${row.id}/edit`)}>
              <Edit2 className="mr-2 h-4 w-4" />
              Chỉnh sửa
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-destructive"
              onClick={(e) => {
                e.stopPropagation();
                setDeleteId(row.id);
              }}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Xoá
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Bệnh nhân</h2>
          <p className="text-sm text-muted-foreground">Quản lý hồ sơ bệnh nhân</p>
        </div>
        <Link href="/patients/new" className={cn(buttonVariants({ variant: "default" }), "gap-2 self-start")}>
          <Plus className="h-4 w-4" />
          Tạo bệnh nhân mới
          <kbd className="ml-1 text-xs opacity-60 border rounded px-1 py-0.5">F2</kbd>
        </Link>
      </div>

      {/* Search + Filters */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
          <Input
            ref={searchInputRef}
            value={searchQ}
            onChange={(e) => setSearchQ(e.target.value)}
            placeholder="Tìm theo tên, SĐT, CMND, BHYT..."
            className="pl-9 pr-16"
            aria-label="Tìm kiếm bệnh nhân"
          />
          <kbd className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-muted-foreground border rounded px-1 py-0.5 hidden sm:inline">
            Ctrl+K
          </kbd>
        </div>
        <div className="flex gap-2">
          <Select value={gender} onValueChange={(v) => setGender(v ?? "all")}>
            <SelectTrigger className="w-32">
              <Filter className="h-3.5 w-3.5 mr-1" />
              <SelectValue placeholder="Giới tính" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tất cả</SelectItem>
              <SelectItem value="MALE">Nam</SelectItem>
              <SelectItem value="FEMALE">Nữ</SelectItem>
              <SelectItem value="OTHER">Khác</SelectItem>
            </SelectContent>
          </Select>
          <Select value={status} onValueChange={(v) => setStatus(v ?? "all")}>
            <SelectTrigger className="w-36">
              <SelectValue placeholder="Trạng thái" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tất cả</SelectItem>
              <SelectItem value="ACTIVE">Hoạt động</SelectItem>
              <SelectItem value="INACTIVE">Không hoạt động</SelectItem>
              <SelectItem value="DECEASED">Đã mất</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Table */}
      <DataTable
        columns={columns}
        data={patients}
        isLoading={isLoading}
        meta={meta}
        onPageChange={setPage}
        onRowClick={(row) => router.push(`/patients/${row.id}`)}
        skeletonRows={8}
        emptyState={
          <div className="flex flex-col items-center gap-3 py-6">
            <div className="rounded-full bg-muted p-4">
              <UserPlus className="h-8 w-8 text-muted-foreground" />
            </div>
            <div className="text-center">
              <p className="font-medium">
                {isSearching ? "Không tìm thấy bệnh nhân" : "Chưa có bệnh nhân"}
              </p>
              <p className="text-sm text-muted-foreground mt-1">
                {isSearching
                  ? "Thử tìm với từ khoá khác"
                  : "Tạo hồ sơ bệnh nhân đầu tiên để bắt đầu"}
              </p>
            </div>
            {!isSearching && (
              <Link href="/patients/new" className={cn(buttonVariants({ variant: "default" }), "gap-2 mt-2")}>
                <Plus className="h-4 w-4" />
                Tạo bệnh nhân đầu tiên
              </Link>
            )}
          </div>
        }
      />

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteId}
        onOpenChange={(open) => { if (!open) setDeleteId(null); }}
        title="Xoá bệnh nhân"
        description="Bạn có chắc muốn xoá bệnh nhân này? Hành động này không thể hoàn tác."
        confirmLabel="Xoá bệnh nhân"
        variant="destructive"
        isLoading={deleteMutation.isPending}
        onConfirm={() => {
          if (deleteId) deleteMutation.mutate(deleteId);
          setDeleteId(null);
        }}
      />
    </div>
  );
}
