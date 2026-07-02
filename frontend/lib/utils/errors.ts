import type { AxiosError } from "axios";

const ERROR_CODE_MAP: Record<string, string> = {
  AUTH_INVALID_CREDENTIALS: "Email hoặc mật khẩu không đúng",
  AUTH_TOKEN_INVALID: "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại",
  PERMISSION_DENIED: "Bạn không có quyền thực hiện thao tác này",
  TENANT_NOT_FOUND: "Không tìm thấy phòng khám",
  TENANT_SUBDOMAIN_TAKEN: "Subdomain đã được sử dụng",
  USER_EMAIL_EXISTS: "Email đã được đăng ký",
  USER_INVITE_EXPIRED: "Liên kết mời đã hết hạn",
  PASSWORD_TOO_WEAK:
    "Mật khẩu phải có tối thiểu 12 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt",
  ROLE_SYSTEM_PROTECTED: "Không thể xóa vai trò hệ thống",
  TWO_FA_INVALID_CODE: "Mã xác thực 2 lớp không đúng",
  TWO_FA_ALREADY_ENABLED: "Xác thực 2 lớp đã được kích hoạt",
};

export function getErrorMessage(error: unknown, fallback?: string): string {
  const axiosErr = error as AxiosError<{ error: { code: string; message: string } }>;

  if (axiosErr?.response?.data?.error) {
    const { code, message } = axiosErr.response.data.error;
    return ERROR_CODE_MAP[code] ?? message ?? fallback ?? "Đã xảy ra lỗi";
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallback ?? "Đã xảy ra lỗi";
}
