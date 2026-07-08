import apiClient from "./client";
import type { ApiMeta } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────
// Bảng nguồn: diab_his_sch_appointments (id INT — không phải GUID như đa số entity khác)

export type AppointmentStatus =
  | "PENDING"
  | "CONFIRMED"
  | "CHECKED_IN"
  | "CANCELLED"
  | "NO_SHOW";

export type AppointmentSource = "WALK_IN" | "PHONE" | "WEB" | "API" | "APP";

export interface AppointmentResponse {
  id: number;
  appointment_at: string;
  duration_minutes: number;
  status: AppointmentStatus;
  source: AppointmentSource;
  patient_ref: string | null;
  patient_name: string;
  patient_phone: string | null;
  doctor_ref: string | null;
  doctor_name: string | null;
  note: string | null;
}

export interface AppointmentListParams {
  from?: string;
  to?: string;
  doctor_ref?: string;
  status?: AppointmentStatus;
  q?: string;
  page?: number;
  page_size?: number;
}

export interface AppointmentListResponse {
  data: AppointmentResponse[];
  meta: ApiMeta;
}

export interface AppointmentUpsertRequest {
  patient_ref?: string;
  patient_name_temp?: string;
  patient_phone?: string;
  doctor_ref?: string;
  appointment_at: string;
  duration_minutes?: number;
  source?: AppointmentSource;
  note?: string;
}

export interface OptionItem {
  value: string;
  label: string;
}

export interface PatientOptionItem extends OptionItem {
  phone?: string | null;
}

// ─── CRUD ─────────────────────────────────────────────────────────────────────

export async function listAppointments(
  params?: AppointmentListParams
): Promise<AppointmentListResponse> {
  const { data } = await apiClient.get<AppointmentListResponse>("/appointments", { params });
  return data;
}

export async function getAppointment(id: number | string): Promise<AppointmentResponse> {
  const { data } = await apiClient.get<{ data: AppointmentResponse }>(`/appointments/${id}`);
  return data.data;
}

export async function createAppointment(
  body: AppointmentUpsertRequest
): Promise<AppointmentResponse> {
  const { data } = await apiClient.post<{ data: AppointmentResponse }>("/appointments", body);
  return data.data;
}

export async function updateAppointment(
  id: number | string,
  body: AppointmentUpsertRequest
): Promise<AppointmentResponse> {
  const { data } = await apiClient.put<{ data: AppointmentResponse }>(
    `/appointments/${id}`,
    body
  );
  return data.data;
}

export async function changeAppointmentStatus(
  id: number | string,
  status: AppointmentStatus
): Promise<AppointmentResponse> {
  const { data } = await apiClient.patch<{ data: AppointmentResponse }>(
    `/appointments/${id}/status`,
    { status }
  );
  return data.data;
}

// ─── Options (combobox) ───────────────────────────────────────────────────────

export async function getDoctorOptions(): Promise<OptionItem[]> {
  const { data } = await apiClient.get<{ data: OptionItem[] }>(
    "/appointments/options/doctors"
  );
  return data.data;
}

export async function searchPatientOptions(q: string): Promise<PatientOptionItem[]> {
  const { data } = await apiClient.get<{ data: PatientOptionItem[] }>(
    "/appointments/options/patients",
    { params: { q } }
  );
  return data.data;
}

// ─── Giấy hẹn tái khám (slip PDF, letterhead branded) ─────────────────────────
// Backend: GET /api/v1/appointments/{id}/slip-pdf (permission appointment.read)

export function getAppointmentSlipPdfUrl(appointmentId: number | string): string {
  return `${apiClient.defaults.baseURL}/appointments/${appointmentId}/slip-pdf`;
}

export async function printAppointmentSlipPdf(appointmentId: number | string): Promise<void> {
  const { printPdfBlob } = await import("@/lib/utils/printPdfBlob");
  await printPdfBlob(getAppointmentSlipPdfUrl(appointmentId));
}
