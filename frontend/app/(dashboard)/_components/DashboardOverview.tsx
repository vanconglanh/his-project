"use client";

import { useState, useCallback, useEffect } from "react";
import { useTranslations } from "next-intl";
import {
  TrendingUp,
  Stethoscope,
  Users,
  ClipboardList,
  RefreshCw,
} from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { vi } from "date-fns/locale";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { KpiCard } from "@/components/domain/KpiCard";
import { AlertBanner } from "@/components/domain/AlertBanner";
import { RevenueTrendChart } from "@/components/domain/charts/RevenueTrendChart";
import { EncountersTrendChart } from "@/components/domain/charts/EncountersTrendChart";
import { HorizontalBarChart } from "@/components/domain/charts/HorizontalBarChart";
import { Hba1cDistributionChart } from "@/components/domain/charts/Hba1cDistributionChart";
import { ComplicationsRateChart } from "@/components/domain/charts/ComplicationsRateChart";
import { RecentActivityTimeline } from "@/components/domain/RecentActivityTimeline";
import {
  useDashboardOverview,
  useRevenueTrend,
  useEncountersTrend,
  useTopDoctors,
  useTopDrugs,
  useHba1cDistribution,
  useDashboardAlerts,
} from "@/lib/hooks/use-dashboard";
import { useDiabetesCohort } from "@/lib/hooks/use-reports";
import { cn } from "@/lib/utils";

function ChartCard({
  title,
  loading,
  error,
  children,
  className,
}: {
  title: string;
  loading?: boolean;
  error?: boolean;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <Card className={cn("flex flex-col", className)}>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-semibold">{title}</CardTitle>
      </CardHeader>
      <CardContent className="flex-1 pt-0">
        {error ? (
          <div className="flex h-48 items-center justify-center rounded-md border border-dashed">
            <p className="text-sm text-muted-foreground">Chưa có dữ liệu (BE chưa sẵn sàng)</p>
          </div>
        ) : loading ? (
          <Skeleton className="h-48 w-full" />
        ) : (
          children
        )}
      </CardContent>
    </Card>
  );
}

