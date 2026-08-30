"use client";

import { Fragment, useMemo, useState } from "react";
import { isAxiosError } from "axios";
import { subDays, format } from "date-fns";
import { Building2, TrendingUp, TrendingDown, Minus, Users2 } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/ui/page-header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ReportFilterBar, type DateRange } from "@/components/domain/ReportFilterBar";
import { HorizontalBarChart } from "@/components/domain/charts/HorizontalBarChart";
import { useBranchRanking, useBranchDetail } from "@/lib/hooks/use-chain-dashboard";
import { formatCurrency } from "@/lib/utils/format";
import { cn } from "@/lib/utils";

const fmt = (d: Date) => format(d, "yyyy-MM-dd");
const DEFAULT_FROM = fmt(subDays(new Date(), 29));
const DEFAULT_TO = fmt(new Date());

function PctChangeBadge({ value }: { value: number | null }) {
  if (value === null || value === undefined) {
    return <span className="text-xs text-muted-foreground">—</span>;
  }
  const isPositive = value > 0;
  const isNegative = value < 0;
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 text-xs font-medium",
        isPositive && "text-emerald-600",
        isNegative && "text-red-500",
        !isPositive && !isNegative && "text-muted-foreground"
      )}
    >
      {isPositive ? (
        <TrendingUp className="h-3 w-3" />
      ) : isNegative ? (
        <TrendingDown className="h-3 w-3" />
      ) : (
        <Minus className="h-3 w-3" />
      )}
      {isPositive ? "+" : ""}
      {value.toFixed(1)}%
    </span>
  );
}

