"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  getPortalMe,
  listPortalEncounters,
  getPortalEncounter,
  listPortalPrescriptions,
  downloadPortalPrescriptionPdf,
  listPortalLabResults,
  downloadPortalLabResultPdf,
  listPortalAppointments,
  createPortalAppointment,
  cancelPortalAppointment,
} from "@/lib/api/portal";
import type { PortalAppointmentCreateRequest } from "@/lib/api/portal";

export const portalKeys = {
  me: ["portal", "me"] as const,
  encounters: (params?: object) => ["portal", "encounters", params] as const,
  encounter: (id: string) => ["portal", "encounter", id] as const,
  prescriptions: ["portal", "prescriptions"] as const,
  labResults: ["portal", "lab-results"] as const,
  appointments: ["portal", "appointments"] as const,
};

export function usePortalMe() {
  return useQuery({
    queryKey: portalKeys.me,
    queryFn: getPortalMe,
  });
}

export function usePortalEncounters(params?: { page?: number; page_size?: number }) {
  return useQuery({
    queryKey: portalKeys.encounters(params),
    queryFn: () => listPortalEncounters(params),
  });
}

export function usePortalEncounter(id: string) {
  return useQuery({
    queryKey: portalKeys.encounter(id),
    queryFn: () => getPortalEncounter(id),
    enabled: !!id,
  });
}

export function usePortalPrescriptions() {
  return useQuery({
    queryKey: portalKeys.prescriptions,
    queryFn: listPortalPrescriptions,
  });
}

export function usePortalLabResults() {
  return useQuery({
    queryKey: portalKeys.labResults,
    queryFn: listPortalLabResults,
  });
}

export function usePortalAppointments() {
  return useQuery({
    queryKey: portalKeys.appointments,
    queryFn: listPortalAppointments,
  });
}

export function useCreatePortalAppointment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: PortalAppointmentCreateRequest) => createPortalAppointment(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: portalKeys.appointments });
      toast.success("Đặt lịch hẹn thành công");
    },
    onError: (err: { response?: { data?: { error?: { code?: string } } } }) => {
      const code = err?.response?.data?.error?.code;
      if (code === "APPOINTMENT_SLOT_TAKEN") {
        toast.error("Khung giờ đã được đặt, vui lòng chọn giờ khác");
      } else {
        toast.error("Đặt lịch thất bại");
      }
    },
  });
}

export function useCancelPortalAppointment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => cancelPortalAppointment(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: portalKeys.appointments });
      toast.success("Đã huỷ lịch hẹn");
    },
    onError: (err: { response?: { data?: { error?: { code?: string } } } }) => {
      const code = err?.response?.data?.error?.code;
      if (code === "APPOINTMENT_CANCEL_TOO_LATE") {
        toast.error("Không thể huỷ trong vòng 2 giờ trước hẹn");
      } else {
        toast.error("Huỷ lịch hẹn thất bại");
      }
    },
  });
}

export function useDownloadPrescriptionPdf() {
  return useMutation({
    mutationFn: (id: string) => downloadPortalPrescriptionPdf(id),
    onSuccess: (blob, id) => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `don-thuoc-${id}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    },
    onError: () => toast.error("Tải đơn thuốc thất bại"),
  });
}

export function useDownloadLabResultPdf() {
  return useMutation({
    mutationFn: (id: string) => downloadPortalLabResultPdf(id),
    onSuccess: (blob, id) => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `ket-qua-xn-${id}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    },
    onError: () => toast.error("Tải kết quả thất bại"),
  });
}
