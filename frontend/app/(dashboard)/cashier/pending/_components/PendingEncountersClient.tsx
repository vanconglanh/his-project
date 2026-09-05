"use client";

import { useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/ui/page-header";
import { Receipt, FlaskConical, ScanLine, Pill } from "lucide-react";
import { format } from "date-fns";
import { formatCurrency } from "@/lib/utils/format";
import { usePendingEncounters, useCreateBilling } from "@/lib/hooks/use-billing";
import type { PendingEncounter } from "@/lib/api/billing";

export function PendingEncountersClient() {
  const router = useRouter();
  const { data, isLoading, isError, refetch } = usePendingEncounters();
  const createBilling = useCreateBilling();

  const rows: PendingEncounter[] = data?.data ?? [];

  function handleCreateBilling(row: PendingEncounter) {
    createBilling.mutate(
      { encounter_id: row.encounter_id, include_dispensing: true },
      {
        onSuccess: (billing) => {
          toast.success("Đã lập hoá đơn");
          router.push(`/billings/${billing.id}`);
        },
        onError: (err: unknown) => {
          const message =
            (err as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error
              ?.message ?? "Lập hoá đơn thất bại. Vui lòng thử lại.";
          toast.error(message);
        },
      }
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Hàng chờ thu ngân"
        description="Danh sách lượt khám đã có dịch vụ nhưng chưa lập hoá đơn"
      />

      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Mã BN</TableHead>
              <TableHead>Bệnh nhân</TableHead>
              <TableHead>Bác sĩ</TableHead>
              <TableHead>Dịch vụ</TableHead>
              <TableHead className="text-right">Tạm tính</TableHead>
              <TableHead>Thời gian</TableHead>
              <TableHead className="w-40" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 7 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-5 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : isError ? (
              <TableRow>
                <TableCell colSpan={7} className="h-32 text-center text-muted-foreground">
                  Không tải được dữ liệu.{" "}
                  <Button variant="link" className="h-auto p-0" onClick={() => refetch()}>
                    Thử lại
                  </Button>
                </TableCell>
              </TableRow>
            ) : rows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="h-32 text-center text-muted-foreground">
                  Không có lượt khám nào chờ lập hoá đơn
                </TableCell>
              </TableRow>
            ) : (
              rows.map((row) => (
                <TableRow key={row.encounter_id}>
                  <TableCell className="font-mono text-xs font-semibold">{row.patient_code}</TableCell>
                  <TableCell className="font-medium text-sm">{row.patient_name}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{row.doctor_name}</TableCell>
                  <TableCell>
                    <div className="flex flex-wrap gap-1">
                      {row.has_lab && (
                        <Badge variant="outline" className="gap-1 text-xs">
                          <FlaskConical className="h-3 w-3" /> XN
                        </Badge>
                      )}
                      {row.has_rad && (
                        <Badge variant="outline" className="gap-1 text-xs">
                          <ScanLine className="h-3 w-3" /> CĐHA
                        </Badge>
                      )}
                      {row.has_drug && (
                        <Badge variant="outline" className="gap-1 text-xs">
                          <Pill className="h-3 w-3" /> Thuốc
                        </Badge>
                      )}
                    </div>
                  </TableCell>
                  <TableCell className="text-right font-medium tabular-nums">
                    {formatCurrency(row.estimated_total)}
                  </TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {row.created_at ? format(new Date(row.created_at), "dd/MM/yyyy HH:mm") : "—"}
                  </TableCell>
                  <TableCell>
                    <Button
                      size="sm"
                      className="min-h-[44px] w-full"
                      disabled={createBilling.isPending}
                      onClick={() => handleCreateBilling(row)}
                    >
                      <Receipt className="mr-2 h-4 w-4" />
                      Lập hoá đơn
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
