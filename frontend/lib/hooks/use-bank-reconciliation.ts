import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getBankStatementLineCandidates,
  getBankStatementLines,
  ignoreBankStatementLine,
  importBankStatement,
  listBankStatements,
  manualMatchBankStatementLine,
  unmatchBankStatementLine,
  type ListBankStatementsParams,
} from "@/lib/api/bank-reconciliation";

export function useBankStatements(params: ListBankStatementsParams) {
  return useQuery({
    queryKey: ["bank-statements", params],
    queryFn: () => listBankStatements(params),
    retry: false,
  });
}

export function useImportBankStatement() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: importBankStatement,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["bank-statements"] });
    },
  });
}

export function useBankStatementLines(statementId: string | null) {
  return useQuery({
    queryKey: ["bank-statement-lines", statementId],
    queryFn: () => getBankStatementLines(statementId as string),
    enabled: !!statementId,
    retry: false,
  });
}

export function useBankStatementLineCandidates(lineId: string | null) {
  return useQuery({
    queryKey: ["bank-statement-line-candidates", lineId],
    queryFn: () => getBankStatementLineCandidates(lineId as string),
    enabled: !!lineId,
    retry: false,
  });
}

export function useManualMatchBankStatementLine(statementId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ lineId, paymentId }: { lineId: string; paymentId: string }) =>
      manualMatchBankStatementLine(lineId, paymentId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["bank-statement-lines", statementId] });
      queryClient.invalidateQueries({ queryKey: ["bank-statements"] });
    },
  });
}

export function useIgnoreBankStatementLine(statementId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (lineId: string) => ignoreBankStatementLine(lineId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["bank-statement-lines", statementId] });
      queryClient.invalidateQueries({ queryKey: ["bank-statements"] });
    },
  });
}

export function useUnmatchBankStatementLine(statementId: string | null) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (lineId: string) => unmatchBankStatementLine(lineId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["bank-statement-lines", statementId] });
      queryClient.invalidateQueries({ queryKey: ["bank-statements"] });
    },
  });
}
