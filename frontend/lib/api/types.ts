// ─── Common ───────────────────────────────────────────────────────────────────

export interface ApiMeta {
  page: number;
  page_size: number;
  total: number;
  total_pages: number;
}

export interface ApiResponse<T> {
  data: T;
  meta?: ApiMeta;
}

export interface ApiError {
  error: {
    code: string;
    message: string;
    details?: Record<string, unknown>;
  };
}

// ─── Auth (existing) ──────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: UserProfile & { roles?: string[]; roleCodes?: string[] };
  permissions: string[];
}

export interface UserProfile {
  id: number;
  email: string;
  fullName: string;
  role: UserRole;
  tenantId: number;
  clinicId: number;
  clinicName: string;
  avatarUrl?: string;
}

export type UserRole =
  | "Admin"
  | "BacSi"
  | "LeTan"
  | "DuocSi"
  | "KeToan"
  | "KyThuatVien";

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

// ─── Tenant ───────────────────────────────────────────────────────────────────

export type TenantStatus = "ACTIVE" | "SUSPENDED" | "TERMINATED";

export interface TenantResponse {
  id: string;
  code: string;
  name: string;
  cskcb_code?: string;
  status: TenantStatus;
  tax_code?: string;
  address?: string;
  phone?: string;
  email?: string;
  subdomain: string;
  storage_quota_gb?: number;
  expires_at?: string | null;
  created_at: string;
  updated_at?: string;
}

export interface CreateTenantRequest {
  code: string;
  name: string;
  cskcb_code?: string;
  tax_code?: string;
  address?: string;
  phone?: string;
  email: string;
  subdomain: string;
  storage_quota_gb?: number;
  admin_email: string;
  admin_full_name: string;
}

export interface UpdateTenantRequest {
  name?: string;
  cskcb_code?: string;
  tax_code?: string;
  address?: string;
  phone?: string;
  email?: string;
  storage_quota_gb?: number;
  expires_at?: string | null;
}

export interface UpdateTenantProfileRequest {
  name?: string;
  address?: string;
  phone?: string;
  email?: string;
  cskcb_code?: string;
  bhyt_token?: string;
}

// ─── User ─────────────────────────────────────────────────────────────────────

export type UserStatus = "PENDING" | "ACTIVE" | "LOCKED" | "DISABLED";

// Role code theo seed DB (db/migrations/9001_create_sec_all.sql + 9007): snake_case thường
export type SystemRoleCode =
  | "admin"
  | "bac_si"
  | "le_tan"
  | "duoc_si"
  | "ke_toan"
  | "ky_thuat_vien";

export interface RoleRef {
  code: string;
  name: string;
}

export interface UserResponse {
  id: string;
  tenant_id: number;
  email: string;
  full_name: string;
  phone?: string;
  avatar_url?: string | null;
  status: UserStatus;
  roles: RoleRef[];
  permissions: string[];
  two_fa_enabled: boolean;
  last_login_at?: string | null;
  created_at: string;
}

export interface InviteUserRequest {
  email: string;
  full_name: string;
  phone?: string;
  role_codes: string[];
}

export interface AcceptInviteRequest {
  token: string;
  password: string;
  full_name?: string;
}

export interface UpdateUserRequest {
  full_name?: string;
  phone?: string;
  avatar_url?: string;
}

export interface UpdateMeRequest {
  full_name?: string;
  phone?: string;
  avatar_url?: string;
}

export interface ChangePasswordRequest {
  old_password: string;
  new_password: string;
}

export interface Setup2FAResponse {
  secret: string;
  otpauth_url: string;
  qr_png_base64: string;
}

export interface Enable2FAResponse {
  recovery_codes: string[];
}

// ─── Role ─────────────────────────────────────────────────────────────────────

export type RoleType = "SYSTEM" | "CUSTOM";

export interface RoleResponse {
  code: string;
  name: string;
  description?: string;
  role_type: RoleType;
  tenant_id?: string | null;
  permission_codes: string[];
}

export interface CreateRoleRequest {
  code: string;
  name: string;
  description?: string;
  permission_codes: string[];
}

export interface UpdateRoleRequest {
  name?: string;
  description?: string;
  permission_codes?: string[];
}

