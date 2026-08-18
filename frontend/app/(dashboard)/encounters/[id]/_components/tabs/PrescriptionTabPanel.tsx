"use client";

import { PrescriptionForm } from "@/components/domain/PrescriptionForm";
import { usePrescriptions } from "@/lib/hooks/use-prescriptions";

interface Props {
  encounterId: string;
  patientId: string;
}

export function PrescriptionTabPanel({ encounterId, patientId }: Props) {
  const { data } = usePrescriptions({ encounter_id: encounterId, page_size: 1 });
  const existingId = data?.data?.[0]?.id;
  return (
    <PrescriptionForm
      encounterId={encounterId}
      patientId={patientId}
      existingPrescriptionId={existingId}
    />
  );
}
