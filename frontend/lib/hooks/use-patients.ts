"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listPatients,
  searchPatients,
  getPatient,
  createPatient,
  updatePatient,
  deletePatient,
  uploadPatientAvatar,
  getPatientEncounters,
  listAllergies,
  addAllergy,
  deleteAllergy,
  listInsurance,
  addInsurance,
  updateInsurance,
  deleteInsurance,
  listEmergencyContacts,
  addEmergencyContact,
  updateEmergencyContact,
  deleteEmergencyContact,
  listConsents,
  addConsent,
  updateReceptionNote,
} from "@/lib/api/patients";
import type {
  PatientListParams,
  PatientSearchParams,
} from "@/lib/api/patients";
import type {
  CreatePatientRequest,
  UpdatePatientRequest,
  AllergyRequest,
  InsuranceRequest,
  EmergencyContactRequest,
} from "@/lib/api/types";
import { getErrorMessage } from "@/lib/utils/errors";

// ─── Query Keys ───────────────────────────────────────────────────────────────

export const patientKeys = {
  all: ["patients"] as const,
  list: (params?: PatientListParams) => [...patientKeys.all, "list", params] as const,
  search: (params: PatientSearchParams) => [...patientKeys.all, "search", params] as const,
  detail: (id: string) => [...patientKeys.all, "detail", id] as const,
  encounters: (id: string) => [...patientKeys.all, "encounters", id] as const,
  allergies: (id: string) => [...patientKeys.all, "allergies", id] as const,
  insurance: (id: string) => [...patientKeys.all, "insurance", id] as const,
  emergencyContacts: (id: string) => [...patientKeys.all, "emergency-contacts", id] as const,
  consents: (id: string) => [...patientKeys.all, "consents", id] as const,
};

// ─── Queries ──────────────────────────────────────────────────────────────────

export function usePatients(params?: PatientListParams) {
  return useQuery({
    queryKey: patientKeys.list(params),
    queryFn: () => listPatients(params),
  });
}

export function usePatientSearch(params: PatientSearchParams, enabled = true) {
  return useQuery({
    queryKey: patientKeys.search(params),
    queryFn: () => searchPatients(params),
    enabled: enabled && params.q.length >= 1,
    staleTime: 30_000,
  });
}

export function usePatient(id: string) {
  return useQuery({
    queryKey: patientKeys.detail(id),
    queryFn: () => getPatient(id),
    enabled: !!id,
  });
}

export function usePatientEncounters(id: string, page = 1) {
  return useQuery({
    queryKey: patientKeys.encounters(id),
    queryFn: () => getPatientEncounters(id, { page, page_size: 10 }),
    enabled: !!id,
  });
}

export function useAllergies(patientId: string) {
  return useQuery({
    queryKey: patientKeys.allergies(patientId),
    queryFn: () => listAllergies(patientId),
    enabled: !!patientId,
  });
}

export function useInsurance(patientId: string) {
  return useQuery({
    queryKey: patientKeys.insurance(patientId),
    queryFn: () => listInsurance(patientId),
    enabled: !!patientId,
  });
}

export function useEmergencyContacts(patientId: string) {
  return useQuery({
    queryKey: patientKeys.emergencyContacts(patientId),
    queryFn: () => listEmergencyContacts(patientId),
    enabled: !!patientId,
  });
}

export function useConsents(patientId: string) {
  return useQuery({
    queryKey: patientKeys.consents(patientId),
    queryFn: () => listConsents(patientId),
    enabled: !!patientId,
  });
}

// ─── Mutations ────────────────────────────────────────────────────────────────

export function useCreatePatient() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreatePatientRequest) => createPatient(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.all });
      toast.success("Tạo bệnh nhân thành công");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUpdatePatient(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: UpdatePatientRequest) => updatePatient(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.detail(id) });
      qc.invalidateQueries({ queryKey: patientKeys.all });
      toast.success("Cập nhật thành công");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useDeletePatient() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deletePatient(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.all });
      toast.success("Đã xoá bệnh nhân");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUploadAvatar(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => uploadPatientAvatar(patientId, file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.detail(patientId) });
      qc.invalidateQueries({ queryKey: patientKeys.all });
      toast.success("Cập nhật ảnh đại diện thành công");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useAddAllergy(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: AllergyRequest) => addAllergy(patientId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.allergies(patientId) });
      toast.success("Đã thêm thông tin dị ứng");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useDeleteAllergy(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (allergyId: string) => deleteAllergy(patientId, allergyId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.allergies(patientId) });
      toast.success("Đã xoá thông tin dị ứng");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useAddInsurance(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: InsuranceRequest) => addInsurance(patientId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.insurance(patientId) });
      qc.invalidateQueries({ queryKey: patientKeys.detail(patientId) });
      toast.success("Đã thêm thẻ BHYT");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUpdateInsurance(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ insuranceId, body }: { insuranceId: string; body: InsuranceRequest }) =>
      updateInsurance(patientId, insuranceId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.insurance(patientId) });
      toast.success("Cập nhật thẻ BHYT thành công");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useDeleteInsurance(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (insuranceId: string) => deleteInsurance(patientId, insuranceId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.insurance(patientId) });
      toast.success("Đã xoá thẻ BHYT");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useAddEmergencyContact(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: EmergencyContactRequest) => addEmergencyContact(patientId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.emergencyContacts(patientId) });
      toast.success("Đã thêm liên hệ khẩn cấp");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUpdateEmergencyContact(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ contactId, body }: { contactId: string; body: EmergencyContactRequest }) =>
      updateEmergencyContact(patientId, contactId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.emergencyContacts(patientId) });
      toast.success("Cập nhật liên hệ thành công");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useDeleteEmergencyContact(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (contactId: string) => deleteEmergencyContact(patientId, contactId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.emergencyContacts(patientId) });
      toast.success("Đã xoá liên hệ khẩn cấp");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useAddConsent(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { consent_type: string; signed_by?: string; document_file_id?: string }) =>
      addConsent(patientId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.consents(patientId) });
      toast.success("Đã thêm đồng ý điều trị");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}

export function useUpdateReceptionNote(patientId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (note: string) => updateReceptionNote(patientId, note),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: patientKeys.detail(patientId) });
      toast.success("Đã lưu ghi chú tiếp đón");
    },
    onError: (e) => toast.error(getErrorMessage(e)),
  });
}
