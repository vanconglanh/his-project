// Kiểu dữ liệu dùng chung, khớp hợp đồng API Cổng bệnh nhân (portal API)

export interface TenantInfo {
  tenantId: number | string;
  name: string;
  logoUrl?: string | null;
  vapidPublicKey?: string | null;
}

export interface AuthSession {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  patientCode: string;
  fullName: string;
}

export interface PatientProfile {
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
  id: number | string;
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

export interface EncounterDetail extends EncounterListItem {
  conclusion: string;
  doctorAdvice: string;
  prescriptionItems: PrescriptionItemDetail[];
}

export interface PrescriptionListItem {
  id: number | string;
  prescriptionCode: string;
  issuedAt: string;
  doctorName: string;
  note: string;
  items: Array<{
    drugName: string;
    dosage: string;
    quantity: number;
    usageInstruction: string;
  }>;
}

export interface LabResultItem {
  id: number | string;
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

export interface AppointmentItem {
  id: number | string;
  appointmentCode: string;
  status: string;
  appointmentAt: string;
  doctorName: string;
}

export interface DoctorItem {
  doctorRef: string;
  fullName: string;
}

export interface SlotItem {
  slotAt: string;
  available: boolean;
}

export interface MedReminderItem {
  id: number | string;
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

export interface ApiErrorPayload {
  error: {
    code: string;
    message: string;
    details?: Record<string, unknown>;
  };
}
