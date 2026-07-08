"use client";

import { useState } from "react";
import { format, subDays } from "date-fns";
import { Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { ReportFilterBar, type DateRange } from "@/components/domain/ReportFilterBar";
import { ExportReportDialog } from "@/components/domain/ExportReportDialog";
import { EncountersTrendChart } from "@/components/domain/charts/EncountersTrendChart";
import { useEncountersTrend, useTopDiagnoses, useDiabetesCohort } from "@/lib/hooks/use-reports";

const fmt = (d: Date) => format(d, "yyyy-MM-dd");
const DEFAULT_FROM = fmt(subDays(new Date(), 29));
const DEFAULT_TO = fmt(new Date());

export function ClinicalTab() {
  const [range, setRange] = useState<DateRange>({ from: DEFAULT_FROM, to: DEFAULT_TO });
  const [exportOpen, setExportOpen] = useState(false);

  const { data: encounters, isLoading: encLoading } = useEncountersTrend("DAY", range.from, range.to);
  const { data: diagnoses, isLoading: diagLoading } = useTopDiagnoses(range.from, range.to);
  const { data: cohort } = useDiabetesCohort();

  const encChartData = (encounters ?? []).map((x) => ({
    label: (x.period_label ?? "").slice(5),
    value: x.count ?? 0,
  }));

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
              `/reports/print/clinical?from=${range.from}&to=${range.to}`,
              "_blank"
            )
          }
        >
          <Printer className="h-4 w-4" />
          In báo cáo
        </Button>
      </div>

      {/* Encounters chart */}
      <Card>
        <CardHeader className="pb-2">
          <CardTitle className="text-sm font-semibold">Lượt khám theo ngày</CardTitle>
        </CardHeader>
        <CardContent>
          {encLoading ? (
            <Skeleton className="h-48 w-full" />
          ) : encChartData.length > 0 ? (
            <EncountersTrendChart data={encChartData} />
          ) : null}
        </CardContent>
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* Top diagnoses */}
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-semibold">Top chẩn đoán ICD-10</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {diagLoading ? (
              <div className="p-4"><Skeleton className="h-48 w-full" /></div>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Mã / Tên</TableHead>
                    <TableHead className="text-right">Lượt</TableHead>
                    <TableHead className="text-right">%</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {(diagnoses ?? []).slice(0, 8).map((d, idx) => (
                    <TableRow key={d.icd10_code ?? `dx-${idx}`}>
                      <TableCell>
                        <div className="flex flex-col gap-0.5">
                          <Badge variant="outline" className="w-fit text-xs">{d.icd10_code}</Badge>
                          <span className="text-xs text-muted-foreground">{d.icd10_name}</span>
                        </div>
                      </TableCell>
                      <TableCell className="text-right font-medium">{d.count ?? 0}</TableCell>
                      <TableCell className="text-right text-muted-foreground">{(d.percentage ?? 0).toFixed(1)}%</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>

        {/* Diabetes cohort summary */}
        {cohort && (
          <Card>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-semibold">Bệnh nhân đái tháo đường</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <p className="text-xs text-muted-foreground">Tổng BN</p>
                  <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums">{cohort.total_patients}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Type 2</p>
                  <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums">{cohort.by_type.t2}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">HbA1c kiểm soát tốt (&lt;7%)</p>
                  <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums text-[color:var(--status-done)]">{cohort.hba1c_distribution.lt_7}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">HbA1c kém (&gt;9%)</p>
                  <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums text-[color:var(--status-critical)]">{cohort.hba1c_distribution.gt_9}</p>
                </div>
                <div className="col-span-2">
                  <p className="text-xs text-muted-foreground mb-1">Biến chứng</p>
                  <div className="flex flex-wrap gap-1.5">
                    {[
                      { label: "Võng mạc", v: cohort.complications.retinopathy },
                      { label: "Thần kinh", v: cohort.complications.neuropathy },
                      { label: "Thận", v: cohort.complications.nephropathy },
                    ].map((c) => (
                      <Badge key={c.label} variant="secondary" className="text-xs">
                        {c.label}: {c.v}
                      </Badge>
                    ))}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      <ExportReportDialog
        open={exportOpen}
        onOpenChange={setExportOpen}
        reportType="ENCOUNTERS_COUNT"
        filters={{ from: range.from, to: range.to }}
        filterSummary={`${range.from} → ${range.to}`}
      />
    </div>
  );
}