export type PermissionAction =
  | "read"
  | "write"
  | "delete"
  | "sign"
  | "export"
  | "invite"
  | "assign_role";

export interface PermissionResponse {
  code: string;
  resource: string;
  action: PermissionAction;
  description?: string;
}

// ─── Patient ──────────────────────────────────────────────────────────────────

export type Gender = "MALE" | "FEMALE" | "OTHER";
export type PatientStatus = "ACTIVE" | "INACTIVE" | "DECEASED";
export type PatientType = "SERVICE" | "BHYT" | "FREE" | "CONTRACT";
export type MaritalStatus = "SINGLE" | "MARRIED" | "DIVORCED" | "WIDOWED" | "OTHER";
export type VisitType = "FIRST_VISIT" | "FOLLOW_UP" | "EMERGENCY" | "SPECIALIST";
export type BloodType =
  | "A_POS"
  | "A_NEG"
  | "B_POS"
  | "B_NEG"
  | "AB_POS"
  | "AB_NEG"
  | "O_POS"
  | "O_NEG"
  | "UNKNOWN";
export type AllergySeverity = "MILD" | "MODERATE" | "SEVERE" | "LIFE_THREATENING";
export type InsuranceType = "BHYT" | "PRIVATE" | "OTHER";
export type Relationship = "FATHER" | "MOTHER" | "SPOUSE" | "CHILD" | "SIBLING" | "OTHER";
export type ConsentType = "TREATMENT" | "DATA_PROCESSING" | "MARKETING" | "SURGERY" | "RESEARCH";

export interface PatientAddress {
  province_code?: string;
  district_code?: string;
  ward_code?: string;
  street?: string;
}

export interface PatientResponse {
  id: string;
  tenant_id: string;
  code: string;
  full_name: string;
  gender?: Gender;
  date_of_birth?: string;
  age?: number;
  id_number?: string;
  phone?: string;
  email?: string;
  address?: PatientAddress;
  occupation?: string;
  ethnicity?: string;
  avatar_url?: string | null;
  reception_note?: string | null;
  blood_type?: BloodType;
  allergies_summary?: string;
  bhyt_card_no?: string;
  bhyt_valid_to?: string | null;
  status: PatientStatus;
  id_card_issued_date?: string | null;
  id_card_issued_place?: string | null;
  nationality?: string;
  patient_type?: PatientType;
  marital_status?: MaritalStatus | null;
  visit_type?: VisitType | null;
  created_at: string;
  updated_at: string;
}

export interface CreatePatientRequest {
  full_name: string;
  gender?: Gender;
  date_of_birth?: string;
  id_number?: string;
  phone?: string;
  email?: string;
  address?: PatientAddress;
  occupation?: string;
  ethnicity?: string;
  blood_type?: BloodType;
  id_card_issued_date?: string;
  id_card_issued_place?: string;
  nationality?: string;
  patient_type?: PatientType;
  marital_status?: MaritalStatus;
  visit_type?: VisitType;
}

export interface UpdatePatientRequest extends CreatePatientRequest {
  status?: PatientStatus;
}

export interface EncounterSummary {
  id: string;
  encounter_no: string;
  encounter_date: string;
  doctor_name?: string;
  room_name?: string;
  chief_complaint?: string;
  diagnosis_icd10?: string[];
  status: "IN_PROGRESS" | "DONE" | "CANCELLED" | "WAITING";
}

export interface AllergyResponse {
  id: string;
  patient_id: string;
  allergen: string;
  reaction?: string;
  severity: AllergySeverity;
  onset_date?: string | null;
  note?: string | null;
  created_at: string;
}

export interface AllergyRequest {
  allergen: string;
  reaction?: string;
  severity: AllergySeverity;
  onset_date?: string;
  note?: string;
}

export interface InsuranceResponse {
  id: string;
  patient_id: string;
  type: InsuranceType;
  card_no: string;
  valid_from: string;
  valid_to: string;
  hospital_code?: string;
  coverage_percent?: number;
  created_at: string;
}

export interface InsuranceRequest {
  type: InsuranceType;
  card_no: string;
  valid_from: string;
  valid_to: string;
  hospital_code?: string;
  coverage_percent?: number;
}

