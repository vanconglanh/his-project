"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as roundsApi from "@/lib/api/cls-rounds";
import type { CreateClsRoundRequest } from "@/lib/api/cls-rounds";
import { clsKeys } from "./use-cls-orders";

export const clsRoundKeys = {
  all: ["cls-rounds"] as const,
  byEncounter: (encounterId: string) => ["cls-rounds", "encounter", encounterId] as const,
  detail: (roundId: string) => ["cls-rounds", "detail", roundId] as const,
};

function extractErrorMessage(err: unknown, fallback: string): string {
  const message = (err as { response?: { data?: { error?: { message?: string } } } })?.response?.data
    ?.error?.message;
  return message || fallback;
}

export function useClsRounds(encounterId: string) {
  return useQuery({
    queryKey: clsRoundKeys.byEncounter(encounterId),
    queryFn: () => roundsApi.listClsRounds(encounterId),
    enabled: !!encounterId,
    staleTime: 30_000,
    retry: 2,
  });
}

function useRoundMutationInvalidate(encounterId: string) {
  const qc = useQueryClient();
  return () => {
    qc.invalidateQueries({ queryKey: clsRoundKeys.byEncounter(encounterId) });
    qc.invalidateQueries({ queryKey: clsKeys.labOrders(encounterId) });
    qc.invalidateQueries({ queryKey: clsKeys.radOrders(encounterId) });
  };
}

export function useCreateClsRound(encounterId: string) {
  const invalidate = useRoundMutationInvalidate(encounterId);
  return useMutation({
    mutationFn: (body: CreateClsRoundRequest) => roundsApi.createClsRound(encounterId, body),
    onSuccess: () => {
      invalidate();
      toast.success("Đã tạo đợt chỉ định cận lâm sàng");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Tạo đợt chỉ định thất bại")),
  });
}

export function useSubmitClsRound(encounterId: string) {
  const invalidate = useRoundMutationInvalidate(encounterId);
  return useMutation({
    mutationFn: (roundId: string) => roundsApi.submitClsRound(roundId),
    onSuccess: () => {
      invalidate();
      toast.success("Đã chốt đợt chỉ định, chuyển thu ngân");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Chốt đợt chỉ định thất bại")),
  });
}

export function useCancelClsRound(encounterId: string) {
  const invalidate = useRoundMutationInvalidate(encounterId);
  return useMutation({
    mutationFn: ({ roundId, reason }: { roundId: string; reason?: string }) =>
      roundsApi.cancelClsRound(roundId, reason),
    onSuccess: () => {
      invalidate();
      toast.success("Đã huỷ đợt chỉ định");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Huỷ đợt chỉ định thất bại")),
  });
}
