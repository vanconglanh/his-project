// BM-69: Chuẩn hoá đơn vị đường huyết (glucose) dùng chung cho mọi màn Khám bệnh.
//
// QUYẾT ĐỊNH DỮ LIỆU: cột lưu trong DB (vd `glucose_mg_dl` ở sinh hiệu) GIỮ NGUYÊN
// đơn vị theo tên cột (mg/dL) làm NGUỒN SỰ THẬT — KHÔNG quy đổi dữ liệu cũ hàng loạt,
// KHÔNG đổi schema. Việc "chuẩn hoá mmol/L" chỉ áp ở TẦNG HIỂN THỊ/NHẬP: mặc định cho
// người dùng thấy & nhập theo mmol/L (đơn vị chuẩn), có bộ chọn chuyển sang mg/dL.
//
// Hệ số quy đổi (leader quy định): mg/dL = mmol/L × 18.016; mmol/L = mg/dL ÷ 18.016.

export const GLUCOSE_MGDL_PER_MMOL = 18.016;

export type GlucoseUnit = "mmol/L" | "mg/dL";

/** Đơn vị chuẩn mặc định để hiển thị/nhập (theo quyết định leader). */
export const DEFAULT_GLUCOSE_UNIT: GlucoseUnit = "mmol/L";

export function mgdlToMmol(mgdl: number): number {
  return mgdl / GLUCOSE_MGDL_PER_MMOL;
}

export function mmolToMgdl(mmol: number): number {
  return mmol * GLUCOSE_MGDL_PER_MMOL;
}

/** Quy đổi giá trị giữa hai đơn vị (no-op nếu cùng đơn vị). */
export function convertGlucose(value: number, from: GlucoseUnit, to: GlucoseUnit): number {
  if (from === to) return value;
  return from === "mg/dL" ? mgdlToMmol(value) : mmolToMgdl(value);
}

/** Làm tròn hợp lý: mmol/L 1 chữ số thập phân, mg/dL số nguyên. */
export function roundGlucose(value: number, unit: GlucoseUnit): number {
  return unit === "mmol/L" ? Math.round(value * 10) / 10 : Math.round(value);
}

/**
 * Định dạng chuỗi hiển thị từ giá trị LƯU theo mg/dL (nguồn sự thật) sang đơn vị đích.
 * Trả về vd "7.0 mmol/L" hoặc "126 mg/dL".
 */
export function formatGlucoseFromMgdl(valueMgdl: number, unit: GlucoseUnit): string {
  const converted = roundGlucose(convertGlucose(valueMgdl, "mg/dL", unit), unit);
  return `${unit === "mmol/L" ? converted.toFixed(1) : converted} ${unit}`;
}