export interface EmergencyContactResponse {
  id: string;
  patient_id: string;
  full_name: string;
  relationship: Relationship;
  phone: string;
  address?: string;
}

export interface EmergencyContactRequest {
  full_name: string;
  relationship: Relationship;
  phone: string;
  address?: string;
}

export interface ConsentResponse {
  id: string;
  patient_id: string;
  consent_type: ConsentType;
  signed_at: string;
  signed_by?: string;
  document_url?: string | null;
  revoked_at?: string | null;
}

// ─── Reception ────────────────────────────────────────────────────────────────

export type TicketStatus = "WAITING" | "CALLED" | "IN_PROGRESS" | "DONE" | "SKIPPED" | "CANCELLED";
export type TicketPriority = "NORMAL" | "PRIORITY" | "EMERGENCY";

export interface PatientSummary {
  id: string;
  code: string;
  full_name: string;
  dob?: string;
  gender?: Gender;
  bhyt_summary?: string | null;
}

export interface CheckInRequest {
  patient_id: string;
  room_id: string;
  service_package_ids?: string[];
  reason_for_visit?: string;
  note?: string;
  priority?: TicketPriority;
}

export interface ServicePackage {
  id: string;
  name: string;
  price: number;
}

export interface ReceptionTicketResponse {
  id: string;
  tenant_id: string;
  patient_id: string;
  patient_summary?: PatientSummary;
  ticket_no: string;
  room_id: string;
  room_name?: string;
  doctor_id?: string | null;
  doctor_name?: string;
  service_packages?: ServicePackage[];
  reason_for_visit?: string;
  status: TicketStatus;
  priority: TicketPriority;
  checked_in_at: string;
  called_at?: string | null;
  started_at?: string | null;
  finished_at?: string | null;
  created_by: string;
  note?: string | null;
}

export interface RoomResponse {
  id: string;
  name: string;
  room_code: string;
  on_duty_doctor?: {
    id: string;
    full_name: string;
  } | null;
  max_per_day?: number;
  current_waiting?: number;
}

export interface ReceptionStats {
  date: string;
  total_checked_in: number;
  waiting: number;
  in_progress: number;
  done: number;
  skipped: number;
  cancelled: number;
  avg_wait_minutes?: number;
}

// ─── CLS Uploads ──────────────────────────────────────────────────────────────

export interface ClsUploadResponse {
  id: string;
  patient_id: string;
  encounter_id?: string | null;
  doc_type: string;
  file_id: string;
  file_name: string;
  file_size_bytes: number;
  mime_type: string;
  signed_url?: string;
  uploaded_at: string;
  uploaded_by: string;
  uploaded_by_name?: string;
  note?: string | null;
}

// ─── Files ────────────────────────────────────────────────────────────────────

export interface FileUploadResponse {
  id: string;
  file_name: string;
  mime_type: string;
  file_size_bytes: number;
  signed_url: string;
  signed_url_expires_at?: string;
}

// ─── Encounter ────────────────────────────────────────────────────────────────

export type EncounterStatus = "WAITING" | "IN_PROGRESS" | "DONE" | "CANCELLED";
export type EncounterType = "FIRST_VISIT" | "FOLLOW_UP" | "EMERGENCY" | "CONSULTATION";
export type DiagnosisType = "PRIMARY" | "SECONDARY";

export interface EncounterPatientSummary {
  full_name: string;
  year_of_birth?: number;
  gender?: string;
  phone?: string;
}

export interface DiagnosisResponse {
  id: string;
  icd10_code: string;
  name: string;
  type: DiagnosisType;
  note?: string | null;
  created_at: string;
}

export interface DiagnosisRequest {
  icd10_code: string;
  type: DiagnosisType;
  note?: string;
}

export interface EncounterResponse {
  id: string;
  tenant_id: string;
  patient_id: string;
  patient_summary?: EncounterPatientSummary;
  room_id?: string | null;
  room_name?: string | null;
  doctor_id?: string | null;
  doctor_name?: string | null;
  encounter_type: EncounterType;
  reason_for_visit: string;
  chief_complaint?: string | null;
  status: EncounterStatus;
  started_at?: string | null;
  finished_at?: string | null;
  alert_over_12h: boolean;
  diagnoses: DiagnosisResponse[];
  vital_signs_latest?: Record<string, unknown> | null;
  has_emr_signed: boolean;
  has_prescription: boolean;
  created_at: string;
}