export function DashboardOverview() {
  const t = useTranslations("Dashboard");
  const qc = useQueryClient();
  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);
  const [dismissed, setDismissed] = useState<Set<string>>(new Set());

  // Chỉ set lastRefresh ở client để tránh hydration mismatch
  useEffect(() => {
    setLastRefresh(new Date());
  }, []);

  const { data: overview, isLoading: overviewLoading, isError: overviewError } = useDashboardOverview();
  const { data: revenueTrend, isLoading: revLoading, isError: revError } = useRevenueTrend("30d");
  const { data: encountersTrend, isLoading: encLoading, isError: encError } = useEncountersTrend("30d");
  const { data: topDoctors, isLoading: docLoading, isError: docError } = useTopDoctors("30d");
  const { data: topDrugs, isLoading: drugLoading, isError: drugError } = useTopDrugs("30d");
  const { data: hba1c, isLoading: hba1cLoading, isError: hba1cError } = useHba1cDistribution();
  const { data: alerts } = useDashboardAlerts();
  const { data: cohort } = useDiabetesCohort();

  const handleRefresh = useCallback(async () => {
    await qc.invalidateQueries({ queryKey: ["dashboard"] });
    setLastRefresh(new Date()); // client-only, không gây hydration issue
  }, [qc]);

  const visibleAlerts = (alerts ?? []).filter((a) => !dismissed.has(a.id));

  const complicationData = cohort
    ? [
        { name: "Võng mạc", value: cohort.complications.retinopathy },
        { name: "Thần kinh", value: cohort.complications.neuropathy },
        { name: "Thận", value: cohort.complications.nephropathy },
        { name: "Tim mạch", value: cohort.complications.cad },
        { name: "Mạch ngoại", value: cohort.complications.pad },
      ]
    : [];

  const formatRevenue = (v: number) =>
    new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 }).format(v);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">{t("title")}</h2>
          <p className="text-sm text-muted-foreground" suppressHydrationWarning>
            {t("subtitle")}
            {lastRefresh && (
              <> &middot; {format(lastRefresh, "HH:mm:ss", { locale: vi })}</>
            )}
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={handleRefresh}
          className="gap-2 shrink-0"
          aria-label="Làm mới dữ liệu"
        >
          <RefreshCw className="h-4 w-4" />
          Làm mới
        </Button>
      </div>

      {/* Alerts */}
      {visibleAlerts.length > 0 && (
        <AlertBanner
          alerts={visibleAlerts}
          onDismiss={(id) => setDismissed((prev) => new Set([...prev, id]))}
        />
      )}

      {/* KPI Cards */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          title={t("kpi.revenue")}
          value={
            overviewLoading ? "—"
            : overviewError || overview?.today === undefined ? "—"
            : formatRevenue(overview.today.revenue ?? 0)
          }
          icon={TrendingUp}
          delta={overview?.delta_vs_yesterday?.revenue_pct}
          loading={overviewLoading}
        />
        <KpiCard
          title={t("kpi.encounters")}
          value={
            overviewLoading ? "—"
            : overviewError || overview?.today === undefined ? "—"
            : (overview.today.encounter_count ?? "—")
          }
          icon={Stethoscope}
          delta={overview?.delta_vs_yesterday?.encounter_pct}
          loading={overviewLoading}
        />
        <KpiCard
          title={t("kpi.newPatients")}
          value={
            overviewLoading ? "—"
            : overviewError || overview?.today === undefined ? "—"
            : (overview.today.new_patient_count ?? "—")
          }
          icon={Users}
          loading={overviewLoading}
        />
        <KpiCard
          title={t("kpi.prescriptions")}
          value={
            overviewLoading ? "—"
            : overviewError || overview?.today === undefined ? "—"
            : (overview.today.prescription_count ?? "—")
          }
          icon={ClipboardList}
          loading={overviewLoading}
        />
      </div>

      {/* Charts 2x2 */}
      <div className="grid gap-4 lg:grid-cols-2">
        <ChartCard title={t("chart.revenueTrend")} loading={revLoading} error={revError}>
          <RevenueTrendChart data={revenueTrend?.points ?? []} />
        </ChartCard>

        <ChartCard title={t("chart.encountersTrend")} loading={encLoading} error={encError}>
          <EncountersTrendChart data={encountersTrend?.points ?? []} />
        </ChartCard>

        <ChartCard title={t("chart.topDoctors")} loading={docLoading} error={docError}>
          <HorizontalBarChart
            data={topDoctors?.points ?? []}
            valueLabel="Doanh thu"
            color="hsl(var(--primary))"
          />
        </ChartCard>

        <ChartCard title={t("chart.topDrugs")} loading={drugLoading} error={drugError}>
          <HorizontalBarChart
            data={topDrugs?.points ?? []}
            valueLabel="Doanh thu"
            color="#8b5cf6"
          />
        </ChartCard>
      </div>

      {/* Diabetes Section */}
      <div>
        <h3 className="text-base font-semibold mb-3">{t("diabetes.sectionTitle")}</h3>
        <div className="grid gap-4 lg:grid-cols-2">
          <ChartCard title={t("diabetes.hba1cDistribution")} loading={hba1cLoading} error={hba1cError}>
            <Hba1cDistributionChart data={hba1c?.points ?? []} />
          </ChartCard>

          <ChartCard title={t("diabetes.complicationsRate")}>
            {complicationData.length > 0 ? (
              <ComplicationsRateChart data={complicationData} />
            ) : (
              <Skeleton className="h-48 w-full" />
            )}
          </ChartCard>
        </div>

        {/* Cohort summary */}
        {cohort && (
          <div className="grid gap-3 sm:grid-cols-3 mt-4">
            <Card>
              <CardHeader className="pb-1">
                <CardTitle className="text-xs text-muted-foreground">{t("diabetes.totalPatients")}</CardTitle>
              </CardHeader>
              <CardContent>
                <span className="text-2xl font-bold">{cohort.total_patients}</span>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-1">
                <CardTitle className="text-xs text-muted-foreground">{t("diabetes.hba1cControlled")}</CardTitle>
              </CardHeader>
              <CardContent>
                <span className="text-2xl font-bold text-emerald-600">
                  {cohort.hba1c_distribution.lt_7}
                </span>
                <span className="text-sm text-muted-foreground ml-1">
                  ({cohort.total_patients > 0 ? ((cohort.hba1c_distribution.lt_7 / cohort.total_patients) * 100).toFixed(0) : 0}%)
                </span>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-1">
                <CardTitle className="text-xs text-muted-foreground">{t("diabetes.type2")}</CardTitle>
              </CardHeader>
              <CardContent>
                <span className="text-2xl font-bold">{cohort.by_type.t2}</span>
              </CardContent>
            </Card>
          </div>
        )}
      </div>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <CardTitle className="text-sm font-semibold">{t("recentActivity")}</CardTitle>
        </CardHeader>
        <CardContent>
          <RecentActivityTimeline />
        </CardContent>
      </Card>
    </div>
  );
}
