"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useEncounters, useOver12hAlerts } from "@/lib/hooks/use-encounters";
import { EncounterStatusBadge } from "@/components/domain/EncounterStatusBadge";
import { SimpleAvatar } from "@/components/domain/SimpleAvatar";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { AlertTriangle, Stethoscope, Clock, RefreshCw, Plus, MoreHorizontal, Activity, ClipboardList, CheckCircle } from "lucide-react";
import Link from "next/link";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { EncounterResponse } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { format } from "date-fns";
import { vi } from "date-fns/locale";

const ENCOUNTER_TYPE_LABELS: Record<string, string> = {
  FIRST_VISIT: "Khám mới",
  FOLLOW_UP: "Tái khám",
  EMERGENCY: "Cấp cứu",
  CONSULTATION: "Hội chẩn",
};

export function EncountersPageClient() {
  const router = useRouter();
  const [quickFilter, setQuickFilter] = useState<"today" | "waiting" | "over12h" | "">("");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [page, setPage] = useState(1);
  // Dialog tạo lượt khám đã chuyển sang route /encounters/new

  const today = format(new Date(), "yyyy-MM-dd");

  const params = {
    ...(statusFilter !== "all" && { status: statusFilter }),
    ...(quickFilter === "today" && { date_from: today, date_to: today }),
    ...(quickFilter === "waiting" && { status: "WAITING", date_from: today, date_to: today }),
    ...(dateFrom && !quickFilter && { date_from: dateFrom }),
    ...(dateTo && !quickFilter && { date_to: dateTo }),
    page,
    page_size: 20,
  };

  const { data, isLoading, refetch } = useEncounters(params);
  const { data: alerts } = useOver12hAlerts();

  const encounters = data?.data ?? [];
  const meta = data?.meta;

  const displayList =
    quickFilter === "over12h"
      ? encounters.filter((e) => e.alert_over_12h)
      : encounters;

  function handleRowClick(id: string) {
    router.push(`/encounters/${id}`);
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Khám bệnh</h2>
          <p className="text-sm text-muted-foreground">Quản lý lượt khám bệnh</p>
        </div>
        <div className="flex gap-2">
          <Link href="/encounters/new">
            <Button size="sm" className="gap-2 min-h-[44px]">
              <Plus className="h-4 w-4" />
              Tạo lượt khám
            </Button>
          </Link>
          <Button size="sm" variant="outline" onClick={() => refetch()} className="gap-2">
            <RefreshCw className="h-4 w-4" />
            Làm mới
          </Button>
        </div>
      </div>

      {/* Quick filters */}
      <div className="flex gap-2 flex-wrap">
        <QuickFilterBtn
          active={quickFilter === "today"}
          onClick={() => { setQuickFilter(quickFilter === "today" ? "" : "today"); setPage(1); }}
          icon={<Stethoscope className="h-4 w-4" />}
        >
          Đang khám hôm nay
        </QuickFilterBtn>
        <QuickFilterBtn
          active={quickFilter === "waiting"}
          onClick={() => { setQuickFilter(quickFilter === "waiting" ? "" : "waiting"); setPage(1); }}
          icon={<Clock className="h-4 w-4" />}
        >
          Chờ khám
        </QuickFilterBtn>
        <QuickFilterBtn
          active={quickFilter === "over12h"}
          onClick={() => { setQuickFilter(quickFilter === "over12h" ? "" : "over12h"); setPage(1); }}
          icon={<AlertTriangle className="h-4 w-4 text-red-500" />}
          danger
        >
          Quá 12h{alerts && alerts.length > 0 && (
            <Badge variant="destructive" className="ml-1 h-5 px-1 text-xs">{alerts.length}</Badge>
          )}
        </QuickFilterBtn>
      </div>

      {/* Filters */}
      <div className="flex gap-3 flex-wrap items-center">
        <Select
          value={statusFilter}
          onValueChange={(v) => { setStatusFilter(v ?? "all"); setPage(1); setQuickFilter(""); }}
        >
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Trạng thái" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả</SelectItem>
            <SelectItem value="WAITING">Chờ khám</SelectItem>
            <SelectItem value="IN_PROGRESS">Đang khám</SelectItem>
            <SelectItem value="DONE">Hoàn thành</SelectItem>
            <SelectItem value="CANCELLED">Đã hủy</SelectItem>
          </SelectContent>
        </Select>
        <Input
          type="date"
          value={dateFrom}
          onChange={(e) => { setDateFrom(e.target.value); setQuickFilter(""); setPage(1); }}
          className="w-40"
          aria-label="Từ ngày"
        />
        <Input
          type="date"
          value={dateTo}
          onChange={(e) => { setDateTo(e.target.value); setQuickFilter(""); setPage(1); }}
          className="w-40"
          aria-label="Đến ngày"
        />
        {(statusFilter !== "all" || dateFrom || dateTo || quickFilter) && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setStatusFilter("all");
              setDateFrom("");
              setDateTo("");
              setQuickFilter("");
              setPage(1);
            }}
          >
            Xóa bộ lọc
          </Button>
        )}
      </div>

      {/* Table */}
      <div className="rounded-xl border overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/40">
              <th className="text-left px-4 py-3 font-medium">Bệnh nhân</th>
              <th className="text-left px-4 py-3 font-medium">Bác sĩ</th>
              <th className="text-left px-4 py-3 font-medium">Loại khám</th>
              <th className="text-left px-4 py-3 font-medium">Trạng thái</th>
              <th className="text-left px-4 py-3 font-medium">Bắt đầu</th>
              <th className="text-left px-4 py-3 font-medium">Cảnh báo</th>
              <th className="text-right px-4 py-3 font-medium"></th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {isLoading ? (
              Array.from({ length: 6 }).map((_, i) => (
                <tr key={i}>
                  {Array.from({ length: 7 }).map((__, j) => (
                    <td key={j} className="px-4 py-3">
                      <Skeleton className="h-5 w-full" />
                    </td>
                  ))}
                </tr>
              ))
            ) : displayList.length === 0 ? (
              <tr>
                <td colSpan={7} className="py-12 text-center text-sm text-muted-foreground">
                  <Stethoscope className="mx-auto h-10 w-10 opacity-30 mb-2" />
                  Không có lượt khám nào
                </td>
              </tr>
            ) : (
              displayList.map((encounter) => (
                <EncounterRow
                  key={encounter.id}
                  encounter={encounter}
                  onClick={() => handleRowClick(encounter.id)}
                />
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {meta && meta.total > meta.page_size && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Tổng {meta.total} lượt khám
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Trước
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page * meta.page_size >= meta.total}
              onClick={() => setPage((p) => p + 1)}
            >
              Sau
            </Button>
          </div>
        </div>
      )}

      {/* CreateEncounterDialog đã chuyển sang /encounters/new */}
    </div>
  );
}

