import { useQuery, useMutation } from "@tanstack/react-query";
import {
  getRevenueReport,
  getRevenueByPaymentMethod,
  getTopDoctorsReport,
  getEncountersCount,
  getTopDiagnoses,
  getDiabetesCohort,
  getTopPharmacyDrugs,
  getInventoryValue,
  getNearExpirySummary,
  exportReport,
  getReportCatalog,
  getReportData,
  getReportOptions,
  type ReportPeriod,
  type ExportReportRequest,
  type ReportDataQueryParams,
} from "@/lib/api/reports";

// ---- Report Engine (config-driven) ----

/** Danh mục toàn bộ báo cáo — cache dài (5 phút), hiếm khi đổi trong phiên làm việc. */
export function useReportCatalog() {
  return useQuery({
    queryKey: ["reports", "engine", "catalog"],
    queryFn: getReportCatalog,
    staleTime: 5 * 60 * 1000,
    retry: 1,
  });
}

/** Danh sách lựa chọn cho 1 filter dropdown (collectors, counters, doctors, clinics, patients...). */
export function useReportOptions(source?: string | null) {
  return useQuery({
    queryKey: ["reports", "engine", "options", source],
    queryFn: () => getReportOptions(source as string),
    enabled: !!source,
    staleTime: 5 * 60 * 1000,
    retry: 1,
  });
}

/**
 * Dữ liệu lưới của 1 báo cáo theo code + tham số ĐÃ ÁP DỤNG (applied filters).
 * QUAN TRỌNG: `params` phải là state riêng chỉ cập nhật khi user bấm "Lấy dữ liệu"
 * — không auto-fetch mỗi lần đổi filter nháp (draft).
 */
export function useReportData(code: string | null, params: ReportDataQueryParams | null) {
  return useQuery({
    queryKey: ["reports", "engine", "data", code, params],
    queryFn: () => getReportData(code as string, params as ReportDataQueryParams),
    enabled: !!code && !!params && !!params.from && !!params.to,
    retry: 1,
  });
}

export function useRevenueReport(period: ReportPeriod, from: string, to: string) {
  return useQuery({
    queryKey: ["reports", "revenue", period, from, to],
    queryFn: () => getRevenueReport(period, from, to),
    enabled: !!from && !!to,
    retry: 1,
  });
}

export function useRevenueByMethod(from: string, to: string) {
  return useQuery({
    queryKey: ["reports", "revenue-by-method", from, to],
    queryFn: () => getRevenueByPaymentMethod(from, to),
    enabled: !!from && !!to,
    retry: 1,
  });
}

export function useTopDoctorsReport(from: string, to: string) {
  return useQuery({
    queryKey: ["reports", "top-doctors", from, to],
    queryFn: () => getTopDoctorsReport(from, to),
    enabled: !!from && !!to,
    retry: 1,
  });
}

export function useEncountersTrend(period: "DAY" | "WEEK" | "MONTH", from: string, to: string) {
  return useQuery({
    queryKey: ["reports", "encounters-count", period, from, to],
    queryFn: () => getEncountersCount(period, from, to),
    enabled: !!from && !!to,
    retry: 1,
  });
}

export function useTopDiagnoses(from: string, to: string) {
  return useQuery({
    queryKey: ["reports", "top-diagnoses", from, to],
    queryFn: () => getTopDiagnoses(from, to),
    enabled: !!from && !!to,
    retry: 1,
  });
}

export function useDiabetesCohort() {
  return useQuery({
    queryKey: ["reports", "diabetes-cohort"],
    queryFn: () => getDiabetesCohort(),
    retry: 1,
    // Provide placeholder data so cohort card renders immediately before API resolves
    placeholderData: {
      as_of: new Date().toISOString().slice(0, 10),
      total_patients: 412,
      by_type: { t1: 45, t2: 348, gdm: 19 },
      hba1c_distribution: { lt_7: 120, between_7_8: 145, between_8_9: 98, gt_9: 49 },
      complications: { retinopathy: 87, neuropathy: 134, nephropathy: 62, cad: 44, pad: 31 },
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
}

export function useTopPharmacyDrugs(from: string, to: string) {
  return useQuery({
    queryKey: ["reports", "top-pharmacy-drugs", from, to],
    queryFn: () => getTopPharmacyDrugs(from, to),
    enabled: !!from && !!to,
    retry: 1,
  });
}

export function useInventoryValue() {
  return useQuery({
    queryKey: ["reports", "inventory-value"],
    queryFn: getInventoryValue,
    retry: 1,
  });
}

export function useNearExpirySummary() {
  return useQuery({
    queryKey: ["reports", "near-expiry"],
    queryFn: getNearExpirySummary,
    retry: 1,
  });
}

export function useExportReport() {
  return useMutation({
    mutationFn: (req: ExportReportRequest) => exportReport(req),
  });
}
