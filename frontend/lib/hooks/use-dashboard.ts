import { useQuery } from "@tanstack/react-query";
import {
  getDashboardOverview,
  getRevenueTrend,
  getEncountersTrend,
  getTopDoctors,
  getTopDrugs,
  getHba1cDistribution,
  getDashboardAlerts,
} from "@/lib/api/dashboard";

const FIVE_MINUTES = 5 * 60 * 1000;

export function useDashboardOverview() {
  return useQuery({
    queryKey: ["dashboard", "overview"],
    queryFn: getDashboardOverview,
    refetchInterval: FIVE_MINUTES,
    retry: false,
  });
}

export function useRevenueTrend(range: "7d" | "30d" | "90d" = "30d") {
  return useQuery({
    queryKey: ["dashboard", "revenue-trend", range],
    queryFn: () => getRevenueTrend(range),
    refetchInterval: FIVE_MINUTES,
    retry: false,
  });
}

export function useEncountersTrend(range: "7d" | "30d" | "90d" = "30d") {
  return useQuery({
    queryKey: ["dashboard", "encounters-trend", range],
    queryFn: () => getEncountersTrend(range),
    refetchInterval: FIVE_MINUTES,
    retry: false,
  });
}

export function useTopDoctors(range: "7d" | "30d" | "90d" = "30d") {
  return useQuery({
    queryKey: ["dashboard", "top-doctors", range],
    queryFn: () => getTopDoctors(range, 10),
    refetchInterval: FIVE_MINUTES,
    retry: false,
  });
}

export function useTopDrugs(range: "7d" | "30d" | "90d" = "30d") {
  return useQuery({
    queryKey: ["dashboard", "top-drugs", range],
    queryFn: () => getTopDrugs(range, 10),
    refetchInterval: FIVE_MINUTES,
    retry: false,
  });
}

export function useHba1cDistribution() {
  return useQuery({
    queryKey: ["dashboard", "hba1c-distribution"],
    queryFn: getHba1cDistribution,
    refetchInterval: FIVE_MINUTES,
    retry: false,
  });
}

export function useDashboardAlerts() {
  return useQuery({
    queryKey: ["dashboard", "alerts"],
    queryFn: () => getDashboardAlerts(),
    refetchInterval: FIVE_MINUTES,
    retry: false,
  });
}
