import apiClient from "./client";

// ---- Report Engine (config-driven, /reports/catalog + /reports/{code}/*) ----
// Nguồn chân lý: docs/api/reports-engine-contract.md

export interface ReportFilterDescriptor {
  key: string;
  label: string;
  /** "Select" | "Enum" | "MultiSelect" (mở rộng) — dùng string để không vỡ khi BE thêm loại mới */
  type: string;
  options_source?: string | null;
  required?: boolean;
}

export type ReportOrientation = "Portrait" | "Landscape";

export type ReportViewType = "TABLE" | "CHART";
export type ReportChartType = "bar" | "line" | "area" | "pie";

export interface ReportChartConfig {
  type: ReportChartType;
  dims: string[];
  measure: string;
}

export interface ReportCatalogItem {
  code: string;
  title: string;
  group: string;
  group_order: number;
  icon?: string | null;
  orientation: ReportOrientation;
  group_by_key?: string | null;
  filters: ReportFilterDescriptor[];
  view_type: ReportViewType;
  chart: ReportChartConfig | null;
}

export type ReportColumnType = "Text" | "Money" | "Number" | "Date" | "DateTime" | "Enum";
export type ReportColumnAlign = "Left" | "Right" | "Center";

export interface ReportColumn {
  key: string;
  label: string;
  type: ReportColumnType;
  align: ReportColumnAlign;
  width?: number;
  is_group_subtotal?: boolean;
}

export type ReportCellValue = string | number | boolean | null | undefined;
export type ReportRow = Record<string, ReportCellValue>;

export interface ReportGroupData {
  key: string;
  label: string;
  count: number;
  rows: ReportRow[];
  subtotals: Record<string, number>;
}

export type ReportKpiTintToken = "brand" | "done" | "warning" | "critical" | "insurance" | "neutral";

export interface ReportKpi {
  label: string;
  tint?: string | null;
  tint_token?: ReportKpiTintToken | null;
  value: number;
  is_money: boolean;
}

export interface ReportDataPayload {
  columns: ReportColumn[];
  groups: ReportGroupData[] | null;
  rows: ReportRow[] | null;
  totals: Record<string, number>;
  kpis: ReportKpi[];
}

export interface ReportEngineMeta {
  page: number;
  page_size: number;
  total: number;
}

export interface ReportDataResult {
  data: ReportDataPayload;
  meta: ReportEngineMeta;
}

export interface ReportOption {
  value: string;
  label: string;
}

export interface ReportDataQueryParams {
  from: string;
  to: string;
  page?: number;
  page_size?: number;
  [extraFilterKey: string]: string | number | undefined;
}

export type ReportExportFormat = "pdf" | "excel";

export interface ReportExportResult {
  blob: Blob;
  fileName: string;
}

/** GET /reports/catalog — danh mục toàn bộ báo cáo config-driven (sắp xếp theo group/group_order). */
export async function getReportCatalog(): Promise<ReportCatalogItem[]> {
  const { data } = await apiClient.get<{ data: ReportCatalogItem[] }>("/reports/catalog");
  return data.data;
}

/** GET /reports/{code}/data — lấy dữ liệu lưới (group hoặc phẳng) theo khoảng ngày + filter riêng. */
export async function getReportData(
  code: string,
  params: ReportDataQueryParams
): Promise<ReportDataResult> {
  const { data } = await apiClient.get<{ data: ReportDataPayload; meta: ReportEngineMeta }>(
    `/reports/${code}/data`,
    { params }
  );
  return { data: data.data, meta: data.meta };
}

/** GET /reports/options/{source} — danh sách lựa chọn cho filter dropdown (collectors, counters, ...). */
export async function getReportOptions(source: string): Promise<ReportOption[]> {
  const { data } = await apiClient.get<{ data: ReportOption[] }>(`/reports/options/${source}`);
  return data.data;
}

function parseFileNameFromDisposition(disposition: string | undefined, fallback: string): string {
  if (!disposition) return fallback;
  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1]);
    } catch {
      return utf8Match[1];
    }
  }
  const plainMatch = /filename="?([^";]+)"?/i.exec(disposition);
  return plainMatch?.[1] ?? fallback;
}

