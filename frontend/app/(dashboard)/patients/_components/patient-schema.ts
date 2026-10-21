import { z } from "zod";

const PHONE_VN = /^(\+84|0)\d{9,10}$/;
const ID_NUMBER = /^\d{9}$|^\d{12}$/;

// Danh sách fallback (offline/SSR) — nguồn thật lấy từ API /codes/{groupId} (useCodes).
// Không còn dùng làm z.enum() chặn giá trị — BE là nguồn sự thật cho danh mục.
export const PATIENT_TYPES = ["SERVICE", "BHYT", "FREE", "CONTRACT"] as const;
export const MARITAL_STATUSES = ["SINGLE", "MARRIED", "DIVORCED", "WIDOWED", "OTHER"] as const;
export const VISIT_TYPES = ["FIRST_VISIT", "RE_EXAM", "RE_EXAM_IN_PACKAGE"] as const;

export type PatientType = string;
export type MaritalStatus = string;
export type VisitType = string;

const patientBaseSchema = z.object({
  full_name: z.string().min(2, "Họ tên tối thiểu 2 ký tự").max(200),
  gender: z.enum(["MALE", "FEMALE", "OTHER"]).optional(),
  date_of_birth: z
    .string()
    .optional()
    .refine((v) => !v || new Date(v) < new Date(), {
      message: "Ngày sinh phải trước hôm nay",
    })
    .refine(
      (v) => {
        if (!v) return true;
        // BM-81: ngày sinh không được quá 150 năm trong quá khứ
        const minDate = new Date();
        minDate.setFullYear(minDate.getFullYear() - 150);
        return new Date(v) >= minDate;
      },
      { message: "Ngày sinh không hợp lệ (tối đa 150 tuổi)" }
    ),
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
  visit_type: z.string().min(1, "Vui lòng chọn hình thức đến khám").optional().default("FIRST_VISIT"),
  // BM-89: liên hệ khẩn cấp / người giám hộ (không bắt buộc)
  guardian_full_name: z.string().max(200).optional(),
  guardian_relationship: z.string().optional(),
  guardian_phone: z
    .string()
    .optional()
    .refine((v) => !v || PHONE_VN.test(v), { message: "Số điện thoại VN không hợp lệ" }),
  guardian_id_number: z
    .string()
    .optional()
    .refine((v) => !v || ID_NUMBER.test(v), { message: "CMND 9 số hoặc CCCD 12 số" }),
});

const filled = (v?: string) => !!v && v.trim() !== "";

export const patientSchema = patientBaseSchema.superRefine((v, ctx) => {
  // BM-89: chỉ khi đã nhập một phần thông tin người liên hệ thì cần đủ họ tên + số điện thoại
  const anyGuardian =
    filled(v.guardian_full_name) ||
    filled(v.guardian_phone) ||
    filled(v.guardian_relationship) ||
    filled(v.guardian_id_number);
  if (anyGuardian) {
    if (!filled(v.guardian_full_name)) {
      ctx.addIssue({ code: "custom", path: ["guardian_full_name"], message: "Vui lòng nhập họ tên người liên hệ" });
    }
    if (!filled(v.guardian_phone)) {
      ctx.addIssue({ code: "custom", path: ["guardian_phone"], message: "Vui lòng nhập số điện thoại người liên hệ" });
    }
  }
});

export type PatientFormValues = z.infer<typeof patientSchema>;

// BM-81: khi TAO benh nhan moi bat buoc toi thieu ngay sinh. Form Edit van dung patientSchema
// (date_of_birth optional) de khong lam vo viec sua benh nhan cu chua co ngay sinh.
export const createPatientSchema = patientSchema.refine(
  (v) => !!v.date_of_birth && v.date_of_birth.trim() !== "",
  { message: "Vui lòng nhập ngày sinh", path: ["date_of_birth"] }
);