export interface EncounterDetailResponse extends EncounterResponse {
  vital_signs: VitalSignsResponse[];
  lab_orders: LabOrderResponse[];
  rad_orders: RadOrderResponse[];
  prescriptions: unknown[];
  emr_summary?: {
    id: string;
    signed_at?: string | null;
    version: number;
  } | null;
}

export interface EncounterCreateRequest {
  patient_id: string;
  room_id?: string;
  doctor_id?: string;
  encounter_type: EncounterType;
  reason_for_visit: string;
  chief_complaint?: string;
}

export interface EncounterUpdateRequest {
  room_id?: string;
  doctor_id?: string;
  encounter_type?: EncounterType;
  reason_for_visit?: string;
  chief_complaint?: string;
}

export interface TimelineEvent {
  timestamp: string;
  event_type: "VITAL" | "LAB_ORDER" | "RAD_ORDER" | "PRESCRIPTION" | "NOTE" | "DIAGNOSIS" | "EMR_SAVED" | "EMR_SIGNED";
  actor: string;
  actor_role: string;
  ref_id?: string;
  summary: string;
  payload?: Record<string, unknown>;
}

export interface Over12hAlert {
  encounter_id: string;
  patient_name: string;
  doctor_name?: string | null;
  started_at: string;
  hours_open: number;
  alert_sent_at?: string | null;
}

// ─── Vital Signs ──────────────────────────────────────────────────────────────

export interface VitalSignsRequest {
  recorded_at?: string;
  temperature_c?: number;
  heart_rate_bpm?: number;
  respiratory_rate?: number;
  bp_systolic?: number;
  bp_diastolic?: number;
  spo2_percent?: number;
  weight_kg?: number;
  height_cm?: number;
  pain_scale?: number;
  glucose_mg_dl?: number;
  note?: string;
}

export interface VitalSignsResponse extends VitalSignsRequest {
  id: string;
  encounter_id: string;
  recorded_by: string;
  recorded_by_name: string;
  bmi?: number;
  record_sequence: number;
  created_at: string;
}

// ─── EMR ──────────────────────────────────────────────────────────────────────

export interface EmrSaveRequest {
  content_json: Record<string, unknown>;
  content_html?: string;
  template_id?: string;
}

export interface EmrContentResponse {
  id: string;
  encounter_id: string;
  content_json: Record<string, unknown>;
  content_html: string;
  template_id?: string | null;
  signed_at?: string | null;
  signed_by?: string | null;
  signed_by_name?: string | null;
  signature_certificate?: {
    serial: string;
    subject: string;
    algorithm: string;
  } | null;
  version: number;
  updated_at: string;
  updated_by: string;
}

export interface SignEmrRequest {
  signature_data: string;
  certificate_id: string;
  signature_algorithm?: string;
}

export interface EmrVersionMeta {
  version_id: string;
  version: number;
  saved_at: string;
  saved_by: string;
  saved_by_name: string;
  is_signed: boolean;
  bytes_size: number;
}

export type EmrTemplateSpeciality = "GENERAL" | "DIABETES" | "CARDIOLOGY" | "ENDOCRINOLOGY" | "NEPHROLOGY" | "OPHTHALMOLOGY" | "OTHER";

export interface EmrTemplateRequest {
  name: string;
  content_json: Record<string, unknown>;
  speciality: EmrTemplateSpeciality;
}

export interface EmrTemplateResponse extends EmrTemplateRequest {
  id: string;
  is_system: boolean;
  tenant_id?: string | null;
  created_by: string;
  created_at: string;
}

// ─── Diabetes Assessment ──────────────────────────────────────────────────────

export type DiabetesType = "TYPE_1" | "TYPE_2" | "GESTATIONAL" | "MODY" | "OTHER";

export interface DiabetesComplications {
  retinopathy?: boolean;
  neuropathy?: boolean;
  nephropathy?: boolean;
  cad?: boolean;
  pad?: boolean;
  diabetic_foot?: boolean;
}

