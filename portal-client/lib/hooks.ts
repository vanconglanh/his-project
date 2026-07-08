"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, fetchFileBlob } from "@/lib/api";
import type {
  AppointmentListItem,
  AuthResult,
  DoctorOption,
  EncounterDetail,
  EncounterListItem,
  LabResultListItem,
  MeProfile,
  MedReminder,
  NotificationPreferences,
  PrescriptionListItem,
  QueueInfo,
  SlotOption,
  TenantInfo,
} from "@/lib/types";

export function useTenantInfo() {
  return useQuery({
    queryKey: ["tenant-info"],
    queryFn: () => api.get<TenantInfo>("/tenant-info", { auth: false }),
    staleTime: 60 * 60 * 1000,
  });
}

export function useMe(enabled = true) {
  return useQuery({
    queryKey: ["me"],
    queryFn: () => api.get<MeProfile>("/me"),
    enabled,
  });
}

export function useActivateMutation() {
  return useMutation({
    mutationFn: (payload: { phone: string; activationCode: string; pin: string }) =>
      api.post<AuthResult>("/auth/activate", payload, { auth: false }),
  });
}

export function useLoginPinMutation() {
  return useMutation({
    mutationFn: (payload: { phone: string; pin: string }) =>
      api.post<AuthResult>("/auth/login-pin", payload, { auth: false }),
  });
}

export function useForgotPinMutation() {
  return useMutation({
    mutationFn: (payload: { phone: string }) =>
      api.post<void>("/auth/forgot-pin", payload, { auth: false }),
  });
}

export function useResetPinMutation() {
  return useMutation({
    mutationFn: (payload: { phone: string; otp: string; newPin: string }) =>
      api.post<AuthResult>("/auth/reset-pin", payload, { auth: false }),
  });
}

export function useLogoutMutation() {
  return useMutation({
    mutationFn: () => api.post<void>("/auth/logout"),
  });
}

export function useEncounters(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: ["encounters", page, pageSize],
    queryFn: () =>
      api.get<EncounterListItem[]>(`/me/encounters?page=${page}&page_size=${pageSize}`),
  });
}

export function useEncounterDetail(id: string) {
  return useQuery({
    queryKey: ["encounter", id],
    queryFn: () => api.get<EncounterDetail>(`/me/encounters/${id}`),
    enabled: Boolean(id),
  });
}

export function usePrescriptions() {
  return useQuery({
    queryKey: ["prescriptions"],
    queryFn: () => api.get<PrescriptionListItem[]>("/me/prescriptions"),
  });
}

export function useDownloadPrescriptionPdf() {
  return useMutation({
    mutationFn: (id: string) => fetchFileBlob(`/me/prescriptions/${id}/pdf`),
  });
}

export function useLabResults() {
  return useQuery({
    queryKey: ["lab-results"],
    queryFn: () => api.get<LabResultListItem[]>("/me/lab-results"),
  });
}

export function useDownloadLabResultPdf() {
  return useMutation({
    mutationFn: (id: string) => fetchFileBlob(`/me/lab-results/${id}/pdf`),
  });
}

export function useQueueInfo() {
  return useQuery({
    queryKey: ["queue"],
    queryFn: () => api.get<QueueInfo | null>("/me/queue"),
    refetchInterval: 15000,
  });
}

export function useAppointments() {
  return useQuery({
    queryKey: ["appointments"],
    queryFn: () => api.get<AppointmentListItem[]>("/me/appointments"),
  });
}

export function useCreateAppointmentMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: { appointmentAt: string; doctorId: string; note?: string }) =>
      api.post<void>("/me/appointments", payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["appointments"] });
    },
  });
}

export function useCancelAppointmentMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.del<void>(`/me/appointments/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["appointments"] });
    },
  });
}

export function useDoctors() {
  return useQuery({
    queryKey: ["booking-doctors"],
    queryFn: () => api.get<DoctorOption[]>("/booking/doctors"),
  });
}

export function useSlots(doctorRef: string, date: string) {
  return useQuery({
    queryKey: ["booking-slots", doctorRef, date],
    queryFn: () =>
      api.get<SlotOption[]>(
        `/booking/slots?doctor_ref=${encodeURIComponent(doctorRef)}&date=${encodeURIComponent(date)}`,
      ),
    enabled: Boolean(doctorRef && date),
  });
}

export function useMedReminders() {
  return useQuery({
    queryKey: ["med-reminders"],
    queryFn: () => api.get<MedReminder[]>("/me/med-reminders"),
  });
}

export function useEnableReminderFromPrescription() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (prescriptionId: string) =>
      api.post<void>(`/me/med-reminders/from-prescription/${prescriptionId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["med-reminders"] });
    },
  });
}

export function useToggleReminder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: { id: string; enabled: boolean }) =>
      api.put<void>(`/me/med-reminders/${payload.id}`, { enabled: payload.enabled }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["med-reminders"] });
    },
  });
}

export function useNotificationPreferences() {
  return useQuery({
    queryKey: ["notification-preferences"],
    queryFn: () => api.get<NotificationPreferences>("/me/notification-preferences"),
  });
}

export function useUpdateNotificationPreferences() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: NotificationPreferences) =>
      api.put<void>("/me/notification-preferences", payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["notification-preferences"] });
    },
  });
}

export function useSavePushSubscription() {
  return useMutation({
    mutationFn: (payload: { endpoint: string; p256dh: string; auth: string }) =>
      api.post<void>("/me/push-subscriptions", payload),
  });
}
