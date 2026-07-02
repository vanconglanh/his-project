/**
 * personas.ts — Sinh danh sách 50 bệnh nhân mô phỏng (đơn tất định, không random) rải theo
 * 5 "ngày" x 10 bệnh nhân/ngày, đa dạng chẩn đoán ĐTĐ/tăng HA/lipid/thận + vài ca cấp,
 * kèm vài ca được gắn exceptionTag để exceptions.spec.ts tái sử dụng.
 */
import {
  DDI_PAIR_PRIMARY,
  DRUG_NEAR_EXPIRY,
  DRUG_OUT_OF_STOCK,
} from "./clinic-config";

export type Gender = "MALE" | "FEMALE";
export type PatientType = "BHYT" | "SERVICE";
export type ExceptionTag = "OUT_OF_STOCK" | "DDI" | "BHYT_INVALID" | "NEAR_EXPIRY";
export type SimDay = 1 | 2 | 3 | 4 | 5;

export interface Persona {
  /** Mã định danh ngắn dùng trong log/report, vd "BN-01". */
  code: string;
  fullName: string;
  gender: Gender;
  /** yyyy-MM-dd — khớp input type=date (#date_of_birth). */
  dob: string;
  phone: string;
  idNumber: string;
  patientType: PatientType;
  /** true => ưu tiên tìm bệnh nhân đã tồn tại trước khi tạo mới (mô phỏng tái khám). */
  isReturning: boolean;
  day: SimDay;
  reason: string;
  icd10: string;
  icd10Name: string;
  drugs: string[];
  needsCls: boolean;
  exceptionTag?: ExceptionTag;
}

// ─── Bộ dữ liệu tên tiếng Việt (tất định, không random) ───────────────────────

const SURNAMES = [
  "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ",
];
const MIDDLE_MALE = ["Văn", "Hữu", "Minh", "Quốc", "Đức", "Thành", "Công", "Anh"];
const MIDDLE_FEMALE = ["Thị", "Ngọc", "Thu", "Kim", "Bích", "Diệu", "Hồng", "Mỹ"];
const GIVEN_MALE = [
  "An", "Bình", "Cường", "Dũng", "Đạt", "Hải", "Hùng", "Khang",
  "Long", "Minh", "Nam", "Phong", "Quang", "Sơn", "Tâm", "Tuấn", "Việt", "Thắng",
];
const GIVEN_FEMALE = [
  "Anh", "Chi", "Dung", "Giang", "Hà", "Hoa", "Hương", "Lan",
  "Linh", "Mai", "Nga", "Nhung", "Phương", "Thảo", "Trang", "Trinh", "Vân", "Yến",
];

// ─── Danh mục chẩn đoán ICD-10 xoay vòng ───────────────────────────────────────

interface DiagTemplate {
  code: string;
  name: string;
  reason: string;
  drugs: string[];
}

const ICD10_CATALOG: DiagTemplate[] = [
  {
    code: "E11.9",
    name: "Đái tháo đường típ 2 không biến chứng",
    reason: "Đái tháo đường tái khám định kỳ, kiểm tra đường huyết",
    drugs: ["Metformin 500mg"],
  },
  {
    code: "E11.4",
    name: "Đái tháo đường típ 2 có biến chứng thần kinh",
    reason: "Đái tháo đường, tê bì đầu chi, tái khám",
    drugs: ["Metformin 500mg", "Gliclazide 30mg"],
  },
  {
    code: "E10.9",
    name: "Đái tháo đường típ 1 không biến chứng",
    reason: "Đái tháo đường típ 1, chỉnh liều insulin",
    drugs: ["Insulin Glargine"],
  },
  {
    code: "I10",
    name: "Tăng huyết áp vô căn (nguyên phát)",
    reason: "Tăng huyết áp, đau đầu nhẹ, tái khám định kỳ",
    drugs: ["Amlodipine 5mg"],
  },
  {
    code: "E78.5",
    name: "Rối loạn chuyển hoá lipid máu, không đặc hiệu",
    reason: "Rối loạn mỡ máu, khám định kỳ theo dõi lipid",
    drugs: ["Atorvastatin 20mg"],
  },
  {
    code: "N18.9",
    name: "Bệnh thận mạn, không xác định",
    reason: "Bệnh thận mạn, tái khám theo dõi chức năng thận",
    drugs: ["Metformin 500mg", "Amlodipine 5mg"],
  },
  {
    code: "J06.9",
    name: "Nhiễm khuẩn hô hấp trên cấp, không đặc hiệu",
    reason: "Sốt, ho, đau họng 3 ngày",
    drugs: ["Paracetamol 500mg"],
  },
  {
    code: "K29.7",
    name: "Viêm dạ dày, không đặc hiệu",
    reason: "Đau thượng vị, ợ chua sau ăn",
    drugs: ["Omeprazole 20mg"],
  },
];

function diagTemplateFor(i: number): DiagTemplate {
  // Rải đa số ca vào 6 chẩn đoán mạn tính (ĐTĐ/HA/lipid/thận), xen kẽ vài ca cấp (hô hấp/dạ dày).
  if (i % 12 === 6) return ICD10_CATALOG[6]; // J06.9 — ca cấp hô hấp
  if (i % 15 === 9) return ICD10_CATALOG[7]; // K29.7 — ca cấp tiêu hoá
  return ICD10_CATALOG[i % 6];
}

