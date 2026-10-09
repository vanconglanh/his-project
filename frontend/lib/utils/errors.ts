import type { AxiosError } from "axios";

// BM-77: Map tất cả error code nội bộ sang thông báo tiếng Việt thân thiện.
// Thêm code mới vào đây thay vì để hiện mã nội bộ ra giao diện.
const ERROR_CODE_MAP: Record<string, string> = {
  // Auth
  AUTH_INVALID_CREDENTIALS: "Email hoặc mật khẩu không đúng",
  AUTH_TOKEN_INVALID: "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại",
  AUTH_TOKEN_EXPIRED: "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại",
  AUTH_UNAUTHORIZED: "Bạn chưa đăng nhập hoặc phiên đã hết hạn",
  AUTH_MFA_TOKEN_INVALID: "Phiên xác thực 2 lớp đã hết hạn, vui lòng đăng nhập lại",
  AUTH_MFA_TOO_MANY_ATTEMPTS: "Bạn đã nhập sai mã quá nhiều lần, vui lòng thử lại sau ít phút",
  AUTH_MFA_INVALID_CODE: "Mã xác thực 2 lớp không đúng",
  TWO_FA_INVALID_CODE: "Mã xác thực 2 lớp không đúng",
  TWO_FA_ALREADY_ENABLED: "Xác thực 2 lớp đã được kích hoạt",
  // Quyền
  PERMISSION_DENIED: "Bạn không có quyền thực hiện thao tác này",
  FORBIDDEN: "Bạn không có quyền truy cập chức năng này",
  // Tenant/Clinic
  TENANT_NOT_FOUND: "Không tìm thấy phòng khám",
  TENANT_SUBDOMAIN_TAKEN: "Subdomain đã được sử dụng",
  // User
  USER_EMAIL_EXISTS: "Email đã được đăng ký",
  USER_INVITE_EXPIRED: "Liên kết mời đã hết hạn",
  USER_NOT_FOUND: "Không tìm thấy người dùng",
  PASSWORD_TOO_WEAK: "Mật khẩu phải có tối thiểu 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt",
  ROLE_SYSTEM_PROTECTED: "Không thể xóa vai trò hệ thống",
  // Patient
  PATIENT_NOT_FOUND: "Không tìm thấy bệnh nhân",
  PATIENT_EXISTS: "Bệnh nhân đã tồn tại trong hệ thống",
  // Encounter
  ENCOUNTER_NOT_FOUND: "Không tìm thấy lượt khám",
  ENCOUNTER_CREATE_FAILED: "Tạo lượt khám thất bại",
  ENCOUNTER_INVALID_TRANSITION: "Không thể chuyển trạng thái lượt khám",
  // Prescription / Drug
  DRUG_NOT_FOUND: "Không tìm thấy thuốc",
  PRESCRIPTION_NOT_FOUND: "Không tìm thấy đơn thuốc",
  DRUG_OUT_OF_STOCK: "Thuốc đã hết hàng trong kho",
  DRUG_IMPORT_INVALID_FORMAT: "File nhập thuốc không đúng định dạng, vui lòng dùng file Excel",
  // Lab/Rad
  LAB_ORDER_NOT_FOUND: "Không tìm thấy chỉ định xét nghiệm",
  LAB_ORDER_CANNOT_DELETE: "Không thể xóa chỉ định xét nghiệm đã có kết quả",
  // Billing / Cashier
  BILLING_NOT_FOUND: "Không tìm thấy phiếu thu",
  CASHIER_SHIFT_NOT_OPEN: "Ca thu ngân chưa được mở",
  // File upload
  FILE_UPLOAD_FAILED: "Tải tệp thất bại, vui lòng thử lại",
  DOC_TOO_LARGE: "Tệp vượt quá dung lượng tối đa cho phép (20MB)",
  DOC_UPLOAD_FAILED: "Tải tài liệu thất bại, vui lòng thử lại",
  // Generic
  VALIDATION_ERROR: "Dữ liệu đầu vào không hợp lệ",
  INTERNAL_SERVER_ERROR: "Lỗi máy chủ nội bộ, vui lòng thử lại sau",
  NOT_FOUND: "Không tìm thấy dữ liệu yêu cầu",
  CONFLICT: "Dữ liệu bị xung đột, vui lòng làm mới trang",
  RATE_LIMIT_EXCEEDED: "Bạn thao tác quá nhanh, vui lòng thử lại sau ít giây",
};

export function getErrorMessage(error: unknown, fallback?: string): string {
  const axiosErr = error as AxiosError<{
    error: { code: string; message: string; details?: Record<string, string[] | string> };
  }>;

  if (axiosErr?.response?.data?.error) {
    const { code, message, details } = axiosErr.response.data.error;
    if (ERROR_CODE_MAP[code]) return ERROR_CODE_MAP[code];
    // BM-45: loi validate tra ve "Du lieu dau vao khong hop le" + details -> hien ly do cu the.
    if (code === "VALIDATION_ERROR" && details && typeof details === "object") {
      const reasons = Object.values(details)
        .flat()
        .filter((m): m is string => typeof m === "string" && m.length > 0);
      if (reasons.length > 0) return reasons.join("; ");
    }
    return message ?? fallback ?? "Đã xảy ra lỗi";
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback ?? "Đã xảy ra lỗi";
}
