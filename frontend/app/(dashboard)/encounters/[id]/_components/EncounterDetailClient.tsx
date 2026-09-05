"use client";

import { useCallback, useMemo, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { AlertTriangle, ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { EncounterAlertBanner } from "@/components/domain/EncounterAlertBanner";
import { EncounterLockBanner } from "@/components/domain/EncounterLockBanner";
import { EncounterToolbar } from "@/components/domain/EncounterToolbar";
import { EncounterPatientSidebar } from "@/components/domain/EncounterPatientSidebar";
import { PatientStripBar } from "@/components/domain/PatientStripBar";
import { EncounterTimeline } from "@/components/domain/EncounterTimeline";
import { EncounterAmendDialog } from "@/components/domain/EncounterAmendDialog";
import { EmrSignDialog } from "@/components/domain/EmrSignDialog";
import { VitalSignsHistoryDrawer } from "@/components/domain/VitalSignsHistoryDrawer";
import { VitalSignsForm } from "@/components/domain/VitalSignsForm";
import { InBodyImportPanel } from "@/components/domain/InBodyImportPanel";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  EncounterTabs,
  isEncounterTabValue,
  type EncounterTabValue,
} from "./EncounterTabs";
import {
  useAddDiagnosis,
  useCloseEncounter,
  useCreateEncounterAddendum,
  useDeleteDiagnosis,
  useEncounter,
  useEncounterLockState,
  useStartEncounter,
} from "@/lib/hooks/use-encounters";
import { useSignEmr } from "@/lib/hooks/use-emr";
import { useCreateVitalSigns } from "@/lib/hooks/use-vital-signs";
import type { VitalSignsRequest } from "@/lib/api/types";
import { useClsRounds } from "@/lib/hooks/use-cls-rounds";
import { useAllergies } from "@/lib/hooks/use-patients";
import {
  useReassignTicket,
  useReceptionQueue,
  useResumeTicket,
  useWaitClsTicket,
} from "@/lib/hooks/use-reception";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { useBillingsByEncounter, useCreateBilling } from "@/lib/hooks/use-billing";
import { toast } from "sonner";
import type { DiagnosisType, Icd10Response } from "@/lib/api/types";

interface Props {
  encounterId: string;
}

export function EncounterDetailClient({ encounterId }: Props) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { has } = usePermissions();

  const { data: encounter, isLoading } = useEncounter(encounterId);
  const { data: lockState } = useEncounterLockState(encounterId);
  const { data: clsRounds } = useClsRounds(encounterId);
  const { data: allergies } = useAllergies(encounter?.patient_id ?? "");
  const { data: queue } = useReceptionQueue();

  // Item 5 — kiểm tra lượt khám đã có hoá đơn chưa để tránh lập trùng
  const { data: billings, isLoading: isBillingLoading } = useBillingsByEncounter(encounterId);
  const createBilling = useCreateBilling();
  const existingBilling = billings?.[0];

  const startEncounter = useStartEncounter(encounterId);
  const closeEncounter = useCloseEncounter(encounterId);
  const addDiagnosis = useAddDiagnosis(encounterId);
  const deleteDiagnosis = useDeleteDiagnosis(encounterId);
  const createAddendum = useCreateEncounterAddendum(encounterId);
  const signEmr = useSignEmr(encounterId);
  const createVital = useCreateVitalSigns(encounterId);
  const reassignTicket = useReassignTicket();
  const waitClsTicket = useWaitClsTicket();
  const resumeTicket = useResumeTicket();

  const [sheetOpen, setSheetOpen] = useState(false);
  const [timelineOpen, setTimelineOpen] = useState(false);
  const [vitalDrawerOpen, setVitalDrawerOpen] = useState(false);
  const [vitalFormOpen, setVitalFormOpen] = useState(false);

  const handleVitalSubmit = useCallback(
    async (data: VitalSignsRequest) => {
      await createVital.mutateAsync(data);
      setVitalFormOpen(false);
    },
    [createVital]
  );
  const [signDialogOpen, setSignDialogOpen] = useState(false);
  const [amendDialogOpen, setAmendDialogOpen] = useState(false);

  const tabParam = searchParams.get("tab");
  const activeTab: EncounterTabValue = isEncounterTabValue(tabParam) ? tabParam : "emr";

  // Deep-link tab: dùng replace để KHÔNG tạo history entry rác
  const handleTabChange = useCallback(
    (v: EncounterTabValue) => {
      router.replace(`${pathname}?tab=${v}`, { scroll: false });
    },
    [pathname, router]
  );

  /** Vé tiếp đón tương ứng (BE chưa gắn ticket_id vào encounter → dò theo bệnh nhân trong hàng đợi hôm nay) */
  const ticket = useMemo(() => {
    if (!encounter) return undefined;
    return (queue ?? []).find(
      (t) =>
        t.patient_id === encounter.patient_id &&
        !["DONE", "CANCELLED", "SKIPPED"].includes(t.status)
    );
  }, [queue, encounter]);

  const isDone = encounter?.status === "DONE";
  const isLocked = lockState?.is_locked ?? isDone;
  const canEdit = encounter?.status === "IN_PROGRESS" && !isLocked;

  const counters = useMemo<Partial<Record<EncounterTabValue, number>>>(
    () => ({
      history: allergies?.length ?? 0,
      "cls-orders": clsRounds?.meta.unpaid_rounds ?? 0,
      diagnosis: encounter?.diagnoses.length ?? 0,
    }),
    [allergies?.length, clsRounds?.meta.unpaid_rounds, encounter?.diagnoses.length]
  );

  const handleAddDiagnosis = useCallback(
    (item: Icd10Response, type: DiagnosisType) => {
      addDiagnosis.mutate({ icd10_code: item.code, type, note: undefined });
    },
    [addDiagnosis]
  );

  if (isLoading) {
    return (
      <div className="space-y-4 p-4">
        <Skeleton className="h-14 w-full" />
        <div className="grid grid-cols-12 gap-4">
          <div className="col-span-12 space-y-4 lg:col-span-4 xl:col-span-3">
            {[1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className="h-28 w-full" />
            ))}
          </div>
          <Skeleton className="col-span-12 h-[520px] lg:col-span-8 xl:col-span-9" />
        </div>
      </div>
    );
  }

  if (!encounter) {
    return (
      <div className="flex flex-col items-center gap-4 py-20">
        <AlertTriangle className="h-12 w-12 text-muted-foreground" aria-hidden="true" />
        <p className="text-muted-foreground">Không tải được thông tin lượt khám</p>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => router.refresh()}>
            Thử lại
          </Button>
          <Link href="/encounters">
            <Button variant="outline">Quay lại danh sách</Button>
          </Link>
        </div>
      </div>
    );
  }

  const patientName = encounter.patient_summary?.full_name ?? "Bệnh nhân";
  const isWaitingCls = ticket?.status === "WAITING_CLS";
  const vital = (encounter.vital_signs_latest ?? null) as Record<string, number> | null;
  const bloodPressure =
    vital?.bp_systolic != null && vital?.bp_diastolic != null
      ? `${vital.bp_systolic}/${vital.bp_diastolic}`
      : null;

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Link href="/encounters" className="flex items-center gap-1 hover:text-foreground">
          <ArrowLeft className="h-4 w-4" aria-hidden="true" />
          Khám bệnh
        </Link>
        <span>/</span>
        <span className="font-medium text-foreground">{patientName}</span>
      </div>

      <EncounterToolbar
        status={encounter.status}
        patientName={patientName}
        roomId={encounter.room_id}
        roomName={encounter.room_name}
        isWaitingCls={isWaitingCls}
        hasTicket={!!ticket}
        isEmrSigned={encounter.has_emr_signed}
        diagnosisCount={encounter.diagnoses.length}
        canEdit={!!canEdit}
        isPending={
          startEncounter.isPending ||
          closeEncounter.isPending ||
          reassignTicket.isPending ||
          waitClsTicket.isPending ||
          resumeTicket.isPending
        }
        onStart={() => startEncounter.mutate()}
        onSignEmr={() => setSignDialogOpen(true)}
        onWaitForCls={() => {
          if (ticket) waitClsTicket.mutate({ ticketId: ticket.id });
        }}
        onResume={() => {
          if (ticket) resumeTicket.mutate({ ticketId: ticket.id });
        }}
        onClose={() => closeEncounter.mutate()}
        onTransferRoom={(roomId) => {
          if (ticket)
            reassignTicket.mutate({
              ticketId: ticket.id,
              body: { room_id: roomId, reason: "Bác sĩ chuyển phòng từ màn khám bệnh" },
            });
        }}
        onPrintEncounter={() => window.open(`/encounters/${encounterId}/print`, "_blank")}
        onPrintCls={() => window.open(`/encounters/${encounterId}/cls-print`, "_blank")}
        canManageBilling={has("billing.write")}
        hasBilling={!!existingBilling}
        isBillingLoading={isBillingLoading}
        isCreatingBilling={createBilling.isPending}
        onCreateBilling={() => {
          createBilling.mutate(
            { encounter_id: encounterId },
            {
              onSuccess: (billing) => {
                toast.success("Đã lập hoá đơn cho lượt khám");
                router.push(`/billings/${billing.id}`);
              },
              onError: () => {
                toast.error("Lập hoá đơn thất bại, vui lòng thử lại");
              },
            }
          );
        }}
        onViewBilling={() => {
          if (existingBilling) router.push(`/billings/${existingBilling.id}`);
        }}
      />

      {isLocked && (
        <EncounterLockBanner
          finishedAt={lockState?.finished_at ?? encounter.finished_at}
          closedByName={lockState?.locked_by_name}
          amendmentCount={lockState?.amendment_count}
          canAmend={lockState?.can_amend ?? has("encounter.amend")}
          onAmend={() => setAmendDialogOpen(true)}
        />
      )}

      {encounter.alert_over_12h && encounter.started_at && (
        <EncounterAlertBanner
          hoursOpen={(Date.now() - new Date(encounter.started_at).getTime()) / 3_600_000}
          startedAt={encounter.started_at}
        />
      )}

      <PatientStripBar
        className="lg:hidden"
        fullName={patientName}
        subtitle={encounter.reason_for_visit}
        bloodPressure={bloodPressure}
        bloodPressureAbnormal={
          !!vital && (Number(vital.bp_systolic) > 140 || Number(vital.bp_diastolic) > 90)
        }
        onOpenProfile={() => setSheetOpen(true)}
      />

      <div className="grid grid-cols-12 gap-4 xl:gap-6">
        <aside className="hidden lg:col-span-4 lg:block xl:col-span-3">
          <div className="sticky top-[7rem] max-h-[calc(100vh-8rem)] space-y-4 overflow-y-auto pr-1">
            <EncounterPatientSidebar
              encounter={encounter}
              variant="desktop"
              canEdit={!!canEdit}
              onOpenVitalDrawer={() => setVitalDrawerOpen(true)}
              onOpenVitalForm={() => setVitalFormOpen(true)}
              onOpenTimeline={() => setTimelineOpen(true)}
              onNavigateHistoryTab={() => handleTabChange("history")}
            />
          </div>
        </aside>

        <section className="col-span-12 lg:col-span-8 xl:col-span-9">
          <EncounterTabs
            encounter={encounter}
            value={activeTab}
            onValueChange={handleTabChange}
            canEdit={!!canEdit}
            counters={counters}
            onAddDiagnosis={handleAddDiagnosis}
            onDeleteDiagnosis={(id) => deleteDiagnosis.mutate(id)}
          />
        </section>
      </div>

      {/* Hồ sơ bệnh nhân — drawer cho tablet dọc */}
      <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
        <SheetContent side="right" className="overflow-y-auto px-6 pb-6 sm:max-w-xl">
          <SheetHeader>
            <SheetTitle>Hồ sơ bệnh nhân</SheetTitle>
          </SheetHeader>
          <EncounterPatientSidebar
            encounter={encounter}
            variant="drawer"
            canEdit={!!canEdit}
            onOpenVitalDrawer={() => setVitalDrawerOpen(true)}
            onOpenVitalForm={() => {
              setSheetOpen(false);
              setVitalFormOpen(true);
            }}
            onOpenTimeline={() => setTimelineOpen(true)}
            onNavigateHistoryTab={() => {
              setSheetOpen(false);
              handleTabChange("history");
            }}
          />
        </SheetContent>
      </Sheet>

      {/* Timeline lượt khám — tra cứu, không chiếm 1 tab ngang */}
      <Sheet open={timelineOpen} onOpenChange={setTimelineOpen}>
        <SheetContent side="right" className="overflow-y-auto px-6 pb-6 sm:max-w-xl">
          <SheetHeader>
            <SheetTitle>Diễn biến lượt khám</SheetTitle>
          </SheetHeader>
          <div className="mt-4">
            <EncounterTimeline encounterId={encounterId} />
          </div>
        </SheetContent>
      </Sheet>

      <VitalSignsHistoryDrawer
        encounterId={encounterId}
        open={vitalDrawerOpen}
        onClose={() => setVitalDrawerOpen(false)}
      />

      {/* Ghi sinh hiệu — form nhập mới cho bệnh nhân đang khám */}
      <Sheet open={vitalFormOpen} onOpenChange={setVitalFormOpen}>
        <SheetContent side="right" className="overflow-y-auto px-6 pb-6 sm:max-w-xl">
          <SheetHeader>
            <SheetTitle>Ghi sinh hiệu</SheetTitle>
          </SheetHeader>
          <div className="mt-4">
            {vitalFormOpen && (
              <Tabs defaultValue="manual">
                <TabsList>
                  <TabsTrigger value="manual">Nhập tay</TabsTrigger>
                  <TabsTrigger value="inbody">Nhập từ máy InBody (PDF)</TabsTrigger>
                </TabsList>
                <TabsContent value="manual" className="mt-4">
                  <VitalSignsForm
                    onSubmit={handleVitalSubmit}
                    isLoading={createVital.isPending}
                  />
                </TabsContent>
                <TabsContent value="inbody" className="mt-4">
                  {encounter && (
                    <InBodyImportPanel
                      patientId={encounter.patient_id}
                      encounterId={encounterId}
                      onSaved={() => setVitalFormOpen(false)}
                    />
                  )}
                </TabsContent>
              </Tabs>
            )}
          </div>
        </SheetContent>
      </Sheet>

      <EmrSignDialog
        open={signDialogOpen}
        onClose={() => setSignDialogOpen(false)}
        isLoading={signEmr.isPending}
        onSign={(sigData, certId) => {
          signEmr.mutate(
            { signature_data: sigData, certificate_id: certId },
            { onSuccess: () => setSignDialogOpen(false) }
          );
        }}
      />

      <EncounterAmendDialog
        open={amendDialogOpen}
        onOpenChange={setAmendDialogOpen}
        isPending={createAddendum.isPending}
        onSubmit={(body) =>
          createAddendum.mutate(body, { onSuccess: () => setAmendDialogOpen(false) })
        }
      />
    </div>
  );
}
