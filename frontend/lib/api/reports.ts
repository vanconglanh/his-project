import apiClient from "./client";

// ---- Types ----

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
