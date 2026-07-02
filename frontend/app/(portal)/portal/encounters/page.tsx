"use client";

import { useState } from "react";
import Link from "next/link";
import { FileText, ChevronRight } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { PortalLayout } from "@/components/domain/PortalLayout";
import { usePortalEncounters } from "@/lib/hooks/use-portal";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";

export default function PortalEncountersPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = usePortalEncounters({ page, page_size: 15 });

  const encounters = data?.data ?? [];
  const total = data?.meta?.total ?? 0;
  const totalPages = Math.ceil(total / 15);

  return (
    <PortalLayout>
      <div className="space-y-4">
        <div>
          <h2 className="text-xl font-bold">Lịch sử khám bệnh</h2>
          <p className="text-sm text-muted-foreground">Tổng cộng {total} lần khám</p>
        </div>

        {isLoading ? (
          <div className="space-y-3">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="h-20 animate-pulse rounded-lg bg-muted" />
            ))}
          </div>
        ) : encounters.length === 0 ? (
          <div className="flex flex-col items-center py-16 text-center">
            <FileText className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="font-medium">Chưa có lịch sử khám</p>
          </div>
        ) : (
          <div className="divide-y rounded-lg border">
            {encounters.map((enc) => (
              <Link key={enc.id} href={`/portal/encounters/${enc.id}`}>
                <div className="flex items-center gap-4 p-4 hover:bg-muted/30 transition-colors">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="font-medium text-sm">{enc.doctor_name}</p>
                      <Badge variant="outline" className="text-xs">
                        {enc.encounter_code}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      {format(parseISO(enc.visited_at), "EEEE, dd/MM/yyyy HH:mm", { locale: vi })}
                    </p>
                    {enc.diagnosis && enc.diagnosis.length > 0 && (
                      <p className="text-xs text-muted-foreground mt-1 truncate">
                        {enc.diagnosis.map((d) => d.name).join(", ")}
                      </p>
                    )}
                  </div>
                  <ChevronRight className="h-4 w-4 text-muted-foreground shrink-0" />
                </div>
              </Link>
            ))}
          </div>
        )}

        {totalPages > 1 && (
          <div className="flex items-center justify-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Trước
            </Button>
            <span className="text-sm text-muted-foreground">
              {page}/{totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Tiếp
            </Button>
          </div>
        )}
      </div>
    </PortalLayout>
  );
}