/**
 * GET /reports/{code}/export — xuất PDF/Excel toàn bộ dữ liệu trong kỳ (không phân trang).
 * Trả về Blob + tên file gợi ý (ưu tiên Content-Disposition, fallback code-from-to.ext).
 */
export async function exportReportEngine(
  code: string,
  params: Omit<ReportDataQueryParams, "page" | "page_size">,
  format: ReportExportFormat
): Promise<ReportExportResult> {
  const res = await apiClient.get(`/reports/${code}/export`, {
    params: { ...params, format },
    responseType: "blob",
  });
  const contentType = format === "pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
  const blob = res.data instanceof Blob ? res.data : new Blob([res.data], { type: contentType });
  const ext = format === "pdf" ? "pdf" : "xlsx";
  const fallbackName = `${code}-${params.from}-${params.to}.${ext}`;
  const disposition = res.headers?.["content-disposition"] as string | undefined;
  const fileName = parseFileNameFromDisposition(disposition, fallbackName);
  return { blob, fileName };
}

// ---- Report Builder (self-service, /reports/datasets + /reports/definitions + /reports/preview) ----

export type DatasetFieldRole = "DIMENSION" | "MEASURE";
export type DatasetFieldDataType = "Money" | "Number" | "Date" | "DateTime" | "Text" | "Enum";
export type ReportAggregation = "SUM" | "COUNT" | "AVG" | "MIN" | "MAX" | "COUNT_DISTINCT";

export interface DatasetField {
  key: string;
  label: string;
  role: DatasetFieldRole;
  data_type: DatasetFieldDataType;
  aggregations: ReportAggregation[];
}

export interface ReportDataset {
  key: string;
  label: string;
  fields: DatasetField[];
}

export type ReportFilterOp = "=" | "<>" | "in" | "between" | "like" | ">" | "<" | ">=" | "<=";

export interface ReportDefinitionColumn {
  field: string;
  label: string;
  agg: ReportAggregation | null;
  is_subtotal: boolean;
}

export interface ReportDefinitionFilter {
  field: string;
  op: ReportFilterOp;
  value: string[];
}

export interface ReportDefinitionSort {
  field: string;
  desc: boolean;
}

export interface ReportDefinitionKpi {
  label: string;
  field: string;
  agg: ReportAggregation;
}

/** Cột tính toán tự định nghĩa — formula chỉ dùng field measure của dataset + số + `+ - * / ( )`. */
export interface ReportCalcField {
  key: string;
  label: string;
  formula: string;
  data_type: DatasetFieldDataType;
}

export interface ReportDefinitionBody {
  columns: ReportDefinitionColumn[];
  filters: ReportDefinitionFilter[];
  group_by: string[];
  sort: ReportDefinitionSort[];
  kpis: ReportDefinitionKpi[];
  calc_fields: ReportCalcField[];
}

export type ReportVisibility = "TENANT" | "PRIVATE" | "ROLE";

/** Vai trò hệ thống (code snake_case dùng cho chia sẻ báo cáo theo vai trò). */
export type ReportRoleCode = "admin" | "bac_si" | "le_tan" | "duoc_si" | "ke_toan" | "ky_thuat_vien";

export interface SaveReportDefinitionRequest {
  title: string;
  dataset_key: string;
  definition: ReportDefinitionBody;
  chart: ReportChartConfig | null;
  view_type: ReportViewType;
  visibility: ReportVisibility;
  shared_roles: ReportRoleCode[];
}

export interface ReportDefinition extends SaveReportDefinitionRequest {
  id: number | string;
  code: string;
  created_at?: string;
  updated_at?: string;
  owner_id?: number | string | null;
}

/** GET /reports/datasets — 4 dataset khả dụng cho Trình tạo báo cáo (thu-ngan/luot-kham/kho/don-thuoc). */
export async function getReportDatasets(): Promise<ReportDataset[]> {
  const { data } = await apiClient.get<{ data: ReportDataset[] }>("/reports/datasets");
  return data.data;
}

/** GET /reports/definitions — danh sách báo cáo tự tạo của tenant hiện tại. */
export async function listReportDefinitions(): Promise<ReportDefinition[]> {
  const { data } = await apiClient.get<{ data: ReportDefinition[] }>("/reports/definitions");
  return data.data;
}

