"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { LabResultTable } from "@/components/domain/LabResultTable";
import { HisStatusBadge } from "@/components/ui/status-badge";
import { useLabResults } from "@/lib/hooks/use-lab-results";
import { useRadResults } from "@/lib/hooks/use-rad-results";
import { formatVnDateTime } from "@/lib/utils/encounter-format";

interface Props {
  encounterId: string;
}

export function ClsResultTabPanel({ encounterId }: Props) {
  const { data: labData, isLoading: labLoading } = useLabResults({ encounter_id: encounterId });
  const { data: radResults, isLoading: radLoading } = useRadResults({ encounter_id: encounterId });

  const labResults = labData?.data ?? [];
  const isLoading = labLoading || radLoading;

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (labResults.length === 0 && (radResults ?? []).length === 0) {
    return (
      <EmptyState
        variant="labrad"
        title="Chưa có kết quả"
        description="Kết quả sẽ hiện tại đây khi khoa cận lâm sàng trả về."
      />
    );
  }

  return (
    <div className="space-y-6">
      {labResults.length > 0 && (
        <div className="space-y-2">
          <h3 className="text-lg font-semibold">Kết quả xét nghiệm</h3>
          <LabResultTable data={labResults} />
        </div>
      )}

      {(radResults ?? []).length > 0 && (
        <div className="space-y-2">
          <h3 className="text-lg font-semibold">Kết quả chẩn đoán hình ảnh</h3>
          {(radResults ?? []).map((r) => (
            <Card key={r.id}>
              <CardHeader className="pb-2 pt-4 px-4">
                <CardTitle className="text-sm font-semibold flex flex-wrap items-center gap-2">
                  {r.modality}
                  <span className="text-xs font-normal text-muted-foreground">
                    {formatVnDateTime(r.performed_at)}
                  </span>
                  {r.verified_at ? (
                    <HisStatusBadge variant="done">Đã duyệt</HisStatusBadge>
                  ) : (
                    <HisStatusBadge variant="waiting">Chờ duyệt</HisStatusBadge>
                  )}
                </CardTitle>
              </CardHeader>
              <CardContent className="px-4 pb-4 space-y-2 text-sm">
                <div>
                  <p className="text-xs text-muted-foreground">Mô tả</p>
                  <p>{r.findings || "—"}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Kết luận</p>
                  <p className="font-medium">{r.conclusion || "—"}</p>
                </div>
                {r.recommendations && (
                  <div>
                    <p className="text-xs text-muted-foreground">Đề nghị</p>
                    <p>{r.recommendations}</p>
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
