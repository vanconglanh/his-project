import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  getTodayClosing,
  openShift,
  closeShift,
  listClosingHistory,
  listDebts,
  type ClosingHistoryParams,
  type DebtListParams,
} from "@/lib/api/cashier";

export const CASHIER_KEYS = {
  today: (params?: object) => ["cashier", "today", params] as const,
  history: (params?: ClosingHistoryParams) => ["cashier", "history", params] as const,
  debts: (params?: DebtListParams) => ["cashier", "debts", params] as const,
};

export function useTodayClosing(params?: { cashier_user_id?: string; date?: string }) {
  return useQuery({
    queryKey: CASHIER_KEYS.today(params),
    queryFn: () => getTodayClosing(params),
    refetchInterval: 30_000,
    retry: 1,
  });
}

export function useOpenShift() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body?: { opening_balance?: number; note?: string }) => openShift(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cashier"] }),
  });
}

export function useCloseShift() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { shift_id?: string | null; actual_cash: number; note?: string; accept_difference?: boolean }) =>
      closeShift(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["cashier"] }),
  });
}

export function useClosingHistory(params?: ClosingHistoryParams) {
  return useQuery({
    queryKey: CASHIER_KEYS.history(params),
    queryFn: () => listClosingHistory(params),
  });
}

export function useDebts(params?: DebtListParams) {
  return useQuery({
    queryKey: CASHIER_KEYS.debts(params),
    queryFn: () => listDebts(params),
  });
}
