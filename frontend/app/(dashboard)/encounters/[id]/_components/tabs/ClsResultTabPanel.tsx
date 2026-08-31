"use client";

import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { LabResultTable } from "@/components/domain/LabResultTable";
import { LabResultForm } from "@/components/domain/LabResultForm";
import { LabResultOcrPanel } from "@/components/domain/LabResultOcrPanel";
import { HisStatusBadge } from "@/components/ui/status-badge";
import { useLabResults, useCreateLabResult } from "@/lib/hooks/use-lab-results";
import { useRadResults } from "@/lib/hooks/use-rad-results";
import { formatVnDateTime } from "@/lib/utils/encounter-format";

interface Props {
  encounterId: string;
}

export function ClsResultTabPanel({ encounterId }: Props) {
  const [drawerOpen, setDrawerOpen] = useState(false);
  const { data: labData, isLoading: labLoading } = useLabResults({ encounter_id: encounterId });
  const { data: radResults, isLoading: radLoading } = useRadResults({ encounter_id: encounterId });
  const createMutation = useCreateLabResult();

  const labResults = labData?.data ?? [];
  const isLoading = labLoading || radLoading;

  const entryDrawer = (
    <Sheet open={drawerOpen} onOpenChange={setDrawerOpen}>
      <SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto px-6 pb-6">
        <SheetHeader>
          <SheetTitle>Nhập kết quả xét nghiệm</SheetTitle>
        </SheetHeader>
        <div className="mt-6">
          <Tabs defaultValue="manual">
            <TabsList>
              <TabsTrigger value="manual">Nhập tay</TabsTrigger>
              <TabsTrigger value="ocr">Đọc từ file</TabsTrigger>
            </TabsList>
            <TabsContent value="manual">
              <LabResultForm
                onSubmit={async (data) => {
                  await createMutation.mutateAsync(
                    data as Parameters<typeof createMutation.mutateAsync>[0]
                  );
                  setDrawerOpen(false);
                }}
                onCancel={() => setDrawerOpen(false)}
                isSubmitting={createMutation.isPending}
              />
            </TabsContent>
            <TabsContent value="ocr">
              <LabResultOcrPanel encounterId={encounterId} onSaved={() => setDrawerOpen(false)} />
            </TabsContent>
          </Tabs>
        </div>
      </SheetContent>
    </Sheet>
  );

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
      <>
        <div className="flex justify-end">
          <Button onClick={() => setDrawerOpen(true)}>+ Nhập kết quả XN</Button>
        </div>
        <EmptyState
          variant="labrad"
          title="Chưa có kết quả"
          description="Kết quả sẽ hiện tại đây khi khoa cận lâm sàng trả về."
        />
        {entryDrawer}
      </>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-end">
        <Button onClick={() => setDrawerOpen(true)}>+ Nhập kết quả XN</Button>
      </div>
      {entryDrawer}

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
                {r.source_file_url && (
                  <a
                    href={r.source_file_url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-xs text-primary flex items-center gap-1 hover:underline w-fit"
                  >
                    Xem file gốc
                  </a>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
