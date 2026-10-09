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

// ---- API functions ----

export async function getDashboardOverview(): Promise<DashboardOverview> {
  const { data } = await apiClient.get<{ data: DashboardOverview }>("/dashboard/overview");
  return data.data;
}

export async function getRevenueTrend(range: "7d" | "30d" | "90d" = "30d"): Promise<ChartResponse> {
  const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/revenue-trend", {
    params: { range },
  });
  return data.data;
}

export async function getEncountersTrend(range: "7d" | "30d" | "90d" = "30d"): Promise<ChartResponse> {
  const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/encounters-trend", {
    params: { range },
  });
  return data.data;
}

export async function getTopDoctors(range: "7d" | "30d" | "90d" = "30d", top = 10): Promise<ChartResponse> {
  const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/top-doctors", {
    params: { range, top },
  });
  return data.data;
}

export async function getTopDrugs(range: "7d" | "30d" | "90d" = "30d", top = 10): Promise<ChartResponse> {
  const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/top-drugs", {
    params: { range, top },
  });
  return data.data;
}

export async function getHba1cDistribution(): Promise<ChartResponse> {
  const { data } = await apiClient.get<{ data: ChartResponse }>("/dashboard/charts/diabetes-hba1c");
  return data.data;
}

export async function getDashboardAlerts(severity?: string): Promise<AlertItem[]> {
  const { data } = await apiClient.get<{ data: AlertItem[] }>("/dashboard/alerts", {
    params: severity ? { severity } : {},
  });
  return data.data;
}