/** POST /reports/definitions — lưu báo cáo tự tạo mới. */
export async function saveReportDefinition(body: SaveReportDefinitionRequest): Promise<ReportDefinition> {
  const { data } = await apiClient.post<{ data: ReportDefinition }>("/reports/definitions", body);
  return data.data;
}

/** PUT /reports/definitions/{id} — cập nhật báo cáo tự tạo (chủ sở hữu/admin). */
export async function updateReportDefinition(
  id: number | string,
  body: SaveReportDefinitionRequest
): Promise<ReportDefinition> {
  const { data } = await apiClient.put<{ data: ReportDefinition }>(`/reports/definitions/${id}`, body);
  return data.data;
}

/** DELETE /reports/definitions/{id} — xoá báo cáo tự tạo (chủ sở hữu/admin). */
export async function deleteReportDefinition(id: number | string): Promise<void> {
  await apiClient.delete(`/reports/definitions/${id}`);
}

export interface PreviewReportDefinitionBody {
  dataset_key: string;
  definition: ReportDefinitionBody;
  chart: ReportChartConfig | null;
}

/** POST /reports/preview?from=&to= — xem trước báo cáo đang xây (LIMIT 200), không lưu. */
export async function previewReportDefinition(
  params: { from: string; to: string },
  body: PreviewReportDefinitionBody
): Promise<ReportDataResult> {
  const { data } = await apiClient.post<{ data: ReportDataPayload; meta: ReportEngineMeta }>(
    "/reports/preview",
    body,
    { params }
  );
  return { data: data.data, meta: data.meta };
}

// ---- Dashboard (P2.2, ghép nhiều báo cáo đã lưu thành lưới widget) ----

export type DashboardWidgetType = "TABLE" | "CHART" | "KPI";

export interface ReportDashboardWidget {
  report_code: string;
  title: string;
  widget_type: DashboardWidgetType;
  w: number;
  h: number;
  x: number;
  y: number;
}

export interface ReportDashboard {
  id: number | string;
  code: string;
  title: string;
  widgets: ReportDashboardWidget[];
  visibility: ReportVisibility;
}

export interface SaveReportDashboardRequest {
  title: string;
  widgets: ReportDashboardWidget[];
  visibility: ReportVisibility;
}

export interface ReportDashboardWidgetData {
  report_code: string;
  title: string;
  widget_type: DashboardWidgetType;
  payload: ReportDataPayload;
}

export interface ReportDashboardData {
  title: string;
  widgets: ReportDashboardWidgetData[];
}

/** GET /reports/dashboards — danh sách dashboard khả dụng (sở hữu + chia sẻ) của tenant hiện tại. */
export async function listReportDashboards(): Promise<ReportDashboard[]> {
  const { data } = await apiClient.get<{ data: ReportDashboard[] }>("/reports/dashboards");
  return data.data;
}

/** GET /reports/dashboards/{id} — chi tiết cấu hình 1 dashboard (dùng để nạp lại builder khi sửa). */
export async function getReportDashboard(id: number | string): Promise<ReportDashboard> {
  const { data } = await apiClient.get<{ data: ReportDashboard }>(`/reports/dashboards/${id}`);
  return data.data;
}

/** POST /reports/dashboards — tạo dashboard mới. */
export async function createReportDashboard(body: SaveReportDashboardRequest): Promise<ReportDashboard> {
  const { data } = await apiClient.post<{ data: ReportDashboard }>("/reports/dashboards", body);
  return data.data;
}

/** PUT /reports/dashboards/{id} — cập nhật dashboard. */
export async function updateReportDashboard(
  id: number | string,
  body: SaveReportDashboardRequest
): Promise<ReportDashboard> {
  const { data } = await apiClient.put<{ data: ReportDashboard }>(`/reports/dashboards/${id}`, body);
  return data.data;
}

/** DELETE /reports/dashboards/{id} — xoá dashboard. */
export async function deleteReportDashboard(id: number | string): Promise<void> {
  await apiClient.delete(`/reports/dashboards/${id}`);
}

/** GET /reports/dashboards/{id}/data?from=&to= — dữ liệu đã render cho toàn bộ widget trong dashboard. */
export async function getReportDashboardData(
  id: number | string,
  params: { from: string; to: string }
): Promise<ReportDashboardData> {
  const { data } = await apiClient.get<{ data: ReportDashboardData }>(`/reports/dashboards/${id}/data`, { params });
  return data.data;
}

