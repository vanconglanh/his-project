"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listUsers,
  inviteUser,
  acceptInvite,
  getUser,
  updateUser,
  deleteUser,
  assignRoles,
  revokeRole,
  disableUser,
  enableUser,
  getMe,
  updateMe,
  changePassword,
  setup2FA,
  enable2FA,
  disable2FA,
  forgotPassword,
  resetPassword,
  type ListUsersParams,
} from "@/lib/api/users";
import type {
  InviteUserRequest,
  UpdateUserRequest,
  UpdateMeRequest,
  ChangePasswordRequest,
} from "@/lib/api/types";
import { getErrorMessage } from "@/lib/utils/errors";

export const USERS_KEY = "users";
export const ME_KEY = "me";

export function useUsers(params?: ListUsersParams) {
  return useQuery({
    queryKey: [USERS_KEY, params],
    queryFn: () => listUsers(params),
  });
}

export function useUser(id: string) {
  return useQuery({
    queryKey: [USERS_KEY, id],
    queryFn: () => getUser(id),
    enabled: !!id,
  });
}

export function useMe() {
  return useQuery({
    queryKey: [ME_KEY],
    queryFn: getMe,
    staleTime: 5 * 60 * 1000,
  });
}

export function useHasPermission(permission: string): boolean {
  const { data } = useMe();
  return data?.permissions?.includes(permission) ?? false;
}

export function useHasRole(role: string): boolean {
  const { data } = useMe();
  return data?.roles?.some((r) => r.code === role) ?? false;
}

export function useInviteUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: InviteUserRequest) => inviteUser(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [USERS_KEY] });
      toast.success("Đã gửi email mời người dùng");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useAcceptInvite() {
  return useMutation({
    mutationFn: (payload: { token: string; password: string; full_name?: string }) =>
      acceptInvite(payload),
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: UpdateUserRequest }) =>
      updateUser(id, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [USERS_KEY] });
      toast.success("Cập nhật thông tin người dùng thành công");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useUpdateMe() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateMeRequest) => updateMe(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [ME_KEY] });
      toast.success("Cập nhật hồ sơ thành công");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDeleteUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteUser(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [USERS_KEY] });
      toast.success("Đã xoá người dùng");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useAssignRoles() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, role_codes }: { id: string; role_codes: string[] }) =>
      assignRoles(id, role_codes),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [USERS_KEY] });
      toast.success("Gán vai trò thành công");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useRevokeRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, roleCode }: { id: string; roleCode: string }) =>
      revokeRole(id, roleCode),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [USERS_KEY] });
      toast.success("Đã thu hồi vai trò");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDisableUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => disableUser(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [USERS_KEY] });
      toast.success("Đã khoá người dùng");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useEnableUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => enableUser(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [USERS_KEY] });
      toast.success("Đã mở khoá người dùng");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (payload: ChangePasswordRequest) => changePassword(payload),
    onSuccess: () => toast.success("Đổi mật khẩu thành công"),
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useSetup2FA() {
  return useMutation({
    mutationFn: () => setup2FA(),
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useEnable2FA() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (code: string) => enable2FA(code),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [ME_KEY] });
      toast.success("Đã bật xác thực 2 lớp");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useDisable2FA() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ password, code }: { password: string; code?: string }) =>
      disable2FA(password, code),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [ME_KEY] });
      toast.success("Đã tắt xác thực 2 lớp");
    },
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}

export function useForgotPassword() {
  return useMutation({
    mutationFn: (email: string) => forgotPassword(email),
  });
}

export function useResetPassword() {
  return useMutation({
    mutationFn: ({ token, new_password }: { token: string; new_password: string }) =>
      resetPassword(token, new_password),
    onError: (err) => toast.error(getErrorMessage(err)),
  });
}
