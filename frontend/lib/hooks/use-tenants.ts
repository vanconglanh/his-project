"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listTenants,
  createTenant,
  getTenant,
  updateTenant,
  suspendTenant,
  activateTenant,
  deleteTenant,
  getMyTenant,
  updateMyTenant,
  type ListTenantsParams,
} from "@/lib/api/tenants";
import type { CreateTenantRequest, UpdateTenantRequest, UpdateTenantProfileRequest } from "@/lib/api/types";
import { getErrorMessage } from "@/lib/utils/errors";

export const TENANTS_KEY = "tenants";

export function useTenants(params?: ListTenantsParams) {
  return useQuery({
    queryKey: [TENANTS_KEY, params],
    queryFn: () => listTenants(params),
  });
}

export function useTenant(id: string) {
  return useQuery({
    queryKey: [TENANTS_KEY, id],
    queryFn: () => getTenant(id),
    enabled: !!id,
  });
}

export function useMyTenant() {
  return useQuery({
    queryKey: [TENANTS_KEY, "me"],
    queryFn: getMyTenant,
  });
}

export function useCreateTenant() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateTenantRequest) => createTenant(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [TENANTS_KEY] });
      toast.success("Tạo phòng khám thành công");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateTenant() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateTenantRequest }) =>
      updateTenant(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [TENANTS_KEY] });
      toast.success("Cập nhật phòng khám thành công");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateMyTenant() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateTenantProfileRequest) => updateMyTenant(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [TENANTS_KEY, "me"] });
      toast.success("Cập nhật thông tin phòng khám thành công");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useSuspendTenant() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      suspendTenant(id, reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [TENANTS_KEY] });
      toast.success("Đã tạm ngưng phòng khám");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useActivateTenant() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => activateTenant(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [TENANTS_KEY] });
      toast.success("Đã kích hoạt phòng khám");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteTenant() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteTenant(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [TENANTS_KEY] });
      toast.success("Đã chấm dứt phòng khám");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