export interface DiabetesTreatmentTarget {
  hba1c_target?: number;
  ldl_target?: number;
  bp_target?: string;
}

export interface DiabetesAssessmentRequest {
  hba1c?: number;
  fasting_glucose?: number;
  postprandial_glucose?: number;
  random_glucose?: number;
  egfr?: number;
  serum_creatinine?: number;
  urine_acr?: number;
  bp_systolic?: number;
  bp_diastolic?: number;
  bmi?: number;
  waist_circumference?: number;
  diabetes_type?: DiabetesType;
  complications?: DiabetesComplications;
  treatment_target?: DiabetesTreatmentTarget;
  note?: string;
}

export interface DiabetesAssessmentResponse extends DiabetesAssessmentRequest {
  id: string;
  encounter_id: string;
  patient_id: string;
  assessed_at: string;
  assessed_by: string;
  assessed_by_name: string;
}

// ─── CLS Orders ───────────────────────────────────────────────────────────────

export type LabOrderStatus = "ordered" | "sample_taken" | "processing" | "done" | "cancelled";
export type RadOrderStatus = "ordered" | "scheduled" | "in_progress" | "done" | "cancelled";
export type ClsPriority = "NORMAL" | "URGENT" | "STAT";
export type Modality = "XRAY" | "US" | "CT" | "MRI" | "MAMMO" | "ECG" | "ENDO";

export interface LabOrderRequest {
  test_code: string;
  sample_type?: string;
  priority?: ClsPriority;
  scheduled_for?: string;
  lab_partner_id?: string;
  note?: string;
}

export interface LabOrderResponse extends LabOrderRequest {
  id: string;
  encounter_id: string;
  test_name: string;
  status: LabOrderStatus;
  ordered_at: string;
  ordered_by: string;
}

export interface RadOrderRequest {
  modality: Modality;
  body_part?: string;
  contrast?: boolean;
  procedure_code: string;
  priority?: ClsPriority;
  note?: string;
}

export interface RadOrderResponse extends RadOrderRequest {
  id: string;
  encounter_id: string;
  procedure_name: string;
  status: RadOrderStatus;
  ordered_at: string;
  ordered_by: string;
}

export interface ClsCatalogItem {
  code: string;
  name: string;
  kind: "LAB" | "RAD";
  sample_type?: string | null;
  modality?: string | null;
  default_price: number;
  bhyt_price?: number | null;
}

// ─── ICD-10 ───────────────────────────────────────────────────────────────────

export interface Icd10Response {
  code: string;
  name_vi: string;
  name_en: string;
  category: string;
  parent_code?: string | null;
  is_billable: boolean;
}

export interface Icd10Category {
  code_range: string;
  chapter: string;
  name_vi: string;
  name_en: string;
  count: number;
}

// ─── Audit Log ────────────────────────────────────────────────────────────────

export type AuditAction =
  | "CREATE"
  | "UPDATE"
  | "DELETE"
  | "LOGIN"
  | "LOGOUT"
  | "EXPORT"
  | "SIGN";

export interface AuditLogResponse {
  id: string;
  tenant_id: string;
  user_id?: string | null;
  user_email?: string | null;
  action: AuditAction;
  resource_type?: string;
  resource_id?: string | null;
  ip_address?: string;
  user_agent?: string;
  details?: Record<string, unknown>;
  created_at: string;
}

// ─── CDSS ─────────────────────────────────────────────────────────────────────

export type CdssSeverity = "CONTRAINDICATED" | "MAJOR" | "MODERATE" | "MINOR";

export interface CdssDrugInput {
  drug_id?: string;
  ingredient?: string;
  atc_code?: string;
}

export interface CdssCheckRequest {
  patient_id?: string;
  encounter_id?: string;
  prescription_id?: string;
  items: CdssDrugInput[];
}

export interface CdssAlertResponse {
  rule_type: string;
  rule_code?: string;
  severity: CdssSeverity;
  is_interruptive: boolean;
  title: string;
  detail: string;
  management?: string;
  drug_refs: string[];
}

export interface CdssCheckResponse {
  alerts: CdssAlertResponse[];
  has_interruptive: boolean;
}

