"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listAppointments,
  getAppointment,
  createAppointment,
  updateAppointment,
  changeAppointmentStatus,
  getDoctorOptions,
  searchPatientOptions,
} from "@/lib/api/appointments";
import type {
  AppointmentListParams,
  AppointmentStatus,
  AppointmentUpsertRequest,
} from "@/lib/api/appointments";
import { getErrorMessage } from "@/lib/utils/errors";
import { useDebounce } from "@/lib/hooks/use-debounce";

export const APPOINTMENT_KEYS = {
  all: ["appointments"] as const,
  list: (params?: AppointmentListParams) => ["appointments", "list", params] as const,
  detail: (id: number | string) => ["appointments", id] as const,
  doctorOptions: () => ["appointments", "options", "doctors"] as const,
  patientOptions: (q: string) => ["appointments", "options", "patients", q] as const,
};

export function useAppointments(params?: AppointmentListParams) {
  return useQuery({
    queryKey: APPOINTMENT_KEYS.list(params),
    queryFn: () => listAppointments(params),
    placeholderData: (prev) => prev,
  });
}

export function useAppointment(id: number | string | undefined) {
  return useQuery({
    queryKey: APPOINTMENT_KEYS.detail(id ?? ""),
    queryFn: () => getAppointment(id as number | string),
    enabled: id !== undefined && id !== null && id !== "",
  });
}

export function useDoctorOptions() {
  return useQuery({
    queryKey: APPOINTMENT_KEYS.doctorOptions(),
    queryFn: getDoctorOptions,
    staleTime: 60_000,
  });
}

/** Tìm bệnh nhân cho combobox — debounce 300ms, chỉ gọi khi q >= 2 ký tự */
export function useSearchPatients(q: string) {
  const debouncedQ = useDebounce(q, 300);
  return useQuery({
    queryKey: APPOINTMENT_KEYS.patientOptions(debouncedQ),
    queryFn: () => searchPatientOptions(debouncedQ),
    enabled: debouncedQ.trim().length >= 2,
    staleTime: 10_000,
  });
}

export function useCreateAppointment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: AppointmentUpsertRequest) => createAppointment(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: APPOINTMENT_KEYS.all });
      toast.success("Đã tạo lịch hẹn");
    },
    onError: (e) => toast.error(getErrorMessage(e, "Tạo lịch hẹn thất bại")),
  });
}

export function useUpdateAppointment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: number | string; body: AppointmentUpsertRequest }) =>
      updateAppointment(id, body),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: APPOINTMENT_KEYS.all });
      qc.invalidateQueries({ queryKey: APPOINTMENT_KEYS.detail(id) });
      toast.success("Đã cập nhật lịch hẹn");
    },
    onError: (e) => toast.error(getErrorMessage(e, "Cập nhật lịch hẹn thất bại")),
  });
}

export function useChangeAppointmentStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: number | string; status: AppointmentStatus }) =>
      changeAppointmentStatus(id, status),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: APPOINTMENT_KEYS.all });
      qc.invalidateQueries({ queryKey: APPOINTMENT_KEYS.detail(id) });
      toast.success("Đã đổi trạng thái lịch hẹn");
    },
    onError: (e) => toast.error(getErrorMessage(e, "Đổi trạng thái thất bại")),
  });
}
