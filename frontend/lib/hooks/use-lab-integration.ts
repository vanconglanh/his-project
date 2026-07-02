"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  sendLabOrder,
  listOutbound,
  retryOutbound,
  listInbound,
  reprocessInbound,
  getInboundRaw,
  getIntegrationStats,
  type OutboundListParams,
  type InboundListParams,
  type SendOutboundRequest,
} from "@/lib/api/lab-integration";
import { getErrorMessage } from "@/lib/utils/errors";

export const labIntegrationKeys = {
  outbound: (params?: OutboundListParams) => ["lab-integration", "outbound", params] as const,
  inbound: (params?: InboundListParams) => ["lab-integration", "inbound", params] as const,
  inboundRaw: (id: string) => ["lab-integration", "inbound-raw", id] as const,
  stats: (days?: number) => ["lab-integration", "stats", days] as const,
};

export function useOutbound(params?: OutboundListParams) {
  return useQuery({
    queryKey: labIntegrationKeys.outbound(params),
    queryFn: () => listOutbound(params),
    retry: 2,
    refetchInterval: 30_000,
  });
}

export function useInbound(params?: InboundListParams) {
  return useQuery({
    queryKey: labIntegrationKeys.inbound(params),
    queryFn: () => listInbound(params),
    retry: 2,
    refetchInterval: 30_000,
  });
}

export function useInboundRaw(id: string) {
  return useQuery({
    queryKey: labIntegrationKeys.inboundRaw(id),
    queryFn: () => getInboundRaw(id),
    enabled: !!id,
  });
}

export function useIntegrationStats(days = 7) {
  return useQuery({
    queryKey: labIntegrationKeys.stats(days),
    queryFn: () => getIntegrationStats(days),
    staleTime: 60_000,
  });
}

export function useSendLabOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ lab_order_id, body }: { lab_order_id: string; body: SendOutboundRequest }) =>
      sendLabOrder(lab_order_id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["lab-integration", "outbound"] });
      toast.success("Đã gửi chỉ định xét nghiệm tới đối tác");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useRetryOutbound() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => retryOutbound(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["lab-integration", "outbound"] });
      toast.success("Đã đưa vào hàng đợi gửi lại");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useReprocessInbound() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => reprocessInbound(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["lab-integration", "inbound"] });
      toast.success("Đã đưa vào hàng đợi xử lý lại");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}
