"use client";

/**
 * Registry trung tâm map route pattern -> tour tương ứng.
 * Một route có thể có nhiều tour (phục vụ nhiều vai trò); khi đó chọn tour đầu tiên
 * mà user đủ quyền (requiredPermission). Tour không requiredPermission = fallback chung.
 */
import type { TourDefinition } from "./types";
import type { PermissionChecker } from "./engine";

import { receptionTour } from "./reception";
import { encounterDetailTour } from "./encounter-detail";
import { labradTour } from "./labrad";
import { cashierTour } from "./cashier";
import { billingDetailTour } from "./billing-detail";
import { pharmacyTour } from "./pharmacy";
import { adminBranchesTour } from "./admin-branches";

/**
 * Mỗi entry: matcher kiểm tra pathname có khớp không (đã chuẩn hoá) + danh sách tour ứng viên.
 * Xếp từ cụ thể tới tổng quát; resolveTour chọn entry KHỚP ĐẦU TIÊN.
 */
interface RouteTourEntry {
  /** Regex khớp với pathname (không gồm querystring). */
  match: RegExp;
  tours: TourDefinition[];
}

const REGISTRY: RouteTourEntry[] = [
  { match: /^\/reception(\/.*)?$/, tours: [receptionTour] },
  { match: /^\/encounters\/[^/]+$/, tours: [encounterDetailTour] },
  { match: /^\/labrad(\/.*)?$/, tours: [labradTour] },
  { match: /^\/billings\/[^/]+$/, tours: [billingDetailTour] },
  { match: /^\/cashier(\/.*)?$/, tours: [cashierTour] },
  { match: /^\/pharmacy(\/.*)?$/, tours: [pharmacyTour] },
  { match: /^\/admin\/branches(\/.*)?$/, tours: [adminBranchesTour] },
];

/**
 * Tìm tour phù hợp cho pathname + quyền hiện tại.
 * Trả về tour đầu tiên trong entry khớp mà user đủ requiredPermission.
 */
export function resolveTour(
  pathname: string,
  can: PermissionChecker,
): TourDefinition | null {
  const entry = REGISTRY.find((e) => e.match.test(pathname));
  if (!entry) return null;
  const tour =
    entry.tours.find((t) => !t.requiredPermission || can(t.requiredPermission)) ??
    null;
  return tour;
}

/** Route dùng làm key localStorage: chuẩn hoá [id] động về dạng ổn định. */
export function normalizeRouteKey(pathname: string): string {
  return pathname
    .replace(/\/encounters\/[^/]+$/, "/encounters/[id]")
    .replace(/\/billings\/[^/]+$/, "/billings/[id]");
}

/** Danh mục tour dùng cho trang "Trung tâm trợ giúp" (/help). */
export interface TourCatalogEntry {
  tour: TourDefinition;
  /** Nhóm hiển thị theo module nghiệp vụ. */
  module: string;
  /** Route để điều hướng tới khi bấm "Xem lại hướng dẫn". */
  route: string;
  /** Mô tả ngắn cho người dùng ở trang trợ giúp. */
  description: string;
  /**
   * true nếu route gắn với 1 bản ghi cụ thể (vd /encounters/[id]) — không thể
   * tự kích hoạt tour ngay từ trang danh sách, chỉ điều hướng tới trang danh sách
   * để người dùng tự mở 1 bản ghi rồi bấm nút "Hướng dẫn" trên topbar.
   */
  requiresRecord?: boolean;
  /** Permission code(s) cần có (giống nav-items.ts) để tour này hiển thị ở /help — trống = ai cũng thấy. */
  permissions?: string[];
}

export const TOUR_CATALOG: TourCatalogEntry[] = [
  {
    tour: receptionTour,
    module: "Tiếp đón",
    route: "/reception",
    description: "Tra cứu/tạo bệnh nhân, quét CCCD, check-in và theo dõi hàng đợi khám.",
    permissions: ["reception.read"],
  },
  {
    tour: encounterDetailTour,
    module: "Khám bệnh",
    route: "/encounters",
    description:
      "Khám bệnh, ghi nhận sinh hiệu, chẩn đoán ICD-10, kê đơn và chỉ định CLS. Mở 1 lượt khám bất kỳ rồi bấm nút \"Hướng dẫn\" để xem chi tiết.",
    requiresRecord: true,
    permissions: ["encounter.read"],
  },
  {
    tour: labradTour,
    module: "Cận lâm sàng",
    route: "/labrad",
    description: "Quản lý chỉ định và trả kết quả xét nghiệm, chẩn đoán hình ảnh.",
    permissions: ["lab.read", "rad.read"],
  },
  {
    tour: cashierTour,
    module: "Thu ngân",
    route: "/cashier",
    description: "Mở/đóng ca, thu tiền hoá đơn, theo dõi công nợ bệnh nhân.",
    permissions: ["cashier.read"],
  },
  {
    tour: billingDetailTour,
    module: "Thu ngân",
    route: "/cashier",
    description:
      "Chi tiết 1 hoá đơn: dòng dịch vụ/thuốc, thanh toán, in hoá đơn. Mở 1 hoá đơn bất kỳ rồi bấm nút \"Hướng dẫn\" để xem chi tiết.",
    requiresRecord: true,
    permissions: ["billing.read"],
  },
  {
    tour: pharmacyTour,
    module: "Kho dược",
    route: "/pharmacy",
    description: "Cấp phát thuốc theo đơn, tồn kho, lô/hạn sử dụng.",
    permissions: ["pharmacy.read"],
  },
  {
    tour: adminBranchesTour,
    module: "Quản trị chi nhánh",
    route: "/admin/branches",
    description: "Tạo mới, nhân bản cấu hình và quản lý danh sách chi nhánh.",
    permissions: ["tenant.read"],
  },
];

export type { TourDefinition } from "./types";
