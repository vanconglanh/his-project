/**
 * Parser chuỗi QR CCCD phía client (BR-QR-006 — không gọi API để parse).
 * Định dạng 7 field: soCCCD|soCMNDCu|hoTen|ngaySinh(ddMMyyyy)|gioiTinh|diaChiThuongTru|ngayCap(ddMMyyyy)
 * Mirror logic với backend/src/ProDiabHis.Application/Patients/CccdQrParser.cs — giữ đồng bộ khi sửa.
 */

export interface CccdQrData {
  id_number: string | null;
  old_id_number: string | null;
  full_name: string | null;
  /** yyyy-MM-dd — khớp định dạng input type="date" của form */
  date_of_birth: string | null;
  gender: "MALE" | "FEMALE" | null;
  address: string | null;
  /** yyyy-MM-dd */
  issued_date: string | null;
  has_encoding_warning: boolean;
}

export interface CccdQrParseResult {
  success: boolean;
  data: CccdQrData | null;
  error_code: string | null;
  error_message: string | null;
}

const EXPECTED_FIELD_COUNT = 7;

function nullIfEmpty(s: string | undefined): string | null {
  if (s === undefined) return null;
  const trimmed = s.trim();
  return trimmed === "" ? null : trimmed;
}

/** BR-QR-003: ngày phải đúng định dạng ddMMyyyy (8 chữ số), không hợp lệ -> null */
function parseDate(field: string): string | null {
  if (!field || field.length !== 8 || !/^\d{8}$/.test(field)) return null;
  const day = parseInt(field.slice(0, 2), 10);
  const month = parseInt(field.slice(2, 4), 10);
  const year = parseInt(field.slice(4, 8), 10);

  const date = new Date(year, month - 1, day);
  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month - 1 ||
    date.getDate() !== day
  ) {
    return null;
  }
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${year}-${pad(month)}-${pad(day)}`;
}

/** GA-005: giới tính QR chỉ "Nam"/"Nữ"; giá trị khác -> để trống */
function mapGender(g: string | null): "MALE" | "FEMALE" | null {
  if (g === null) return null;
  const trimmed = g.trim().toLowerCase();
  if (trimmed === "nam") return "MALE";
  if (trimmed === "nữ" || trimmed === "nu") return "FEMALE";
  return null;
}

function containsReplacementChar(s: string | null): boolean {
  return s !== null && s.includes("�");
}

/** Parse chuỗi QR CCCD. BR-QR-002: từng field xử lý độc lập, không throw vỡ luồng. */
export function parseCccdQr(raw: string | null | undefined): CccdQrParseResult {
  if (!raw || raw.trim() === "") {
    return { success: false, data: null, error_code: "CCCD_QR_EMPTY", error_message: "Chuỗi quét rỗng" };
  }

  const fields = raw.split("|");
  if (fields.length !== EXPECTED_FIELD_COUNT) {
    return {
      success: false,
      data: null,
      error_code: "CCCD_QR_INVALID_FIELD_COUNT",
      error_message: `Số trường không hợp lệ (${fields.length}/${EXPECTED_FIELD_COUNT} field)`,
    };
  }

  const idNumber = nullIfEmpty(fields[0]);
  const oldIdNumber = nullIfEmpty(fields[1]);
  const fullName = nullIfEmpty(fields[2]);
  const dob = parseDate(fields[3]?.trim() ?? "");
  const genderRaw = nullIfEmpty(fields[4]);
  const gender = mapGender(genderRaw);
  const address = nullIfEmpty(fields[5]);
  const issuedDate = parseDate(fields[6]?.trim() ?? "");

  const hasEncodingWarning = containsReplacementChar(fullName) || containsReplacementChar(address);

  return {
    success: true,
    data: {
      id_number: idNumber,
      old_id_number: oldIdNumber,
      full_name: fullName,
      date_of_birth: dob,
      gender,
      address,
      issued_date: issuedDate,
      has_encoding_warning: hasEncodingWarning,
    },
    error_code: null,
    error_message: null,
  };
}
