"use client";

import { Activity } from "lucide-react";
import { useVitalSignsHistory } from "@/lib/hooks/use-vital-signs";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { format } from "date-fns";

interface Props {
  patientId: string;
  className?: string;
}

function v(n: number | null | undefined, digits?: number): string {
  if (n === null || n === undefined) return "—";
  return digits === undefined ? String(n) : Number(n).toFixed(digits);
}

/** Lịch sử các lần đo sinh hiệu của một bệnh nhân (mới nhất trước). */
export function VitalSignsHistory({ patientId, className }: Props) {
  const { data, isLoading, isError } = useVitalSignsHistory(patientId);

  const rows = [...(data ?? [])].sort((a, b) => {
    const ta = new Date(a.recorded_at ?? a.created_at).getTime();
    const tb = new Date(b.recorded_at ?? b.created_at).getTime();
    return tb - ta;
  });

  if (isLoading) {
    return (
      <div className={cn("space-y-2", className)}>
        <Skeleton className="h-8 w-full" />
        <Skeleton className="h-8 w-full" />
      </div>
    );
  }

  if (isError) {
    return (
      <p className={cn("text-sm text-destructive", className)}>
        Không tải được lịch sử sinh hiệu
      </p>
    );
  }

  if (rows.length === 0) {
    return (
      <div className={cn("py-6 text-center text-sm text-muted-foreground", className)}>
        <Activity className="mx-auto mb-2 h-8 w-8 opacity-30" />
        Chưa có dữ liệu đo
      </div>
    );
  }

  return (
    <div className={cn("overflow-x-auto rounded-lg border", className)}>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Ngày đo</TableHead>
            <TableHead>Nhiệt độ (°C)</TableHead>
            <TableHead>Mạch</TableHead>
            <TableHead>HA (mmHg)</TableHead>
            <TableHead>SpO2 (%)</TableHead>
            <TableHead>Cân nặng (kg)</TableHead>
            <TableHead>BMI</TableHead>
            <TableHead>Vòng eo (cm)</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((r) => (
            <TableRow key={r.id}>
              <TableCell className="whitespace-nowrap">
                {format(new Date(r.recorded_at ?? r.created_at), "dd/MM/yyyy HH:mm")}
              </TableCell>
              <TableCell>{v(r.temperature_c)}</TableCell>
              <TableCell>{v(r.heart_rate_bpm)}</TableCell>
              <TableCell>
                {r.bp_systolic != null && r.bp_diastolic != null
                  ? `${r.bp_systolic}/${r.bp_diastolic}`
                  : "—"}
              </TableCell>
              <TableCell>{v(r.spo2_percent)}</TableCell>
              <TableCell>{v(r.weight_kg)}</TableCell>
              <TableCell>{v(r.bmi, 1)}</TableCell>
              <TableCell>{v(r.waist_cm)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