export interface CdssOverrideRequest {
  prescription_id?: string;
  encounter_id?: string;
  rule_type: string;
  rule_code?: string;
  severity: CdssSeverity;
  override_reason: string;
  reason_code?: string;
}

export interface CdssOverrideResponse {
  id: string;
}

// ─── Dashboard xu hướng ĐTĐ ───────────────────────────────────────────────────

export type DiabetesTargetParam =
  | "HBA1C"
  | "BP_SYS"
  | "BP_DIA"
  | "LDL"
  | "EGFR"
  | string;

export interface DiabetesTrendPoint {
  assessed_at: string;
  hba1c?: number | null;
  fasting_glucose?: number | null;
  egfr?: number | null;
  bp_systolic?: number | null;
  bp_diastolic?: number | null;
  bmi?: number | null;
}

export interface CarePathwayTargetItem {
  param: DiabetesTargetParam;
  target_op: string;
  target_value: number;
  unit?: string | null;
}

export interface DiabetesTrajectoryResponse {
  patient_id: string;
  series: DiabetesTrendPoint[];
  targets: CarePathwayTargetItem[];
}

export type DeteriorationSeverity = "HIGH" | "MEDIUM" | "LOW" | string;

export interface DeteriorationFlag {
  code: string;
  message: string;
  severity: DeteriorationSeverity;
}

export interface DeteriorationFlagsResponse {
  patient_id: string;
  flags: DeteriorationFlag[];
}

export type RiskLevel = "HIGH" | "MEDIUM" | "LOW";

export interface RiskListItem {
  patient_id: string;
  patient_code: string;
  patient_full_name: string;
  phone?: string | null;
  risk_level: RiskLevel;
  risk_score: number;
  latest_hba1c?: number | null;
  latest_egfr?: number | null;
  latest_bp_sys?: number | null;
  latest_bp_dia?: number | null;
  hba1c_trend?: string | null;
  last_visit_at?: string | null;
  computed_at: string;
}

export interface RiskListParams {
  level?: RiskLevel | "ALL";
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface ListMeta {
  page: number;
  page_size: number;
  total: number;
}

export interface RiskListResponse {
  data: RiskListItem[];
  meta: ListMeta;
}

export interface CarePathwayTargetDto {
  param: string;
  target_op: string;
  target_value: number;
  unit?: string | null;
  note?: string | null;
}

// ─── Recall / Nhắc tái khám ───────────────────────────────────────────────────

export type RecallStatus = "PENDING" | "CONTACTED" | "SCHEDULED" | "DONE" | "DISMISSED";
export type RecallPriority = "NORMAL" | "HIGH" | "URGENT" | string;

export interface RecallItem {
  id: string;
  patient_id: string;
  patient_code: string;
  patient_full_name: string;
  phone?: string | null;
  recall_type: string;
  due_date?: string | null;
  priority: RecallPriority;
  status: RecallStatus;
  channel?: string | null;
  note?: string | null;
  contacted_at?: string | null;
  created_at: string;
}

export interface RecallListParams {
  status?: RecallStatus | "ALL";
  dueBefore?: string;
  page?: number;
  pageSize?: number;
}

export interface RecallListResponse {
  data: RecallItem[];
  meta: ListMeta;
}

export interface UpdateRecallStatusRequest {
  status: RecallStatus;
  note?: string;
  channel?: string;
}

export interface NotifyRecallRequest {
  channel?: string;
}

export interface NotifyRecallResponse {
  notified: boolean;
  channel: string;
}

// ─── AI Suggestion ────────────────────────────────────────────────────────────

export interface GuidelineRecommendation {
  code: string;
  text: string;
  source: string;
}

export interface TreatmentSuggestionRequest {
  encounter_id?: string;
}

export interface TreatmentSuggestionResponse {
  log_id: string;
  disclaimer_text: string;
  body_text: string;
  fallback_used: boolean;
  rule_derived: GuidelineRecommendation[];
}

export type AiSuggestionStatus = "ACCEPTED" | "REJECTED" | "EDITED";

export interface UpdateAiSuggestionStatusRequest {
  status: AiSuggestionStatus;
}
