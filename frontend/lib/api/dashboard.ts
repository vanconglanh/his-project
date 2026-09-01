import apiClient from "./client";

// ---- Types ----

/**
 * Khớp với backend DashboardOverviewResponse (flat, snake_case).
 * GET /api/v1/dashboard/overview → { data: DashboardOverview }
 */
export interface DashboardOverview {
  today_encounters: number;
  waiting_patients: number;
  today_revenue: number;
  low_stock_alerts: number;
  near_expiry_alerts: number;
  bhyt_pending_count: number;
  dtqg_failed_count: number;
}

/**
 * Khớp với backend ChartDataPoint record: { label, value, color? }.
 * FE cũ dùng secondary_value — đã đổi thành color để khớp BE.
 */
export interface ChartDataPoint {
  label: string;
  value: number;
  color?: string | null;
}

/**
 * Khớp với shape backend chart endpoints trả về:
 * { data: { series: ChartDataPoint[] } }
 * FE cũ dùng "points" — đã đổi thành "series" để khớp BE.
 */
export interface ChartResponse {
  chart_type: "line" | "bar" | "pie" | "histogram";
  x_label: string;
  y_label: string;
  series: ChartDataPoint[];
}

export interface AlertItem {
  id: string;
  type: "LOW_STOCK" | "NEAR_EXPIRY" | "ENCOUNTER_OVER_12H" | "BHYT_PENDING" | "DTQG_FAILED";
  severity: "INFO" | "WARNING" | "CRITICAL";
  message: string;
  link?: string | null;
  count: number;
  created_at: string;
}

// ---- Mock helpers ----

function mockRevenueTrend(days = 30): ChartDataPoint[] {
  return Array.from({ length: days }, (_, i) => {
    const d = new Date();
    d.setDate(d.getDate() - (days - 1 - i));
    return {
      label: `${d.getDate()}/${d.getMonth() + 1}`,
      value: Math.round(3_000_000 + Math.random() * 7_000_000),
    };
  });
}

function mockEncounterTrend(days = 30): ChartDataPoint[] {
  return Array.from({ length: days }, (_, i) => {
    const d = new Date();
    d.setDate(d.getDate() - (days - 1 - i));
    return {
      label: `${d.getDate()}/${d.getMonth() + 1}`,
      value: Math.round(10 + Math.random() * 40),
    };
  });
}

// ---- API functions ----

export async function getDashboardOverview(): Promise<DashboardOverview> {
  try {
    const { data } = await apiClient.get<{ data: DashboardOverview }>("/dashboard/overview");
    return data.data;
  } catch {
    // Graceful fallback mock — shape khớp DashboardOverviewResponse backend
    return {
      today_encounters: 38,
      waiting_patients: 7,
      today_revenue: 12_500_000,
      low_stock_alerts: 2,
      near_expiry_alerts: 1,
      bhyt_pending_count: 3,
      dtqg_failed_count: 0,
    };
  }
}

export async function getRevenueTrend(range: "7d" | "30d" | "90d" = "30d"): Promise<ChartResponse> {
  try {
    const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/revenue-trend", {
      params: { range },
    });
    return data.data;
  } catch {
    const days = range === "7d" ? 7 : range === "90d" ? 90 : 30;
    return { chart_type: "line", x_label: "Ngày", y_label: "VND", series: mockRevenueTrend(days) };
  }
}

export async function getEncountersTrend(range: "7d" | "30d" | "90d" = "30d"): Promise<ChartResponse> {
  try {
    const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/encounters-trend", {
      params: { range },
    });
    return data.data;
  } catch {
    const days = range === "7d" ? 7 : range === "90d" ? 90 : 30;
    return { chart_type: "bar", x_label: "Ngày", y_label: "Lượt", series: mockEncounterTrend(days) };
  }
}

export async function getTopDoctors(range: "7d" | "30d" | "90d" = "30d", top = 10): Promise<ChartResponse> {
  try {
    const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/top-doctors", {
      params: { range, top },
    });
    return data.data;
  } catch {
    const names = ["BS. Nguyễn A", "BS. Trần B", "BS. Lê C", "BS. Phạm D", "BS. Hoàng E",
      "BS. Vũ F", "BS. Đặng G", "BS. Bùi H", "BS. Đỗ I", "BS. Ngô K"];
    return {
      chart_type: "bar", x_label: "Bác sĩ", y_label: "VND",
      series: names.slice(0, top).map((name) => ({
        label: name,
        value: Math.round(5_000_000 + Math.random() * 20_000_000),
      })),
    };
  }
}

export async function getTopDrugs(range: "7d" | "30d" | "90d" = "30d", top = 10): Promise<ChartResponse> {
  try {
    const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/top-drugs", {
      params: { range, top },
    });
    return data.data;
  } catch {
    const drugs = ["Metformin 500mg", "Glibenclamide 5mg", "Insulin Glargine", "Sitagliptin 100mg",
      "Atorvastatin 20mg", "Amlodipine 5mg", "Losartan 50mg", "Aspirin 100mg",
      "Empagliflozin 10mg", "Linagliptin 5mg"];
    return {
      chart_type: "bar", x_label: "Thuốc", y_label: "VND",
      series: drugs.slice(0, top).map((name) => ({
        label: name,
        value: Math.round(1_000_000 + Math.random() * 8_000_000),
      })),
    };
  }
}

export async function getHba1cDistribution(): Promise<ChartResponse> {
  try {
    const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/diabetes-hba1c");
    return data.data;
  } catch {
    const bins = ["<6", "6-6.5", "6.5-7", "7-7.5", "7.5-8", "8-8.5", "8.5-9", "9-9.5", "9.5-10", ">10"];
    return {
      chart_type: "histogram", x_label: "HbA1c (%)", y_label: "Bệnh nhân",
      series: bins.map((label) => ({ label, value: Math.round(5 + Math.random() * 40) })),
    };
  }
}

export async function getDashboardAlerts(severity?: string): Promise<AlertItem[]> {
  try {
    const { data } = await apiClient.get<{ data: AlertItem[] }>("/dashboard/alerts", {
      params: severity ? { severity } : {},
    });
    return data.data;
  } catch {
    return [
      { id: "1", type: "LOW_STOCK", severity: "WARNING", message: "Metformin 500mg tồn kho thấp (< 50 viên)", link: "/pharmacy", count: 2, created_at: new Date().toISOString() },
      { id: "2", type: "NEAR_EXPIRY", severity: "WARNING", message: "3 lô thuốc sắp hết hạn trong 30 ngày", link: "/pharmacy", count: 3, created_at: new Date().toISOString() },
      { id: "3", type: "DTQG_FAILED", severity: "CRITICAL", message: "1 đơn thuốc chưa đẩy ĐTQG thành công", link: "/prescriptions", count: 1, created_at: new Date().toISOString() },
    ];
  }
}
