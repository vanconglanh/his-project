import axios from "axios";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000";

const portalClient = axios.create({
  baseURL: `${API_BASE_URL}/api/portal/v1`,
  headers: { "Content-Type": "application/json" },
  timeout: 30000,
});

// Inject portal session token from cookie/localStorage
portalClient.interceptors.request.use((config) => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("portal-token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }
  return config;
});

// ─── Types ────────────────────────────────────────────────────────────────────

export interface PortalAuthOtpRequest {
  phone: string;
  tenant_code?: string;
}

export interface PortalVerifyRequest {
  phone: string;
  otp: string;
}

export interface PortalAuthResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  patient_code: string;
  full_name: string;
}

export interface PortalMeResponse {
  patient_code: string;
  full_name: string;
  gender?: string;
  dob?: string;
  phone?: string;
  address?: string;
  bhyt_number?: string | null;
}

export interface PortalEncounterResponse {
  id: string;
  encounter_code: string;
  visited_at: string;
  doctor_name: string;
  chief_complaint?: string;
  diagnosis: { icd10: string; name: string }[];
  status: string;
}

export interface PortalPrescriptionResponse {
  id: string;
  prescription_code: string;
  issued_at: string;
  doctor_name: string;
  dtqg_code?: string | null;
  items: {
    drug_name: string;
    dosage: string;
    quantity: number;
    usage_instruction: string;
  }[];
}

export interface PortalLabResultResponse {
  id: string;
  test_name: string;
  ordered_at: string;
  reported_at?: string;
  value?: string;
  unit?: string;
  reference_range?: string;
  status: string;
}

export interface PortalAppointmentResponse {
  id: string;
  appointment_code: string;
  appointment_at: string;
  doctor_name?: string;
  status: string;
  note?: string;
}

export interface PortalAppointmentCreateRequest {
  doctor_id?: string;
  department_id?: string;
  appointment_at: string;
  note?: string;
}

export interface PortalApiMeta {
  page: number;
  total: number;
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

export async function portalRequestOtp(body: PortalAuthOtpRequest) {
  await portalClient.post("/auth/request-otp", body);
}

export async function portalVerifyOtp(body: PortalVerifyRequest) {
  const res = await portalClient.post<PortalAuthResponse>("/auth/verify-otp", body);
  return res.data;
}

export async function portalLogout() {
  await portalClient.post("/auth/logout");
}

// ─── Profile ─────────────────────────────────────────────────────────────────

export async function getPortalMe() {
  const res = await portalClient.get<PortalMeResponse>("/me");
  return res.data;
}

// ─── Encounters ──────────────────────────────────────────────────────────────

export async function listPortalEncounters(params?: { page?: number; page_size?: number }) {
  const res = await portalClient.get<{ data: PortalEncounterResponse[]; meta: PortalApiMeta }>(
    "/me/encounters",
    { params }
  );
  return res.data;
}

export async function getPortalEncounter(id: string) {
  const res = await portalClient.get<PortalEncounterResponse>(`/me/encounters/${id}`);
  return res.data;
}

// ─── Prescriptions ────────────────────────────────────────────────────────────

export async function listPortalPrescriptions() {
  const res = await portalClient.get<{ data: PortalPrescriptionResponse[] }>("/me/prescriptions");
  return res.data;
}

export async function downloadPortalPrescriptionPdf(id: string) {
  const res = await portalClient.get(`/me/prescriptions/${id}/pdf`, { responseType: "blob" });
  return res.data as Blob;
}

// ─── Lab Results ──────────────────────────────────────────────────────────────

export async function listPortalLabResults() {
  const res = await portalClient.get<{ data: PortalLabResultResponse[] }>("/me/lab-results");
  return res.data;
}

export async function downloadPortalLabResultPdf(id: string) {
  const res = await portalClient.get(`/me/lab-results/${id}/pdf`, { responseType: "blob" });
  return res.data as Blob;
}

// ─── Appointments ────────────────────────────────────────────────────────────

export async function listPortalAppointments() {
  const res = await portalClient.get<{ data: PortalAppointmentResponse[] }>("/me/appointments");
  return res.data;
}

export async function createPortalAppointment(body: PortalAppointmentCreateRequest) {
  const res = await portalClient.post<PortalAppointmentResponse>("/me/appointments", body);
  return res.data;
}

export async function cancelPortalAppointment(id: string) {
  await portalClient.delete(`/me/appointments/${id}`);
}
