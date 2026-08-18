"use client";

import { cn } from "@/lib/utils";
import { PatientIdentityCard } from "@/components/domain/PatientIdentityCard";
import { VitalSignsSummaryCard } from "@/components/domain/VitalSignsSummaryCard";
import { AllergyAlertCard } from "@/components/domain/AllergyAlertCard";
import { MedicalHistoryCard } from "@/components/domain/MedicalHistoryCard";
import { PatientVisitHistoryCard } from "@/components/domain/PatientVisitHistoryCard";
import { usePatient, useAllergies } from "@/lib/hooks/use-patients";
import type { EncounterDetailResponse } from "@/lib/api/types";

export interface EncounterPatientSidebarProps {
  encounter: EncounterDetailResponse;
  variant?: "desktop" | "drawer";
  onOpenVitalDrawer: () => void;
  onOpenTimeline: () => void;
  onNavigateHistoryTab: () => void;
  canEdit: boolean;
}

export function EncounterPatientSidebar({
  encounter,
  variant = "desktop",
  onOpenVitalDrawer,
  onOpenTimeline,
  onNavigateHistoryTab,
  canEdit,
}: EncounterPatientSidebarProps) {
  const patientId = encounter.patient_id;
  const { data: patient } = usePatient(patientId);
  const { data: allergies, isLoading: allergiesLoading } = useAllergies(patientId);

  const latestVital = (encounter.vital_signs_latest ?? null) as Record<string, unknown> | null;
  const measuredAt =
    (latestVital?.created_at as string | undefined) ??
    (encounter.vital_signs?.[0]?.created_at as string | undefined) ??
    null;

  return (
    <div className={cn("space-y-4", variant === "drawer" && "pt-2")}>
      <PatientIdentityCard
        fullName={patient?.full_name ?? encounter.patient_summary?.full_name ?? "Bệnh nhân"}
        patientCode={patient?.code}
        gender={patient?.gender ?? encounter.patient_summary?.gender}
        dob={patient?.date_of_birth}
        yearOfBirth={encounter.patient_summary?.year_of_birth}
        avatarUrl={patient?.avatar_url}
        phone={patient?.phone ?? encounter.patient_summary?.phone}
        bhytCardNo={patient?.bhyt_card_no}
        bhytValidTo={patient?.bhyt_valid_to}
        doctorName={encounter.doctor_name}
        roomName={encounter.room_name}
        reasonForVisit={encounter.reason_for_visit}
      />

      <VitalSignsSummaryCard
        vital={latestVital}
        measuredAt={measuredAt}
        onViewAll={onOpenVitalDrawer}
        onAddNew={canEdit ? onOpenVitalDrawer : undefined}
      />

      <AllergyAlertCard
        allergies={allergies}
        isLoading={allergiesLoading}
        onViewDetail={onNavigateHistoryTab}
      />

      <MedicalHistoryCard
        summary={patient?.allergies_summary}
        bloodType={patient?.blood_type}
        onViewDetail={onNavigateHistoryTab}
      />

      <PatientVisitHistoryCard
        patientId={patientId}
        currentEncounterId={encounter.id}
        onViewTimeline={onOpenTimeline}
      />
    </div>
  );
}
