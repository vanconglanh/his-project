"use client";

/**
 * Map route hiện tại + vai trò user -> đúng trang/mục trong tài liệu hướng dẫn
 * (public/user-guide/*.html, xem docs/user-guide/ trong repo là nguồn gốc, đã copy
 * sang frontend/public để Next.js serve tĩnh qua /user-guide/*.html).
 *
 * Khác với tour (driver.js, highlight UI ngay trên trang) — đây là mở TÀI LIỆU
 * HTML (ảnh chụp + hướng dẫn text step-by-step, có nút Xuất PDF) trong tab mới.
 */

/** Tên file tài liệu (không gồm .html) theo từng vai trò. */
const ROLE_DOC_FILE: Record<string, string> = {
  admin: "quan-tri-vien",
  bac_si: "bac-si",
  le_tan: "le-tan",
  duoc_si: "duoc-si",
  ke_toan: "ke-toan",
  ky_thuat_vien: "ky-thuat-vien",
};

/** Nhãn hiển thị vai trò (dùng khi user có nhiều role, để chọn đúng tài liệu ưu tiên). */
const ROLE_PRIORITY = ["admin", "bac_si", "le_tan", "duoc_si", "ke_toan", "ky_thuat_vien"];

/**
 * Map route pattern -> anchor (id section) trong tài liệu của TỪNG vai trò.
 * Chỉ khai báo route có tương ứng rõ ràng; route không khớp -> mở đầu tài liệu (#tong-quan).
 */
interface RouteAnchorEntry {
  match: RegExp;
  anchors: Partial<Record<string, string>>; // roleCode -> anchor id
}

const ROUTE_ANCHORS: RouteAnchorEntry[] = [
  { match: /^\/reception(\/.*)?$/, anchors: { le_tan: "b1" } },
  { match: /^\/patients(\/.*)?$/, anchors: { le_tan: "b2" } },
  { match: /^\/encounters\/[^/]+$/, anchors: { bac_si: "b2", ky_thuat_vien: "b2" } },
  { match: /^\/encounters(\/.*)?$/, anchors: { bac_si: "b1" } },
  { match: /^\/prescriptions(\/.*)?$/, anchors: { bac_si: "b5", duoc_si: "b1" } },
  { match: /^\/drugs(\/.*)?$/, anchors: { duoc_si: "b2" } },
  { match: /^\/pharmacy\/stock-transfers(\/.*)?$/, anchors: { duoc_si: "b3" } },
  { match: /^\/labrad\/results(\/.*)?$/, anchors: { bac_si: "cls", ky_thuat_vien: "b2" } },
  { match: /^\/billings\/[^/]+$/, anchors: { ke_toan: "b2" } },
  { match: /^\/billings(\/.*)?$/, anchors: { ke_toan: "b1" } },
  { match: /^\/$/, anchors: { admin: "dash" } },
  { match: /^\/reports(\/.*)?$/, anchors: { admin: "bc" } },
  { match: /^\/admin(\/.*)?$/, anchors: { admin: "qt" } },
];

export interface UserGuideLink {
  href: string;
  /** Nhãn ngắn cho tooltip/aria-label, vd "Xem tài liệu — Bác sĩ". */
  label: string;
}

/**
 * Trả về link tài liệu phù hợp nhất cho route + danh sách role hiện tại của user.
 * `roles` là mảng roles/roleCodes thô từ auth-store (có thể lẫn tên hiển thị lẫn code,
 * xem LoginCommandHandler/establishSession) — chỉ cần chứa đúng 1 code hợp lệ là đủ.
 */
export function resolveUserGuideLink(pathname: string, roles: string[]): UserGuideLink | null {
  const activeRole = ROLE_PRIORITY.find((code) => roles.includes(code));
  if (!activeRole) return null;

  const docFile = ROLE_DOC_FILE[activeRole];
  if (!docFile) return null;

  const entry = ROUTE_ANCHORS.find((e) => e.match.test(pathname));
  const anchor = entry?.anchors[activeRole];

  const href = anchor ? `/user-guide/${docFile}.html#${anchor}` : `/user-guide/${docFile}.html`;
  const roleLabel: Record<string, string> = {
    admin: "Quản trị viên",
    bac_si: "Bác sĩ",
    le_tan: "Lễ tân",
    duoc_si: "Dược sĩ",
    ke_toan: "Kế toán",
    ky_thuat_vien: "Kỹ thuật viên",
  };
  return { href, label: `Xem tài liệu hướng dẫn — ${roleLabel[activeRole] ?? activeRole}` };
}
