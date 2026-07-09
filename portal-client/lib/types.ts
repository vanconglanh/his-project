// Kiểu dữ liệu dùng chung cho Portal bệnh nhân
// Nguồn: hợp đồng API ${BASE}/api/portal/v1 — response luôn bọc {data} hoặc {error:{code,message}}

export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
}

export interface ApiErrorEnvelope {
  error: ApiError;
}

export interface TenantInfo {
  tenantId: string;
  name: string;
  logoUrl: string | null;
  vapidPublicKey: string;
}

export interface AuthResult {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  patientCode: string;
  fullName: string;
}

export interface MeProfile {
  patientCode: string;
  fullName: string;
  gender: string;
  dob: string;
  phone: string;
  address: string;
  bhytNumber: string | null;
}

export interface DiagnosisItem {
  icd10: string;
  name: string;
}

export interface EncounterListItem {
  id: string;
  encounterCode: string;
  visitedAt: string;
  doctorName: string;
  chiefComplaint: string;
  diagnosis: DiagnosisItem[];
  status: string;
}

export interface PrescriptionItemDetail {
  drugName: string;
  dosage: string;
  frequency: string;
  durationDays: number;
  instructions: string;
}

export interface EncounterDetail {
  id: string;
  encounterCode: string;
  visitedAt: string;
  doctorName: string;
  chiefComplaint: string;
  diagnosis: DiagnosisItem[];
  conclusion: string;
  doctorAdvice: string;
  prescriptionItems: PrescriptionItemDetail[];
}

export interface PrescriptionItem {
  drugName: string;
  dosage: string;
  quantity: number;
  usageInstruction: string;
}

export interface PrescriptionListItem {
  id: string;
  prescriptionCode: string;
  issuedAt: string;
  doctorName: string;
  note: string;
  items: PrescriptionItem[];
}

export interface LabResultListItem {
  id: string;
  testName: string;
  resultDate: string;
  conclusion: string;
  status: string;
}

export interface QueueInfo {
  ticketNo: string;
  roomName: string;
  status: string;
  currentCalledNo: string;
  waitingAhead: number;
  estWaitMinutes: number;
}

export interface AppointmentListItem {
  id: string;
  appointmentCode: string;
  status: string;
  appointmentAt: string;
  doctorName: string;
}

export interface DoctorOption {
  doctorRef: string;
  fullName: string;
}

export interface SlotOption {
  slotAt: string;
  available: boolean;
}

export interface MedReminder {
  id: string;
  drugName: string;
  doseLabel: string;
  timeSlot: string;
  remindTime: string;
  startDate: string;
  endDate: string;
  enabled: boolean;
}

export interface NotificationPreferences {
  push: boolean;
  email: boolean;
}

export interface HealthTrendPoint {
  date: string;
  value: number;
}

export interface HealthTrendMetric {
  testCode: string;
  testName: string;
  unit: string | null;
  latestValue: number;
  latestFlag: string | null;
  latestDate: string;
  series: HealthTrendPoint[];
}
