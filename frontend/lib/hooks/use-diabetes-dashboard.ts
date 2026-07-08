"use client";

import { useQuery } from "@tanstack/react-query";
import * as diabetesDashboardApi from "@/lib/api/diabetes-dashboard";
import type { RiskListParams } from "@/lib/api/types";

export const diabetesDashboardKeys = {
  trajectory: (patientId: string, params?: { from?: string; to?: string }) =>
    ["diabetes-dashboard", "trajectory", patientId, params] as const,
  deteriorationFlags: (patientId: string) => ["diabetes-dashboard", "deterioration-flags", patientId] as const,
  riskList: (params?: RiskListParams) => ["diabetes-dashboard", "risk-list", params] as const,
  carePathwayTargets: (code?: string) => ["diabetes-dashboard", "care-pathway-targets", code] as const,
};

export function useDiabetesTrajectory(patientId: string, params?: { from?: string; to?: string }) {
  return useQuery({
    queryKey: diabetesDashboardKeys.trajectory(patientId, params),
    queryFn: () => diabetesDashboardApi.getDiabetesTrajectory(patientId, params),
    enabled: !!patientId,
    staleTime: 60_000,
    retry: 1,
  });
}

export function useDeteriorationFlags(patientId: string) {
  return useQuery({
    queryKey: diabetesDashboardKeys.deteriorationFlags(patientId),
    queryFn: () => diabetesDashboardApi.getDeteriorationFlags(patientId),
    enabled: !!patientId,
    staleTime: 60_000,
    retry: 1,
  });
}

export function useRiskList(params?: RiskListParams) {
  return useQuery({
    queryKey: diabetesDashboardKeys.riskList(params),
    queryFn: () => diabetesDashboardApi.getRiskList(params),
    staleTime: 30_000,
    retry: 1,
  });
}

export function useCarePathwayTargets(code = "DM_T2_5481") {
  return useQuery({
    queryKey: diabetesDashboardKeys.carePathwayTargets(code),
    queryFn: () => diabetesDashboardApi.getCarePathwayTargets(code),
    staleTime: 5 * 60_000,
    retry: 1,
  });
}
