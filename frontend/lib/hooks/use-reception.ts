"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  checkIn,
  getQueue,
  callTicket,
  skipTicket,
  cancelTicket,
  getRooms,
  getReceptionStats,
} from "@/lib/api/reception";
import type { QueueParams } from "@/lib/api/reception";
import type { CheckInRequest } from "@/lib/api/types";
import { getErrorMessage } from "@/lib/utils/errors";

export const receptionKeys = {
  all: ["reception"] as const,
  queue: (params?: QueueParams) => [...receptionKeys.all, "queue", params] as const,
  rooms: () => [...receptionKeys.all, "rooms"] as const,
  stats: () => [...receptionKeys.all, "stats"] as const,
};

export function useReceptionQueue(params?: QueueParams) {
  return useQuery({
    queryKey: receptionKeys.queue(params),
    queryFn: () => getQueue(params),
    refetchInterval: 5_000,
    staleTime: 0,
  });
}

export function useRooms() {
  return useQuery({
    queryKey: receptionKeys.rooms(),
    queryFn: getRooms,
    staleTime: 60_000,
  });
}

export function useReceptionStats() {
  return useQuery({
    queryKey: receptionKeys.stats(),
    queryFn: getReceptionStats,
    refetchInterval: 10_000,
    staleTime: 0,
  });
}

export function useCheckIn() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CheckInRequest) => checkIn(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: receptionKeys.all });
      toast.success("Tiếp đón thành công");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useCallTicket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ticketId: string) => callTicket(ticketId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: receptionKeys.all });
      toast.success("Đã gọi bệnh nhân");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useSkipTicket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (ticketId: string) => skipTicket(ticketId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: receptionKeys.all });
      toast.info("Đã bỏ qua lượt");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useCancelTicket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ ticketId, reason }: { ticketId: string; reason?: string }) =>
      cancelTicket(ticketId, reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: receptionKeys.all });
      toast.success("Đã huỷ tiếp đón");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}
