export const APP_NAME = "Pro-Diab HIS";
export const APP_VERSION = "0.1.0";

/** Domain goc cua nen tang (cau hinh qua NEXT_PUBLIC_BASE_DOMAIN). Dung dung ghep subdomain phong kham. */
export const BASE_DOMAIN = process.env.NEXT_PUBLIC_BASE_DOMAIN ?? "prodiab.vn";

export const USER_ROLES = {
  Admin: "Admin",
  BacSi: "BacSi",
  LeTan: "LeTan",
  DuocSi: "DuocSi",
  KeToan: "KeToan",
  KyThuatVien: "KyThuatVien",
} as const;

export const ROLE_LABELS: Record<string, string> = {
  Admin: "Quản trị viên",
  BacSi: "Bác sĩ",
  LeTan: "Lễ tân",
  DuocSi: "Dược sĩ",
  KeToan: "Kế toán",
  KyThuatVien: "Kỹ thuật viên",
};

export const KEYBOARD_SHORTCUTS = {
  NEW_PATIENT: "F2",
  SAVE: "F4",
  SEARCH: "F3",
  PRINT: "F5",
} as const;

export const QUERY_KEYS = {
  PATIENTS: "patients",
  ENCOUNTERS: "encounters",
  PRESCRIPTIONS: "prescriptions",
  PHARMACY: "pharmacy",
  CASHIER: "cashier",
  REPORTS: "reports",
} as const;

export const PAGE_SIZE = 20;
