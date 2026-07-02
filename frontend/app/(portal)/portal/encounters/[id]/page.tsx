"use client";

import { use } from "react";
import Link from "next/link";
import { ArrowLeft, Stethoscope } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PortalLayout } from "@/components/domain/PortalLayout";
import { usePortalEncounter } from "@/lib/hooks/use-portal";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";

interface Params {
  id: string;
}

export default function PortalEncounterDetailPage({ params }: { params: Promise<Params> }) {
  const { id } = use(params);
  const { data: encounter, isLoading } = usePortalEncounter(id);

  return (
    <PortalLayout>
      <div className="space-y-5">
        <div className="flex items-center gap-3">
          <Link href="/portal/encounters">
            <Button variant="ghost" size="icon">
              <ArrowLeft className="h-4 w-4" />
            </Button>
          </Link>
          <h2 className="text-xl font-bold">Chi tiết lượt khám</h2>
        </div>

        {isLoading ? (
          <div className="space-y-4">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-32 animate-pulse rounded-lg bg-muted" />
            ))}
          </div>
        ) : !encounter ? (
          <div className="text-center py-16">
            <Stethoscope className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
            <p className="font-medium">Không tìm thấy lượt khám</p>
          </div>
        ) : (
          <>
            <Card>
              <CardHeader>
                <div className="flex items-start justify-between">
                  <CardTitle className="text-base">Thông tin lượt khám</CardTitle>
                  <Badge variant="outline">{encounter.encounter_code}</Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-2 text-sm">
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <p className="text-xs text-muted-foreground">Ngày khám</p>
                    <p className="font-medium">
                      {format(parseISO(encounter.visited_at), "dd/MM/yyyy HH:mm", { locale: vi })}
                    </p>
                  </div>
                  <div>
                    <p className="text-xs text-muted-foreground">Bác sĩ</p>
                    <p className="font-medium">{encounter.doctor_name}</p>
                  </div>
                </div>
                {encounter.chief_complaint && (
                  <div>
                    <p className="text-xs text-muted-foreground">Lý do khám</p>
                    <p>{encounter.chief_complaint}</p>
                  </div>
                )}
              </CardContent>
            </Card>

            {encounter.diagnosis && encounter.diagnosis.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Chẩn đoán</CardTitle>
                </CardHeader>
                <CardContent>
                  <ul className="space-y-2">
                    {encounter.diagnosis.map((d) => (
                      <li key={d.icd10} className="flex items-start gap-2">
                        <Badge variant="secondary" className="shrink-0 font-mono text-xs">
                          {d.icd10}
                        </Badge>
                        <span className="text-sm">{d.name}</span>
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            )}
          </>
        )}
      </div>
    </PortalLayout>
  );
}
