"use client";

import { useState } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { useDebts } from "@/lib/hooks/use-cashier";
import { formatCurrency } from "@/lib/utils/format";
import { cn } from "@/lib/utils";
import { format } from "date-fns";

function AgingBadge({ days }: { days: number | null }) {
  if (days == null) return <Badge variant="outline" className="text-xs">Chưa xác định</Badge>;
  if (days <= 30) return <Badge className="text-xs bg-green-100 text-green-700 border-green-200 hover:bg-green-100">0-30 ngày</Badge>;
  if (days <= 60) return <Badge className="text-xs bg-amber-100 text-amber-700 border-amber-200 hover:bg-amber-100">30-60 ngày</Badge>;
  if (days <= 90) return <Badge className="text-xs bg-orange-100 text-orange-700 border-orange-200 hover:bg-orange-100">60-90 ngày</Badge>;
  return <Badge className="text-xs bg-red-100 text-red-700 border-red-200 hover:bg-red-100">&gt;90 ngày</Badge>;
}

export function DebtsTab() {
  const [search, setSearch] = useState("");
  const { data, isLoading } = useDebts({ min_balance: 1, q: search || undefined });
  const rows = data?.data ?? [];

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <Input
          placeholder="Tìm bệnh nhân..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-sm"
        />
        {data?.summary && (
          <div className="text-sm text-muted-foreground">
            {data.summary.total_patients} bệnh nhân — Tổng nợ:{" "}
            <span className="font-semibold text-destructive">{formatCurrency(data.summary.total_debt)}</span>
          </div>
        )}
      </div>

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Mã BN</TableHead>
              <TableHead>Tên bệnh nhân</TableHead>
              <TableHead>Điện thoại</TableHead>
              <TableHead className="text-right">Đã thu</TableHead>
              <TableHead className="text-right">Còn nợ</TableHead>
              <TableHead>Lần TT cuối</TableHead>
              <TableHead>Aging</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading
              ? Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    {Array.from({ length: 7 }).map((_, j) => (
                      <TableCell key={j}><Skeleton className="h-5 w-full" /></TableCell>
                    ))}
                  </TableRow>
                ))
              : rows.length === 0
              ? (
                <TableRow>
                  <TableCell colSpan={7} className="h-32 text-center text-muted-foreground">
                    Không có công nợ
                  </TableCell>
                </TableRow>
              )
              : rows.map((d) => (
                <TableRow key={d.patient_id}>
                  <TableCell className="font-mono text-xs">{d.patient_code}</TableCell>
                  <TableCell className="font-medium">{d.patient_name}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{d.phone ?? "—"}</TableCell>
                  <TableCell className="text-right text-green-600">{formatCurrency(d.total_paid)}</TableCell>
                  <TableCell className="text-right font-bold text-destructive">{formatCurrency(d.balance)}</TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {d.last_payment_at ? format(new Date(d.last_payment_at), "dd/MM/yyyy") : "—"}
                  </TableCell>
                  <TableCell><AgingBadge days={d.days_overdue} /></TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
