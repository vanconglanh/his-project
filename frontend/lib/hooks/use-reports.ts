import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
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
  getReportDatasets,
  listReportDefinitions,
  saveReportDefinition,
  updateReportDefinition,
  deleteReportDefinition,
  previewReportDefinition,
  listReportDashboards,
  getReportDashboard,
  createReportDashboard,
  updateReportDashboard,
  deleteReportDashboard,
  getReportDashboardData,
  listReportSchedules,
  getReportSchedule,
  createReportSchedule,
  updateReportSchedule,
  deleteReportSchedule,
  type ReportPeriod,
  type ExportReportRequest,
  type ReportDataQueryParams,
  type SaveReportDefinitionRequest,
  type PreviewReportDefinitionBody,
  type SaveReportDashboardRequest,
  type SaveReportScheduleRequest,
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

// ---- Report Builder (self-service) ----

/** 4 dataset khả dụng cho Trình tạo báo cáo — cache dài, hiếm khi đổi trong phiên làm việc. */
export function useReportDatasets() {
  return useQuery({
    queryKey: ["reports", "builder", "datasets"],
    queryFn: getReportDatasets,
    staleTime: 5 * 60 * 1000,
    retry: 1,
  });
}

/** Danh sách báo cáo tự tạo của tenant hiện tại — dùng cho catalog nhóm "UserDefined" + trang Sửa/Xoá. */
export function useReportDefinitions(enabled = true) {
  return useQuery({
    queryKey: ["reports", "builder", "definitions"],
    queryFn: listReportDefinitions,
    enabled,
    retry: 1,
  });
}

/** Xem trước báo cáo đang xây (không lưu) — gọi thủ công khi bấm "Xem trước", không auto mỗi lần đổi cấu hình. */
export function usePreviewReportDefinition() {
  return useMutation({
    mutationFn: (args: { from: string; to: string; body: PreviewReportDefinitionBody }) =>
      previewReportDefinition({ from: args.from, to: args.to }, args.body),
  });
}

export function useSaveReportDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: SaveReportDefinitionRequest) => saveReportDefinition(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reports", "engine", "catalog"] });
      queryClient.invalidateQueries({ queryKey: ["reports", "builder", "definitions"] });
    },
  });
}

export function useUpdateReportDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { id: number | string; body: SaveReportDefinitionRequest }) =>
      updateReportDefinition(args.id, args.body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reports", "engine", "catalog"] });
      queryClient.invalidateQueries({ queryKey: ["reports", "builder", "definitions"] });
    },
  });
}

export function useDeleteReportDefinition() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number | string) => deleteReportDefinition(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reports", "engine", "catalog"] });
      queryClient.invalidateQueries({ queryKey: ["reports", "builder", "definitions"] });
    },
  });
}

// ---- Dashboard (P2.2) ----

/** Danh sách dashboard đã lưu (sở hữu + chia sẻ) của tenant hiện tại. */
export function useReportDashboards(enabled = true) {
  return useQuery({
    queryKey: ["reports", "dashboards", "list"],
    queryFn: listReportDashboards,
    enabled,
    retry: 1,
  });
}

/** Chi tiết cấu hình 1 dashboard — dùng cho màn hình sửa dashboard. */
export function useReportDashboard(id: string | null) {
  return useQuery({
    queryKey: ["reports", "dashboards", "detail", id],
    queryFn: () => getReportDashboard(id as string),
    enabled: !!id,
    retry: 1,
  });
}

/** Dữ liệu render của toàn bộ widget trong dashboard theo khoảng ngày — dùng cho màn hình xem. */
export function useReportDashboardData(id: string | null, params: { from: string; to: string } | null) {
  return useQuery({
    queryKey: ["reports", "dashboards", "data", id, params],
    queryFn: () => getReportDashboardData(id as string, params as { from: string; to: string }),
    enabled: !!id && !!params?.from && !!params?.to,
    retry: 1,
  });
}

export function useCreateReportDashboard() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: SaveReportDashboardRequest) => createReportDashboard(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reports", "dashboards", "list"] });
    },
  });
}

export function useUpdateReportDashboard() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { id: string; body: SaveReportDashboardRequest }) => updateReportDashboard(args.id, args.body),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ["reports", "dashboards", "list"] });
      queryClient.invalidateQueries({ queryKey: ["reports", "dashboards", "detail", variables.id] });
    },
  });
}

export function useDeleteReportDashboard() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteReportDashboard(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reports", "dashboards", "list"] });
    },
  });
}

// ---- Lịch gửi báo cáo (P3.3) ----

export function useReportSchedules(enabled = true) {
  return useQuery({
    queryKey: ["reports", "schedules", "list"],
    queryFn: listReportSchedules,
    enabled,
    retry: 1,
  });
}

export function useReportSchedule(id: string | null) {
  return useQuery({
    queryKey: ["reports", "schedules", "detail", id],
    queryFn: () => getReportSchedule(id as string),
    enabled: !!id,
    retry: 1,
  });
}

export function useCreateReportSchedule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: SaveReportScheduleRequest) => createReportSchedule(body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reports", "schedules", "list"] });
    },
  });
}

export function useUpdateReportSchedule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { id: string; body: SaveReportScheduleRequest }) => updateReportSchedule(args.id, args.body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reports", "schedules", "list"] });
    },
  });
}

export function useDeleteReportSchedule() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteReportSchedule(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reports", "schedules", "list"] });
    },
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
