"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listLabPartners,
  getLabPartner,
  createLabPartner,
  updateLabPartner,
  deleteLabPartner,
  testLabPartnerConnection,
  updateLabPartnerCredentials,
  rotateLabPartnerCredentials,
  type LabPartnerStatus,
  type LabPartnerCreateRequest,
  type LabPartnerUpdateRequest,
  type LabPartnerCredentialsRequest,
} from "@/lib/api/lab-partners";
import { getErrorMessage } from "@/lib/utils/errors";

export const labPartnerKeys = {
  all: ["lab-partners"] as const,
  list: (params?: { status?: LabPartnerStatus; q?: string }) => [...labPartnerKeys.all, "list", params] as const,
  detail: (id: string) => [...labPartnerKeys.all, "detail", id] as const,
};

export function useLabPartners(params?: { status?: LabPartnerStatus; q?: string }) {
  return useQuery({
    queryKey: labPartnerKeys.list(params),
    queryFn: () => listLabPartners(params),
    retry: 2,
  });
}

export function useLabPartner(id: string) {
  return useQuery({
    queryKey: labPartnerKeys.detail(id),
    queryFn: () => getLabPartner(id),
    enabled: !!id,
  });
}

export function useCreateLabPartner() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: LabPartnerCreateRequest) => createLabPartner(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: labPartnerKeys.all });
      toast.success("Đã tạo đối tác lab");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUpdateLabPartner(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: LabPartnerUpdateRequest) => updateLabPartner(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: labPartnerKeys.all });
      qc.invalidateQueries({ queryKey: labPartnerKeys.detail(id) });
      toast.success("Đã cập nhật đối tác lab");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useDeleteLabPartner() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteLabPartner(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: labPartnerKeys.all });
      toast.success("Đã xoá đối tác lab");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useTestLabPartnerConnection() {
  return useMutation({
    mutationFn: (id: string) => testLabPartnerConnection(id),
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUpdateLabPartnerCredentials(id: string) {
  return useMutation({
    mutationFn: (body: LabPartnerCredentialsRequest) => updateLabPartnerCredentials(id, body),
    onSuccess: () => toast.success("Đã cập nhật credentials"),
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useRotateLabPartnerCredentials(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => rotateLabPartnerCredentials(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: labPartnerKeys.detail(id) });
      toast.success("Đã xoay credentials thành công");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}