function pad2(n: number): string {
  return String(n).padStart(2, "0");
}

function buildDob(ageYears: number): string {
  const now = new Date();
  const year = now.getFullYear() - ageYears;
  const month = ((now.getMonth() + 1 + 3) % 12) + 1; // rải tháng sinh, tránh dồn 1 tháng
  const day = ((now.getDate() + 5) % 27) + 1; // 1..28, tránh lỗi tháng 2
  return `${year}-${pad2(month)}-${pad2(day)}`;
}

function buildPersona(i: number): Persona {
  const gender: Gender = i % 2 === 0 ? "MALE" : "FEMALE";
  const surname = SURNAMES[i % SURNAMES.length];
  const middle = gender === "MALE" ? MIDDLE_MALE[i % MIDDLE_MALE.length] : MIDDLE_FEMALE[i % MIDDLE_FEMALE.length];
  const given = gender === "MALE" ? GIVEN_MALE[i % GIVEN_MALE.length] : GIVEN_FEMALE[i % GIVEN_FEMALE.length];
  const fullName = `${surname} ${middle} ${given} ${pad2(i + 1)}`;

  const day = ((Math.floor(i / 10) + 1) as SimDay);
  const template = diagTemplateFor(i);
  const ageYears = 35 + ((i * 3) % 45); // 35..79 tuổi — phù hợp nhóm bệnh mạn tính

  const phone = `09${(10_000_000 + i * 137).toString().padStart(8, "0")}`;
  const idNumber = `079${(200_000_000 + i).toString().padStart(9, "0")}`;

  const patientType: PatientType = i % 3 === 0 ? "BHYT" : "SERVICE";
  const isReturning = day > 1 && i % 4 === 3;
  const needsCls = i % 3 === 0;

  return {
    code: `BN-${pad2(i + 1)}`,
    fullName,
    gender,
    dob: buildDob(ageYears),
    phone,
    idNumber,
    patientType,
    isReturning,
    day,
    reason: template.reason,
    icd10: template.code,
    icd10Name: template.name,
    drugs: [...template.drugs],
    needsCls,
  };
}

const TOTAL_PERSONAS = 50;

const BASE_PERSONAS: Persona[] = Array.from({ length: TOTAL_PERSONAS }, (_, i) => buildPersona(i));

/**
 * Gán exceptionTag cho vài persona cụ thể (chỉ số 0-based), rải đều qua các ngày,
 * để exceptions.spec.ts tái sử dụng dữ liệu nhất quán với luồng mô phỏng chính.
 */
const EXCEPTION_OVERRIDES: Record<number, ExceptionTag> = {
  5: "DDI", // Ngày 1
  16: "OUT_OF_STOCK", // Ngày 2
  27: "NEAR_EXPIRY", // Ngày 3
  38: "BHYT_INVALID", // Ngày 4
  44: "OUT_OF_STOCK", // Ngày 5 — ca thứ 2 để test kịch bản quá tải/hàng chờ
};

for (const [idxStr, tag] of Object.entries(EXCEPTION_OVERRIDES)) {
  const idx = Number(idxStr);
  const persona = BASE_PERSONAS[idx];
  if (!persona) continue;
  persona.exceptionTag = tag;
  if (tag === "DDI") {
    persona.drugs = [...DDI_PAIR_PRIMARY];
    persona.reason = `${persona.reason} — kê 2 thuốc thuộc cặp DDI để test cảnh báo tương tác`;
  } else if (tag === "OUT_OF_STOCK") {
    if (!persona.drugs.includes(DRUG_OUT_OF_STOCK)) persona.drugs = [...persona.drugs, DRUG_OUT_OF_STOCK];
  } else if (tag === "NEAR_EXPIRY") {
    if (!persona.drugs.includes(DRUG_NEAR_EXPIRY)) persona.drugs = [...persona.drugs, DRUG_NEAR_EXPIRY];
  } else if (tag === "BHYT_INVALID") {
    persona.patientType = "BHYT";
  }
}

/** Toàn bộ 50 persona, đã sắp theo ngày (1..5) tăng dần. */
export const PERSONAS: Persona[] = BASE_PERSONAS;

/** Lọc persona theo ngày mô phỏng (mặc định lọc trên toàn bộ PERSONAS). */
export function patientsForDay(day: SimDay, source: Persona[] = PERSONAS): Persona[] {
  return source.filter((p) => p.day === day);
}

/** Lấy n persona đầu tiên (dùng để rút gọn quy mô mô phỏng qua SIM_PATIENTS). */
export function limitTo(n: number, source: Persona[] = PERSONAS): Persona[] {
  if (!Number.isFinite(n) || n <= 0) return [];
  return source.slice(0, Math.min(n, source.length));
}

/** Tìm persona mẫu theo exceptionTag — dùng trong exceptions.spec.ts. */
export function findByExceptionTag(tag: ExceptionTag, source: Persona[] = PERSONAS): Persona | undefined {
  return source.find((p) => p.exceptionTag === tag);
}
