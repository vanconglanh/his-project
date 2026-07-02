"use client";

import { useState } from "react";
import { format, subDays } from "date-fns";
import { Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ReportFilterBar, type DateRange } from "@/components/domain/ReportFilterBar";
import { ExportReportDialog } from "@/components/domain/ExportReportDialog";
import { RevenueTrendChart } from "@/components/domain/charts/RevenueTrendChart";
import { ComplicationsRateChart } from "@/components/domain/charts/ComplicationsRateChart";
import { useRevenueReport, useRevenueByMethod, useTopDoctorsReport } from "@/lib/hooks/use-reports";

const fmt = (d: Date) => format(d, "yyyy-MM-dd");
const DEFAULT_FROM = fmt(subDays(new Date(), 29));
const DEFAULT_TO = fmt(new Date());
const vnd = (n: unknown) =>
  typeof n === "number" && !isNaN(n) ? n.toLocaleString("vi-VN") : "—";

export function FinancialTab() {
  const [range, setRange] = useState<DateRange>({ from: DEFAULT_FROM, to: DEFAULT_TO });
  const [exportOpen, setExportOpen] = useState(false);

  const { data: revenue, isLoading: revLoading } = useRevenueReport("DAY", range.from, range.to);
  const { data: byMethod, isLoading: methodLoading } = useRevenueByMethod(range.from, range.to);
  const { data: doctors, isLoading: docLoading } = useTopDoctorsReport(range.from, range.to);

  const methodPieData = (byMethod ?? []).map((x) => ({ name: x.label, value: x.value }));

  return (
    <div className="space-y-5">
      <div className="flex items-center gap-2">
        <div className="flex-1">
          <ReportFilterBar
            onRangeChange={setRange}
            onExport={() => setExportOpen(true)}
          />
        </div>
        <Button
          variant="outline"
          size="sm"
          className="gap-2 shrink-0 min-h-[44px]"
          onClick={() =>
            window.open(
              `/reports/print/financial?from=${range.from}&to=${range.to}`,
              "_blank"
            )
          }
        >
          <Printer className="h-4 w-4" />
          In báo cáo
        </Button>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Xu hướng doanh thu</CardTitle>
          </CardHeader>
          <CardContent>
            {revLoading ? (
              <Skeleton className="h-48 w-full" />
            ) : revenue ? (
              <>
                <p className="text-xs text-muted-foreground mb-2">
                  Tổng:{" "}
                  <span className="font-semibold text-foreground">
                    {vnd(revenue.total)} ₫
                  </span>
                </p>
                <RevenueTrendChart data={(revenue.by_breakdown ?? []).map((x) => ({ label: (x.period_label ?? "").slice(5), value: x.total ?? 0 }))} />
              </>
            ) : null}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Theo phương thức TT</CardTitle>
          </CardHeader>
          <CardContent>
            {methodLoading ? (
              <Skeleton className="h-48 w-full" />
            ) : (
              <ComplicationsRateChart data={methodPieData} />
            )}
          </CardContent>
        </Card>
      </div>

      {/* Doctor table */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-semibold">KPI theo bác sĩ</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {docLoading ? (
            <div className="p-4">
              <Skeleton className="h-40 w-full" />
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Bác sĩ</TableHead>
                  <TableHead className="text-right">Lượt khám</TableHead>
                  <TableHead className="text-right">Doanh thu</TableHead>
                  <TableHead className="text-right">TB/lượt</TableHead>
                  <TableHead className="text-right">RVU</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(doctors ?? []).map((d, idx) => (
                  <TableRow key={d.doctor_id ?? `doc-${idx}`}>
                    <TableCell className="font-medium">{d.name}</TableCell>
                    <TableCell className="text-right">{d.encounter_count}</TableCell>
                    <TableCell className="text-right">{vnd(d.revenue)} ₫</TableCell>
                    <TableCell className="text-right">{vnd(d.avg_revenue_per_encounter)} ₫</TableCell>
                    <TableCell className="text-right">{d.rvu}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <ExportReportDialog
        open={exportOpen}
        onOpenChange={setExportOpen}
        reportType="REVENUE"
        filters={{ from: range.from, to: range.to }}
        filterSummary={`${range.from} → ${range.to}`}
      />
    </div>
  );
}
