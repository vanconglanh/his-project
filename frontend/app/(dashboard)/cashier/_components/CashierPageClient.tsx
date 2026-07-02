"use client";

import { useState } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  TrendingUp,
  Receipt,
  RefreshCcw,
  AlertCircle,
  Play,
  StopCircle,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { formatCurrency } from "@/lib/utils/format";
import { useTodayClosing } from "@/lib/hooks/use-cashier";
import { useBillings } from "@/lib/hooks/use-billing";
import { usePayments } from "@/lib/hooks/use-payments";
import { useDebts } from "@/lib/hooks/use-cashier";
import { useClosingHistory } from "@/lib/hooks/use-cashier";
import { CashierShiftOpenDialog } from "@/components/domain/CashierShiftOpenDialog";
import { CashierShiftCloseDialog } from "@/components/domain/CashierShiftCloseDialog";
import { BillingsTab } from "./BillingsTab";
import { PaymentHistoryTab } from "./PaymentHistoryTab";
import { DebtsTab } from "./DebtsTab";
import { ShiftHistoryTab } from "./ShiftHistoryTab";

export function CashierPageClient() {
  const [openShiftDialog, setOpenShiftDialog] = useState(false);
  const [closeShiftDialog, setCloseShiftDialog] = useState(false);

  const { data: today, isLoading: shiftLoading } = useTodayClosing();
  const { data: billingsData } = useBillings({ from_date: new Date().toISOString().slice(0, 10) });
  const { data: paymentsData } = usePayments({ from_date: new Date().toISOString().slice(0, 10) });
  const { data: debtsData } = useDebts({ min_balance: 1 });

  const isOpen = today?.status === "OPEN";
  const totalToday = today?.summary?.net_collected ?? 0;
  const txCount = today?.summary?.count_transactions ?? 0;
  const refundTotal = today?.summary?.total_refund ?? 0;
  const totalDebt = debtsData?.summary?.total_debt ?? 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold tracking-tight">Thu ngân</h2>
          <p className="text-sm text-muted-foreground">Quản lý thu phí, hoá đơn, công nợ bệnh nhân</p>
        </div>
        <div className="flex items-center gap-2">
          {shiftLoading ? (
            <Skeleton className="h-9 w-32" />
          ) : isOpen ? (
            <Button variant="destructive" size="sm" onClick={() => setCloseShiftDialog(true)}>
              <StopCircle className="mr-2 h-4 w-4" />
              Đóng ca
            </Button>
          ) : (
            <Button size="sm" onClick={() => setOpenShiftDialog(true)}>
              <Play className="mr-2 h-4 w-4" />
              Mở ca
            </Button>
          )}
          <Badge
            variant="outline"
            className={cn(
              "px-3 py-1 text-sm font-semibold",
              isOpen
                ? "border-green-300 bg-green-50 text-green-700"
                : "border-gray-200 bg-gray-50 text-gray-600"
            )}
          >
            {isOpen ? "Ca đang mở" : "Ca đóng"}
          </Badge>
        </div>
      </div>

      {/* Stats cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <StatCard
          title="Tổng thu hôm nay"
          value={formatCurrency(totalToday)}
          icon={<TrendingUp className="h-5 w-5 text-green-600" />}
          loading={shiftLoading}
          accent="green"
        />
        <StatCard
          title="Số giao dịch"
          value={String(txCount)}
          icon={<Receipt className="h-5 w-5 text-blue-600" />}
          loading={shiftLoading}
          accent="blue"
        />
        <StatCard
          title="Refund / Void"
          value={formatCurrency(refundTotal)}
          icon={<RefreshCcw className="h-5 w-5 text-amber-600" />}
          loading={shiftLoading}
          accent="amber"
        />
        <StatCard
          title="Công nợ"
          value={formatCurrency(totalDebt)}
          icon={<AlertCircle className="h-5 w-5 text-red-600" />}
          loading={false}
          accent="red"
        />
      </div>

      {/* Tabs */}
      <Tabs defaultValue="pending">
        <TabsList className="w-full md:w-auto">
          <TabsTrigger value="pending">Hoá đơn chờ thu</TabsTrigger>
          <TabsTrigger value="history">Lịch sử hôm nay</TabsTrigger>
          <TabsTrigger value="debts">Công nợ</TabsTrigger>
          <TabsTrigger value="shifts">Ca làm việc</TabsTrigger>
        </TabsList>

        <TabsContent value="pending" className="mt-4">
          <BillingsTab filterStatus={["FINALIZED", "PARTIAL_PAID"]} />
        </TabsContent>

        <TabsContent value="history" className="mt-4">
          <PaymentHistoryTab />
        </TabsContent>

        <TabsContent value="debts" className="mt-4">
          <DebtsTab />
        </TabsContent>

        <TabsContent value="shifts" className="mt-4">
          <ShiftHistoryTab currentShift={today} />
        </TabsContent>
      </Tabs>

      <CashierShiftOpenDialog open={openShiftDialog} onOpenChange={setOpenShiftDialog} />
      <CashierShiftCloseDialog
        open={closeShiftDialog}
        onOpenChange={setCloseShiftDialog}
        shiftId={today?.id}
        expectedCash={today?.expected_cash ?? undefined}
      />
    </div>
  );
}

function StatCard({
  title,
  value,
  icon,
  loading,
  accent,
}: {
  title: string;
  value: string;
  icon: React.ReactNode;
  loading: boolean;
  accent: "green" | "blue" | "amber" | "red";
}) {
  const bg: Record<string, string> = {
    green: "bg-green-50 dark:bg-green-950/30",
    blue: "bg-blue-50 dark:bg-blue-950/30",
    amber: "bg-amber-50 dark:bg-amber-950/30",
    red: "bg-red-50 dark:bg-red-950/30",
  };

  return (
    <Card className="overflow-hidden">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <div className={cn("rounded-full p-2", bg[accent])}>{icon}</div>
      </CardHeader>
      <CardContent>
        {loading ? (
          <Skeleton className="h-8 w-32" />
        ) : (
          <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums">{value}</p>
        )}
      </CardContent>
    </Card>
  );
}
