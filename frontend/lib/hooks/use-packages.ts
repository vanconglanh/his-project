"use client";

import { useQuery } from "@tanstack/react-query";
import { getPatientPackageSummary } from "@/lib/api/packages";

export const packageKeys = {
  all: ["packages"] as const,
  patientSummary: (patientId: string) => [...packageKeys.all, "patient-summary", patientId] as const,
};

/** Tóm tắt gói dịch vụ của bệnh nhân (FR-1205/FR-1206) — dùng ở tiếp đón + chi tiết BN. */
export function usePatientPackageSummary(patientId: string | undefined) {
  return useQuery({
    queryKey: packageKeys.patientSummary(patientId ?? ""),
    queryFn: () => getPatientPackageSummary(patientId as string),
    enabled: !!patientId,
    staleTime: 30_000,
  });
}
