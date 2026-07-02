"use client";

import { useState } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Skeleton } from "@/components/ui/skeleton";
import { useEncounters } from "@/lib/hooks/use-encounters";
import { useCreateVitalSigns, useVitalSigns } from "@/lib/hooks/use-vital-signs";
import { VitalSignsForm } from "@/components/domain/VitalSignsForm";
import { EncounterStatusBadge } from "@/components/domain/EncounterStatusBadge";
import { SimpleAvatar } from "@/components/domain/SimpleAvatar";
import { Activity, Plus } from "lucide-react";
import type { EncounterResponse, VitalSignsRequest } from "@/lib/api/types";
import { format } from "date-fns";
import { vi } from "date-fns/locale";

export function NursePageClient() {
  const today = format(new Date(), "yyyy-MM-dd");
  const [selectedEncounter, setSelectedEncounter] = useState<EncounterResponse | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);

  const { data: todayEncounters, isLoading } = useEncounters({
    status: "WAITING",
    date_from: today,
    date_to: today,
    page_size: 50,
  });

  const createVital = useCreateVitalSigns(selectedEncounter?.id ?? "");

  function openVitalForm(encounter: EncounterResponse) {
    setSelectedEncounter(encounter);
    setDrawerOpen(true);
  }

  async function handleVitalSubmit(data: VitalSignsRequest) {
    if (!selectedEncounter) return;
    await createVital.mutateAsync(data);
    setDrawerOpen(false);
    setSelectedEncounter(null);
  }

  async function handleVitalSubmitAndNext(data: VitalSignsRequest) {
    if (!selectedEncounter) return;
    await createVital.mutateAsync(data);
    // Keep drawer open, clear form by remounting
    // The form will reset internally via useEffect
  }

  const encounters = todayEncounters?.data ?? [];

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Điều dưỡng</h2>
        <p className="text-sm text-muted-foreground">
          Quản lý sinh hiệu bệnh nhân
        </p>
      </div>

      <Tabs defaultValue="queue">
        <TabsList>
          <TabsTrigger value="queue">Danh sách chờ</TabsTrigger>
          <TabsTrigger value="today">Sinh hiệu hôm nay</TabsTrigger>
        </TabsList>

        <TabsContent value="queue" className="mt-4">
          <div className="rounded-xl border overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/40">
                  <th className="text-left px-4 py-3 font-medium">Bệnh nhân</th>
                  <th className="text-left px-4 py-3 font-medium">Trạng thái</th>
                  <th className="text-left px-4 py-3 font-medium">Bác sĩ</th>
                  <th className="text-left px-4 py-3 font-medium">Lý do khám</th>
                  <th className="text-right px-4 py-3 font-medium">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {isLoading ? (
                  Array.from({ length: 5 }).map((_, i) => (
                    <tr key={i}>
                      {Array.from({ length: 5 }).map((__, j) => (
                        <td key={j} className="px-4 py-3">
                          <Skeleton className="h-5 w-full" />
                        </td>
                      ))}
                    </tr>
                  ))
                ) : encounters.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-12 text-center text-sm text-muted-foreground">
                      <Activity className="mx-auto h-10 w-10 opacity-30 mb-2" />
                      Không có bệnh nhân chờ
                    </td>
                  </tr>
                ) : (
                  encounters.map((enc) => (
                    <tr key={enc.id} className="hover:bg-accent/30">
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <SimpleAvatar name={enc.patient_summary?.full_name ?? "?"} size="sm" />
                          <div>
                            <p className="font-medium">{enc.patient_summary?.full_name}</p>
                            <p className="text-xs text-muted-foreground">
                              {enc.patient_summary?.year_of_birth}
                            </p>
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <EncounterStatusBadge status={enc.status} />
                      </td>
                      <td className="px-4 py-3 text-muted-foreground">
                        {enc.doctor_name ?? "Chưa phân công"}
                      </td>
                      <td className="px-4 py-3 max-w-xs truncate text-muted-foreground">
                        {enc.reason_for_visit}
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => openVitalForm(enc)}
                          className="gap-1 min-h-[36px]"
                        >
                          <Plus className="h-3.5 w-3.5" />
                          Nhập sinh hiệu
                        </Button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </TabsContent>

        <TabsContent value="today" className="mt-4">
          <TodayVitalsList />
        </TabsContent>
      </Tabs>

      {/* Vital signs drawer */}
      <Sheet open={drawerOpen} onOpenChange={(v) => { if (!v) { setDrawerOpen(false); setSelectedEncounter(null); } }}>
        <SheetContent side="right" className="w-full sm:max-w-xl overflow-y-auto">
          <SheetHeader>
            <SheetTitle className="flex items-center gap-2">
              <Activity className="h-5 w-5 text-primary" />
              Nhập sinh hiệu
              {selectedEncounter && (
                <span className="text-base font-normal text-muted-foreground">
                  — {selectedEncounter.patient_summary?.full_name}
                </span>
              )}
            </SheetTitle>
          </SheetHeader>
          <div className="mt-4">
            {selectedEncounter && (
              <VitalSignsForm
                key={selectedEncounter.id}
                isLoading={createVital.isPending}
                onSubmit={handleVitalSubmit}
                onSubmitAndNext={handleVitalSubmitAndNext}
              />
            )}
          </div>
        </SheetContent>
      </Sheet>
    </div>
  );
}

function TodayVitalsList() {
  const today = format(new Date(), "yyyy-MM-dd");
  const { data: encounters, isLoading } = useEncounters({
    date_from: today,
    date_to: today,
    page_size: 50,
  });

  if (isLoading) {
    return <div className="space-y-2">{[1, 2, 3].map((i) => <Skeleton key={i} className="h-16 w-full" />)}</div>;
  }

  const encList = encounters?.data ?? [];

  return (
    <div className="space-y-3">
      {encList.length === 0 ? (
        <p className="text-center text-sm text-muted-foreground py-8">Chưa có dữ liệu hôm nay</p>
      ) : (
        encList.map((enc) => (
          <EncounterVitalRow key={enc.id} encounter={enc} />
        ))
      )}
    </div>
  );
}

function EncounterVitalRow({ encounter }: { encounter: EncounterResponse }) {
  const { data: vitals } = useVitalSigns(encounter.id);
  const vitalCount = vitals?.length ?? 0;

  return (
    <div className="rounded-lg border bg-card p-3 flex items-center justify-between">
      <div className="flex items-center gap-3">
        <SimpleAvatar name={encounter.patient_summary?.full_name ?? "?"} size="sm" />
        <div>
          <p className="font-medium text-sm">{encounter.patient_summary?.full_name}</p>
          <p className="text-xs text-muted-foreground">
            BS: {encounter.doctor_name ?? "Chưa phân"} · {encounter.room_name ?? "Chưa phân phòng"}
          </p>
        </div>
      </div>
      <div className="flex items-center gap-2">
        <Badge variant="outline" className="text-xs">
          <Activity className="h-3 w-3 mr-1" />
          {vitalCount} sinh hiệu
        </Badge>
        <EncounterStatusBadge status={encounter.status} />
      </div>
    </div>
  );
}
