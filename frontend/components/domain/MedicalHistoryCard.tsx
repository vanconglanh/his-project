"use client";

import { HeartPulse } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

export interface MedicalHistoryCardProps {
  /** Tóm tắt tiền sử / bệnh nền (chuỗi tự do từ hồ sơ bệnh nhân) */
  summary?: string | null;
  bloodType?: string | null;
  onViewDetail: () => void;
}

export function MedicalHistoryCard({ summary, bloodType, onViewDetail }: MedicalHistoryCardProps) {
  return (
    <Card>
      <CardHeader className="pb-1 pt-3 px-4">
        <CardTitle className="text-sm font-semibold flex items-center gap-1.5">
          <HeartPulse className="h-4 w-4" aria-hidden="true" />
          Tiền sử
        </CardTitle>
      </CardHeader>
      <CardContent className="px-4 pb-3 space-y-2">
        {bloodType && (
          <p className="text-sm">
            <span className="text-muted-foreground">Nhóm máu: </span>
            <span className="font-medium">{bloodType}</span>
          </p>
        )}
        <p className="text-sm text-muted-foreground">
          {summary?.trim() ? summary : "Chưa ghi nhận tiền sử bệnh."}
        </p>
        <Button variant="ghost" size="sm" className="h-7 px-0 text-xs" onClick={onViewDetail}>
          Xem chi tiết
        </Button>
      </CardContent>
    </Card>
  );
}
