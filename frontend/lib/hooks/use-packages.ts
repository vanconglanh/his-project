"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  extendPackageSubscription,
  getPatientPackageSummary,
} from "@/lib/api/packages";

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

/** H-14 (FR-1211): Gia hạn gói đã hết hạn còn định mức. Invalidate lại summary của bệnh nhân sau khi gia hạn. */
export function useExtendPackageSubscription(patientId: string | undefined) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ subscriptionId, note }: { subscriptionId: string; note?: string }) =>
      extendPackageSubscription(subscriptionId, note),
    onSuccess: () => {
      if (patientId) {
        queryClient.invalidateQueries({
          queryKey: packageKeys.patientSummary(patientId),
        });
      }
    },
  });
}
