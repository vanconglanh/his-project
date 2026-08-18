"use client";

import Link from "next/link";
import { History } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { usePatientEncounters } from "@/lib/hooks/use-patients";
import { formatVnDate } from "@/lib/utils/encounter-format";

export interface PatientVisitHistoryCardProps {
  patientId: string;
  currentEncounterId: string;
  onViewTimeline: () => void;
}

export function PatientVisitHistoryCard({
  patientId,
  currentEncounterId,
  onViewTimeline,
}: PatientVisitHistoryCardProps) {
  const { data, isLoading } = usePatientEncounters(patientId);
  const visits = (data?.data ?? []).filter((v) => v.id !== currentEncounterId).slice(0, 5);

  return (
    <Card>
      <CardHeader className="pb-1 pt-3 px-4">
        <CardTitle className="text-sm font-semibold flex items-center gap-1.5">
          <History className="h-4 w-4" aria-hidden="true" />
          Lịch sử khám
        </CardTitle>
      </CardHeader>
      <CardContent className="px-4 pb-3 space-y-2">
        {isLoading ? (
          <div className="space-y-2">
            {[1, 2, 3].map((i) => (
              <Skeleton key={i} className="h-6 w-full" />
            ))}
          </div>
        ) : visits.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Lần khám đầu tiên — bệnh nhân chưa có lượt khám nào trước đây.
          </p>
        ) : (
          <ul className="space-y-1.5">
            {visits.map((v) => (
              <li key={v.id} className="text-sm">
                <Link
                  href={`/encounters/${v.id}`}
                  className="hover:text-primary flex items-baseline gap-2"
                >
                  <span className="text-xs text-muted-foreground tabular-nums shrink-0">
                    {formatVnDate(v.encounter_date)}
                  </span>
                  <span className="truncate">{v.chief_complaint || v.encounter_no}</span>
                </Link>
              </li>
            ))}
          </ul>
        )}
        <Button variant="ghost" size="sm" className="h-7 px-0 text-xs" onClick={onViewTimeline}>
          Xem tất cả
        </Button>
      </CardContent>
    </Card>
  );
}
