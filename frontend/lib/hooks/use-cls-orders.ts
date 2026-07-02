"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as clsApi from "@/lib/api/cls-orders";
import type { LabOrderRequest, RadOrderRequest, LabOrderStatus, RadOrderStatus } from "@/lib/api/types";

export const clsKeys = {
  labOrders: (encounterId: string) => ["cls", "lab", encounterId] as const,
  radOrders: (encounterId: string) => ["cls", "rad", encounterId] as const,
  catalog: (params?: object) => ["cls", "catalog", params] as const,
};

export function useLabOrders(encounterId: string) {
  return useQuery({
    queryKey: clsKeys.labOrders(encounterId),
    queryFn: () => clsApi.listLabOrders(encounterId),
    enabled: !!encounterId,
    staleTime: 30_000,
  });
}

export function useRadOrders(encounterId: string) {
  return useQuery({
    queryKey: clsKeys.radOrders(encounterId),
    queryFn: () => clsApi.listRadOrders(encounterId),
    enabled: !!encounterId,
    staleTime: 30_000,
  });
}

export function useClsCatalog(params: { q?: string; kind?: "LAB" | "RAD"; limit?: number }) {
  return useQuery({
    queryKey: clsKeys.catalog(params),
    queryFn: () => clsApi.searchClsCatalog(params),
    enabled: !!(params.q && params.q.length >= 1),
    staleTime: 60_000,
  });
}

export function useCreateLabOrder(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (tests: LabOrderRequest[]) => clsApi.createLabOrders(encounterId, tests),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clsKeys.labOrders(encounterId) });
      toast.success("Đã tạo chỉ định xét nghiệm");
    },
    onError: () => toast.error("Tạo chỉ định xét nghiệm thất bại"),
  });
}

export function useCreateRadOrder(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (orders: RadOrderRequest[]) => clsApi.createRadOrders(encounterId, orders),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clsKeys.radOrders(encounterId) });
      toast.success("Đã tạo chỉ định CĐHA");
    },
    onError: () => toast.error("Tạo chỉ định CĐHA thất bại"),
  });
}

export function useUpdateLabOrderStatus(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status, note }: { id: string; status: LabOrderStatus; note?: string }) =>
      clsApi.updateLabOrderStatus(id, status, note),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clsKeys.labOrders(encounterId) });
      toast.success("Cập nhật trạng thái thành công");
    },
    onError: () => toast.error("Cập nhật trạng thái thất bại"),
  });
}

export function useDeleteLabOrder(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => clsApi.deleteLabOrder(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clsKeys.labOrders(encounterId) });
      toast.success("Đã hủy chỉ định");
    },
    onError: (err: unknown) => {
      const code = (err as { response?: { data?: { error?: { code?: string } } } })?.response?.data?.error?.code;
      if (code === "LAB_ORDER_CANNOT_DELETE") toast.error("Không thể hủy sau khi đã lấy mẫu");
      else toast.error("Hủy chỉ định thất bại");
    },
  });
}

export function useDeleteRadOrder(encounterId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => clsApi.deleteRadOrder(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: clsKeys.radOrders(encounterId) });
      toast.success("Đã hủy chỉ định");
    },
    onError: () => toast.error("Hủy chỉ định thất bại"),
  });
}