function BranchDetailPanel({
  branchId,
  branchName,
  range,
}: {
  branchId: number;
  branchName: string;
  range: DateRange;
}) {
  const { data, isLoading, isError, error } = useBranchDetail(branchId, range);

  if (isError) {
    const denied = isAxiosError(error) && error.response?.status === 403;
    return (
      <div className="p-4 text-sm text-muted-foreground">
        {denied
          ? "Bạn không có quyền xem chi nhánh này."
          : "Không tải được dữ liệu chi tiết chi nhánh."}
      </div>
    );
  }

  return (
    <div className="p-4 bg-muted/30">
      <p className="text-sm font-semibold mb-2">Bác sĩ tại {branchName}</p>
      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-8 w-full" />
          ))}
        </div>
      ) : !data || data.doctors.length === 0 ? (
        <p className="text-sm text-muted-foreground">Chưa có dữ liệu bác sĩ trong khoảng thời gian này.</p>
      ) : (
        <div className="rounded-lg border bg-background">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Bác sĩ</TableHead>
                <TableHead className="text-right">Doanh thu</TableHead>
                <TableHead className="text-right">Lượt khám</TableHead>
                <TableHead className="text-right">Doanh thu/lượt</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.doctors.map((doc) => (
                <TableRow key={doc.doctor_id}>
                  <TableCell className="font-medium">{doc.doctor_name}</TableCell>
                  <TableCell className="text-right">{formatCurrency(doc.revenue)}</TableCell>
                  <TableCell className="text-right">{doc.encounter_count}</TableCell>
                  <TableCell className="text-right">{formatCurrency(doc.revenue_per_encounter)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}

export function ChainDashboardClient() {
  const [range, setRange] = useState<DateRange>({ from: DEFAULT_FROM, to: DEFAULT_TO });
  const [expandedBranchId, setExpandedBranchId] = useState<number | null>(null);

  const { data, isLoading, isError, error } = useBranchRanking(range);

  const rows = useMemo(() => {
    return [...(data?.data ?? [])].sort((a, b) => b.revenue - a.revenue);
  }, [data]);

  const chartData = useMemo(
    () => rows.slice(0, 10).map((r) => ({ label: r.branch_name, value: r.revenue })),
    [rows]
  );

  function handleToggleBranch(branchId: number) {
    setExpandedBranchId((prev) => (prev === branchId ? null : branchId));
  }

  if (isError) {
    const denied = isAxiosError(error) && error.response?.status === 403;
    if (denied) {
      toast.error("Bạn không có quyền xem chi nhánh này");
    }
  }

  return (
    <div className="space-y-5">
      <PageHeader
        title="Dashboard chuỗi chi nhánh"
        description="Bảng xếp hạng doanh thu, lượt khám và bệnh nhân mới theo chi nhánh"
      />

      <ReportFilterBar onRangeChange={setRange} showExport={false} />

      {isLoading ? (
        <Skeleton className="h-8 w-96" />
      ) : data?.meta ? (
        <div className="flex items-start gap-2 rounded-lg border bg-muted/40 p-3 text-sm">
          <Building2 className="h-4 w-4 mt-0.5 shrink-0 text-muted-foreground" />
          <p>
            <span className="font-medium">
              Dữ liệu: {data.meta.included_branch_count}/{data.meta.total_branch_count} chi nhánh
            </span>
            {data.meta.included_branch_names.length > 0 && (
              <span className="text-muted-foreground"> — {data.meta.included_branch_names.join(", ")}</span>
            )}
          </p>
        </div>
      ) : null}

      <div className="grid gap-4 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Xếp hạng chi nhánh theo doanh thu</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {isLoading ? (
              <div className="p-4 space-y-2">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Skeleton key={i} className="h-10 w-full" />
                ))}
              </div>
            ) : isError ? (
              <div className="p-6 text-center text-sm text-muted-foreground">
                Không tải được dữ liệu xếp hạng chi nhánh. Vui lòng thử lại.
              </div>
            ) : rows.length === 0 ? (
              <div className="p-10 text-center">
                <Users2 className="h-10 w-10 mx-auto text-muted-foreground mb-2" />
                <p className="text-sm text-muted-foreground">Chưa có dữ liệu trong khoảng thời gian đã chọn.</p>
              </div>
            ) : (
              <div className="rounded-b-lg border-t">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-12">Hạng</TableHead>
                      <TableHead>Chi nhánh</TableHead>
                      <TableHead className="text-right">Doanh thu</TableHead>
                      <TableHead className="text-right">Lượt khám</TableHead>
                      <TableHead className="text-right">Doanh thu/lượt</TableHead>
                      <TableHead className="text-right">BN mới</TableHead>
                      <TableHead className="text-right">% thay đổi</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {rows.map((row, idx) => (
                      <Fragment key={row.branch_id}>
                        <TableRow
                          className="cursor-pointer hover:bg-muted/50 min-h-[44px]"
                          onClick={() => handleToggleBranch(row.branch_id)}
                        >
                          <TableCell>
                            <Badge variant={idx === 0 ? "default" : "outline"} className="text-xs">
                              #{idx + 1}
                            </Badge>
                          </TableCell>
                          <TableCell className="font-medium">{row.branch_name}</TableCell>
                          <TableCell className="text-right font-semibold">
                            {formatCurrency(row.revenue)}
                          </TableCell>
                          <TableCell className="text-right">{row.encounter_count}</TableCell>
                          <TableCell className="text-right">
                            {formatCurrency(row.revenue_per_encounter)}
                          </TableCell>
                          <TableCell className="text-right">{row.new_patient_count}</TableCell>
                          <TableCell className="text-right">
                            <PctChangeBadge value={row.pct_change_revenue} />
                          </TableCell>
                        </TableRow>
                        {expandedBranchId === row.branch_id && (
                          <TableRow key={`${row.branch_id}-detail`}>
                            <TableCell colSpan={7} className="p-0">
                              <BranchDetailPanel
                                branchId={row.branch_id}
                                branchName={row.branch_name}
                                range={range}
                              />
                            </TableCell>
                          </TableRow>
                        )}
                      </Fragment>
                    ))}
                  </TableBody>
                </Table>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Top chi nhánh theo doanh thu</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-48 w-full" />
            ) : chartData.length === 0 ? (
              <div className="flex h-[180px] items-center justify-center text-sm text-muted-foreground">
                Chưa có dữ liệu
              </div>
            ) : (
              <HorizontalBarChart data={chartData} valueLabel="Doanh thu" />
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