// ---- Lịch gửi báo cáo qua email (P3.3) ----

export type ReportScheduleFrequency = "DAILY" | "WEEKLY" | "MONTHLY";
export type ReportSchedulePeriod = "TODAY" | "THIS_WEEK" | "THIS_MONTH" | "LAST_MONTH";
export type ReportScheduleFormat = "PDF" | "EXCEL";

export interface ReportSchedule {
  id: number | string;
  report_code: string;
  title: string;
  frequency: ReportScheduleFrequency;
  hour: number;
  day_of_week?: number | null;
  day_of_month?: number | null;
  period: ReportSchedulePeriod;
  format: ReportScheduleFormat;
  recipients: string[];
  enabled: boolean;
}

export type SaveReportScheduleRequest = Omit<ReportSchedule, "id">;

/** GET /reports/schedules — danh sách lịch gửi báo cáo tự động của tenant hiện tại. */
export async function listReportSchedules(): Promise<ReportSchedule[]> {
  const { data } = await apiClient.get<{ data: ReportSchedule[] }>("/reports/schedules");
  return data.data;
}

/** GET /reports/schedules/{id} — chi tiết 1 lịch gửi báo cáo. */
export async function getReportSchedule(id: number | string): Promise<ReportSchedule> {
  const { data } = await apiClient.get<{ data: ReportSchedule }>(`/reports/schedules/${id}`);
  return data.data;
}

/** POST /reports/schedules — tạo lịch gửi báo cáo mới. */
export async function createReportSchedule(body: SaveReportScheduleRequest): Promise<ReportSchedule> {
  const { data } = await apiClient.post<{ data: ReportSchedule }>("/reports/schedules", body);
  return data.data;
}

/** PUT /reports/schedules/{id} — cập nhật lịch gửi báo cáo. */
export async function updateReportSchedule(
  id: number | string,
  body: SaveReportScheduleRequest
): Promise<ReportSchedule> {
  const { data } = await apiClient.put<{ data: ReportSchedule }>(`/reports/schedules/${id}`, body);
  return data.data;
}

/** DELETE /reports/schedules/{id} — xoá lịch gửi báo cáo. */
export async function deleteReportSchedule(id: number | string): Promise<void> {
  await apiClient.delete(`/reports/schedules/${id}`);
}

// ---- Types (dashboard cũ) ----

export type ReportPeriod = "DAY" | "WEEK" | "MONTH" | "YEAR";

export interface RevenueBreakdownItem {
  period_label: string;
  total: number;
  secondary_value?: number | null;
}

export interface RevenueReport {
  total: number;
  currency: string;
  by_breakdown: RevenueBreakdownItem[];
}

export interface BreakdownItem {
  label: string;
  value: number;
  count?: number | null;
  percentage?: number | null;
}

export interface DoctorKpi {
  doctor_id: string;
  name: string;
  encounter_count: number;
  revenue: number;
  avg_revenue_per_encounter: number;
  rvu: number;
}

export interface EncounterCountItem {
  period_label: string;
  group_key?: string | null;
  count: number;
}

export interface TopDiagnosis {
  icd10_code: string;
  icd10_name: string;
  count: number;
  percentage: number;
}

export interface DiabetesCohort {
  as_of: string;
  total_patients: number;
  by_type: { t1: number; t2: number; gdm: number };
  hba1c_distribution: { lt_7: number; between_7_8: number; between_8_9: number; gt_9: number };
  complications: { retinopathy: number; neuropathy: number; nephropathy: number; cad: number; pad: number };
}

export interface TopDrug {
  drug_id: string;
  drug_code: string;
  drug_name: string;
  quantity_sold: number;
  revenue: number;
  prescription_count: number;
}

export interface InventoryValue {
  total_value: number;
  total_skus: number;
  by_category: { category: string; value: number; sku_count: number }[];
}

export interface NearExpirySummary {
  total_lots: number;
  total_value_at_risk: number;
  by_bucket: { bucket: string; lot_count: number; value: number }[];
}

export type ExportReportType =
  | "REVENUE" | "REVENUE_BY_DOCTOR" | "REVENUE_BY_SERVICE" | "CASHIER_DAILY"
  | "DEBTS_AGING" | "BHYT_SUMMARY" | "ENCOUNTERS_COUNT" | "TOP_DIAGNOSES"
  | "DIABETES_COHORT" | "TOP_DRUGS" | "INVENTORY_VALUE";

