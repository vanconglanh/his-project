import { useQuery } from "@tanstack/react-query";
import { getBranchRanking, getBranchDetail, type DateRangeParams } from "@/lib/api/chain-dashboard";

const FIVE_MINUTES = 5 * 60 * 1000;

export function useBranchRanking(range: DateRangeParams) {
  return useQuery({
    queryKey: ["chain-dashboard", "branch-ranking", range.from, range.to],
    queryFn: () => getBranchRanking(range),
    refetchInterval: FIVE_MINUTES,
    retry: false,
  });
}

export function useBranchDetail(branchId: number | null, range: DateRangeParams) {
  return useQuery({
    queryKey: ["chain-dashboard", "branch-detail", branchId, range.from, range.to],
    queryFn: () => getBranchDetail(branchId as number, range),
    enabled: branchId !== null,
    retry: false,
  });
}
