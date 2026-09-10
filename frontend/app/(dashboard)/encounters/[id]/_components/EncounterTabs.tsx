"use client";

import {
  CalendarClock,
  ClipboardCheck,
  FileText,
  FlaskConical,
  HeartPulse,
  Paperclip,
  Pill,
  Stethoscope,
} from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { cn } from "@/lib/utils";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { isNursingRole } from "@/lib/utils/roles";
import { EmrTabPanel } from "./tabs/EmrTabPanel";
import { HistoryTabPanel } from "./tabs/HistoryTabPanel";
import { ClsOrderTabPanel } from "./tabs/ClsOrderTabPanel";
import { ClsResultTabPanel } from "./tabs/ClsResultTabPanel";
import { DiagnosisTabPanel } from "./tabs/DiagnosisTabPanel";
import { PrescriptionTabPanel } from "./tabs/PrescriptionTabPanel";
import { FollowUpTabPanel } from "./tabs/FollowUpTabPanel";
import { FileTabPanel } from "./tabs/FileTabPanel";
import type { DiagnosisType, EncounterDetailResponse, Icd10Response } from "@/lib/api/types";

export const ENCOUNTER_TAB_VALUES = [
  "emr",
  "history",
  "cls-orders",
  "cls-results",
  "diagnosis",
  "prescription",
  "followup",
  "files",
] as const;

export type EncounterTabValue = (typeof ENCOUNTER_TAB_VALUES)[number];

export function isEncounterTabValue(v: string | null): v is EncounterTabValue {
  return !!v && (ENCOUNTER_TAB_VALUES as readonly string[]).includes(v);
}

// BM-05 (Lark Bug-Feedback): role Dieu duong/KTV tren man "Kham benh" CHI duoc thay 3 tab
// nghiep vu cua ho (tao/xem chi dinh CLS + tep tin) - cac tab con lai (Benh an, Tien su,
// Chan doan, Don thuoc, Tai kham) thuoc pham vi Bac si nen an hoan toan.
export const NURSING_VISIBLE_TABS: readonly EncounterTabValue[] = ["cls-orders", "cls-results", "files"];

const TAB_META: Record<
  EncounterTabValue,
  { icon: React.ElementType; long: string; short: string }
> = {
  emr: { icon: FileText, long: "Bệnh án", short: "Bệnh án" },
  history: { icon: HeartPulse, long: "Tiền sử", short: "Tiền sử" },
  // BM-11 (Lark Bug-Feedback): doi ten tab "Can lam sang" -> "Chi dinh" de phan
  // anh dung pham vi (tab nay dung de tao/quan ly dot CHI DINH XN/CDHA, khong
  // phai xem ket qua lam sang) - tab "Ket qua CLS" ben canh moi la noi xem KQ.
  "cls-orders": { icon: FlaskConical, long: "Chỉ định", short: "Chỉ định" },
  "cls-results": { icon: ClipboardCheck, long: "Kết quả CLS", short: "Kết quả" },
  diagnosis: { icon: Stethoscope, long: "Chẩn đoán", short: "Chẩn đoán" },
  prescription: { icon: Pill, long: "Đơn thuốc", short: "Đơn thuốc" },
  followup: { icon: CalendarClock, long: "Tái khám", short: "Tái khám" },
  files: { icon: Paperclip, long: "Tập tin", short: "Tập tin" },
};

const TRIGGER_CLASS =
  "min-h-[44px] gap-2 rounded-none border-b-2 border-transparent px-4 " +
  "data-[state=active]:border-primary data-[state=active]:text-primary " +
  "data-[state=active]:bg-transparent data-[state=active]:shadow-none " +
  "focus-visible:ring-2 focus-visible:ring-[color:var(--focus-ring)]";

export interface EncounterTabsProps {
  encounter: EncounterDetailResponse;
  value: EncounterTabValue;
  onValueChange: (v: EncounterTabValue) => void;
  canEdit: boolean;
  counters?: Partial<Record<EncounterTabValue, number>>;
  onAddDiagnosis: (item: Icd10Response, type: DiagnosisType) => void;
  onDeleteDiagnosis: (id: string) => void;
}

export function EncounterTabs({
  encounter,
  value,
  onValueChange,
  canEdit,
  counters,
  onAddDiagnosis,
  onDeleteDiagnosis,
}: EncounterTabsProps) {
  const { roles } = usePermissions();
  const isNursing = isNursingRole(roles);
  const visibleTabValues = isNursing ? NURSING_VISIBLE_TABS : ENCOUNTER_TAB_VALUES;

  return (
    <Tabs value={value} onValueChange={(v) => onValueChange(v as EncounterTabValue)}>
      <TabsList
        className={cn(
          "sticky top-14 z-10 flex h-auto w-full justify-start gap-1 overflow-x-auto rounded-none",
          "border-b border-border bg-card/95 p-0 backdrop-blur"
        )}
        data-tour="enc-tabs"
      >
        {visibleTabValues.map((tab) => {
          const meta = TAB_META[tab];
          const Icon = meta.icon;
          const count = counters?.[tab];
          return (
            <TabsTrigger key={tab} value={tab} className={TRIGGER_CLASS}>
              <Icon className="h-4 w-4" aria-hidden="true" />
              <span className="hidden xl:inline">{meta.long}</span>
              <span className="xl:hidden">{meta.short}</span>
              {!!count && count > 0 && (
                <span className="ml-1 rounded-full bg-[color:var(--status-warning)]/10 px-1.5 text-xs font-medium tabular-nums text-[color:var(--status-warning)]">
                  {count}
                </span>
              )}
            </TabsTrigger>
          );
        })}
      </TabsList>

      <div className="min-h-[520px] pt-4">
        <TabsContent value="emr">
          <EmrTabPanel encounterId={encounter.id} canEdit={canEdit} />
        </TabsContent>

        <TabsContent value="history">
          <HistoryTabPanel
            encounterId={encounter.id}
            patientId={encounter.patient_id}
            canEdit={canEdit}
          />
        </TabsContent>

        <TabsContent value="cls-orders">
          <ClsOrderTabPanel encounterId={encounter.id} canEdit={canEdit} />
        </TabsContent>

        <TabsContent value="cls-results">
          <ClsResultTabPanel encounterId={encounter.id} />
        </TabsContent>

        <TabsContent value="diagnosis">
          <DiagnosisTabPanel
            diagnoses={encounter.diagnoses}
            canEdit={canEdit}
            onAddSingle={onAddDiagnosis}
            onDelete={onDeleteDiagnosis}
          />
        </TabsContent>

        <TabsContent value="prescription">
          <PrescriptionTabPanel encounterId={encounter.id} patientId={encounter.patient_id} />
        </TabsContent>

        <TabsContent value="followup">
          <FollowUpTabPanel
            patientId={encounter.patient_id}
            patientName={encounter.patient_summary?.full_name ?? ""}
            doctorId={encounter.doctor_id}
            canEdit={canEdit}
          />
        </TabsContent>

        <TabsContent value="files">
          <FileTabPanel patientId={encounter.patient_id} encounterId={encounter.id} />
        </TabsContent>
      </div>
    </Tabs>
  );
}
