using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Whitelist 4 Dataset cho Report Builder P1 (docs/prd/report-builder-prd.md §2). Moi Dataset khai bao san
/// base+joins CO DINH (da bake dung COLLATE theo tung bang — xem ghi chu tung dataset) + danh sach truong
/// duoc phep dung. Nguoi dung KHONG bao gio thay/sua SqlExpr — chi chon field theo Key qua UI.
/// </summary>
public class DatasetRegistry : IDatasetRegistry
{
    private readonly IReadOnlyList<Dataset> _all;
    private readonly IReadOnlyDictionary<string, Dataset> _byKey;

    public DatasetRegistry()
    {
        _all = new List<Dataset> { ThuNgan(), LuotKham(), Kho(), DonThuoc(), CongNo(), Cls() };
        _byKey = _all.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<Dataset> GetAll() => _all;

    public Dataset? GetByKey(string key) => _byKey.TryGetValue(key, out var d) ? d : null;

    // ================= Dataset 1: Thu ngan (Cashier/Billing) ================= //
    // diab_his_bil_payments (unicode_ci) JOIN bil_billing (unicode_ci, cung nhom) JOIN pat_patients
    // (0900_ai_ci, khac collation voi bil_* -> phai COLLATE khi so sanh id).
    private static Dataset ThuNgan()
    {
        const string from = @"
            diab_his_bil_payments p
            INNER JOIN diab_his_bil_billing b ON b.id = p.billing_id AND b.tenant_id = p.tenant_id
            LEFT JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
            LEFT JOIN diab_his_sec_users u ON u.id COLLATE utf8mb4_unicode_ci = p.paid_by";

        const string baseWhere = "p.tenant_id = @tenantId AND p.status IN ('COMPLETED','REFUNDED') AND b.deleted_at IS NULL";

        var fields = new List<DatasetField>
        {
            DatasetField.Dimension("paidDate", "Ngày thu", "DATE(p.paid_at)", ReportColumnType.Date),
            DatasetField.Dimension("collectorName", "Người thu", "COALESCE(u.full_name, N'Chưa xác định')", ReportColumnType.Text),
            DatasetField.Dimension("method", "Phương thức", "p.method", ReportColumnType.Text),
            DatasetField.Dimension("patientName", "Bệnh nhân", "pt.full_name", ReportColumnType.Text),
            DatasetField.Dimension("patientCode", "Mã bệnh nhân", "pt.code", ReportColumnType.Text),
            DatasetField.Measure("amount", "Thực thu", "p.amount", ReportColumnType.Money,
                ReportAggregation.Sum, ReportAggregation.Avg, ReportAggregation.Min, ReportAggregation.Max),
            DatasetField.Measure("paymentCount", "Số phiếu", "p.id", ReportColumnType.Number,
                ReportAggregation.Count, ReportAggregation.CountDistinct)
        };

        return new Dataset("thu-ngan", "Thu ngân", from, baseWhere, "paidDate", fields);
    }

    // ================= Dataset 2: Luot kham (Encounters) ================= //
    // diab_his_enc_encounters/diagnoses/sec_users/sys_rooms deu 0900_ai_ci -> khong can COLLATE.
    private static Dataset LuotKham()
    {
        const string from = @"
            diab_his_enc_encounters e
            LEFT JOIN diab_his_enc_diagnoses d ON d.encounter_id = e.id AND d.type = 'PRIMARY' AND d.deleted_at IS NULL
            LEFT JOIN diab_his_sec_users doc ON doc.id = e.doctor_id
            LEFT JOIN diab_his_sys_rooms r ON r.id = e.room_id";

        const string baseWhere = "e.tenant_id = @tenantId AND e.deleted_at IS NULL";

        var fields = new List<DatasetField>
        {
            DatasetField.Dimension("visitDate", "Ngày khám", "DATE(COALESCE(e.started_at, e.created_at))", ReportColumnType.Date),
            DatasetField.Dimension("doctorName", "Bác sĩ", "COALESCE(doc.full_name, N'Chưa xác định')", ReportColumnType.Text),
            DatasetField.Dimension("roomName", "Phòng", "COALESCE(r.name, N'Chưa xếp phòng')", ReportColumnType.Text),
            DatasetField.Dimension("icd10Code", "Mã ICD-10", "COALESCE(d.icd10_code, e.primary_icd10, N'Chưa ghi nhận')", ReportColumnType.Text),
            DatasetField.Dimension("visitHour", "Giờ khám", "HOUR(COALESCE(e.started_at, e.created_at))", ReportColumnType.Number),
            DatasetField.Measure("visitCount", "Số lượt", "e.id", ReportColumnType.Number,
                ReportAggregation.Count, ReportAggregation.CountDistinct),
            DatasetField.Measure("patientCount", "Số BN", "e.patient_id", ReportColumnType.Number,
                ReportAggregation.CountDistinct)
        };

        return new Dataset("luot-kham", "Lượt khám", from, baseWhere, "visitDate", fields);
    }

    // ================= Dataset 3: Kho duoc (Pharmacy stock) ================= //
    // diab_his_pha_stock/pha_drugs deu 0900_ai_ci -> khong can COLLATE. pha_stock KHONG co cot deleted_at
    // (chi co deleted_by) -> khong loc theo deleted_at o bang nay.
    private static Dataset Kho()
    {
        const string from = @"
            diab_his_pha_stock s
            INNER JOIN diab_his_pha_drugs dr ON dr.id = s.drug_id AND dr.tenant_id = s.tenant_id";

        const string baseWhere = "s.tenant_id = @tenantId AND dr.deleted_at IS NULL";

        var fields = new List<DatasetField>
        {
            DatasetField.Dimension("drugName", "Tên thuốc", "dr.name", ReportColumnType.Text),
            DatasetField.Dimension("drugCode", "Mã thuốc", "dr.code", ReportColumnType.Text),
            DatasetField.Dimension("lotNumber", "Số lô", "s.lot_number", ReportColumnType.Text),
            DatasetField.Dimension("expDate", "Hạn sử dụng", "s.exp_date", ReportColumnType.Date),
            DatasetField.Dimension("drugCategory", "Nhóm thuốc", "COALESCE(dr.drug_category, N'Chưa phân nhóm')", ReportColumnType.Text),
            DatasetField.Dimension("importedDate", "Ngày nhập", "DATE(s.created_at)", ReportColumnType.Date),
            DatasetField.Measure("quantity", "SL tồn", "s.quantity", ReportColumnType.Number,
                ReportAggregation.Sum, ReportAggregation.Avg, ReportAggregation.Min, ReportAggregation.Max),
            DatasetField.Measure("stockValue", "Giá trị tồn", "(s.quantity * s.import_price)", ReportColumnType.Money,
                ReportAggregation.Sum, ReportAggregation.Avg)
        };

        // Dataset kho khong co "khoang ngay giao dich" ro rang (ton kho la snapshot) — dung ngay nhap lo
        // (created_at) lam truong ngay bat buoc de tuong thich khung from/to chung cua engine.
        return new Dataset("kho", "Kho dược", from, baseWhere, "importedDate", fields);
    }

    // ================= Dataset 4: Don thuoc (Prescriptions) ================= //
    // diab_his_pha_prescriptions/prescription_items/pha_drugs/sec_users deu 0900_ai_ci -> khong can COLLATE.
    private static Dataset DonThuoc()
    {
        const string from = @"
            diab_his_pha_prescription_items it
            INNER JOIN diab_his_pha_prescriptions rx ON rx.id = it.prescription_id AND rx.tenant_id = it.tenant_id
            INNER JOIN diab_his_pha_drugs dr ON dr.id = it.drug_id AND dr.tenant_id = it.tenant_id
            LEFT JOIN diab_his_sec_users doc ON doc.id = rx.doctor_id";

        const string baseWhere = "it.tenant_id = @tenantId AND it.deleted_at IS NULL AND rx.deleted_at IS NULL";

        var fields = new List<DatasetField>
        {
            DatasetField.Dimension("drugName", "Tên thuốc", "dr.name", ReportColumnType.Text),
            DatasetField.Dimension("doctorName", "Bác sĩ", "COALESCE(doc.full_name, N'Chưa xác định')", ReportColumnType.Text),
            DatasetField.Dimension("prescribedDate", "Ngày kê", "DATE(rx.created_at)", ReportColumnType.Date),
            DatasetField.Dimension("status", "Trạng thái đơn", "rx.status", ReportColumnType.Text),
            DatasetField.Measure("prescribedCount", "Số lần kê", "it.id", ReportColumnType.Number,
                ReportAggregation.Count, ReportAggregation.CountDistinct),
            DatasetField.Measure("totalQuantity", "Tổng SL", "it.quantity", ReportColumnType.Number,
                ReportAggregation.Sum, ReportAggregation.Avg)
        };

        return new Dataset("don-thuoc", "Đơn thuốc", from, baseWhere, "prescribedDate", fields);
    }

    // ================= Dataset 5: Cong no (Billing balance > 0) ================= //
    // diab_his_bil_billing la unicode_ci (nhom bil_* dung chung), pat_patients la 0900_ai_ci -> phai COLLATE
    // khi join. Bang khong co cot bill_date rieng -> dung created_at lam truong ngay bat buoc.
    private static Dataset CongNo()
    {
        const string from = @"
            diab_his_bil_billing b
            LEFT JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id";

        const string baseWhere = "b.tenant_id = @tenantId AND b.deleted_at IS NULL AND b.balance > 0";

        var fields = new List<DatasetField>
        {
            DatasetField.Dimension("billDate", "Ngày", "DATE(b.created_at)", ReportColumnType.Date),
            DatasetField.Dimension("patientName", "Bệnh nhân", "pt.full_name", ReportColumnType.Text),
            DatasetField.Dimension("patientCode", "Mã bệnh nhân", "pt.code", ReportColumnType.Text),
            DatasetField.Dimension("status", "Trạng thái", "b.status", ReportColumnType.Text),
            DatasetField.Measure("balance", "Còn nợ", "b.balance", ReportColumnType.Money,
                ReportAggregation.Sum, ReportAggregation.Avg, ReportAggregation.Min, ReportAggregation.Max),
            DatasetField.Measure("billCount", "Số phiếu", "b.id", ReportColumnType.Number,
                ReportAggregation.Count, ReportAggregation.CountDistinct)
        };

        return new Dataset("cong-no", "Công nợ", from, baseWhere, "billDate", fields);
    }

    // ================= Dataset 6: Chi dinh CLS (Lab + Rad orders gop qua UNION ALL) ================= //
    // diab_his_lab_orders/rad_orders/sec_users deu 0900_ai_ci -> khong can COLLATE. Gop 2 bang bang subquery
    // UNION ALL lam FromSql (alias "cls") — cac DatasetField ben duoi tham chieu truc tiep alias nay, giong
    // nhu tham chieu 1 bang thuong; SafeQueryBuilder khong can biet gi ve UNION ben trong.
    private static Dataset Cls()
    {
        const string from = @"
            (
                SELECT lo.id AS id, lo.tenant_id AS tenant_id, lo.ordered_at AS ordered_at,
                       lo.ordered_by AS ordered_by, N'Xét nghiệm' AS modality, lo.deleted_at AS deleted_at
                FROM diab_his_lab_orders lo
                UNION ALL
                SELECT ro.id AS id, ro.tenant_id AS tenant_id, ro.ordered_at AS ordered_at,
                       ro.ordered_by AS ordered_by, ro.modality AS modality, ro.deleted_at AS deleted_at
                FROM diab_his_rad_orders ro
            ) cls
            LEFT JOIN diab_his_sec_users doc ON doc.id = cls.ordered_by";

        const string baseWhere = "cls.tenant_id = @tenantId AND cls.deleted_at IS NULL";

        var fields = new List<DatasetField>
        {
            DatasetField.Dimension("orderedDate", "Ngày chỉ định", "DATE(cls.ordered_at)", ReportColumnType.Date),
            DatasetField.Dimension("modality", "Loại", "cls.modality", ReportColumnType.Text),
            DatasetField.Dimension("doctorName", "Bác sĩ chỉ định", "COALESCE(doc.full_name, N'Chưa xác định')", ReportColumnType.Text),
            DatasetField.Measure("orderCount", "Số lượt", "cls.id", ReportColumnType.Number,
                ReportAggregation.Count, ReportAggregation.CountDistinct)
        };

        return new Dataset("cls", "Chỉ định CLS", from, baseWhere, "orderedDate", fields);
    }
}
