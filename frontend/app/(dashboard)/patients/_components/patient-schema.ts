import { z } from "zod";

const PHONE_VN = /^(\+84|0)\d{9,10}$/;
const ID_NUMBER = /^\d{9}$|^\d{12}$/;

// Danh sách fallback (offline/SSR) — nguồn thật lấy từ API /codes/{groupId} (useCodes).
// Không còn dùng làm z.enum() chặn giá trị — BE là nguồn sự thật cho danh mục.
export const PATIENT_TYPES = ["SERVICE", "BHYT", "FREE", "CONTRACT"] as const;
export const MARITAL_STATUSES = ["SINGLE", "MARRIED", "DIVORCED", "WIDOWED", "OTHER"] as const;
export const VISIT_TYPES = ["FIRST_VISIT", "FOLLOW_UP", "EMERGENCY", "SPECIALIST"] as const;

export type PatientType = string;
export type MaritalStatus = string;
export type VisitType = string;

export const patientSchema = z.object({
  full_name: z.string().min(2, "Họ tên tối thiểu 2 ký tự").max(200),
  gender: z.enum(["MALE", "FEMALE", "OTHER"]).optional(),
  date_of_birth: z
    .string()
    .optional()
    .refine((v) => !v || new Date(v) < new Date(), {
      message: "Ngày sinh phải trước hôm nay",
    }),
  id_number: z
    .string()
    .optional()
    .refine((v) => !v || ID_NUMBER.test(v), {
      message: "CMND 9 số hoặc CCCD 12 số",
    }),
  phone: z
    .string()
    .optional()
    .refine((v) => !v || PHONE_VN.test(v), {
      message: "Số điện thoại VN không hợp lệ",
    }),
  email: z.string().email("Email không hợp lệ").optional().or(z.literal("")),
  occupation: z.string().optional(),
  ethnicity: z.string().optional(),
  blood_type: z
    .enum(["A_POS", "A_NEG", "B_POS", "B_NEG", "AB_POS", "AB_NEG", "O_POS", "O_NEG", "UNKNOWN"])
    .optional(),
  province_code: z.string().optional(),
  district_code: z.string().optional(),
  ward_code: z.string().optional(),
  street: z.string().optional(),
  // SUNS Phase 1 fields
  id_card_issued_date: z
    .string()
    .optional()
    .refine((v) => !v || new Date(v) < new Date(), {
      message: "Ngày cấp phải trước hôm nay",
    }),
  id_card_issued_place: z.string().max(255).optional(),
  nationality: z.string().optional().default("VN"),
  patient_type: z.string().min(1, "Vui lòng chọn đối tượng").optional().default("SERVICE"),
  marital_status: z.string().optional(),
  visit_type: z.string().min(1, "Vui lòng chọn loại khám").optional().default("FIRST_VISIT"),
});

export type PatientFormValues = z.infer<typeof patientSchema>;
