"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as roundsApi from "@/lib/api/cls-rounds";
import type { CreateClsRoundRequest } from "@/lib/api/cls-rounds";
import * as clsOrdersApi from "@/lib/api/cls-orders";
import type { LabOrderRequest, RadOrderRequest } from "@/lib/api/types";
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
    onSuccess: (round) => {
      invalidate();
      // BUG-05/UX#2: chỉ báo "chuyển thu ngân" khi thực sự có khoản phải thu (total_amount > 0
      // và chưa thanh toán). Đợt CLS không phát sinh phí (miễn phí/0đ) thì không chuyển thu ngân.
      const hasPayable = (round?.total_amount ?? 0) > 0 && round?.payment_status === "UNPAID";
      toast.success(
        hasPayable ? "Đã chốt đợt chỉ định, chuyển thu ngân" : "Đã chốt đợt chỉ định"
      );
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Chốt đợt chỉ định thất bại")),
  });
}

export function usePayClsRound(encounterId: string) {
  const invalidate = useRoundMutationInvalidate(encounterId);
  return useMutation({
    mutationFn: ({
      roundId,
      body,
    }: {
      roundId: string;
      body?: { billing_id?: string; method?: string; amount?: number; note?: string };
    }) => roundsApi.payClsRound(roundId, body),
    onSuccess: () => {
      invalidate();
      toast.success("Đã thu tiền đợt chỉ định");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Thu tiền đợt chỉ định thất bại")),
  });
}

export function useWaiveClsRound(encounterId: string) {
  const invalidate = useRoundMutationInvalidate(encounterId);
  return useMutation({
    mutationFn: ({ roundId, reason }: { roundId: string; reason: string }) =>
      roundsApi.waiveClsRound(roundId, reason),
    onSuccess: () => {
      invalidate();
      toast.success("Đã miễn phí đợt chỉ định");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Miễn phí đợt chỉ định thất bại")),
  });
}

// BM-14: "Dieu chinh" dot (them/bot dich vu) - dung lai chinh API tao/xoa chi dinh da co
// (ClsOrdersController), chi khac la co truyen round_id + phai invalidate CA round query
// (total_amount thay doi) chu khong chi lab/rad-orders query nhu useCreateLabOrder thuong.
export function useAddLabOrderToRound(encounterId: string) {
  const invalidate = useRoundMutationInvalidate(encounterId);
  return useMutation({
    mutationFn: ({ tests, roundId }: { tests: LabOrderRequest[]; roundId: string }) =>
      clsOrdersApi.createLabOrders(encounterId, tests, roundId),
    onSuccess: () => {
      invalidate();
      toast.success("Đã thêm dịch vụ vào đợt");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Thêm dịch vụ thất bại")),
  });
}

export function useAddRadOrderToRound(encounterId: string) {
  const invalidate = useRoundMutationInvalidate(encounterId);
  return useMutation({
    mutationFn: ({ orders, roundId }: { orders: RadOrderRequest[]; roundId: string }) =>
      clsOrdersApi.createRadOrders(encounterId, orders, roundId),
    onSuccess: () => {
      invalidate();
      toast.success("Đã thêm dịch vụ vào đợt");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Thêm dịch vụ thất bại")),
  });
}

export function useRemoveOrderFromRound(encounterId: string) {
  const invalidate = useRoundMutationInvalidate(encounterId);
  return useMutation({
    mutationFn: ({ id, kind }: { id: string; kind: "LAB" | "RAD" }) =>
      kind === "LAB" ? clsOrdersApi.deleteLabOrder(id) : clsOrdersApi.deleteRadOrder(id),
    onSuccess: () => {
      invalidate();
      toast.success("Đã bỏ dịch vụ khỏi đợt");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Bỏ dịch vụ thất bại")),
  });
}

// BM-18: gop thu cong danh sach chi dinh "chua gom dot" vao 1 dot dang mo.
export function useAssignLegacyOrdersToRound(encounterId: string) {
  const invalidate = useRoundMutationInvalidate(encounterId);
  return useMutation({
    mutationFn: ({
      roundId,
      labOrderIds,
      radOrderIds,
    }: {
      roundId: string;
      labOrderIds: string[];
      radOrderIds: string[];
    }) => roundsApi.assignLegacyOrdersToRound(roundId, labOrderIds, radOrderIds),
    onSuccess: () => {
      invalidate();
      toast.success("Đã gộp dịch vụ vào đợt chỉ định");
    },
    onError: (err) => toast.error(extractErrorMessage(err, "Gộp dịch vụ vào đợt thất bại")),
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
