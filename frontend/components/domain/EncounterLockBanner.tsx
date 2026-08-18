"use client";

import { Lock } from "lucide-react";
import { Button } from "@/components/ui/button";
import { formatVnDateTime } from "@/lib/utils/encounter-format";

export interface EncounterLockBannerProps {
  finishedAt?: string | null;
  closedByName?: string | null;
  canAmend: boolean;
  amendmentCount?: number;
  onAmend: () => void;
  onViewAddenda?: () => void;
}

export function EncounterLockBanner({
  finishedAt,
  closedByName,
  canAmend,
  amendmentCount = 0,
  onAmend,
  onViewAddenda,
}: EncounterLockBannerProps) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="flex flex-wrap items-center gap-3 rounded-lg border border-[color:var(--status-warning)]/30 bg-[color:var(--status-warning)]/10 p-3 text-[color:var(--status-warning)]"
    >
      <Lock className="h-4 w-4 shrink-0" aria-hidden="true" />
      <div className="min-w-0 flex-1">
        <p className="text-sm font-semibold">Bệnh án đã khoá — chỉ xem</p>
        <p className="text-sm">
          {finishedAt ? `Lượt khám kết thúc lúc ${formatVnDateTime(finishedAt)}` : "Lượt khám đã kết thúc"}
          {closedByName ? ` bởi ${closedByName}` : ""}. Mọi thay đổi phải tạo bản đính chính.
        </p>
        {amendmentCount > 0 && (
          <p className="text-xs">
            Đã có {amendmentCount} bản đính chính
            {onViewAddenda && (
              <>
                {" · "}
                <button type="button" className="underline" onClick={onViewAddenda}>
                  Xem
                </button>
              </>
            )}
          </p>
        )}
      </div>
      {canAmend && (
        <Button variant="outline" size="sm" className="min-h-[44px]" onClick={onAmend}>
          Tạo bản đính chính
        </Button>
      )}
    </div>
  );
}
