/**
 * clinic-config.ts — Hằng số dùng chung cho bộ mô phỏng phòng khám (Playwright E2E harness).
 * Vai trò: nguồn sự thật duy nhất cho tài khoản đăng nhập, phòng khám, danh mục thuốc dùng để
 * search DrugAutocomplete, và các cờ điều khiển qua biến môi trường.
 *
 * QUAN TRỌNG: các giá trị dưới đây PHẢI khớp tuyệt đối với seed DIAB-TEST (tenant_id=2).
 * Không tự ý đổi giá trị nếu seed thay đổi — hãy cập nhật đồng bộ với script seed.
 */

export const BASE_URL = process.env.BASE_URL || "http://localhost:3000";

/** Mật khẩu mặc định cho mọi tài khoản seed DIAB-TEST. */
const DEFAULT_PASSWORD = process.env.SIM_PASSWORD || "admin123";
/** Cho phép override riêng mật khẩu admin nếu cần (vd môi trường CI khác). */
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD || DEFAULT_PASSWORD;

export interface RoleAccount {
  email: string;
  password: string;
  /** Tên hiển thị đầy đủ trên UI (dùng để chọn trong Select bác sĩ phụ trách). */
  fullName?: string;
}

export const ROLES = {
  admin: { email: "admin.test@diabtest.local", password: ADMIN_PASSWORD },
  letan: { email: "letan.test@diabtest.local", password: DEFAULT_PASSWORD },
  bacsi: {
    email: "bacsi.test@diabtest.local",
    password: DEFAULT_PASSWORD,
    fullName: "BS. Trần Thị Test 1",
  },
  bacsi2: {
    email: "bacsi2.test@diabtest.local",
    password: DEFAULT_PASSWORD,
    fullName: "BS. Lê Văn Test 2",
  },
  duocsi: { email: "duocsi.test@diabtest.local", password: DEFAULT_PASSWORD },
  ketoan: { email: "ketoan.test@diabtest.local", password: DEFAULT_PASSWORD },
  ktv: { email: "ktv.test@diabtest.local", password: DEFAULT_PASSWORD },
} as const satisfies Record<string, RoleAccount>;

export type RoleKey = keyof typeof ROLES;

/** 3 phòng khám dùng để tiếp đón bệnh nhân (không tính quầy thu ngân). */
export const EXAM_ROOMS = ["Phòng khám số 1", "Phòng khám số 2", "Phòng khám số 3"] as const;

/** Toàn bộ "phòng" khai báo trong seed, gồm cả quầy thu ngân. */
export const ROOMS = [...EXAM_ROOMS, "Quầy thu ngân"] as const;

/** Danh mục thuốc dùng để search trong DrugAutocomplete khi kê đơn. */
export const DRUGS = [
  "Metformin 500mg",
  "Amlodipine 5mg",
  "Atorvastatin 20mg",
  "Paracetamol 500mg",
  "Omeprazole 20mg",
  "Gliclazide 30mg",
  "Insulin Glargine",
] as const;

/** Thuốc thiếu tồn — dùng cho kịch bản ngoại lệ "hết thuốc" khi phát tại dược. */
export const DRUG_OUT_OF_STOCK = "Gliclazide 30mg";

/** Thuốc có 2 lô, 1 lô cận HSD — dùng cho kịch bản ngoại lệ test FEFO khi cấp phát. */
export const DRUG_NEAR_EXPIRY = "Insulin Glargine";

/**
 * Cặp thuốc chống chỉ định (DDI) ưu tiên theo đề bài. Nếu "Gemfibrozil" chưa có trong danh mục
 * đã seed, DrugAutocomplete sẽ không trả kết quả — khi đó dùng DDI_PAIR_FALLBACK thay thế.
 */
export const DDI_PAIR_PRIMARY: readonly [string, string] = ["Atorvastatin 20mg", "Gemfibrozil"];

/** Cặp dự phòng nếu DDI_PAIR_PRIMARY không tìm thấy — cả 2 thuốc đều nằm trong DRUGS đã seed. */
export const DDI_PAIR_FALLBACK: readonly [string, string] = ["Metformin 500mg", "Amlodipine 5mg"];

/** true => mọi vai trò đăng nhập bằng tài khoản admin (bypass RBAC), dùng khi phân quyền role chưa đủ. */
export const USE_ADMIN = process.env.SIM_USE_ADMIN === "1";

/** Số bệnh nhân mô phỏng trong clinic-simulation.spec.ts (mặc định 50, rút gọn qua SIM_PATIENTS). */
export const SIM_PATIENTS = Number(process.env.SIM_PATIENTS || 50);
