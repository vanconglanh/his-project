"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { ReconcileSummary } from "@/lib/api/bhyt-reconcile";
import { BhytAmountChart } from "./BhytAmountChart";

function formatVnd(n: number) {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(n);
}

function pct(a: number, b: number) {
  if (!b) return "0%";
  return ((a / b) * 100).toFixed(1) + "%";
}

interface Props {
  summary: ReconcileSummary;
}

export function BhytReconcileSummaryCard({ summary }: Props) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-xs font-medium text-muted-foreground uppercase">Tổng yêu cầu</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-lg font-bold">{formatVnd(summary.total_requested_amount)}</p>
          <p className="text-xs text-muted-foreground mt-0.5">{summary.total_items} dòng</p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-xs font-medium text-green-700 uppercase">Được duyệt</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-lg font-bold text-green-700">{formatVnd(summary.total_approved_amount)}</p>
          <p className="text-xs text-muted-foreground mt-0.5">
            {pct(summary.total_approved_amount, summary.total_requested_amount)} — {summary.approved_items} dòng
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-xs font-medium text-red-700 uppercase">Từ chối</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-lg font-bold text-red-700">{formatVnd(summary.total_rejected_amount)}</p>
          <p className="text-xs text-muted-foreground mt-0.5">
            {pct(summary.total_rejected_amount, summary.total_requested_amount)} — {summary.rejected_items} dòng
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-xs font-medium text-muted-foreground uppercase">Tỉ lệ duyệt</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-2xl font-bold text-primary">
            {pct(summary.total_approved_amount, summary.total_requested_amount)}
          </p>
          <p className="text-xs text-muted-foreground mt-0.5">Đang khiếu nại: {summary.disputed_items} dòng</p>
        </CardContent>
      </Card>

      <Card className="sm:col-span-2 lg:col-span-4">
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-medium">Biểu đồ số tiền</CardTitle>
        </CardHeader>
        <CardContent>
          <BhytAmountChart
            requested={summary.total_requested_amount}
            approved={summary.total_approved_amount}
            rejected={summary.total_rejected_amount}
          />
        </CardContent>
      </Card>

      {summary.top_rejection_reasons.length > 0 && (
        <Card className="sm:col-span-2 lg:col-span-4">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Lý do từ chối phổ biến</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {summary.top_rejection_reasons.map((r) => (
                <div key={r.code} className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">[{r.code}] {r.reason}</span>
                  <span className="font-medium">{r.count} dòng / {formatVnd(r.amount)}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
