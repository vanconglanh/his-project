/**
 * code-labels.ts — Nhãn tiếng Việt tập trung cho các nhóm mã (code groups).
 *
 * Dùng cho prop `items` của <Select> (Base UI) để hiển thị TEXT thay vì code
 * sau khi chọn, và để hiển thị name (không phải code) ở list/detail.
 *
 * PHA 2: đây là nguồn dữ liệu seed cho bảng CODE_MASTER / CODE_DETAIL_MASTER.
 * Khi có API /codes/{groupId}, thay dần các hằng số này bằng hook useCodes(groupId).
 *
 * Quy ước: mỗi nhóm là Record<code, name_vi>. Key nhóm = id CODE_MASTER (SCREAMING_SNAKE).
 */

export type CodeLabelMap = Record<string, string>;

// ----- Bệnh nhân -----
export const GENDER: CodeLabelMap = { MALE: "Nam", FEMALE: "Nữ", OTHER: "Khác" };

export const BLOOD_TYPE: CodeLabelMap = {
  A_POS: "A+", A_NEG: "A-", B_POS: "B+", B_NEG: "B-",
  AB_POS: "AB+", AB_NEG: "AB-", O_POS: "O+", O_NEG: "O-", UNKNOWN: "Chưa xác định",
};

export const NATIONALITY: CodeLabelMap = {
  VN: "Việt Nam", US: "Hoa Kỳ", CN: "Trung Quốc", JP: "Nhật Bản", KR: "Hàn Quốc", OTHER: "Khác",
};

export const MARITAL_STATUS: CodeLabelMap = {
  SINGLE: "Độc thân", MARRIED: "Đã kết hôn", DIVORCED: "Ly hôn", WIDOWED: "Goá", OTHER: "Khác",
};

export const PATIENT_TYPE: CodeLabelMap = {
  SERVICE: "Dịch vụ", BHYT: "Bảo hiểm y tế", FREE: "Miễn phí", CONTRACT: "Hợp đồng",
};

export const VISIT_TYPE: CodeLabelMap = {
  FIRST_VISIT: "Khám lần đầu", FOLLOW_UP: "Tái khám", EMERGENCY: "Cấp cứu", SPECIALIST: "Khám chuyên khoa",
};

export const RELATIONSHIP: CodeLabelMap = {
  FATHER: "Cha", MOTHER: "Mẹ", SPOUSE: "Vợ/Chồng", CHILD: "Con", SIBLING: "Anh/Chị/Em", OTHER: "Khác",
};

// ----- Khám bệnh / CLS -----
export const ENCOUNTER_TYPE: CodeLabelMap = {
  FIRST_VISIT: "Khám mới", FOLLOW_UP: "Tái khám", EMERGENCY: "Cấp cứu", CONSULTATION: "Hội chẩn",
};

export const ENCOUNTER_STATUS: CodeLabelMap = {
  WAITING: "Chờ khám", IN_PROGRESS: "Đang khám", DONE: "Đã khám xong", CANCELLED: "Đã huỷ",
};

export const DIABETES_TYPE: CodeLabelMap = {
  TYPE_1: "Đái tháo đường type 1", TYPE_2: "Đái tháo đường type 2",
  GESTATIONAL: "ĐTĐ thai kỳ", MODY: "MODY", OTHER: "Khác",
};

export const MODALITY: CodeLabelMap = {
  XRAY: "X-quang", US: "Siêu âm", CT: "CT Scan", MRI: "MRI", MAMMO: "Nhũ ảnh", ECG: "Điện tim", ENDO: "Nội soi",
};

export const CLS_PRIORITY: CodeLabelMap = { NORMAL: "Thường", URGENT: "Khẩn", STAT: "Cấp cứu" };

export const LAB_RESULT_FLAG: CodeLabelMap = {
  NORMAL: "Bình thường", HIGH: "Cao", LOW: "Thấp", CRITICAL: "Nguy hiểm", ABNORMAL: "Bất thường",
};

// ----- Dược / Dịch vụ -----
export const DRUG_FORM: CodeLabelMap = {
  TABLET: "Viên nén", CAPSULE: "Viên nang", SYRUP: "Syrup", INJ: "Tiêm", CREAM: "Kem",
  OINTMENT: "Mỡ", DROP: "Nhỏ giọt", INHALER: "Hít", POWDER: "Bột", SUPPOSITORY: "Đặt", OTHER: "Khác",
};

export const SERVICE_CATEGORY: CodeLabelMap = {
  CONSULTATION: "Khám bệnh", PROCEDURE: "Thủ thuật", LAB: "Xét nghiệm", RAD: "CĐHA", PHARMACY: "Dược", OTHER: "Khác",
};

export const VAT_RATE: CodeLabelMap = { "0": "0%", "5": "5%", "8": "8%", "10": "10%" };

export const PRESCRIPTION_STATUS: CodeLabelMap = {
  DRAFT: "Nháp", SIGNED: "Đã ký", CANCELLED: "Đã huỷ", DISPENSED: "Đã phát",
};

export const DRUG_STATUS: CodeLabelMap = { ACTIVE: "Đang dùng", INACTIVE: "Ngừng dùng" };

export const STOCK_ADJUST_REASON: CodeLabelMap = {
  STOCKTAKE: "Kiểm kê", DAMAGED: "Hư hỏng", EXPIRED: "Hết hạn", LOST: "Thất thoát", OTHER: "Khác",
};

