/**
 * API lấy thông tin tiêu đề phòng khám (letterhead) dùng cho báo cáo in A4.
 * Endpoint: GET /api/v1/tenants/me/letterhead — LetterheadDto từ backend (Thảo).
 */
import apiClient from "./client";

/** Khớp LetterheadDto trả về từ backend (snake_case global). */
interface LetterheadDto {
  clinic_name: string;
  cskcb_code: string | null;
  company_name: string;
  address: string;
  phone: string;
  email: string;
  logo_url: string | null;
  email_support?: string;
}

export interface ClinicLetterheadData {
  clinic_name: string;
  cskcb_code: string | null;
  company_name: string;
  address: string;
  phone: string;
  email: string;
  logo_url: string | null;
  email_support?: string;
}

export async function getClinicLetterhead(): Promise<ClinicLetterheadData> {
  const { data } = await apiClient.get<{ data: LetterheadDto }>("/tenants/me/letterhead");
  return data.data;
}