export type ExportFormat = "CSV" | "EXCEL" | "PDF";

export interface ExportReportRequest {
  report_type: ExportReportType;
  format: ExportFormat;
  filters?: Record<string, unknown>;
}

export interface ExportReportResult {
  file_url: string;
  file_name: string;
  expires_at: string;
}

// ---- Mock helpers ----

function mockRevenueSeries(from: string, to: string, period: ReportPeriod): RevenueBreakdownItem[] {
  const start = new Date(from);
  const end = new Date(to);
  const result: RevenueBreakdownItem[] = [];
  const cur = new Date(start);
  while (cur <= end) {
    result.push({
      period_label: cur.toISOString().slice(0, 10),
      total: Math.round(3_000_000 + Math.random() * 8_000_000),
    });
    if (period === "DAY") cur.setDate(cur.getDate() + 1);
    else if (period === "WEEK") cur.setDate(cur.getDate() + 7);
    else if (period === "MONTH") cur.setMonth(cur.getMonth() + 1);
    else cur.setFullYear(cur.getFullYear() + 1);
    if (result.length >= 60) break;
  }
  return result;
}

// ---- Financial ----

export async function getRevenueReport(
  period: ReportPeriod,
  from: string,
  to: string,
  clinic_id?: string
): Promise<RevenueReport> {
  try {
    const { data } = await apiClient.get<{ data: RevenueReport }>("/reports/revenue", {
      params: { period, from, to, ...(clinic_id ? { clinic_id } : {}) },
    });
    return data.data;
  } catch {
    const by_breakdown = mockRevenueSeries(from, to, period);
    return { total: by_breakdown.reduce((s, x) => s + x.total, 0), currency: "VND", by_breakdown };
  }
}

export async function getRevenueByPaymentMethod(from: string, to: string): Promise<BreakdownItem[]> {
  try {
    const { data } = await apiClient.get<{ data: BreakdownItem[] }>("/reports/revenue/by-payment-method", {
      params: { from, to },
    });
    return data.data;
  } catch {
    return [
      { label: "Tiền mặt", value: 45_000_000, percentage: 55 },
      { label: "Chuyển khoản", value: 25_000_000, percentage: 30 },
      { label: "Thẻ", value: 8_000_000, percentage: 10 },
      { label: "QR", value: 4_000_000, percentage: 5 },
    ];
  }
}

export async function getTopDoctorsReport(from: string, to: string, top = 20): Promise<DoctorKpi[]> {
  try {
    const { data } = await apiClient.get<{ data: DoctorKpi[] }>("/reports/revenue/by-doctor", {
      params: { from, to, top },
    });
    return data.data;
  } catch {
    return Array.from({ length: 5 }, (_, i) => ({
      doctor_id: `doc-${i}`,
      name: `BS. Bác sĩ ${i + 1}`,
      encounter_count: Math.round(50 + Math.random() * 150),
      revenue: Math.round(10_000_000 + Math.random() * 50_000_000),
      avg_revenue_per_encounter: Math.round(200_000 + Math.random() * 300_000),
      rvu: Math.round(100 + Math.random() * 400),
    }));
  }
}

// ---- Clinical ----

export async function getEncountersCount(
  period: "DAY" | "WEEK" | "MONTH",
  from: string,
  to: string
): Promise<EncounterCountItem[]> {
  try {
    const { data } = await apiClient.get<{ data: EncounterCountItem[] }>("/reports/encounters/count", {
      params: { period, from, to },
    });
    return data.data;
  } catch {
    return mockRevenueSeries(from, to, period).map((x) => ({
      period_label: x.period_label,
      count: Math.round(10 + Math.random() * 50),
    }));
  }
}