// ----- Thu ngân / BHYT -----
export const BILLING_STATUS: CodeLabelMap = {
  UNPAID: "Chưa thu", PARTIAL: "Thu một phần", PAID: "Đã thu", REFUNDED: "Đã hoàn", VOID: "Đã huỷ",
};

export const INSURANCE_TYPE: CodeLabelMap = {
  BHYT: "Bảo hiểm y tế (BHYT)", PRIVATE: "Bảo hiểm tư nhân", OTHER: "Khác",
};

export const EINVOICE_PROVIDER: CodeLabelMap = { MISA: "MISA", VNPT: "VNPT", EFY: "EFY" };

// ----- Lịch hẹn -----
export const APPOINTMENT_STATUS: CodeLabelMap = {
  PENDING: "Chờ xác nhận", CONFIRMED: "Đã xác nhận", CHECKED_IN: "Đã check-in",
  CANCELLED: "Đã huỷ", NO_SHOW: "Không đến",
};

export const APPOINTMENT_SOURCE: CodeLabelMap = {
  WALK_IN: "Vãng lai", PHONE: "Điện thoại", WEB: "Website", API: "API", APP: "Ứng dụng",
};

// ----- Hệ thống / RBAC -----
export const ROLE: CodeLabelMap = {
  Admin: "Quản trị viên", BacSi: "Bác sĩ", LeTan: "Lễ tân",
  DuocSi: "Dược sĩ", KeToan: "Kế toán", KyThuatVien: "Kỹ thuật viên",
};

export const USER_STATUS: CodeLabelMap = {
  ACTIVE: "Đang hoạt động", PENDING: "Chờ kích hoạt", LOCKED: "Đã khoá", DISABLED: "Vô hiệu hoá",
};

export const TENANT_STATUS: CodeLabelMap = {
  ACTIVE: "Đang hoạt động", SUSPENDED: "Tạm ngưng", TERMINATED: "Đã chấm dứt",
};

export const COMMON_STATUS: CodeLabelMap = { ACTIVE: "Đang hoạt động", INACTIVE: "Ngừng hoạt động" };

/**
 * Registry: map id nhóm -> nhãn nhóm + dữ liệu. Dùng cho seed CODE_MASTER pha 2.
 */
export const CODE_GROUPS: Record<string, { name: string; items: CodeLabelMap }> = {
  GENDER: { name: "Giới tính", items: GENDER },
  BLOOD_TYPE: { name: "Nhóm máu", items: BLOOD_TYPE },
  NATIONALITY: { name: "Quốc tịch", items: NATIONALITY },
  MARITAL_STATUS: { name: "Tình trạng hôn nhân", items: MARITAL_STATUS },
  PATIENT_TYPE: { name: "Đối tượng bệnh nhân", items: PATIENT_TYPE },
  VISIT_TYPE: { name: "Loại khám (tiếp đón)", items: VISIT_TYPE },
  RELATIONSHIP: { name: "Quan hệ liên hệ", items: RELATIONSHIP },
  ENCOUNTER_TYPE: { name: "Loại lượt khám", items: ENCOUNTER_TYPE },
  ENCOUNTER_STATUS: { name: "Trạng thái lượt khám", items: ENCOUNTER_STATUS },
  DIABETES_TYPE: { name: "Loại đái tháo đường", items: DIABETES_TYPE },
  MODALITY: { name: "Phương thức CĐHA", items: MODALITY },
  CLS_PRIORITY: { name: "Độ ưu tiên CLS", items: CLS_PRIORITY },
  LAB_RESULT_FLAG: { name: "Cờ kết quả xét nghiệm", items: LAB_RESULT_FLAG },
  DRUG_FORM: { name: "Dạng bào chế", items: DRUG_FORM },
  SERVICE_CATEGORY: { name: "Nhóm dịch vụ", items: SERVICE_CATEGORY },
  VAT_RATE: { name: "Thuế VAT", items: VAT_RATE },
  PRESCRIPTION_STATUS: { name: "Trạng thái đơn thuốc", items: PRESCRIPTION_STATUS },
  DRUG_STATUS: { name: "Trạng thái thuốc", items: DRUG_STATUS },
  STOCK_ADJUST_REASON: { name: "Lý do điều chỉnh kho", items: STOCK_ADJUST_REASON },
  BILLING_STATUS: { name: "Trạng thái hóa đơn", items: BILLING_STATUS },
  INSURANCE_TYPE: { name: "Loại bảo hiểm", items: INSURANCE_TYPE },
  EINVOICE_PROVIDER: { name: "Nhà cung cấp HĐĐT", items: EINVOICE_PROVIDER },
  APPOINTMENT_STATUS: { name: "Trạng thái lịch hẹn", items: APPOINTMENT_STATUS },
  APPOINTMENT_SOURCE: { name: "Nguồn lịch hẹn", items: APPOINTMENT_SOURCE },
  // LƯU Ý: ROLE có master table (diab_his_sec_roles) -> KHÔNG seed vào CODE_MASTER.
  // Dropdown Vai trò lấy label từ API roles; map ROLE ở trên chỉ dùng hiển thị tạm.
  USER_STATUS: { name: "Trạng thái người dùng", items: USER_STATUS },
  TENANT_STATUS: { name: "Trạng thái phòng khám", items: TENANT_STATUS },
  COMMON_STATUS: { name: "Trạng thái chung", items: COMMON_STATUS },
};
