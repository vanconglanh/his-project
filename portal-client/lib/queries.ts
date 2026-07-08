import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "./api";
import type {
  AppointmentItem,
  AuthSession,
  DoctorItem,
  EncounterDetail,
  EncounterListItem,
  LabResultItem,
  MedReminderItem,
  NotificationPreferences,
  PatientProfile,
  PrescriptionListItem,
  QueueInfo,
  SlotItem,
  TenantInfo,
} from "./types";

// ---------- AUTH / TENANT ----------

export function useTenantInfo() {
  return useQuery({
    queryKey: ["tenant-info"],
    queryFn: () => api.get<TenantInfo>("/tenant-info", { auth: false }),
    staleTime: 5 * 60 * 1000,
    retry: 1,
  });
}

export function useActivateAccount() {
  return useMutation({
    mutationFn: (payload: { phone: string; activationCode: string; pin: string }) =>
      api.post<AuthSession>("/auth/activate", payload, { auth: false }),
  });
}

export function useLoginPin() {
  return useMutation({
    mutationFn: (payload: { phone: string; pin: string }) =>
      api.post<AuthSession>("/auth/login-pin", payload, { auth: false }),
  });
}

export function useForgotPin() {
  return useMutation({
    mutationFn: (payload: { phone: string }) => api.post<void>("/auth/forgot-pin", payload, { auth: false }),
  });
}

export function useResetPin() {
  return useMutation({
    mutationFn: (payload: { phone: string; otp: string; newPin: string }) =>
      api.post<AuthSession>("/auth/reset-pin", payload, { auth: false }),
  });
}

export function useLogout() {
  return useMutation({
    mutationFn: () => api.post<void>("/auth/logout"),
  });
}

// ---------- ME ----------

export function useMe() {
  return useQuery({
    queryKey: ["me"],
    queryFn: () => api.get<PatientProfile>("/me"),
    retry: 1,
  });
}

// ---------- ENCOUNTERS ----------

export function useEncounters(page: number, pageSize = 20) {
  return useQuery({
    queryKey: ["encounters", page, pageSize],
    queryFn: () => api.get<EncounterListItem[]>(`/me/encounters?page=${page}&page_size=${pageSize}`),
  });
}

export function useEncounterDetail(id: string) {
  return useQuery({
    queryKey: ["encounter", id],
    queryFn: () => api.get<EncounterDetail>(`/me/encounters/${id}`),
    enabled: Boolean(id),
  });
}

// ---------- PRESCRIPTIONS ----------

export function usePrescriptions() {
  return useQuery({
    queryKey: ["prescriptions"],
    queryFn: () => api.get<PrescriptionListItem[]>("/me/prescriptions"),
  });
}

// ---------- LAB RESULTS ----------

export function useLabResults() {
  return useQuery({
    queryKey: ["lab-results"],
    queryFn: () => api.get<LabResultItem[]>("/me/lab-results"),
  });
}

// ---------- QUEUE ----------

export function useQueueInfo() {
  return useQuery({
    queryKey: ["queue"],
    queryFn: () => api.get<QueueInfo | null>("/me/queue"),
    refetchInterval: 15_000,
  });
}

// ---------- APPOINTMENTS ----------

export function useAppointments() {
  return useQuery({
    queryKey: ["appointments"],
    queryFn: () => api.get<AppointmentItem[]>("/me/appointments"),
  });
}

export function useCreateAppointment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { appointmentAt: string; doctorId: string; note?: string }) =>
      api.post<AppointmentItem>("/me/appointments", payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["appointments"] });
    },
  });
}

export function useCancelAppointment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string | number) => api.delete<void>(`/me/appointments/${id}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["appointments"] });
    },
  });
}

export function useDoctors() {
  return useQuery({
    queryKey: ["doctors"],
    queryFn: () => api.get<DoctorItem[]>("/booking/doctors"),
  });
}

export function useSlots(doctorRef: string, date: string) {
  return useQuery({
    queryKey: ["slots", doctorRef, date],
    queryFn: () => api.get<SlotItem[]>(`/booking/slots?doctor_ref=${doctorRef}&date=${date}`),
    enabled: Boolean(doctorRef && date),
  });
}

// ---------- MEDICATION REMINDERS ----------

export function useMedReminders() {
  return useQuery({
    queryKey: ["med-reminders"],
    queryFn: () => api.get<MedReminderItem[]>("/me/med-reminders"),
  });
}

export function useCreateRemindersFromPrescription() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (prescriptionId: string | number) =>
      api.post<void>(`/me/med-reminders/from-prescription/${prescriptionId}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["med-reminders"] });
    },
  });
}

export function useToggleMedReminder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { id: string | number; enabled: boolean }) =>
      api.put<void>(`/me/med-reminders/${payload.id}`, { enabled: payload.enabled }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["med-reminders"] });
    },
  });
}

// ---------- NOTIFICATION PREFERENCES ----------

export function useNotificationPreferences() {
  return useQuery({
    queryKey: ["notification-preferences"],
    queryFn: () => api.get<NotificationPreferences>("/me/notification-preferences"),
  });
}

export function useUpdateNotificationPreferences() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: NotificationPreferences) =>
      api.put<NotificationPreferences>("/me/notification-preferences", payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["notification-preferences"] });
    },
  });
}