function EncounterRow({
  encounter,
  onClick,
}: {
  encounter: EncounterResponse;
  onClick: () => void;
}) {
  const router = useRouter();
  const isInProgress = encounter.status === "IN_PROGRESS";

  return (
    <tr
      className="hover:bg-accent/50 cursor-pointer transition-colors"
      onClick={onClick}
      tabIndex={0}
      onKeyDown={(e) => e.key === "Enter" && onClick()}
      role="row"
    >
      <td className="px-4 py-3">
        <div className="flex items-center gap-3">
          <SimpleAvatar name={encounter.patient_summary?.full_name ?? "?"} size="sm" />
          <div>
            <p className="font-medium">{encounter.patient_summary?.full_name ?? "—"}</p>
            {encounter.patient_summary?.year_of_birth && (
              <p className="text-xs text-muted-foreground">
                {encounter.patient_summary.year_of_birth} · {encounter.patient_summary.gender}
              </p>
            )}
          </div>
        </div>
      </td>
      <td className="px-4 py-3 text-muted-foreground">
        {encounter.doctor_name ?? "Chưa phân công"}
      </td>
      <td className="px-4 py-3">
        <Badge variant="outline" className="text-xs">
          {ENCOUNTER_TYPE_LABELS[encounter.encounter_type] ?? encounter.encounter_type}
        </Badge>
      </td>
      <td className="px-4 py-3">
        <EncounterStatusBadge status={encounter.status} />
      </td>
      <td className="px-4 py-3 text-muted-foreground">
        {(() => {
          const s = encounter.started_at;
          return s ? format(new Date(s), "HH:mm, dd/MM", { locale: vi }) : "—";
        })()}
      </td>
      <td className="px-4 py-3">
        {encounter.alert_over_12h && (
          <div className="flex items-center gap-1 text-red-600">
            <AlertTriangle className="h-4 w-4" />
            <span className="text-xs font-medium">Quá 12h</span>
          </div>
        )}
      </td>
      <td className="px-4 py-3 text-right" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-end gap-1">
          <Button
            variant="ghost"
            size="sm"
            className="text-xs h-8 hidden sm:inline-flex"
            onClick={() => router.push(`/encounters/${encounter.id}?tab=vitals`)}
            aria-label="Sinh hiệu"
          >
            <Activity className="mr-1 h-3.5 w-3.5" />
            Sinh hiệu
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="text-xs h-8 hidden sm:inline-flex"
            onClick={() => router.push(`/encounters/${encounter.id}?tab=diagnosis`)}
            aria-label="Chẩn đoán"
          >
            <ClipboardList className="mr-1 h-3.5 w-3.5" />
            Chẩn đoán
          </Button>
          {isInProgress && (
            <Button
              variant="ghost"
              size="sm"
              className="text-xs h-8 text-green-700 hidden sm:inline-flex"
              onClick={() => router.push(`/encounters/${encounter.id}?action=close`)}
              aria-label="Đóng lượt khám"
            >
              <CheckCircle className="mr-1 h-3.5 w-3.5" />
              Đóng
            </Button>
          )}
          <DropdownMenu>
            <DropdownMenuTrigger className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-muted" aria-label="Thao tác">
              <MoreHorizontal className="h-4 w-4" />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => router.push(`/encounters/${encounter.id}`)}>
                <ClipboardList className="mr-2 h-4 w-4" />
                Xem chi tiết
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => router.push(`/encounters/${encounter.id}?tab=vitals`)}>
                <Activity className="mr-2 h-4 w-4" />
                Sinh hiệu
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => router.push(`/encounters/${encounter.id}?tab=diagnosis`)}>
                <ClipboardList className="mr-2 h-4 w-4" />
                Chẩn đoán
              </DropdownMenuItem>
              {isInProgress && (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onClick={() => router.push(`/encounters/${encounter.id}?action=close`)}
                    className="text-green-700"
                  >
                    <CheckCircle className="mr-2 h-4 w-4" />
                    Đóng lượt khám
                  </DropdownMenuItem>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </td>
    </tr>
  );
}

function QuickFilterBtn({
  children,
  active,
  onClick,
  icon,
  danger,
}: {
  children: React.ReactNode;
  active: boolean;
  onClick: () => void;
  icon?: React.ReactNode;
  danger?: boolean;
}) {
  return (
    <Button
      variant={active ? "default" : "outline"}
      size="sm"
      onClick={onClick}
      className={cn("gap-2", danger && !active && "border-red-300 text-red-600 hover:bg-red-50")}
    >
      {icon}
      {children}
    </Button>
  );
}
