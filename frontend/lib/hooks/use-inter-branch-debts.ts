import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  listInterBranchDebts,
  settleInterBranchDebt,
  type ListInterBranchDebtsParams,
} from "@/lib/api/inter-branch-debts";

export function useInterBranchDebts(params: ListInterBranchDebtsParams) {
  return useQuery({
    queryKey: ["inter-branch-debts", params],
    queryFn: () => listInterBranchDebts(params),
    retry: false,
  });
}

export function useSettleInterBranchDebt() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, note }: { id: string; note?: string }) => settleInterBranchDebt(id, note),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["inter-branch-debts"] });
    },
  });
}
