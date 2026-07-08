"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { TrendingUp, TrendingDown, Minus, ShieldAlert } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Can } from "@/components/auth/Can";
import { useRiskList } from "@/lib/hooks/use-diabetes-dashboard";
import { formatDate, formatNumber } from "@/lib/utils/format";
import type { RiskLevel, RiskListItem } from "@/lib/api/types";

const RISK_LABELS: Record<RiskLevel, string> = {
  HIGH: "Cao",
  MEDIUM: "Trung bình",
  LOW: "Thấp",
};

const RISK_BADGE_CLASS: Record<RiskLevel, string> = {
  HIGH: "bg-red-100 text-red-800 border-red-300 dark:bg-red-950/40 dark:text-red-300 dark:border-red-800",
  MEDIUM:
    "bg-amber-100 text-amber-800 border-amber-300 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800",
  LOW: "bg-green-100 text-green-800 border-green-300 dark:bg-green-950/40 dark:text-green-300 dark:border-green-800",
};

function TrendIcon({ trend }: { trend?: string | null }) {
  if (trend === "UP" || trend === "INCREASING") return <TrendingUp className="h-3.5 w-3.5 text-red-600" />;
  if (trend === "DOWN" || trend === "DECREASING") return <TrendingDown className="h-3.5 w-3.5 text-green-600" />;
  if (!trend) return null;
  return <Minus className="h-3.5 w-3.5 text-muted-foreground" />;
}

function RiskListContent() {
  const router = useRouter();
  const [level, setLevel] = useState<string>("ALL");
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useRiskList({
    level: level === "ALL" ? undefined : (level as RiskLevel),
    sort: "risk_score:desc",
    page,
    pageSize,
  });

  const items = data?.data ?? [];
  const meta = data?.meta
    ? {
        page: data.meta.page,
        page_size: data.meta.page_size,
        total: data.meta.total,
        total_pages: Math.max(1, Math.ceil(data.meta.total / (data.meta.page_size || pageSize))),
      }
    : undefined;

  const columns: Column<RiskListItem>[] = [
    {
      key: "patient",
      header: "Bệnh nhân",
      cell: (row) => (
        <div className="min-w-[180px]">
          <p className="font-medium text-sm">{row.patient_full_name}</p>
          <p className="text-xs text-muted-foreground font-mono">{row.patient_code}</p>
        </div>
      ),
    },
    {
      key: "risk_level",
      header: "Mức nguy cơ",
      cell: (row) => (
        <Badge variant="outline" className={RISK_BADGE_CLASS[row.risk_level]}>
          {RISK_LABELS[row.risk_level] ?? row.risk_level} ({formatNumber(Math.round(row.risk_score))})
        </Badge>
      ),
    },
    {
      key: "hba1c",
      header: "HbA1c",
      cell: (row) => (
        <div className="flex items-center gap-1 text-sm tabular-nums">
          {row.latest_hba1c != null ? `${row.latest_hba1c}%` : "—"}
          <TrendIcon trend={row.hba1c_trend} />
        </div>
      ),
    },
    {
      key: "egfr",
      header: "eGFR",
      cell: (row) => (
        <span className="text-sm tabular-nums">{row.latest_egfr != null ? row.latest_egfr : "—"}</span>
      ),
    },
    {
      key: "bp",
      header: "Huyết áp",
      cell: (row) => (
        <span className="text-sm tabular-nums">
          {row.latest_bp_sys != null && row.latest_bp_dia != null
            ? `${row.latest_bp_sys}/${row.latest_bp_dia}`
            : "—"}
        </span>
      ),
    },
    {
      key: "last_visit",
      header: "Khám gần nhất",
      cell: (row) => <span className="text-sm">{row.last_visit_at ? formatDate(row.last_visit_at) : "—"}</span>,
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Danh sách nguy cơ ĐTĐ"
        description="Bệnh nhân đái tháo đường phân tầng nguy cơ theo HbA1c, eGFR, huyết áp"
      />

      <div className="flex items-center gap-2">
        <Select
          items={{ ALL: "Tất cả", HIGH: "Cao", MEDIUM: "Trung bình", LOW: "Thấp" }}
          value={level}
          onValueChange={(v) => {
            setLevel(v ?? "ALL");
            setPage(1);
          }}
        >
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Mức nguy cơ" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả</SelectItem>
            <SelectItem value="HIGH">Cao</SelectItem>
            <SelectItem value="MEDIUM">Trung bình</SelectItem>
            <SelectItem value="LOW">Thấp</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <DataTable
        columns={columns}
        data={items}
        isLoading={isLoading}
        meta={meta}
        onPageChange={setPage}
        onRowClick={(row) => router.push(`/patients/${row.patient_id}/diabetes`)}
        skeletonRows={8}
        emptyState={
          <div className="flex flex-col items-center gap-3 py-6">
            <div className="rounded-full bg-muted p-4">
              <ShieldAlert className="h-8 w-8 text-muted-foreground" />
            </div>
            <div className="text-center">
              <p className="font-medium">Chưa có dữ liệu phân tầng nguy cơ</p>
              <p className="text-sm text-muted-foreground mt-1">
                Dữ liệu sẽ xuất hiện sau khi hệ thống tính toán nguy cơ ĐTĐ cho bệnh nhân
              </p>
            </div>
          </div>
        }
      />
    </div>
  );
}

export default function RiskListPage() {
  return (
    <Can
      permission="risk.read"
      fallback={
        <div className="flex flex-col items-center justify-center h-64 gap-2 text-muted-foreground">
          <ShieldAlert className="h-10 w-10" />
          <p>Bạn không có quyền xem danh sách nguy cơ ĐTĐ</p>
        </div>
      }
    >
      <RiskListContent />
    </Can>
  );
}
