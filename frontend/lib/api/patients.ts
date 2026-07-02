import apiClient from "./client";
import type {
  ApiResponse,
  ApiMeta,
  PatientResponse,
  CreatePatientRequest,
  UpdatePatientRequest,
  EncounterSummary,
  AllergyResponse,
  AllergyRequest,
  InsuranceResponse,
  InsuranceRequest,
  EmergencyContactResponse,
  EmergencyContactRequest,
  ConsentResponse,
} from "./types";

// ─── Patients CRUD ────────────────────────────────────────────────────────────

export interface PatientListParams {
  page?: number;
  page_size?: number;
  sort?: string;
  status?: string;
  gender?: string;
}

export interface PatientSearchParams {
  q: string;
  page?: number;
  page_size?: number;
}

export interface PatientListResponse {
  data: PatientResponse[];
  meta: ApiMeta;
}

export async function listPatients(params?: PatientListParams): Promise<PatientListResponse> {
  const { data } = await apiClient.get<PatientListResponse>("/patients", { params });
  return data;
}

export async function searchPatients(params: PatientSearchParams): Promise<PatientListResponse> {
  const { data } = await apiClient.get<PatientListResponse>("/patients/search", { params });
  return data;
}

export async function getPatient(id: string): Promise<PatientResponse> {
  const { data } = await apiClient.get<ApiResponse<PatientResponse>>(`/patients/${id}`);
  return data.data;
}

export async function createPatient(body: CreatePatientRequest): Promise<PatientResponse> {
  const { data } = await apiClient.post<ApiResponse<PatientResponse>>("/patients", body);
  return data.data;
}

export async function updatePatient(id: string, body: UpdatePatientRequest): Promise<PatientResponse> {
  const { data } = await apiClient.put<ApiResponse<PatientResponse>>(`/patients/${id}`, body);
  return data.data;
}

export async function deletePatient(id: string): Promise<void> {
  await apiClient.delete(`/patients/${id}`);
}

// ─── Avatar ───────────────────────────────────────────────────────────────────

export async function uploadPatientAvatar(id: string, file: File): Promise<{ avatar_url: string }> {
  const form = new FormData();
  form.append("file", file);
  const { data } = await apiClient.post<ApiResponse<{ avatar_url: string }>>(
    `/patients/${id}/avatar`,
    form,
    { headers: { "Content-Type": "multipart/form-data" } }
  );
  return data.data;
}

// ─── Encounters history ───────────────────────────────────────────────────────

export interface EncounterHistoryResponse {
  data: EncounterSummary[];
  meta: ApiMeta;
}

export async function getPatientEncounters(
  id: string,
  params?: { page?: number; page_size?: number }
): Promise<EncounterHistoryResponse> {
  const { data } = await apiClient.get<EncounterHistoryResponse>(`/patients/${id}/encounters`, { params });
  return data;
}

// ─── Allergies ────────────────────────────────────────────────────────────────

export async function listAllergies(patientId: string): Promise<AllergyResponse[]> {
  const { data } = await apiClient.get<ApiResponse<AllergyResponse[]>>(`/patients/${patientId}/allergies`);
  return data.data;
}

export async function addAllergy(patientId: string, body: AllergyRequest): Promise<AllergyResponse> {
  const { data } = await apiClient.post<ApiResponse<AllergyResponse>>(`/patients/${patientId}/allergies`, body);
  return data.data;
}

export async function deleteAllergy(patientId: string, allergyId: string): Promise<void> {
  await apiClient.delete(`/patients/${patientId}/allergies/${allergyId}`);
}

// ─── Insurance ────────────────────────────────────────────────────────────────

export async function listInsurance(patientId: string): Promise<InsuranceResponse[]> {
  const { data } = await apiClient.get<ApiResponse<InsuranceResponse[]>>(`/patients/${patientId}/insurance`);
  return data.data;
}

export async function addInsurance(patientId: string, body: InsuranceRequest): Promise<InsuranceResponse> {
  const { data } = await apiClient.post<ApiResponse<InsuranceResponse>>(`/patients/${patientId}/insurance`, body);
  return data.data;
}

export async function updateInsurance(
  patientId: string,
  insuranceId: string,
  body: InsuranceRequest
): Promise<InsuranceResponse> {
  const { data } = await apiClient.put<ApiResponse<InsuranceResponse>>(
    `/patients/${patientId}/insurance/${insuranceId}`,
    body
  );
  return data.data;
}

export async function deleteInsurance(patientId: string, insuranceId: string): Promise<void> {
  await apiClient.delete(`/patients/${patientId}/insurance/${insuranceId}`);
}

// ─── Emergency Contacts ───────────────────────────────────────────────────────

export async function listEmergencyContacts(patientId: string): Promise<EmergencyContactResponse[]> {
  const { data } = await apiClient.get<ApiResponse<EmergencyContactResponse[]>>(
    `/patients/${patientId}/emergency-contacts`
  );
  return data.data;
}

export async function addEmergencyContact(
  patientId: string,
  body: EmergencyContactRequest
): Promise<EmergencyContactResponse> {
  const { data } = await apiClient.post<ApiResponse<EmergencyContactResponse>>(
    `/patients/${patientId}/emergency-contacts`,
    body
  );
  return data.data;
}

export async function updateEmergencyContact(
  patientId: string,
  contactId: string,
  body: EmergencyContactRequest
): Promise<EmergencyContactResponse> {
  const { data } = await apiClient.put<ApiResponse<EmergencyContactResponse>>(
    `/patients/${patientId}/emergency-contacts/${contactId}`,
    body
  );
  return data.data;
}

export async function deleteEmergencyContact(patientId: string, contactId: string): Promise<void> {
  await apiClient.delete(`/patients/${patientId}/emergency-contacts/${contactId}`);
}

// ─── Consents ─────────────────────────────────────────────────────────────────

export async function listConsents(patientId: string): Promise<ConsentResponse[]> {
  const { data } = await apiClient.get<ApiResponse<ConsentResponse[]>>(`/patients/${patientId}/consents`);
  return data.data;
}

export async function addConsent(
  patientId: string,
  body: { consent_type: string; signed_by?: string; document_file_id?: string }
): Promise<ConsentResponse> {
  const { data } = await apiClient.post<ApiResponse<ConsentResponse>>(
    `/patients/${patientId}/consents`,
    body
  );
  return data.data;
}

// ─── Reception Note ───────────────────────────────────────────────────────────

export async function updateReceptionNote(patientId: string, reception_note: string): Promise<PatientResponse> {
  const { data } = await apiClient.put<ApiResponse<PatientResponse>>(
    `/patients/${patientId}/reception-note`,
    { reception_note }
  );
  return data.data;
}
