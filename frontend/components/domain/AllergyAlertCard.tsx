"use client";

import { AlertTriangle } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import type { AllergyResponse } from "@/lib/api/types";

export interface AllergyAlertCardProps {
  allergies?: AllergyResponse[];
  isLoading?: boolean;
  onViewDetail: () => void;
}

const SEVERITY_LABEL: Record<string, string> = {
  MILD: "Nhẹ",
  MODERATE: "Trung bình",
  SEVERE: "Nặng",
  LIFE_THREATENING: "Nguy hiểm tính mạng",
};

export function AllergyAlertCard({ allergies, isLoading, onViewDetail }: AllergyAlertCardProps) {
  if (isLoading || !allergies || allergies.length === 0) return null;

  return (
    <Card className="border-[color:var(--status-critical)]/30 bg-[color:var(--status-critical)]/5">
      <CardHeader className="pb-1 pt-3 px-4">
        <CardTitle className="text-sm font-semibold flex items-center gap-1.5 text-[color:var(--status-critical)]">
          <AlertTriangle className="h-4 w-4" aria-hidden="true" />
          Dị ứng thuốc ({allergies.length})
        </CardTitle>
      </CardHeader>
      <CardContent className="px-4 pb-3 space-y-1">
        <ul className="space-y-1">
          {allergies.slice(0, 4).map((a) => (
            <li key={a.id} className="text-sm text-[color:var(--status-critical)]">
              {a.allergen}
              <span className="text-xs text-muted-foreground">
                {a.severity ? ` — ${SEVERITY_LABEL[a.severity] ?? a.severity}` : ""}
              </span>
            </li>
          ))}
        </ul>
        <Button variant="ghost" size="sm" className="h-7 px-0 text-xs" onClick={onViewDetail}>
          Xem chi tiết
        </Button>
      </CardContent>
    </Card>
  );
}