export async function getTopDiagnoses(from: string, to: string, top = 20): Promise<TopDiagnosis[]> {
  try {
    const { data } = await apiClient.get<{ data: TopDiagnosis[] }>("/reports/diagnoses/top", {
      params: { from, to, top },
    });
    return data.data;
  } catch {
    const mock = [
      { icd10_code: "E11", icd10_name: "Đái tháo đường type 2", count: 245, percentage: 28.3 },
      { icd10_code: "I10", icd10_name: "Tăng huyết áp nguyên phát", count: 180, percentage: 20.8 },
      { icd10_code: "E78", icd10_name: "Rối loạn chuyển hóa lipoprotein", count: 120, percentage: 13.9 },
      { icd10_code: "E11.4", icd10_name: "ĐTĐ type 2 có biến chứng thần kinh", count: 95, percentage: 11.0 },
      { icd10_code: "N18", icd10_name: "Bệnh thận mạn tính", count: 78, percentage: 9.0 },
    ];
    return mock.slice(0, top);
  }
}

export async function getDiabetesCohort(as_of?: string): Promise<DiabetesCohort> {
  try {
    const { data } = await apiClient.get<{ data: DiabetesCohort }>("/reports/diabetes/cohort", {
      params: { as_of, dm_type: "ALL" },
    });
    return data.data;
  } catch {
    return {
      as_of: as_of ?? new Date().toISOString().slice(0, 10),
      total_patients: 412,
      by_type: { t1: 45, t2: 348, gdm: 19 },
      hba1c_distribution: { lt_7: 120, between_7_8: 145, between_8_9: 98, gt_9: 49 },
      complications: { retinopathy: 87, neuropathy: 134, nephropathy: 62, cad: 44, pad: 31 },
    };
  }
}

// ---- Pharmacy ----

export async function getTopPharmacyDrugs(from: string, to: string, top = 20): Promise<TopDrug[]> {
  try {
    const { data } = await apiClient.get<{ data: TopDrug[] }>("/reports/pharmacy/top-drugs", {
      params: { from, to, top, order_by: "REVENUE" },
    });
    return data.data;
  } catch {
    const names = ["Metformin 500mg", "Glibenclamide 5mg", "Insulin Glargine U-300",
      "Sitagliptin 100mg", "Atorvastatin 20mg", "Amlodipine 5mg", "Losartan 50mg",
      "Aspirin 100mg", "Empagliflozin 10mg", "Linagliptin 5mg"];
    return names.slice(0, top).map((name, i) => ({
      drug_id: `drug-${i}`,
      drug_code: `DT${String(i + 1).padStart(4, "0")}`,
      drug_name: name,
      quantity_sold: Math.round(100 + Math.random() * 900),
      revenue: Math.round(1_000_000 + Math.random() * 10_000_000),
      prescription_count: Math.round(20 + Math.random() * 100),
    }));
  }
}

export async function getInventoryValue(): Promise<InventoryValue> {
  try {
    const { data } = await apiClient.get<{ data: InventoryValue }>("/reports/pharmacy/inventory-value");
    return data.data;
  } catch {
    return {
      total_value: 185_000_000,
      total_skus: 234,
      by_category: [
        { category: "Thuốc nội tiết", value: 68_000_000, sku_count: 42 },
        { category: "Thuốc tim mạch", value: 45_000_000, sku_count: 58 },
        { category: "Kháng sinh", value: 32_000_000, sku_count: 67 },
        { category: "Vitamin & TPCN", value: 24_000_000, sku_count: 38 },
        { category: "Khác", value: 16_000_000, sku_count: 29 },
      ],
    };
  }
}

export async function getNearExpirySummary(): Promise<NearExpirySummary> {
  try {
    const { data } = await apiClient.get<{ data: NearExpirySummary }>("/reports/pharmacy/near-expiry-summary");
    return data.data;
  } catch {
    return {
      total_lots: 18,
      total_value_at_risk: 12_500_000,
      by_bucket: [
        { bucket: "0-30d", lot_count: 4, value: 3_200_000 },
        { bucket: "31-60d", lot_count: 7, value: 5_100_000 },
        { bucket: "61-90d", lot_count: 7, value: 4_200_000 },
      ],
    };
  }
}

// ---- Report Code ----

export type PrintReportType = "financial" | "clinical" | "pharmacy";

export async function reserveReportCode(type: PrintReportType): Promise<string> {
  const { data } = await apiClient.post<{ data: { reportCode: string } }>(
    `/reports/${type}/code`
  );
  return data.data.reportCode;
}

// ---- Export ----

export async function exportReport(req: ExportReportRequest): Promise<ExportReportResult> {
  const { data } = await apiClient.post<{ data: ExportReportResult }>("/reports/export", req);
  return data.data;
}
