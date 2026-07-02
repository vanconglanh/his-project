"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listRoles,
  getRole,
  createRole,
  updateRole,
  deleteRole,
  listPermissions,
  listAuditLogs,
  type ListAuditLogsParams,
} from "@/lib/api/roles";
import type { CreateRoleRequest, UpdateRoleRequest } from "@/lib/api/types";
import { getErrorMessage } from "@/lib/utils/errors";

export const ROLES_KEY = "roles";
export const PERMISSIONS_KEY = "permissions";
export const AUDIT_LOGS_KEY = "audit-logs";

export function useRoles() {
  return useQuery({
    queryKey: [ROLES_KEY],
    queryFn: listRoles,
  });
}

export function useRole(code: string) {
  return useQuery({
    queryKey: [ROLES_KEY, code],
    queryFn: () => getRole(code),
    enabled: !!code,
  });
}

export function usePermissions(resource?: string) {
  return useQuery({
    queryKey: [PERMISSIONS_KEY, resource],
    queryFn: () => listPermissions(resource),
  });
}

export function useAuditLogs(params?: ListAuditLogsParams) {
  return useQuery({
    queryKey: [AUDIT_LOGS_KEY, params],
    queryFn: () => listAuditLogs(params),
  });
}

export function useCreateRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateRoleRequest) => createRole(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [ROLES_KEY] });
      toast.success("Tạo vai trò thành công");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ code, payload }: { code: string; payload: UpdateRoleRequest }) =>
      updateRole(code, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [ROLES_KEY] });
      toast.success("Cập nhật vai trò thành công");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (code: string) => deleteRole(code),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [ROLES_KEY] });
      toast.success("Đã xoá vai trò");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
