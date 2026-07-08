"use client";

import { useState } from "react";
import { format, subDays } from "date-fns";
import { AlertTriangle, Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { ReportFilterBar, type DateRange } from "@/components/domain/ReportFilterBar";
import { ExportReportDialog } from "@/components/domain/ExportReportDialog";
import { HorizontalBarChart } from "@/components/domain/charts/HorizontalBarChart";
import { useTopPharmacyDrugs, useInventoryValue, useNearExpirySummary } from "@/lib/hooks/use-reports";

const fmt = (d: Date) => format(d, "yyyy-MM-dd");
const DEFAULT_FROM = fmt(subDays(new Date(), 29));
const DEFAULT_TO = fmt(new Date());
const vnd = (n: unknown) =>
  typeof n === "number" && !isNaN(n) ? n.toLocaleString("vi-VN") : "—";

export function PharmacyTab() {
  const [range, setRange] = useState<DateRange>({ from: DEFAULT_FROM, to: DEFAULT_TO });
  const [exportOpen, setExportOpen] = useState(false);

  const { data: topDrugs, isLoading: drugsLoading } = useTopPharmacyDrugs(range.from, range.to);
  const { data: inventory, isLoading: invLoading } = useInventoryValue();
  const { data: nearExpiry } = useNearExpirySummary();

  const drugChartData = (topDrugs ?? []).slice(0, 10).map((d) => {
    const name = d.drug_name ?? "";
    return {
      label: name.length > 22 ? name.slice(0, 22) + "…" : name,
      value: d.revenue ?? 0,
    };
  });

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
              `/reports/print/pharmacy?from=${range.from}&to=${range.to}`,
              "_blank"
            )
          }
        >
          <Printer className="h-4 w-4" />
          In báo cáo
        </Button>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* Top drugs */}
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Top thuốc bán chạy (doanh thu)</CardTitle>
          </CardHeader>
          <CardContent>
            {drugsLoading ? (
              <Skeleton className="h-80 w-full" />
            ) : (
              <HorizontalBarChart data={drugChartData} valueLabel="Doanh thu" color="var(--chart-4)" />
            )}
          </CardContent>
        </Card>

        {/* Inventory value */}
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Giá trị tồn kho</CardTitle>
          </CardHeader>
          <CardContent>
            {invLoading ? (
              <Skeleton className="h-32 w-full" />
            ) : inventory ? (
              <div className="space-y-3">
                <div>
                  <p className="text-xs text-muted-foreground">Tổng giá trị</p>
                  <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums">{vnd(inventory.total_value)} ₫</p>
                  <p className="text-xs text-muted-foreground">{inventory.total_skus ?? 0} SKU</p>
                </div>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Nhóm thuốc</TableHead>
                      <TableHead className="text-right">Giá trị</TableHead>
                      <TableHead className="text-right">SKU</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {(inventory.by_category ?? []).map((c, idx) => (
                      <TableRow key={c.category ?? `cat-${idx}`}>
                        <TableCell className="text-sm">{c.category}</TableCell>
                        <TableCell className="text-right text-sm">{vnd(c.value)} ₫</TableCell>
                        <TableCell className="text-right text-sm">{c.sku_count}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            ) : null}
          </CardContent>
        </Card>
      </div>

      {/* Near expiry */}
      {nearExpiry && (
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold flex items-center gap-2">
              <AlertTriangle className="h-4 w-4 text-[color:var(--status-warning)]" />
              Thuốc sắp hết hạn
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-wrap gap-4 mb-3">
              <div>
                <p className="text-xs text-muted-foreground">Tổng lô</p>
                <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums">{nearExpiry.total_lots ?? 0}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Giá trị rủi ro</p>
                <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums text-[color:var(--status-warning)]">{vnd(nearExpiry.total_value_at_risk)} ₫</p>
              </div>
            </div>
            <div className="flex flex-wrap gap-2">
              {(nearExpiry.by_bucket ?? []).map((b, idx) => (
                <Badge key={b.bucket ?? `bucket-${idx}`} variant="outline" className="gap-1 text-xs">
                  {b.bucket}: {b.lot_count} lô — {vnd(b.value)} ₫
                </Badge>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Top drugs table */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-semibold">Chi tiết thuốc bán chạy</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã</TableHead>
                <TableHead>Tên thuốc</TableHead>
                <TableHead className="text-right">SL bán</TableHead>
                <TableHead className="text-right">Doanh thu</TableHead>
                <TableHead className="text-right">Số đơn</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {(topDrugs ?? []).map((d, idx) => (
                <TableRow key={d.drug_id ?? `drug-${idx}`}>
                  <TableCell className="text-xs text-muted-foreground font-mono">{d.drug_code}</TableCell>
                  <TableCell className="font-medium text-sm">{d.drug_name}</TableCell>
                  <TableCell className="text-right">{d.quantity_sold}</TableCell>
                  <TableCell className="text-right">{vnd(d.revenue)} ₫</TableCell>
                  <TableCell className="text-right">{d.prescription_count}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <ExportReportDialog
        open={exportOpen}
        onOpenChange={setExportOpen}
        reportType="TOP_DRUGS"
        filters={{ from: range.from, to: range.to }}
        filterSummary={`${range.from} → ${range.to}`}
      />
    </div>
  );
}
