using Dapper;
using ProDiabHis.Application.Reports.Engine;

namespace ProDiabHis.Infrastructure.Reports;

/// <summary>
/// Dang ky tap trung ReportDescriptor cho toan bo danh muc 23 bao cao (docs/prd/reports-catalog-prd.md).
/// Dot nay implement SQL that cho Nhom A (A1-A6) — Financial. Cac bao cao Nhom B..E se bo sung o cac dot sau,
/// dung chung engine nay (chi can them descriptor, KHONG sua controller/exporter/code-gen).
/// </summary>
public class ReportRegistry : IReportRegistry
{
    private readonly IReadOnlyList<ReportDescriptor> _all;
    private readonly IReadOnlyDictionary<string, ReportDescriptor> _byCode;

    public ReportRegistry()
    {
        _all = BuildDescriptors();
        _byCode = _all.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ReportDescriptor> GetAll() => _all;

    public ReportDescriptor? GetByCode(string code)
        => _byCode.TryGetValue(code, out var d) ? d : null;

    private static IReadOnlyList<ReportDescriptor> BuildDescriptors()
    {
        var list = new List<ReportDescriptor>
        {
            RevenueDaily(),
            RefundReceipts(),
            VoidReceipts(),
            Advances(),
            FeeDetail(),
            LabSummary(),

            // Nhom B — CTDV BN (Clinical)
            CtdvKhamBenh(),
            CtdvSieuAm(),
            CtdvXQuang(),
            CtdvNoiSoi(),
            CtdvThuThuat(),
            CtdvXetNghiem(),

            // Nhom C — So (register)
            SoKhamBenh(),
            SoSieuAm(),
            SoXQuang(),
            SoNoiSoi(),
            SoThuThuat(),
            SoXetNghiem(),
            SoDienTim(),

            // Nhom D — Thong ke luot kham
            LuotKhamTheoBs(),
            LuotKhamTheoPk(),

            // Nhom E — BHXH / lam sang dac thu
            BenhDienTien(),
            NghiHuongBhxh(),

            // Dot 7 — "Gat nhanh" quick-win (descriptor thuan, khong bang/migration moi)
            Icd10Stats(),
            TopDrugs(),
            TopServices(),
            RevenueMonthly(),
            PatientSourceSummary(),
            ClsIndicationStats(),
            Debts(),

            // Dot 9 — Compliance tai chinh (P0-1): So quy tien mat (UNION Thu bil_payments + Chi bil_cash_out)
            SoQuyTienMat(),

            // Nhom F — Kho duoc (Pharmacy) — descriptor thuan tren schema co san (khong bang/migration moi)
            TonKho(),
            TheKhoLo(),
            ThuocCanDate(),
            XuatNhapTon(),
            DanhMucThuoc(),
            ThuocKiemSoat(),
            ThuocDuoiDinhMuc(),

            // Dot 11 — BI (Statistics) + Kiem ke kho (Pharmacy). Ghi chu: bao cao "ty-le-tai-kham"
            // (thong ke ty le tai kham theo patient_source) BO QUA vi trung lap ro rang voi
            // "patient-source-summary" da co san (cung group-by patient_source, cung % — xem GroupOrder 6
            // o tren): chi khac ten/nhan manh, khong them gia tri thong tin moi.
            LuotKhamTheoGio(),
            TyLeNoShow(),
            SuDungKhangSinh(),
            TatCls(),
            KiemKeKho()
        };

        // Tu suy Orientation theo so cot (>=11 cot => Landscape) cho toan bo descriptor.
        return list
            .Select(d => d with { Orientation = ReportDescriptor.OrientationFor(d.Columns.Count) })
            .ToList();
    }

    // ================= A1: BC Doanh Thu Ngay (mau chuan) ================= //

    private static ReportDescriptor RevenueDaily() => new()
    {
        Code = "revenue-daily",
        Title = "BÁO CÁO DOANH THU NGÀY",
        Group = ReportGroupCategory.Financial,
        GroupOrder = 1,
        Icon = "banknote",
        PdfTypeCode = "RVD",
        GroupByKey = "collectorName",
        ShowGroupCount = true,
        Columns = new List<ReportColumn>
        {
            new("date",          "Ngày",          ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("receiptNo",     "Số Phiếu",      ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientCode",   "Mã",            ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientName",   "Họ tên",        ReportColumnType.Text,     ReportAlign.Left,  1.3f),
            new("description",   "Diễn giải",     ReportColumnType.Text,     ReportAlign.Left,  1.6f),
            new("totalCost",     "Tổng CP",       ReportColumnType.Money,    ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("discount",      "Số giảm",       ReportColumnType.Money,    ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("surcharge",     "Số tăng",       ReportColumnType.Money,    ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("refund",        "Hoàn trả",      ReportColumnType.Money,    ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("advance",       "Tạm ứng",       ReportColumnType.Money,    ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("cash",          "Tiền mặt",      ReportColumnType.Money,    ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("bankTransfer",  "Chuyển khoản",  ReportColumnType.Money,    ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("card",          "Thẻ",           ReportColumnType.Money,    ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("netCollected",  "Thực thu",      ReportColumnType.Money,    ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("patientSource", "Nguồn khách",   ReportColumnType.Text,     ReportAlign.Left,  0.9f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG THỰC THU", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "netCollected"))), IsMoney: true),
            new("SỐ PHIẾU THU",  "#FFFBEB", rows => rows.Count, IsMoney: false),
            new("TB / PHIẾU",    "#F0FDFA", rows => rows.Count > 0
                ? rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "netCollected"))) / rows.Count
                : 0m, IsMoney: true)
        },
        Filters = new List<ReportFilter>
        {
            new("collectorId", "Người thu", ReportFilterType.Select, OptionsSource: "collectors"),
            new("counterId",   "Quầy thu",  ReportFilterType.Select, OptionsSource: "counters"),
            new("variance",    "Tiền chênh lệch", ReportFilterType.Enum)
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("collectorId", ctx.Filter("collectorId"));
            p.Add("counterId", ctx.Filter("counterId"));
            p.Add("variance", ctx.Filter("variance") ?? "ALL");

            const string sql = @"
                SELECT
                    COALESCE(MIN(p.paid_at), b.created_at)               AS date,
                    b.bill_no                                            AS receiptNo,
                    pt.code                                               AS patientCode,
                    pt.full_name                                         AS patientName,
                    COALESCE(
                        (SELECT bi.name FROM diab_his_bil_billing_items bi
                          WHERE bi.billing_id = b.id ORDER BY bi.id LIMIT 1),
                        'Thu phí dịch vụ') AS description,
                    b.subtotal                                            AS totalCost,
                    b.discount_amount                                     AS discount,
                    0                                                      AS surcharge,
                    COALESCE(SUM(p.refunded_amount), 0)                   AS refund,
                    0                                                      AS advance,
                    COALESCE(SUM(CASE WHEN p.method = 'CASH' THEN p.amount ELSE 0 END), 0)                     AS cash,
                    COALESCE(SUM(CASE WHEN p.method = 'BANK_TRANSFER' THEN p.amount ELSE 0 END), 0)            AS bankTransfer,
                    COALESCE(SUM(CASE WHEN p.method IN ('VISA','MASTER') THEN p.amount ELSE 0 END), 0)         AS card,
                    b.paid_amount                                          AS netCollected,
                    CASE COALESCE(pt.patient_source, 'WALK_IN')
                        WHEN 'WALK_IN'    THEN N'Vãng lai'
                        WHEN 'REFERRAL'   THEN N'Giới thiệu'
                        WHEN 'RETURN'     THEN N'Tái khám'
                        WHEN 'ONLINE'     THEN N'Đặt khám online'
                        WHEN 'INSURANCE'  THEN N'BHYT/bảo lãnh'
                        WHEN 'MARKETING'  THEN N'Marketing'
                        ELSE N'Khác'
                    END                                                    AS patientSource,
                    COALESCE(
                        (SELECT u.full_name FROM diab_his_bil_payments pu
                          INNER JOIN diab_his_sec_users u ON u.id COLLATE utf8mb4_unicode_ci = pu.paid_by
                          WHERE pu.billing_id = b.id AND pu.paid_by IS NOT NULL
                          ORDER BY pu.paid_at LIMIT 1),
                        N'Chưa xác định') AS collectorName
                FROM diab_his_bil_billing b
                INNER JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
                LEFT JOIN diab_his_bil_payments p
                    ON p.billing_id = b.id AND p.tenant_id = b.tenant_id
                   AND p.status IN ('COMPLETED', 'REFUNDED')
                   AND p.paid_at BETWEEN @from AND @to
                WHERE b.tenant_id = @tenantId
                  AND b.deleted_at IS NULL
                  AND b.status IN ('FINALIZED', 'PARTIAL_PAID', 'PAID')
                  AND EXISTS (
                        SELECT 1 FROM diab_his_bil_payments px
                         WHERE px.billing_id = b.id AND px.tenant_id = b.tenant_id
                           AND px.paid_at BETWEEN @from AND @to)
                  AND (@collectorId IS NULL OR EXISTS (
                        SELECT 1 FROM diab_his_bil_payments pc
                         WHERE pc.billing_id = b.id AND pc.paid_by = @collectorId))
                  AND (@counterId IS NULL OR b.counter_id = @counterId)
                GROUP BY b.id, b.bill_no, pt.code, pt.full_name, b.subtotal, b.discount_amount,
                         b.paid_amount, pt.patient_source, b.created_at
                HAVING (@variance = 'ALL')
                    OR (@variance = 'DIFF'   AND (b.subtotal - b.discount_amount - b.paid_amount) <> 0)
                    OR (@variance = 'NODIFF' AND (b.subtotal - b.discount_amount - b.paid_amount) = 0)
                ORDER BY collectorName, date
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ================= A2: BC Hoan Tra Phieu Thu ================= //
    // TODO schema: diab_his_bil_payments chua co cot rieng "refund_reason" / "refunded_by".
    // Best-effort: dung payments.note lam ly do hoan, payments.paid_by lam nguoi thuc hien (gan dung).
    private static ReportDescriptor RefundReceipts() => new()
    {
        Code = "refund-receipts",
        Title = "BÁO CÁO HOÀN TRẢ PHIẾU THU",
        Group = ReportGroupCategory.Financial,
        GroupOrder = 2,
        Icon = "rotate-ccw",
        PdfTypeCode = "RFD",
        Columns = new List<ReportColumn>
        {
            new("date",        "Ngày",           ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("receiptNo",   "Số phiếu gốc",   ReportColumnType.Text,     ReportAlign.Left,  1f),
            new("patientCode", "Mã BN",          ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientName", "Họ tên",         ReportColumnType.Text,     ReportAlign.Left,  1.3f),
            new("reason",      "Lý do hoàn",     ReportColumnType.Text,     ReportAlign.Left,  1.6f),
            new("refundAmount","Số tiền hoàn",   ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("performedBy", "Người thực hiện",ReportColumnType.Text,     ReportAlign.Left,  1.1f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG TIỀN HOÀN", "#FEF2F2", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "refundAmount"))), IsMoney: true),
            new("SỐ PHIẾU HOÀN",  "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>
        {
            new("collectorId", "Người thu", ReportFilterType.Select, OptionsSource: "collectors"),
            new("counterId",   "Quầy thu",  ReportFilterType.Select, OptionsSource: "counters")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("collectorId", ctx.Filter("collectorId"));
            p.Add("counterId", ctx.Filter("counterId"));

            const string sql = @"
                SELECT
                    pay.paid_at                          AS date,
                    b.bill_no                            AS receiptNo,
                    pt.code                              AS patientCode,
                    pt.full_name                         AS patientName,
                    COALESCE(pay.note, N'Không ghi nhận lý do') AS reason,
                    pay.refunded_amount                  AS refundAmount,
                    COALESCE(u.full_name, N'Chưa xác định') AS performedBy
                FROM diab_his_bil_payments pay
                INNER JOIN diab_his_bil_billing b ON b.id = pay.billing_id AND b.tenant_id = pay.tenant_id
                INNER JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
                LEFT JOIN diab_his_sec_users u ON u.id COLLATE utf8mb4_unicode_ci = pay.paid_by
                WHERE pay.tenant_id = @tenantId
                  AND b.deleted_at IS NULL
                  AND (pay.status = 'REFUNDED' OR pay.refunded_amount > 0)
                  AND pay.paid_at BETWEEN @from AND @to
                  AND (@collectorId IS NULL OR pay.paid_by = @collectorId)
                  AND (@counterId IS NULL OR b.counter_id = @counterId)
                ORDER BY pay.paid_at DESC
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ================= A3: BC Huy Phieu Thu ================= //
    // TODO schema: diab_his_bil_billing chua co cot rieng "voided_by"/"voided_at" —
    // best-effort dung updated_by/updated_at khi status = VOID.
    private static ReportDescriptor VoidReceipts() => new()
    {
        Code = "void-receipts",
        Title = "BÁO CÁO HỦY PHIẾU THU",
        Group = ReportGroupCategory.Financial,
        GroupOrder = 3,
        Icon = "ban",
        PdfTypeCode = "VOD",
        Columns = new List<ReportColumn>
        {
            new("date",        "Ngày",         ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("receiptNo",   "Số phiếu",     ReportColumnType.Text,     ReportAlign.Left,  1f),
            new("patientCode", "Mã BN",        ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientName", "Họ tên",       ReportColumnType.Text,     ReportAlign.Left,  1.3f),
            new("reason",      "Lý do hủy",    ReportColumnType.Text,     ReportAlign.Left,  1.8f),
            new("amount",      "Số tiền",      ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("performedBy", "Người hủy",    ReportColumnType.Text,     ReportAlign.Left,  1.1f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG TIỀN HỦY", "#FEF2F2", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "amount"))), IsMoney: true),
            new("SỐ PHIẾU HỦY",  "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>
        {
            new("collectorId", "Người thu", ReportFilterType.Select, OptionsSource: "collectors"),
            new("counterId",   "Quầy thu",  ReportFilterType.Select, OptionsSource: "counters")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("collectorId", ctx.Filter("collectorId"));
            p.Add("counterId", ctx.Filter("counterId"));

            const string sql = @"
                SELECT
                    b.updated_at                          AS date,
                    b.bill_no                              AS receiptNo,
                    pt.code                                AS patientCode,
                    pt.full_name                           AS patientName,
                    COALESCE(b.void_reason, N'Không ghi nhận lý do') AS reason,
                    b.patient_payable                      AS amount,
                    COALESCE(u.full_name, N'Chưa xác định') AS performedBy
                FROM diab_his_bil_billing b
                INNER JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
                LEFT JOIN diab_his_sec_users u ON u.id COLLATE utf8mb4_unicode_ci = b.updated_by
                WHERE b.tenant_id = @tenantId
                  AND b.status = 'VOID'
                  AND b.updated_at BETWEEN @from AND @to
                  AND (@collectorId IS NULL OR b.created_by = @collectorId)
                  AND (@counterId IS NULL OR b.counter_id = @counterId)
                ORDER BY b.updated_at DESC
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ================= A4: BC Tam Ung ================= //
    // TODO schema: HE THONG CHUA CO bang luu tam ung/dat coc benh nhan (vd diab_his_bil_advances:
    // id, tenant_id, patient_id, billing_id, amount, applied_amount, remaining_amount, status, created_at).
    // Bao cao nay tam thoi tra ve rong (query an toan tenant-scoped) cho den khi bo sung bang.
    private static ReportDescriptor Advances() => new()
    {
        Code = "advances",
        Title = "BÁO CÁO TẠM ỨNG",
        Group = ReportGroupCategory.Financial,
        GroupOrder = 4,
        Icon = "wallet",
        PdfTypeCode = "ADV",
        Columns = new List<ReportColumn>
        {
            new("date",        "Ngày",          ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("receiptNo",   "Số phiếu",      ReportColumnType.Text,     ReportAlign.Left,  1f),
            new("patientCode", "Mã BN",         ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientName", "Họ tên",        ReportColumnType.Text,     ReportAlign.Left,  1.3f),
            new("advanceAmount","Số tạm ứng",   ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("deducted",    "Đã trừ",        ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("remaining",   "Còn lại",       ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG TẠM ỨNG", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "advanceAmount"))), IsMoney: true),
            new("CÒN LẠI",      "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "remaining"))), IsMoney: true)
        },
        Filters = new List<ReportFilter>
        {
            new("status", "Trạng thái", ReportFilterType.Enum)
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            // TODO schema: chua co bang tam ung — tra ve tap rong an toan (tenant-scoped, khong loi).
            const string sql = @"
                SELECT
                    b.created_at AS date, b.bill_no AS receiptNo, pt.code AS patientCode,
                    pt.full_name AS patientName,
                    0 AS advanceAmount, 0 AS deducted, 0 AS remaining
                FROM diab_his_bil_billing b
                INNER JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
                WHERE b.tenant_id = @tenantId AND 1 = 0
                LIMIT 0";

            return (sql, p);
        }
    };

    // ================= A5: BC Chi Tiet Vien Phi ================= //
    private static ReportDescriptor FeeDetail() => new()
    {
        Code = "fee-detail",
        Title = "BÁO CÁO CHI TIẾT VIỆN PHÍ",
        Group = ReportGroupCategory.Financial,
        GroupOrder = 5,
        Icon = "receipt",
        PdfTypeCode = "FEE",
        Columns = new List<ReportColumn>
        {
            new("receiptNo",    "Số phiếu",     ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("patientName",  "Bệnh nhân",    ReportColumnType.Text,   ReportAlign.Left,  1.3f),
            new("serviceGroup", "Nhóm DV",      ReportColumnType.Text,   ReportAlign.Left,  1f),
            new("serviceName",  "Tên DV",       ReportColumnType.Text,   ReportAlign.Left,  1.6f),
            new("quantity",     "SL",           ReportColumnType.Number, ReportAlign.Right, 0.5f),
            new("unitPrice",    "Đơn giá",      ReportColumnType.Money,  ReportAlign.Right, 0.9f),
            new("lineTotal",    "Thành tiền",   ReportColumnType.Money,  ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("bhytAmount",   "BHYT chi trả", ReportColumnType.Money,  ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("patientAmount","BN tự trả",    ReportColumnType.Money,  ReportAlign.Right, 0.9f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG THÀNH TIỀN", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "lineTotal"))), IsMoney: true),
            new("BHYT CHI TRẢ",    "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "bhytAmount"))), IsMoney: true)
        },
        Filters = new List<ReportFilter>
        {
            new("patientId", "Bệnh nhân", ReportFilterType.Select, OptionsSource: "patients"),
            new("itemType",  "Nhóm dịch vụ", ReportFilterType.Select)
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("patientId", ctx.Filter("patientId"));
            p.Add("itemType", ctx.Filter("itemType"));

            const string sql = @"
                SELECT
                    b.bill_no                              AS receiptNo,
                    pt.full_name                            AS patientName,
                    bi.item_type                            AS serviceGroup,
                    bi.name                                 AS serviceName,
                    bi.quantity                              AS quantity,
                    bi.unit_price                            AS unitPrice,
                    bi.line_total                            AS lineTotal,
                    bi.bhyt_amount                           AS bhytAmount,
                    (bi.line_total - bi.bhyt_amount)         AS patientAmount
                FROM diab_his_bil_billing_items bi
                INNER JOIN diab_his_bil_billing b ON b.id = bi.billing_id AND b.tenant_id = bi.tenant_id
                INNER JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
                WHERE bi.tenant_id = @tenantId
                  AND b.deleted_at IS NULL
                  AND b.created_at BETWEEN @from AND @to
                  AND (@patientId IS NULL OR b.patient_id = @patientId)
                  AND (@itemType IS NULL OR bi.item_type = @itemType)
                ORDER BY b.created_at DESC
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ================= A6: BC Tong Hop Xet Nghiem ================= //
    // Best-effort: doanh thu lay tu diab_his_bil_billing_items (item_type='LAB', ref_id = lab order id)
    // neu co gia tri thuc te; neu chua phat sinh hoa don thi fallback ve don gia danh muc (dict_lab_tests).
    private static ReportDescriptor LabSummary() => new()
    {
        Code = "lab-summary",
        Title = "BÁO CÁO TỔNG HỢP XÉT NGHIỆM",
        Group = ReportGroupCategory.Financial,
        GroupOrder = 6,
        Icon = "flask-conical",
        PdfTypeCode = "LAB",
        Columns = new List<ReportColumn>
        {
            new("testGroup", "Nhóm XN",   ReportColumnType.Text,  ReportAlign.Left,  1f),
            new("testName",  "Tên XN",    ReportColumnType.Text,  ReportAlign.Left,  2f),
            new("visitCount","Số lượt",   ReportColumnType.Number,ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("unitPrice", "Đơn giá",   ReportColumnType.Money, ReportAlign.Right, 1f),
            new("revenue",   "Doanh thu", ReportColumnType.Money, ReportAlign.Right, 1.1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG DOANH THU XN", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "revenue"))), IsMoney: true),
            new("SỐ LƯỢT XN",        "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "visitCount"))), IsMoney: false)
        },
        Filters = new List<ReportFilter>
        {
            new("testCode", "Loại XN", ReportFilterType.Select)
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("testCode", ctx.Filter("testCode"));

            const string sql = @"
                SELECT
                    N'Xét nghiệm'                                          AS testGroup,
                    MAX(COALESCE(dt.name COLLATE utf8mb4_0900_ai_ci, lo.test_name)) AS testName,
                    COUNT(DISTINCT lo.id)                                  AS visitCount,
                    COALESCE(AVG(bi.unit_price), MAX(dt.default_price), 0) AS unitPrice,
                    COALESCE(SUM(bi.line_total), 0)                        AS revenue
                FROM diab_his_lab_orders lo
                LEFT JOIN diab_his_dict_lab_tests dt ON dt.code = lo.test_code COLLATE utf8mb4_unicode_ci
                LEFT JOIN diab_his_bil_billing_items bi
                    ON bi.ref_id = lo.id COLLATE utf8mb4_unicode_ci AND bi.item_type = 'LAB' AND bi.tenant_id = @tenantId
                WHERE lo.tenant_id = @tenantId
                  AND lo.deleted_at IS NULL
                  AND lo.ordered_at BETWEEN @from AND @to
                  AND (@testCode IS NULL OR lo.test_code = @testCode)
                GROUP BY lo.test_code
                ORDER BY revenue DESC
                LIMIT 2000";

            return (sql, p);
        }
    };

    // ============================================================================
    // NHOM B — CHI TIET DICH VU BENH NHAN (CTDV BN) — group=Clinical
    // Cung 1 khuon: liet ke dich vu theo luot BN, khac nhau loai dich vu / bang nguon.
    // ============================================================================

    // ================= B1: CTDV BN Kham Benh ================= //
    private static ReportDescriptor CtdvKhamBenh() => new()
    {
        Code = "ctdv-kham-benh",
        Title = "BÁO CÁO CTDV BN KHÁM BỆNH",
        Group = ReportGroupCategory.Clinical,
        GroupOrder = 1,
        Icon = "stethoscope",
        PdfTypeCode = "CTK",
        Columns = new List<ReportColumn>
        {
            new("date",       "Ngày",           ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("patientCode","Mã BN",          ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientName","Họ tên",         ReportColumnType.Text,     ReportAlign.Left,  1.3f),
            new("doctorName", "Bác sĩ",         ReportColumnType.Text,     ReportAlign.Left,  1.1f),
            new("diagnosis",  "Chẩn đoán ICD-10", ReportColumnType.Text,   ReportAlign.Left,  1.6f),
            new("roomName",   "Phòng khám",     ReportColumnType.Text,     ReportAlign.Left,  1f),
            new("amount",     "Thành tiền",     ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LƯỢT KHÁM", "#F0FDFA", rows => rows.Count, IsMoney: false),
            new("TỔNG THÀNH TIỀN", "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "amount"))), IsMoney: true)
        },
        Filters = new List<ReportFilter>
        {
            new("doctorId", "Bác sĩ", ReportFilterType.Select, OptionsSource: "doctors")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("doctorId", ctx.Filter("doctorId"));

            const string sql = @"
                SELECT
                    COALESCE(e.started_at, e.created_at)                AS date,
                    pt.code                                             AS patientCode,
                    pt.full_name                                        AS patientName,
                    COALESCE(doc.full_name, N'Chưa xác định')           AS doctorName,
                    COALESCE(
                        (SELECT CONCAT(d.icd10_code, ' - ', d.name)
                           FROM diab_his_enc_diagnoses d
                          WHERE d.encounter_id = e.id AND d.type = 'PRIMARY' AND d.deleted_at IS NULL
                          ORDER BY d.created_at LIMIT 1),
                        e.primary_icd10, N'Chưa ghi nhận')              AS diagnosis,
                    COALESCE(r.name, N'Chưa xếp phòng')                 AS roomName,
                    COALESCE(
                        (SELECT SUM(b.patient_payable) FROM diab_his_bil_billing b
                          WHERE b.encounter_id = e.id COLLATE utf8mb4_unicode_ci AND b.tenant_id = e.tenant_id
                            AND b.status IN ('FINALIZED','PARTIAL_PAID','PAID') AND b.deleted_at IS NULL),
                        0)                                               AS amount
                FROM diab_his_enc_encounters e
                INNER JOIN diab_his_pat_patients pt ON pt.id = e.patient_id AND pt.tenant_id = e.tenant_id
                LEFT JOIN diab_his_sec_users doc ON doc.id = e.doctor_id
                LEFT JOIN diab_his_sys_rooms r ON r.id = e.room_id
                WHERE e.tenant_id = @tenantId
                  AND e.deleted_at IS NULL
                  AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to
                  AND (@doctorId IS NULL OR e.doctor_id = @doctorId)
                ORDER BY date DESC
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ================= B2/B3/B4: CTDV BN Sieu Am / XQuang / Noi Soi (dung chung diab_his_cli_rad_orders) ================= //
    // TODO schema: modality luu VARCHAR tu do bac si nhap khi chi dinh CDHA — quy uoc gia tri chuan (xem
    // db/migrations/0031_create_lab_rad_orders.sql seed dict_rad_procedures): US=Sieu am, XRAY=X-Quang, ENDO=Noi soi, ECG=Dien tim.
    private static ReportDescriptor CtdvRadByModality(
        string code, string title, string modality, string icon, string pdfTypeCode, string locationLabel)
        => new()
        {
            Code = code,
            Title = title,
            Group = ReportGroupCategory.Clinical,
            GroupOrder = code == "ctdv-sieu-am" ? 2 : code == "ctdv-xquang" ? 3 : 4,
            Icon = icon,
            PdfTypeCode = pdfTypeCode,
            Columns = new List<ReportColumn>
            {
                new("date",        "Ngày",          ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
                new("patientCode", "Mã BN",         ReportColumnType.Text,     ReportAlign.Left,  0.9f),
                new("patientName", "Họ tên",        ReportColumnType.Text,     ReportAlign.Left,  1.3f),
                new("doctorName",  "BS chỉ định",   ReportColumnType.Text,     ReportAlign.Left,  1.1f),
                new("location",    locationLabel,   ReportColumnType.Text,     ReportAlign.Left,  1.2f),
                new("conclusion",  "Kết luận",      ReportColumnType.Text,     ReportAlign.Left,  1.8f),
                new("amount",      "Thành tiền",    ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true)
            },
            Kpis = new List<ReportKpiSpec>
            {
                new("SỐ LƯỢT", "#F0FDFA", rows => rows.Count, IsMoney: false),
                new("TỔNG THÀNH TIỀN", "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "amount"))), IsMoney: true)
            },
            Filters = new List<ReportFilter>
            {
                new("doctorId", "Bác sĩ", ReportFilterType.Select, OptionsSource: "doctors")
            },
            BuildQuery = ctx =>
            {
                var p = new DynamicParameters();
                p.Add("tenantId", ctx.TenantId);
                p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
                p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
                p.Add("doctorId", ctx.Filter("doctorId"));
                p.Add("modality", modality);

                const string sql = @"
                    SELECT
                        ro.ordered_at                                        AS date,
                        pt.code                                              AS patientCode,
                        pt.full_name                                        AS patientName,
                        COALESCE(doc.full_name, N'Chưa xác định')           AS doctorName,
                        COALESCE(ro.body_part, ro.procedure_name)           AS location,
                        COALESCE(rr.impression, N'Chưa có kết quả')         AS conclusion,
                        COALESCE(bi.line_total, 0)                          AS amount
                    FROM diab_his_rad_orders ro
                    INNER JOIN diab_his_enc_encounters e ON e.id = ro.encounter_id AND e.tenant_id = ro.tenant_id
                    INNER JOIN diab_his_pat_patients pt ON pt.id = e.patient_id AND pt.tenant_id = ro.tenant_id
                    LEFT JOIN diab_his_sec_users doc ON doc.id = ro.ordered_by
                    LEFT JOIN diab_his_rad_results rr ON rr.order_id = ro.id AND rr.tenant_id = ro.tenant_id
                    LEFT JOIN diab_his_bil_billing_items bi ON bi.ref_id = ro.id COLLATE utf8mb4_unicode_ci AND bi.item_type = 'RAD' AND bi.tenant_id = ro.tenant_id
                    WHERE ro.tenant_id = @tenantId
                      AND ro.deleted_at IS NULL
                      AND ro.modality = @modality
                      AND ro.ordered_at BETWEEN @from AND @to
                      AND (@doctorId IS NULL OR ro.ordered_by = @doctorId)
                    ORDER BY ro.ordered_at DESC
                    LIMIT 3000";

                return (sql, p);
            }
        };

    private static ReportDescriptor CtdvSieuAm() => CtdvRadByModality("ctdv-sieu-am", "BÁO CÁO CTDV BN SIÊU ÂM", "US", "waves", "CTS", "Vị trí SÂ");
    private static ReportDescriptor CtdvXQuang() => CtdvRadByModality("ctdv-xquang", "BÁO CÁO CTDV BN XQUANG", "XRAY", "scan", "CTQ", "Vùng chụp");
    private static ReportDescriptor CtdvNoiSoi() => CtdvRadByModality("ctdv-noi-soi", "BÁO CÁO CTDV BN NỘI SOI", "ENDO", "activity", "CTN", "Loại nội soi");

    // ================= B5: CTDV BN Thu Thuat ================= //
    // TODO schema: chua co bang chi dinh/ket qua thu thuat rieng (vd diab_his_cli_procedure_orders).
    // Best-effort: lay tu diab_his_bil_billing_items (item_type='PROCEDURE'), bac si suy tu encounter cua hoa don.
    private static ReportDescriptor CtdvThuThuat() => new()
    {
        Code = "ctdv-thu-thuat",
        Title = "BÁO CÁO CTDV BN THỦ THUẬT",
        Group = ReportGroupCategory.Clinical,
        GroupOrder = 5,
        Icon = "syringe",
        PdfTypeCode = "CTT",
        Columns = new List<ReportColumn>
        {
            new("date",        "Ngày",          ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("patientCode", "Mã BN",         ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientName", "Họ tên",        ReportColumnType.Text,     ReportAlign.Left,  1.3f),
            new("doctorName",  "BS thực hiện",  ReportColumnType.Text,     ReportAlign.Left,  1.1f),
            new("procedureName","Tên thủ thuật",ReportColumnType.Text,     ReportAlign.Left,  1.8f),
            new("amount",      "Thành tiền",    ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LƯỢT THỦ THUẬT", "#F0FDFA", rows => rows.Count, IsMoney: false),
            new("TỔNG THÀNH TIỀN",   "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "amount"))), IsMoney: true)
        },
        Filters = new List<ReportFilter>
        {
            new("doctorId", "Bác sĩ", ReportFilterType.Select, OptionsSource: "doctors")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("doctorId", ctx.Filter("doctorId"));

            const string sql = @"
                SELECT
                    b.created_at                                  AS date,
                    pt.code                                       AS patientCode,
                    pt.full_name                                  AS patientName,
                    COALESCE(doc.full_name, N'Chưa xác định')     AS doctorName,
                    bi.name                                       AS procedureName,
                    bi.line_total                                 AS amount
                FROM diab_his_bil_billing_items bi
                INNER JOIN diab_his_bil_billing b ON b.id = bi.billing_id AND b.tenant_id = bi.tenant_id
                INNER JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
                LEFT JOIN diab_his_enc_encounters e ON e.id COLLATE utf8mb4_unicode_ci = b.encounter_id AND e.tenant_id = b.tenant_id
                LEFT JOIN diab_his_sec_users doc ON doc.id = e.doctor_id
                WHERE bi.tenant_id = @tenantId
                  AND bi.item_type = 'PROCEDURE'
                  AND b.deleted_at IS NULL
                  AND b.created_at BETWEEN @from AND @to
                  AND (@doctorId IS NULL OR e.doctor_id = @doctorId)
                ORDER BY b.created_at DESC
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ================= B6: CTDV BN Xet Nghiem ================= //
    private static ReportDescriptor CtdvXetNghiem() => new()
    {
        Code = "ctdv-xet-nghiem",
        Title = "BÁO CÁO CTDV BN XÉT NGHIỆM",
        Group = ReportGroupCategory.Clinical,
        GroupOrder = 6,
        Icon = "flask-conical",
        PdfTypeCode = "CTX",
        Columns = new List<ReportColumn>
        {
            new("date",        "Ngày",          ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("patientCode", "Mã BN",         ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientName", "Họ tên",        ReportColumnType.Text,     ReportAlign.Left,  1.3f),
            new("doctorName",  "BS chỉ định",   ReportColumnType.Text,     ReportAlign.Left,  1.1f),
            new("testGroup",   "Nhóm XN",       ReportColumnType.Text,     ReportAlign.Left,  1.4f),
            new("resultCount", "Số chỉ số",     ReportColumnType.Number,   ReportAlign.Right, 0.8f),
            new("amount",      "Thành tiền",    ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LƯỢT XN",      "#F0FDFA", rows => rows.Count, IsMoney: false),
            new("TỔNG THÀNH TIỀN", "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "amount"))), IsMoney: true)
        },
        Filters = new List<ReportFilter>
        {
            new("doctorId", "Bác sĩ", ReportFilterType.Select, OptionsSource: "doctors")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("doctorId", ctx.Filter("doctorId"));

            const string sql = @"
                SELECT
                    lo.ordered_at                                  AS date,
                    pt.code                                        AS patientCode,
                    pt.full_name                                   AS patientName,
                    COALESCE(doc.full_name, N'Chưa xác định')      AS doctorName,
                    lo.test_name                                   AS testGroup,
                    COALESCE(
                        (SELECT COUNT(*) FROM diab_his_lab_results lr
                          WHERE lr.order_id = lo.id AND lr.tenant_id = lo.tenant_id),
                        0)                                          AS resultCount,
                    COALESCE(bi.line_total, 0)                     AS amount
                FROM diab_his_lab_orders lo
                INNER JOIN diab_his_enc_encounters e ON e.id = lo.encounter_id AND e.tenant_id = lo.tenant_id
                INNER JOIN diab_his_pat_patients pt ON pt.id = e.patient_id AND pt.tenant_id = lo.tenant_id
                LEFT JOIN diab_his_sec_users doc ON doc.id = lo.ordered_by
                LEFT JOIN diab_his_bil_billing_items bi ON bi.ref_id = lo.id COLLATE utf8mb4_unicode_ci AND bi.item_type = 'LAB' AND bi.tenant_id = lo.tenant_id
                WHERE lo.tenant_id = @tenantId
                  AND lo.deleted_at IS NULL
                  AND lo.ordered_at BETWEEN @from AND @to
                  AND (@doctorId IS NULL OR lo.ordered_by = @doctorId)
                ORDER BY lo.ordered_at DESC
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ============================================================================
    // NHOM C — SO (REGISTER / LOGBOOK) — group=Clinical, layout gon, khong group-by,
    // co cot STT (ROW_NUMBER() OVER — MySQL 8 ho tro window function).
    // ============================================================================

    // ================= C1: So Kham Benh ================= //
    private static ReportDescriptor SoKhamBenh() => new()
    {
        Code = "so-kham-benh",
        Title = "BÁO CÁO SỔ KHÁM BỆNH",
        Group = ReportGroupCategory.Clinical,
        GroupOrder = 7,
        Icon = "book-open",
        PdfTypeCode = "SKB",
        Columns = new List<ReportColumn>
        {
            new("stt",        "STT",       ReportColumnType.Number, ReportAlign.Right, 0.5f),
            new("date",       "Ngày",      ReportColumnType.DateTime, ReportAlign.Left, 1.1f),
            new("patientCode","Mã BN",     ReportColumnType.Text,   ReportAlign.Left, 0.9f),
            new("patientName","Họ tên",    ReportColumnType.Text,   ReportAlign.Left, 1.3f),
            new("age",        "Tuổi",      ReportColumnType.Number, ReportAlign.Right, 0.5f),
            new("gender",     "Giới",      ReportColumnType.Text,   ReportAlign.Left, 0.6f),
            new("address",    "Địa chỉ",   ReportColumnType.Text,   ReportAlign.Left, 1.6f),
            new("diagnosis",  "Chẩn đoán", ReportColumnType.Text,   ReportAlign.Left, 1.6f),
            new("doctorName", "Bác sĩ",    ReportColumnType.Text,   ReportAlign.Left, 1.1f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LƯỢT KHÁM", "#F0FDFA", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>
        {
            new("doctorId", "Bác sĩ", ReportFilterType.Select, OptionsSource: "doctors")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("doctorId", ctx.Filter("doctorId"));

            // TODO schema: pat_patients khong co cot dia chi day du (chi co province/district/ward_code + street,
            // khong co bang danh muc dia gioi hanh chinh de resolve ten) — dung tam pt.street lam Dia chi.
            const string sql = @"
                SELECT
                    ROW_NUMBER() OVER (ORDER BY COALESCE(e.started_at, e.created_at)) AS stt,
                    COALESCE(e.started_at, e.created_at)          AS date,
                    pt.code                                       AS patientCode,
                    pt.full_name                                  AS patientName,
                    TIMESTAMPDIFF(YEAR, pt.date_of_birth, COALESCE(e.started_at, e.created_at)) AS age,
                    CASE pt.gender WHEN 'MALE' THEN N'Nam' WHEN 'FEMALE' THEN N'Nữ' ELSE N'Khác' END AS gender,
                    COALESCE(pt.street, N'Chưa ghi nhận')         AS address,
                    COALESCE(
                        (SELECT CONCAT(d.icd10_code, ' - ', d.name)
                           FROM diab_his_enc_diagnoses d
                          WHERE d.encounter_id = e.id AND d.type = 'PRIMARY' AND d.deleted_at IS NULL
                          ORDER BY d.created_at LIMIT 1),
                        e.primary_icd10, N'Chưa ghi nhận')        AS diagnosis,
                    COALESCE(doc.full_name, N'Chưa xác định')     AS doctorName
                FROM diab_his_enc_encounters e
                INNER JOIN diab_his_pat_patients pt ON pt.id = e.patient_id AND pt.tenant_id = e.tenant_id
                LEFT JOIN diab_his_sec_users doc ON doc.id = e.doctor_id
                WHERE e.tenant_id = @tenantId
                  AND e.deleted_at IS NULL
                  AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to
                  AND (@doctorId IS NULL OR e.doctor_id = @doctorId)
                ORDER BY date
                LIMIT 5000";

            return (sql, p);
        }
    };

    // ================= C2/C3/C4/C7: So Sieu Am / XQuang / Noi Soi / Dien Tim (dung chung diab_his_cli_rad_orders) ================= //
    private static ReportDescriptor SoRadByModality(
        string code, string title, string modality, string icon, string pdfTypeCode, string indicationLabel, string conclusionLabel, int groupOrder)
        => new()
        {
            Code = code,
            Title = title,
            Group = ReportGroupCategory.Clinical,
            GroupOrder = groupOrder,
            Icon = icon,
            PdfTypeCode = pdfTypeCode,
            Columns = new List<ReportColumn>
            {
                new("stt",         "STT",           ReportColumnType.Number,   ReportAlign.Right, 0.5f),
                new("date",        "Ngày",          ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
                new("patientCode", "Mã BN",         ReportColumnType.Text,     ReportAlign.Left,  0.9f),
                new("patientName", "Họ tên",        ReportColumnType.Text,     ReportAlign.Left,  1.3f),
                new("indication",  indicationLabel, ReportColumnType.Text,     ReportAlign.Left,  1.4f),
                new("conclusion",  conclusionLabel, ReportColumnType.Text,     ReportAlign.Left,  1.8f),
                new("doctorName",  "BS đọc",        ReportColumnType.Text,     ReportAlign.Left,  1.1f)
            },
            Kpis = new List<ReportKpiSpec>
            {
                new("SỐ LƯỢT", "#F0FDFA", rows => rows.Count, IsMoney: false)
            },
            Filters = new List<ReportFilter>
            {
                new("doctorId", "Bác sĩ", ReportFilterType.Select, OptionsSource: "doctors")
            },
            BuildQuery = ctx =>
            {
                var p = new DynamicParameters();
                p.Add("tenantId", ctx.TenantId);
                p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
                p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
                p.Add("doctorId", ctx.Filter("doctorId"));
                p.Add("modality", modality);

                const string sql = @"
                    SELECT
                        ROW_NUMBER() OVER (ORDER BY ro.ordered_at)          AS stt,
                        ro.ordered_at                                       AS date,
                        pt.code                                             AS patientCode,
                        pt.full_name                                        AS patientName,
                        ro.procedure_name                                   AS indication,
                        COALESCE(rr.impression, N'Chưa có kết quả')        AS conclusion,
                        COALESCE(reader.full_name, rr.performed_by, N'Chưa xác định') AS doctorName
                    FROM diab_his_rad_orders ro
                    INNER JOIN diab_his_enc_encounters e ON e.id = ro.encounter_id AND e.tenant_id = ro.tenant_id
                    INNER JOIN diab_his_pat_patients pt ON pt.id = e.patient_id AND pt.tenant_id = ro.tenant_id
                    LEFT JOIN diab_his_rad_results rr ON rr.order_id = ro.id AND rr.tenant_id = ro.tenant_id
                    LEFT JOIN diab_his_sec_users reader ON reader.id = rr.performed_by
                    WHERE ro.tenant_id = @tenantId
                      AND ro.deleted_at IS NULL
                      AND ro.modality = @modality
                      AND ro.ordered_at BETWEEN @from AND @to
                      AND (@doctorId IS NULL OR ro.ordered_by = @doctorId)
                    ORDER BY ro.ordered_at
                    LIMIT 5000";

                return (sql, p);
            }
        };

    private static ReportDescriptor SoSieuAm() => SoRadByModality("so-sieu-am", "BÁO CÁO SỔ SIÊU ÂM", "US", "waves", "SSA", "Chỉ định", "Kết luận", 8);
    private static ReportDescriptor SoXQuang() => SoRadByModality("so-xquang", "BÁO CÁO SỔ XQUANG", "XRAY", "scan", "SXQ", "Vùng chụp", "Kết luận", 9);
    private static ReportDescriptor SoNoiSoi() => SoRadByModality("so-noi-soi", "BÁO CÁO SỔ NỘI SOI", "ENDO", "activity", "SNS", "Loại nội soi", "Kết luận", 10);
    private static ReportDescriptor SoDienTim() => SoRadByModality("so-dien-tim", "BÁO CÁO SỔ ĐIỆN TIM", "ECG", "heart-pulse", "SDT", "Chỉ định", "Kết luận ECG", 13);

    // ================= C5: So Thu Thuat ================= //
    // TODO schema: chua co bang chi dinh/ket qua thu thuat rieng — best-effort tu diab_his_bil_billing_items
    // (item_type='PROCEDURE'), giong CtdvThuThuat() nhung bo cot Thanh tien + them STT (register layout).
    private static ReportDescriptor SoThuThuat() => new()
    {
        Code = "so-thu-thuat",
        Title = "BÁO CÁO SỔ THỦ THUẬT",
        Group = ReportGroupCategory.Clinical,
        GroupOrder = 11,
        Icon = "syringe",
        PdfTypeCode = "STH",
        Columns = new List<ReportColumn>
        {
            new("stt",          "STT",          ReportColumnType.Number,   ReportAlign.Right, 0.5f),
            new("date",         "Ngày",         ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("patientCode",  "Mã BN",        ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientName",  "Họ tên",       ReportColumnType.Text,     ReportAlign.Left,  1.3f),
            new("procedureName","Tên thủ thuật",ReportColumnType.Text,     ReportAlign.Left,  1.8f),
            new("doctorName",   "BS thực hiện", ReportColumnType.Text,     ReportAlign.Left,  1.1f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LƯỢT THỦ THUẬT", "#F0FDFA", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>
        {
            new("doctorId", "Bác sĩ", ReportFilterType.Select, OptionsSource: "doctors")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("doctorId", ctx.Filter("doctorId"));

            const string sql = @"
                SELECT
                    ROW_NUMBER() OVER (ORDER BY b.created_at)     AS stt,
                    b.created_at                                  AS date,
                    pt.code                                       AS patientCode,
                    pt.full_name                                  AS patientName,
                    bi.name                                       AS procedureName,
                    COALESCE(doc.full_name, N'Chưa xác định')     AS doctorName
                FROM diab_his_bil_billing_items bi
                INNER JOIN diab_his_bil_billing b ON b.id = bi.billing_id AND b.tenant_id = bi.tenant_id
                INNER JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
                LEFT JOIN diab_his_enc_encounters e ON e.id COLLATE utf8mb4_unicode_ci = b.encounter_id AND e.tenant_id = b.tenant_id
                LEFT JOIN diab_his_sec_users doc ON doc.id = e.doctor_id
                WHERE bi.tenant_id = @tenantId
                  AND bi.item_type = 'PROCEDURE'
                  AND b.deleted_at IS NULL
                  AND b.created_at BETWEEN @from AND @to
                  AND (@doctorId IS NULL OR e.doctor_id = @doctorId)
                ORDER BY b.created_at
                LIMIT 5000";

            return (sql, p);
        }
    };

    // ================= C6: So Xet Nghiem ================= //
    private static ReportDescriptor SoXetNghiem() => new()
    {
        Code = "so-xet-nghiem",
        Title = "BÁO CÁO SỔ XÉT NGHIỆM",
        Group = ReportGroupCategory.Clinical,
        GroupOrder = 12,
        Icon = "flask-conical",
        PdfTypeCode = "SXN",
        Columns = new List<ReportColumn>
        {
            new("stt",         "STT",       ReportColumnType.Number,   ReportAlign.Right, 0.5f),
            new("date",        "Ngày",      ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("patientCode", "Mã BN",     ReportColumnType.Text,     ReportAlign.Left,  0.9f),
            new("patientName", "Họ tên",    ReportColumnType.Text,     ReportAlign.Left,  1.3f),
            new("testGroup",   "Nhóm XN",   ReportColumnType.Text,     ReportAlign.Left,  1.4f),
            new("resultCount", "Số chỉ số", ReportColumnType.Number,   ReportAlign.Right, 0.8f),
            new("ktvName",     "KTV",       ReportColumnType.Text,     ReportAlign.Left,  1.1f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LƯỢT XN", "#F0FDFA", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>
        {
            new("doctorId", "Bác sĩ", ReportFilterType.Select, OptionsSource: "doctors")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("doctorId", ctx.Filter("doctorId"));

            const string sql = @"
                SELECT
                    ROW_NUMBER() OVER (ORDER BY lo.ordered_at)    AS stt,
                    lo.ordered_at                                  AS date,
                    pt.code                                        AS patientCode,
                    pt.full_name                                   AS patientName,
                    lo.test_name                                   AS testGroup,
                    COALESCE(
                        (SELECT COUNT(*) FROM diab_his_lab_results lr
                          WHERE lr.order_id = lo.id AND lr.tenant_id = lo.tenant_id),
                        0)                                          AS resultCount,
                    COALESCE(
                        (SELECT COALESCE(ktv.full_name, lr2.performed_by) FROM diab_his_lab_results lr2
                           LEFT JOIN diab_his_sec_users ktv ON ktv.id = lr2.performed_by
                          WHERE lr2.order_id = lo.id AND lr2.tenant_id = lo.tenant_id
                          ORDER BY lr2.performed_at DESC LIMIT 1),
                        N'Chưa xác định')                          AS ktvName
                FROM diab_his_lab_orders lo
                INNER JOIN diab_his_enc_encounters e ON e.id = lo.encounter_id AND e.tenant_id = lo.tenant_id
                INNER JOIN diab_his_pat_patients pt ON pt.id = e.patient_id AND pt.tenant_id = lo.tenant_id
                WHERE lo.tenant_id = @tenantId
                  AND lo.deleted_at IS NULL
                  AND lo.ordered_at BETWEEN @from AND @to
                  AND (@doctorId IS NULL OR lo.ordered_by = @doctorId)
                ORDER BY lo.ordered_at
                LIMIT 5000";

            return (sql, p);
        }
    };

    // ============================================================================
    // NHOM D — THONG KE LUOT KHAM — group=Statistics. Moi row la 1 dong da tong hop
    // (khong dung GroupByKey engine vi SQL da GROUP BY san).
    // ============================================================================

    // ================= D1: Luot Kham Theo Bac Si ================= //
    private static ReportDescriptor LuotKhamTheoBs() => new()
    {
        Code = "luot-kham-theo-bs",
        Title = "BÁO CÁO LƯỢT KHÁM THEO BÁC SĨ",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 1,
        Icon = "user-round",
        PdfTypeCode = "LKB",
        Columns = new List<ReportColumn>
        {
            new("doctorName",      "Bác sĩ",       ReportColumnType.Text,   ReportAlign.Left,  1.4f),
            new("visitCount",      "Số lượt",      ReportColumnType.Number, ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("newPatientCount", "Số BN mới",    ReportColumnType.Number, ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("prescriptionCount","Số toa",      ReportColumnType.Number, ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("revenue",         "Doanh thu",    ReportColumnType.Money,  ReportAlign.Right, 1.1f, IsGroupSubtotal: true),
            new("avgPerVisit",     "TB/lượt",      ReportColumnType.Money,  ReportAlign.Right, 1f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG LƯỢT KHÁM", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "visitCount"))), IsMoney: false),
            new("TỔNG DOANH THU", "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "revenue"))), IsMoney: true)
        },
        Filters = new List<ReportFilter>
        {
            new("doctorId", "Bác sĩ", ReportFilterType.Select, OptionsSource: "doctors")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("doctorId", ctx.Filter("doctorId"));

            // "So BN moi" = luot kham co encounter_type = FIRST_VISIT (truong co san tren diab_his_enc_encounters).
            const string sql = @"
                WITH enc_agg AS (
                    SELECT
                        e.id AS encounter_id, e.doctor_id, e.encounter_type,
                        (SELECT COALESCE(SUM(b2.patient_payable), 0) FROM diab_his_bil_billing b2
                          WHERE b2.encounter_id = e.id COLLATE utf8mb4_unicode_ci AND b2.tenant_id = e.tenant_id
                            AND b2.status IN ('FINALIZED','PARTIAL_PAID','PAID') AND b2.deleted_at IS NULL) AS revenue,
                        (SELECT COUNT(*) FROM diab_his_pha_prescriptions p
                          WHERE p.encounter_id = e.id AND p.tenant_id = e.tenant_id AND p.deleted_at IS NULL) AS presc_count
                    FROM diab_his_enc_encounters e
                    WHERE e.tenant_id = @tenantId
                      AND e.deleted_at IS NULL
                      AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to
                      AND (@doctorId IS NULL OR e.doctor_id = @doctorId)
                )
                SELECT
                    COALESCE(u.full_name, N'Chưa xác định')                          AS doctorName,
                    COUNT(*)                                                          AS visitCount,
                    SUM(CASE WHEN ea.encounter_type = 'FIRST_VISIT' THEN 1 ELSE 0 END) AS newPatientCount,
                    SUM(ea.presc_count)                                               AS prescriptionCount,
                    SUM(ea.revenue)                                                   AS revenue,
                    CASE WHEN COUNT(*) > 0 THEN SUM(ea.revenue) / COUNT(*) ELSE 0 END  AS avgPerVisit
                FROM enc_agg ea
                LEFT JOIN diab_his_sec_users u ON u.id = ea.doctor_id
                GROUP BY ea.doctor_id, u.full_name
                ORDER BY revenue DESC
                LIMIT 500";

            return (sql, p);
        }
    };

    // ================= D2: Luot Kham Theo Phong Kham ================= //
    private static ReportDescriptor LuotKhamTheoPk() => new()
    {
        Code = "luot-kham-theo-pk",
        Title = "BÁO CÁO LƯỢT KHÁM THEO PHÒNG KHÁM",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 2,
        Icon = "door-open",
        PdfTypeCode = "LKP",
        Columns = new List<ReportColumn>
        {
            new("roomName",    "Phòng khám", ReportColumnType.Text,   ReportAlign.Left,  1.4f),
            new("visitCount",  "Số lượt",    ReportColumnType.Number, ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("visitPercent","Tỷ trọng %", ReportColumnType.Number, ReportAlign.Right, 0.9f),
            new("revenue",     "Doanh thu",  ReportColumnType.Money,  ReportAlign.Right, 1.1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG LƯỢT KHÁM", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "visitCount"))), IsMoney: false),
            new("TỔNG DOANH THU", "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "revenue"))), IsMoney: true)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                WITH room_agg AS (
                    SELECT
                        COALESCE(r.name, N'Chưa xếp phòng') AS roomName,
                        COUNT(*)                            AS visitCount,
                        (SELECT COALESCE(SUM(b.patient_payable), 0) FROM diab_his_bil_billing b
                          WHERE b.encounter_id = e.id COLLATE utf8mb4_unicode_ci AND b.tenant_id = e.tenant_id
                            AND b.status IN ('FINALIZED','PARTIAL_PAID','PAID') AND b.deleted_at IS NULL) AS revenue
                    FROM diab_his_enc_encounters e
                    LEFT JOIN diab_his_sys_rooms r ON r.id = e.room_id
                    WHERE e.tenant_id = @tenantId
                      AND e.deleted_at IS NULL
                      AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to
                    GROUP BY r.id, r.name, e.id
                ),
                room_sum AS (
                    SELECT roomName, COUNT(*) AS visitCount, SUM(revenue) AS revenue
                    FROM room_agg
                    GROUP BY roomName
                )
                SELECT
                    roomName,
                    visitCount,
                    ROUND(visitCount / NULLIF(SUM(visitCount) OVER (), 0) * 100, 1) AS visitPercent,
                    revenue
                FROM room_sum
                ORDER BY revenue DESC
                LIMIT 500";

            return (sql, p);
        }
    };

    // ============================================================================
    // NHOM E — BHXH / LAM SANG DAC THU
    // ============================================================================

    // ================= E1: Benh Dien Tien (theo doi BN man tinh / DTD) ================= //
    private static ReportDescriptor BenhDienTien() => new()
    {
        Code = "benh-dien-tien",
        Title = "BÁO CÁO BỆNH DIỄN TIẾN",
        Group = ReportGroupCategory.Clinical,
        GroupOrder = 14,
        Icon = "trending-up",
        PdfTypeCode = "BDT",
        Columns = new List<ReportColumn>
        {
            new("date",      "Ngày khám", ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("diagnosis", "Chẩn đoán", ReportColumnType.Text,     ReportAlign.Left,  1.6f),
            new("hba1c",     "HbA1c",     ReportColumnType.Number,  ReportAlign.Right, 0.8f),
            new("glucose",   "Glucose",   ReportColumnType.Number,  ReportAlign.Right, 0.8f),
            new("bloodPressure", "HA",    ReportColumnType.Text,    ReportAlign.Right, 0.8f),
            new("bmi",       "BMI",       ReportColumnType.Number,  ReportAlign.Right, 0.7f),
            new("note",      "Ghi chú",   ReportColumnType.Text,    ReportAlign.Left,  1.8f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LẦN THEO DÕI", "#F0FDFA", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>
        {
            new("patientId", "Bệnh nhân", ReportFilterType.Select, OptionsSource: "patients", Required: true)
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("patientId", ctx.Filter("patientId"));

            // TODO schema: chua co bang treatment_monitoring rieng (theo doi dien tien man tinh) —
            // best-effort ghep tu diab_his_enc_encounters + diab_his_enc_diagnoses + diab_his_enc_vital_signs
            // (HA/Glucose/BMI) + cli_lab_orders/cli_lab_results (HbA1c, test_code = 'HBA1C').
            const string sql = @"
                SELECT
                    COALESCE(e.started_at, e.created_at)                          AS date,
                    COALESCE(
                        (SELECT CONCAT(d.icd10_code, ' - ', d.name)
                           FROM diab_his_enc_diagnoses d
                          WHERE d.encounter_id = e.id AND d.type = 'PRIMARY' AND d.deleted_at IS NULL
                          ORDER BY d.created_at LIMIT 1),
                        e.primary_icd10, N'Chưa ghi nhận')                        AS diagnosis,
                    (SELECT lr.value_numeric FROM diab_his_lab_orders lo
                       INNER JOIN diab_his_lab_results lr ON lr.order_id = lo.id AND lr.tenant_id = lo.tenant_id
                      WHERE lo.encounter_id = e.id AND lo.tenant_id = e.tenant_id
                        AND lr.test_code = 'HBA1C'
                      ORDER BY lr.performed_at DESC LIMIT 1)                       AS hba1c,
                    vs.glucose_mg_dl                                               AS glucose,
                    CASE WHEN vs.bp_systolic IS NOT NULL AND vs.bp_diastolic IS NOT NULL
                         THEN CONCAT(vs.bp_systolic, '/', vs.bp_diastolic)
                         ELSE NULL END                                            AS bloodPressure,
                    CASE WHEN vs.height_cm IS NOT NULL AND vs.height_cm > 0 AND vs.weight_kg IS NOT NULL
                         THEN ROUND(vs.weight_kg / POWER(vs.height_cm / 100, 2), 1)
                         ELSE NULL END                                            AS bmi,
                    e.chief_complaint                                              AS note
                FROM diab_his_enc_encounters e
                INNER JOIN diab_his_pat_patients pt ON pt.id = e.patient_id AND pt.tenant_id = e.tenant_id
                LEFT JOIN diab_his_enc_vital_signs vs ON vs.encounter_id = e.id AND vs.tenant_id = e.tenant_id
                WHERE e.tenant_id = @tenantId
                  AND e.deleted_at IS NULL
                  AND (@patientId IS NULL OR e.patient_id = @patientId)
                  AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to
                ORDER BY date
                LIMIT 2000";

            return (sql, p);
        }
    };

    // ================= E2: Nghi Huong BHXH (mau C65-HD1, TT 56/2017/TT-BYT) ================= //
    // TODO schema: HE THONG CHUA CO bang luu giay chung nhan nghi viec huong BHXH.
    // Can them bang moi, ten goi y diab_his_cli_sick_leaves gom cac cot: id, tenant_id, patient_id, encounter_id,
    // cert_no (so seri/so GCN), insurance_card_no_enc, icd10_code, days_off, leave_from, leave_to, doctor_id,
    // issued_at, created_at, created_by. Descriptor nay tam thoi tra ve SQL luon rong (tenant-scoped, an toan)
    // cho den khi bo sung bang (xem PRD reports-catalog-prd.md muc 5.4).
    private static ReportDescriptor NghiHuongBhxh() => new()
    {
        Code = "nghi-huong-bhxh",
        Title = "BÁO CÁO NGHỈ HƯỞNG BHXH",
        Group = ReportGroupCategory.Bhyt,
        GroupOrder = 1,
        Icon = "file-text",
        PdfTypeCode = "BHX",
        Columns = new List<ReportColumn>
        {
            new("certNo",      "Số GCN",         ReportColumnType.Text,     ReportAlign.Left, 1f),
            new("patientCode", "Mã BN",          ReportColumnType.Text,     ReportAlign.Left, 0.9f),
            new("patientName", "Họ tên",         ReportColumnType.Text,     ReportAlign.Left, 1.3f),
            new("insuranceNo", "Số thẻ BHYT",    ReportColumnType.Text,     ReportAlign.Left, 1.1f),
            new("diagnosis",   "Chẩn đoán",      ReportColumnType.Text,     ReportAlign.Left, 1.6f),
            new("daysOff",     "Số ngày nghỉ",   ReportColumnType.Number,   ReportAlign.Right, 0.8f),
            new("leaveFrom",   "Từ ngày",        ReportColumnType.Date,     ReportAlign.Left, 0.9f),
            new("leaveTo",     "Đến ngày",       ReportColumnType.Date,     ReportAlign.Left, 0.9f),
            new("doctorName",  "BS ký",          ReportColumnType.Text,     ReportAlign.Left, 1.1f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ GIẤY CHỨNG NHẬN", "#F0FDFA", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            // TODO schema: chua co bang GCN nghi huong BHXH — tra ve tap rong an toan (tenant-scoped, khong loi).
            const string sql = @"
                SELECT
                    CAST(NULL AS CHAR)     AS certNo,
                    pt.code                AS patientCode,
                    pt.full_name           AS patientName,
                    CAST(NULL AS CHAR)     AS insuranceNo,
                    CAST(NULL AS CHAR)     AS diagnosis,
                    0                       AS daysOff,
                    CAST(NULL AS DATE)     AS leaveFrom,
                    CAST(NULL AS DATE)     AS leaveTo,
                    CAST(NULL AS CHAR)     AS doctorName
                FROM diab_his_pat_patients pt
                WHERE pt.tenant_id = @tenantId AND 1 = 0
                LIMIT 0";

            return (sql, p);
        }
    };

    // ============================================================================
    // DOT 7 — "GAT NHANH" QUICK-WIN (docs/prd/reports-catalog-prd.md muc 7 — gap analysis
    // P0-2 / P1). Toan bo la descriptor THUAN, khong tao bang/migration moi, tai su dung
    // schema da co. Da introspect thuc te schema + du lieu qua WSL mysql truoc khi viet SQL.
    // ============================================================================

    // ================= Q1: Thong Ke ICD-10 / Mo Hinh Benh Tat (P0-2) ================= //
    // Nguon: diab_his_enc_diagnoses (type=PRIMARY) uu tien, fallback ve enc_encounters.primary_icd10
    // khi chua co dong chan doan rieng (dung y het pattern COALESCE da dung o cac descriptor khac).
    // Ten chan doan tra tu diab_his_ref_icd10 (danh muc ICD-10, cung ho collation 0900_ai_ci nen
    // KHONG can COLLATE khi join voi enc_encounters/enc_diagnoses).
    private static ReportDescriptor Icd10Stats() => new()
    {
        Code = "icd10-stats",
        Title = "BÁO CÁO THỐNG KÊ ICD-10",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 3,
        Icon = "activity-square",
        PdfTypeCode = "ICD",
        Columns = new List<ReportColumn>
        {
            new("stt",           "STT",             ReportColumnType.Number, ReportAlign.Right, 0.5f),
            new("icd10Code",     "Mã ICD-10",       ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("diagnosisName", "Tên chẩn đoán",   ReportColumnType.Text,   ReportAlign.Left,  2.2f),
            new("visitCount",    "Số lượt",         ReportColumnType.Number, ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("percent",       "Tỷ lệ %",         ReportColumnType.Number, ReportAlign.Right, 0.8f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG LƯỢT CHẨN ĐOÁN", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "visitCount"))), IsMoney: false),
            new("SỐ MÃ ICD PHÂN BIỆT", "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    ROW_NUMBER() OVER (ORDER BY t.visitCount DESC) AS stt,
                    t.icd10Code                                    AS icd10Code,
                    t.diagnosisName                                AS diagnosisName,
                    t.visitCount                                   AS visitCount,
                    ROUND(t.visitCount / NULLIF(SUM(t.visitCount) OVER (), 0) * 100, 1) AS percent
                FROM (
                    SELECT
                        diag.icd10Code                                     AS icd10Code,
                        COALESCE(r.name_vi, N'Chưa xác định')              AS diagnosisName,
                        COUNT(*)                                           AS visitCount
                    FROM (
                        SELECT
                            COALESCE(
                                (SELECT d.icd10_code FROM diab_his_enc_diagnoses d
                                  WHERE d.encounter_id = e.id AND d.type = 'PRIMARY' AND d.deleted_at IS NULL
                                  ORDER BY d.created_at LIMIT 1),
                                e.primary_icd10) AS icd10Code
                        FROM diab_his_enc_encounters e
                        WHERE e.tenant_id = @tenantId
                          AND e.deleted_at IS NULL
                          AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to
                    ) diag
                    LEFT JOIN diab_his_ref_icd10 r ON r.code = diag.icd10Code
                    WHERE diag.icd10Code IS NOT NULL
                    GROUP BY diag.icd10Code, r.name_vi
                ) t
                ORDER BY t.visitCount DESC
                LIMIT 500";

            return (sql, p);
        }
    };

    // ================= Q2: Top Thuoc Ke Nhieu (P1-6) ================= //
    // TODO schema: diab_his_bil_billing_items.ref_id KHONG khop id thuc te trong diab_his_pha_drugs
    // (du lieu seed hien tai — ref_id luon la NULL-match), nen cot "Hoat chat" (generic_name) hien
    // luon rong. bi.code/bi.name da du de hien thi Ma thuoc/Ten thuoc dung. Neu ref_id duoc lien ket
    // dung trong tuong lai, cot Hoat chat se tu dong co du lieu (khong can sua descriptor).
    private static ReportDescriptor TopDrugs() => new()
    {
        Code = "top-drugs",
        Title = "BÁO CÁO TOP THUỐC KÊ NHIỀU",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 4,
        Icon = "pill",
        PdfTypeCode = "TDR",
        Columns = new List<ReportColumn>
        {
            new("stt",              "STT",           ReportColumnType.Number, ReportAlign.Right, 0.5f),
            new("drugCode",         "Mã thuốc",      ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("drugName",         "Tên thuốc",     ReportColumnType.Text,   ReportAlign.Left,  1.8f),
            new("activeIngredient", "Hoạt chất",     ReportColumnType.Text,   ReportAlign.Left,  1.4f),
            new("quantity",         "SL kê",         ReportColumnType.Number, ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("revenue",          "Doanh thu",     ReportColumnType.Money,  ReportAlign.Right, 1.1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG SL KÊ",     "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "quantity"))), IsMoney: false),
            new("TỔNG DOANH THU", "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "revenue"))), IsMoney: true)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    ROW_NUMBER() OVER (ORDER BY t.revenue DESC) AS stt,
                    t.drugCode                                  AS drugCode,
                    t.drugName                                  AS drugName,
                    t.activeIngredient                          AS activeIngredient,
                    t.quantity                                  AS quantity,
                    t.revenue                                   AS revenue
                FROM (
                    SELECT
                        COALESCE(bi.code, N'—')     AS drugCode,
                        bi.name                     AS drugName,
                        MAX(d.generic_name)         AS activeIngredient,
                        SUM(bi.quantity)            AS quantity,
                        SUM(bi.line_total)          AS revenue
                    FROM diab_his_bil_billing_items bi
                    INNER JOIN diab_his_bil_billing b ON b.id = bi.billing_id AND b.tenant_id = bi.tenant_id
                    LEFT JOIN diab_his_pha_drugs d ON d.id = bi.ref_id COLLATE utf8mb4_unicode_ci AND d.tenant_id = bi.tenant_id
                    WHERE bi.tenant_id = @tenantId
                      AND bi.item_type = 'DRUG'
                      AND b.deleted_at IS NULL
                      AND b.created_at BETWEEN @from AND @to
                    GROUP BY bi.code, bi.name
                ) t
                ORDER BY t.revenue DESC
                LIMIT 500";

            return (sql, p);
        }
    };

    // ================= Q3: Top Dich Vu (P1-6) ================= //
    private static ReportDescriptor TopServices() => new()
    {
        Code = "top-services",
        Title = "BÁO CÁO TOP DỊCH VỤ",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 5,
        Icon = "list-checks",
        PdfTypeCode = "TSV",
        Columns = new List<ReportColumn>
        {
            new("stt",         "STT",       ReportColumnType.Number, ReportAlign.Right, 0.5f),
            new("svcCode",     "Mã DV",     ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("svcName",     "Tên DV",    ReportColumnType.Text,   ReportAlign.Left,  1.8f),
            new("visitCount",  "Số lượt",   ReportColumnType.Number, ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("revenue",     "Doanh thu", ReportColumnType.Money,  ReportAlign.Right, 1.1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG SỐ LƯỢT",   "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "visitCount"))), IsMoney: false),
            new("TỔNG DOANH THU", "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "revenue"))), IsMoney: true)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    ROW_NUMBER() OVER (ORDER BY t.revenue DESC) AS stt,
                    t.svcCode                                   AS svcCode,
                    t.svcName                                   AS svcName,
                    t.visitCount                                AS visitCount,
                    t.revenue                                   AS revenue
                FROM (
                    SELECT
                        COALESCE(bi.code, N'—') AS svcCode,
                        bi.name                 AS svcName,
                        COUNT(*)                AS visitCount,
                        SUM(bi.line_total)      AS revenue
                    FROM diab_his_bil_billing_items bi
                    INNER JOIN diab_his_bil_billing b ON b.id = bi.billing_id AND b.tenant_id = bi.tenant_id
                    WHERE bi.tenant_id = @tenantId
                      AND bi.item_type = 'SERVICE'
                      AND b.deleted_at IS NULL
                      AND b.created_at BETWEEN @from AND @to
                    GROUP BY bi.code, bi.name
                ) t
                ORDER BY t.revenue DESC
                LIMIT 500";

            return (sql, p);
        }
    };

    // ================= Q4: Doanh Thu Theo Thang (P1-7) ================= //
    // Khac revenue-daily (A1): group theo THANG thay vi group theo nguoi thu, va lay Tong thu / Thuc thu
    // truc tiep tu diab_his_bil_billing.paid_amount (khong yeu cau phai co dong diab_his_bil_payments
    // trong ky — vi seed du lieu hien tai CHUA co dong payments nao, neu chi dua vao payments nhu A1 thi
    // bao cao se luon rong). Tien mat/CK/The van lay tu bil_payments (subquery tuong quan theo billing_id) —
    // se hien 0 cho toi khi co seed payments, KHONG phai loi.
    private static ReportDescriptor RevenueMonthly() => new()
    {
        Code = "revenue-monthly",
        Title = "BÁO CÁO DOANH THU THEO THÁNG",
        Group = ReportGroupCategory.Financial,
        GroupOrder = 7,
        Icon = "calendar-range",
        PdfTypeCode = "RVM",
        Columns = new List<ReportColumn>
        {
            new("month",         "Tháng",         ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("receiptCount",  "Số phiếu",      ReportColumnType.Number, ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("totalCollected","Tổng thu",      ReportColumnType.Money,  ReportAlign.Right, 1.1f, IsGroupSubtotal: true),
            new("cash",          "Tiền mặt",      ReportColumnType.Money,  ReportAlign.Right, 1f,   IsGroupSubtotal: true),
            new("bankTransfer",  "Chuyển khoản",  ReportColumnType.Money,  ReportAlign.Right, 1f,   IsGroupSubtotal: true),
            new("card",          "Thẻ",           ReportColumnType.Money,  ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("netCollected",  "Thực thu",      ReportColumnType.Money,  ReportAlign.Right, 1.1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG THỰC THU KỲ", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "netCollected"))), IsMoney: true),
            new("SỐ THÁNG CÓ DỮ LIỆU", "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                WITH billing_month AS (
                    SELECT
                        b.id, b.paid_amount,
                        DATE_FORMAT(b.created_at, '%Y-%m') AS ym,
                        (SELECT COALESCE(SUM(p2.amount), 0) FROM diab_his_bil_payments p2
                          WHERE p2.billing_id = b.id AND p2.tenant_id = b.tenant_id
                            AND p2.method = 'CASH' AND p2.status IN ('COMPLETED','REFUNDED')) AS cashAmt,
                        (SELECT COALESCE(SUM(p2.amount), 0) FROM diab_his_bil_payments p2
                          WHERE p2.billing_id = b.id AND p2.tenant_id = b.tenant_id
                            AND p2.method = 'BANK_TRANSFER' AND p2.status IN ('COMPLETED','REFUNDED')) AS bankAmt,
                        (SELECT COALESCE(SUM(p2.amount), 0) FROM diab_his_bil_payments p2
                          WHERE p2.billing_id = b.id AND p2.tenant_id = b.tenant_id
                            AND p2.method IN ('VISA','MASTER') AND p2.status IN ('COMPLETED','REFUNDED')) AS cardAmt
                    FROM diab_his_bil_billing b
                    WHERE b.tenant_id = @tenantId
                      AND b.deleted_at IS NULL
                      AND b.status IN ('FINALIZED','PARTIAL_PAID','PAID')
                      AND b.created_at BETWEEN @from AND @to
                )
                SELECT
                    ym                       AS month,
                    COUNT(*)                 AS receiptCount,
                    SUM(paid_amount)         AS totalCollected,
                    SUM(cashAmt)             AS cash,
                    SUM(bankAmt)             AS bankTransfer,
                    SUM(cardAmt)             AS card,
                    SUM(paid_amount)         AS netCollected
                FROM billing_month
                GROUP BY ym
                ORDER BY ym
                LIMIT 500";

            return (sql, p);
        }
    };

    // ================= Q5: Tong Hop Nguon Khach (Domain 1 gap) ================= //
    // Loc theo NGAY DANG KY ho so (pt.created_at) trong ky — don gian, khong phu thuoc encounter
    // (BN dang ky nhung chua co lich kham van duoc dem, phu hop y nghia "nguon khach" khi tiep nhan).
    private static ReportDescriptor PatientSourceSummary() => new()
    {
        Code = "patient-source-summary",
        Title = "BÁO CÁO TỔNG HỢP NGUỒN KHÁCH",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 6,
        Icon = "users-round",
        PdfTypeCode = "PSR",
        Columns = new List<ReportColumn>
        {
            new("sourceLabel",  "Nguồn khách",   ReportColumnType.Text,   ReportAlign.Left,  1.6f),
            new("patientCount", "Số bệnh nhân",  ReportColumnType.Number, ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("percent",      "Tỷ lệ %",       ReportColumnType.Number, ReportAlign.Right, 0.9f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG BỆNH NHÂN", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "patientCount"))), IsMoney: false),
            new("SỐ NGUỒN KHÁC BIỆT", "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    t.sourceLabel                                    AS sourceLabel,
                    t.patientCount                                   AS patientCount,
                    ROUND(t.patientCount / NULLIF(SUM(t.patientCount) OVER (), 0) * 100, 1) AS percent
                FROM (
                    SELECT
                        CASE COALESCE(pt.patient_source, 'NONE')
                            WHEN 'WALK_IN'   THEN N'Vãng lai'
                            WHEN 'REFERRAL'  THEN N'Giới thiệu'
                            WHEN 'RETURN'    THEN N'Tái khám'
                            WHEN 'ONLINE'    THEN N'Online'
                            WHEN 'INSURANCE' THEN N'BHYT'
                            WHEN 'MARKETING' THEN N'Marketing'
                            WHEN 'OTHER'     THEN N'Khác'
                            ELSE N'Chưa phân loại'
                        END AS sourceLabel,
                        COUNT(*) AS patientCount
                    FROM diab_his_pat_patients pt
                    WHERE pt.tenant_id = @tenantId
                      AND pt.deleted_at IS NULL
                      AND pt.created_at BETWEEN @from AND @to
                    GROUP BY sourceLabel
                ) t
                ORDER BY t.patientCount DESC
                LIMIT 100";

            return (sql, p);
        }
    };

    // ================= Q6: Thong Ke Chi Dinh CLS (P1-5) ================= //
    // Gop diab_his_lab_orders (XN) + diab_his_rad_orders (CDHA), phan biet theo modality. Ca hai bang
    // deu ho collation utf8mb4_0900_ai_ci nen KHONG can COLLATE. diab_his_rad_orders hien CHUA co
    // du lieu seed (0 dong) — phan CDHA se rong cho toi khi co seed, KHONG phai loi SQL.
    private static ReportDescriptor ClsIndicationStats() => new()
    {
        Code = "cls-indication-stats",
        Title = "BÁO CÁO THỐNG KÊ CHỈ ĐỊNH CLS",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 8,
        Icon = "clipboard-list",
        PdfTypeCode = "CLS",
        Columns = new List<ReportColumn>
        {
            new("clsType",   "Loại CLS",       ReportColumnType.Text,   ReportAlign.Left,  1.1f),
            new("groupName", "Nhóm/Tên",       ReportColumnType.Text,   ReportAlign.Left,  2f),
            new("orderCount","Số lượt chỉ định", ReportColumnType.Number, ReportAlign.Right, 1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG LƯỢT CHỈ ĐỊNH", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "orderCount"))), IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT clsType, groupName, COUNT(*) AS orderCount
                FROM (
                    SELECT N'Xét nghiệm' AS clsType, lo.test_name AS groupName
                    FROM diab_his_lab_orders lo
                    WHERE lo.tenant_id = @tenantId
                      AND lo.deleted_at IS NULL
                      AND lo.ordered_at BETWEEN @from AND @to
                    UNION ALL
                    SELECT
                        CASE ro.modality
                            WHEN 'US'   THEN N'Siêu âm'
                            WHEN 'XRAY' THEN N'X-Quang'
                            WHEN 'ENDO' THEN N'Nội soi'
                            WHEN 'ECG'  THEN N'Điện tim'
                            ELSE N'CĐHA khác'
                        END AS clsType,
                        ro.procedure_name AS groupName
                    FROM diab_his_rad_orders ro
                    WHERE ro.tenant_id = @tenantId
                      AND ro.deleted_at IS NULL
                      AND ro.ordered_at BETWEEN @from AND @to
                ) x
                GROUP BY clsType, groupName
                ORDER BY clsType, orderCount DESC
                LIMIT 500";

            return (sql, p);
        }
    };

    // ================= Q7: Cong No Benh Nhan (P1-3) ================= //
    private static ReportDescriptor Debts() => new()
    {
        Code = "debts",
        Title = "BÁO CÁO CÔNG NỢ BỆNH NHÂN",
        Group = ReportGroupCategory.Financial,
        GroupOrder = 8,
        Icon = "credit-card",
        PdfTypeCode = "DEB",
        Columns = new List<ReportColumn>
        {
            new("receiptNo",   "Số phiếu",         ReportColumnType.Text,   ReportAlign.Left,  1f),
            new("patientCode", "Mã BN",            ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("patientName", "Họ tên",           ReportColumnType.Text,   ReportAlign.Left,  1.3f),
            new("totalPayable","Tổng phải thu",    ReportColumnType.Money,  ReportAlign.Right, 1.1f, IsGroupSubtotal: true),
            new("paidAmount",  "Đã thu",           ReportColumnType.Money,  ReportAlign.Right, 1f,   IsGroupSubtotal: true),
            new("balance",     "Còn nợ",           ReportColumnType.Money,  ReportAlign.Right, 1f,   IsGroupSubtotal: true),
            new("overdueDays", "Số ngày quá hạn",  ReportColumnType.Number, ReportAlign.Right, 1f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG CÒN NỢ", "#FEF2F2", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "balance"))), IsMoney: true),
            new("SỐ PHIẾU CÒN NỢ", "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>
        {
            new("patientId", "Bệnh nhân", ReportFilterType.Select, OptionsSource: "patients")
        },
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));
            p.Add("patientId", ctx.Filter("patientId"));

            const string sql = @"
                SELECT
                    b.bill_no                                                              AS receiptNo,
                    pt.code                                                                AS patientCode,
                    pt.full_name                                                            AS patientName,
                    b.patient_payable                                                       AS totalPayable,
                    b.paid_amount                                                           AS paidAmount,
                    b.balance                                                               AS balance,
                    DATEDIFF(CURDATE(), COALESCE(b.payment_due_date, DATE(b.created_at)))   AS overdueDays
                FROM diab_his_bil_billing b
                INNER JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
                WHERE b.tenant_id = @tenantId
                  AND b.deleted_at IS NULL
                  AND b.balance > 0
                  AND b.created_at BETWEEN @from AND @to
                  AND (@patientId IS NULL OR b.patient_id = @patientId)
                ORDER BY b.balance DESC
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ================= Dot 9 (P0-1): SO QUY TIEN MAT ================= //
    // Nguon: Thu = diab_his_bil_payments (method='CASH'), Chi = diab_his_bil_cash_out (bang moi,
    // migration 9043). UNION ALL 2 chieu, tinh Ton quy luy ke qua window function SUM() OVER
    // (ORDER BY ts, id lam khoa phu de dam bao thu tu on dinh khi trung ts).
    // Luu y: Ton quy la so du LUY KE TRONG KY (bat dau tu 0), CHUA co khai niem so du dau ky
    // persistent (can bo sung sau neu nghiep vu yeu cau chot so theo thang/nam).
    private static ReportDescriptor SoQuyTienMat() => new()
    {
        Code = "so-quy-tien-mat",
        Title = "SỔ QUỸ TIỀN MẶT",
        Group = ReportGroupCategory.Financial,
        GroupOrder = 9,
        Icon = "wallet",
        PdfTypeCode = "SQT",
        Columns = new List<ReportColumn>
        {
            new("date",        "Ngày giờ",   ReportColumnType.DateTime, ReportAlign.Left,  1.1f),
            new("type",        "Loại",       ReportColumnType.Text,     ReportAlign.Left,  0.7f),
            new("receiptNo",   "Số phiếu",   ReportColumnType.Text,     ReportAlign.Left,  1f),
            new("description", "Diễn giải",  ReportColumnType.Text,     ReportAlign.Left,  1.8f),
            new("thu",         "Thu",        ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("chi",         "Chi",        ReportColumnType.Money,    ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("tonQuy",      "Tồn quỹ",    ReportColumnType.Money,    ReportAlign.Right, 1.1f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG THU", "#ECFDF5", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "thu"))), IsMoney: true),
            new("TỔNG CHI", "#FEF2F2", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "chi"))), IsMoney: true),
            new("TỒN QUỸ CUỐI KỲ", "#EFF6FF",
                rows => rows.Count == 0 ? 0m : ReportValueConverter.ToDecimal(ReportValueConverter.Get(rows[^1], "tonQuy")), IsMoney: true)
        },
        Filters = Array.Empty<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    ts                                                     AS date,
                    loai                                                   AS type,
                    soPhieu                                                AS receiptNo,
                    dienGiai                                                AS description,
                    thu                                                    AS thu,
                    chi                                                    AS chi,
                    SUM(thu - chi) OVER (ORDER BY ts, idSort)             AS tonQuy
                FROM (
                    SELECT
                        pay.paid_at                                                                  AS ts,
                        N'Thu'                                                                       AS loai,
                        b.bill_no                                                                    AS soPhieu,
                        COALESCE(CONCAT(b.bill_no, N' - ', pt.full_name COLLATE utf8mb4_unicode_ci), N'Thu tiền mặt') COLLATE utf8mb4_unicode_ci AS dienGiai,
                        pay.amount                                                                   AS thu,
                        0                                                                             AS chi,
                        pay.id                                                                        AS idSort
                    FROM diab_his_bil_payments pay
                    LEFT JOIN diab_his_bil_billing b ON b.id = pay.billing_id AND b.tenant_id = pay.tenant_id
                    LEFT JOIN diab_his_pat_patients pt ON pt.id = b.patient_id COLLATE utf8mb4_unicode_ci AND pt.tenant_id = b.tenant_id
                    WHERE pay.tenant_id = @tenantId
                      AND pay.method = 'CASH'
                      AND (pay.status IS NULL OR pay.status <> 'VOID')
                      AND pay.paid_at BETWEEN @from AND @to

                    UNION ALL

                    SELECT
                        c.paid_at                                                                    AS ts,
                        N'Chi'                                                                       AS loai,
                        c.code                                                                       AS soPhieu,
                        COALESCE(c.reason, c.category, N'Chi tiền mặt')                                AS dienGiai,
                        0                                                                             AS thu,
                        c.amount                                                                     AS chi,
                        c.id                                                                          AS idSort
                    FROM diab_his_bil_cash_out c
                    WHERE c.tenant_id = @tenantId
                      AND c.deleted_at IS NULL
                      AND c.paid_at BETWEEN @from AND @to
                ) t
                ORDER BY ts, idSort
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ============================================================================
    // NHOM F — KHO DUOC (PHARMACY) — group=Pharmacy.
    // Dung schema co san: diab_his_pha_stock (49 dong, CO DATA), diab_his_pha_drugs (38 dong),
    // diab_his_pha_stock_movements (0 dong — rong an toan, KHONG loi).
    // Luu y: diab_his_pha_stock KHONG co cot deleted_at (chi co deleted_by) — filter ton kho
    // dung "quantity > 0" thay vi "deleted_at IS NULL". Tat ca cac bang lien quan cung
    // collation utf8mb4_0900_ai_ci nen KHONG can COLLATE tren join.
    // Ten hien thi thuoc: uu tien name_vi (hien dang rong o data that) roi name.
    // ============================================================================

    // ================= F1: Ton Kho Hien Tai (gop theo thuoc) ================= //
    private static ReportDescriptor TonKho() => new()
    {
        Code = "ton-kho",
        Title = "BÁO CÁO TỒN KHO HIỆN TẠI",
        Group = ReportGroupCategory.Pharmacy,
        GroupOrder = 1,
        Icon = "package",
        PdfTypeCode = "TKH",
        Columns = new List<ReportColumn>
        {
            new("code",           "Mã thuốc",       ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("drugName",       "Tên thuốc",      ReportColumnType.Text,   ReportAlign.Left,  1.8f),
            new("unit",           "ĐVT",            ReportColumnType.Text,   ReportAlign.Left,  0.6f),
            new("stockQty",       "SL tồn",         ReportColumnType.Number, ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("avgImportPrice", "Đơn giá nhập TB",ReportColumnType.Money,  ReportAlign.Right, 1f),
            new("stockValue",     "Giá trị tồn",    ReportColumnType.Money,  ReportAlign.Right, 1.1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG GIÁ TRỊ TỒN", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "stockValue"))), IsMoney: true),
            new("SỐ THUỐC CÓ TỒN",  "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            // Snapshot ton kho hien tai — khong loc theo khoang ngay (from/to nhan de tuong thich engine chung).
            const string sql = @"
                SELECT
                    d.code                                                    AS code,
                    COALESCE(NULLIF(d.name_vi, ''), d.name)                   AS drugName,
                    d.unit                                                    AS unit,
                    SUM(s.quantity)                                           AS stockQty,
                    ROUND(SUM(s.quantity * s.import_price) / NULLIF(SUM(s.quantity), 0), 2) AS avgImportPrice,
                    SUM(s.quantity * s.import_price)                          AS stockValue
                FROM diab_his_pha_stock s
                INNER JOIN diab_his_pha_drugs d ON d.id = s.drug_id AND d.tenant_id = s.tenant_id
                WHERE s.tenant_id = @tenantId
                  AND s.quantity > 0
                GROUP BY d.id, d.code, d.name_vi, d.name, d.unit
                ORDER BY drugName
                LIMIT 2000";

            return (sql, p);
        }
    };

    // ================= F2: The Kho Chi Tiet Theo Lo/HSD ================= //
    private static ReportDescriptor TheKhoLo() => new()
    {
        Code = "the-kho-lo",
        Title = "BÁO CÁO THẺ KHO CHI TIẾT THEO LÔ",
        Group = ReportGroupCategory.Pharmacy,
        GroupOrder = 2,
        Icon = "layers",
        PdfTypeCode = "TKL",
        Columns = new List<ReportColumn>
        {
            new("code",        "Mã thuốc",       ReportColumnType.Text,   ReportAlign.Left,  0.8f),
            new("drugName",    "Tên thuốc",      ReportColumnType.Text,   ReportAlign.Left,  1.6f),
            new("lotNumber",   "Lô",             ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("mfgDate",     "NSX",            ReportColumnType.Date,   ReportAlign.Left,  0.8f),
            new("expDate",     "HSD",            ReportColumnType.Date,   ReportAlign.Left,  0.8f),
            new("stockQty",    "SL tồn",         ReportColumnType.Number, ReportAlign.Right, 0.7f, IsGroupSubtotal: true),
            new("importPrice", "Đơn giá nhập",   ReportColumnType.Money,  ReportAlign.Right, 0.9f),
            new("stockValue",  "Giá trị",        ReportColumnType.Money,  ReportAlign.Right, 1f,   IsGroupSubtotal: true),
            new("expStatus",   "Trạng thái HSD", ReportColumnType.Text,   ReportAlign.Left,  0.9f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG GIÁ TRỊ TỒN", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "stockValue"))), IsMoney: true),
            new("SỐ LÔ",            "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            // Snapshot theo lo — khong loc theo khoang ngay.
            const string sql = @"
                SELECT
                    d.code                                          AS code,
                    COALESCE(NULLIF(d.name_vi, ''), d.name)         AS drugName,
                    s.lot_number                                    AS lotNumber,
                    s.mfg_date                                      AS mfgDate,
                    s.exp_date                                      AS expDate,
                    s.quantity                                      AS stockQty,
                    s.import_price                                  AS importPrice,
                    s.quantity * s.import_price                     AS stockValue,
                    CASE
                        WHEN s.exp_date < CURDATE() THEN N'Hết hạn'
                        WHEN s.exp_date <= DATE_ADD(CURDATE(), INTERVAL 90 DAY) THEN N'Cận date'
                        ELSE N'Còn hạn'
                    END                                              AS expStatus
                FROM diab_his_pha_stock s
                INNER JOIN diab_his_pha_drugs d ON d.id = s.drug_id AND d.tenant_id = s.tenant_id
                WHERE s.tenant_id = @tenantId
                  AND s.quantity > 0
                ORDER BY drugName, s.exp_date
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ================= F3: Thuoc Can Date / Het Han ================= //
    private static ReportDescriptor ThuocCanDate() => new()
    {
        Code = "thuoc-can-date",
        Title = "BÁO CÁO THUỐC CẬN DATE / HẾT HẠN",
        Group = ReportGroupCategory.Pharmacy,
        GroupOrder = 3,
        Icon = "alarm-clock",
        PdfTypeCode = "CDT",
        Columns = new List<ReportColumn>
        {
            new("drugName",   "Tên thuốc",         ReportColumnType.Text,   ReportAlign.Left,  1.8f),
            new("lotNumber",  "Lô",                ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("expDate",    "HSD",               ReportColumnType.Date,   ReportAlign.Left,  0.9f),
            new("daysLeft",   "Số ngày còn lại",   ReportColumnType.Number, ReportAlign.Right, 1f),
            new("stockQty",   "SL tồn",            ReportColumnType.Number, ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("expStatus",  "Trạng thái",        ReportColumnType.Text,   ReportAlign.Left,  0.9f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LÔ CẬN DATE/HẾT HẠN", "#FEF2F2", rows => rows.Count, IsMoney: false),
            new("TỔNG SL TỒN",            "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "stockQty"))), IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            // Snapshot — loc exp_date <= hom nay + 90 ngay (bao gom ca da het han).
            const string sql = @"
                SELECT
                    COALESCE(NULLIF(d.name_vi, ''), d.name)         AS drugName,
                    s.lot_number                                    AS lotNumber,
                    s.exp_date                                      AS expDate,
                    DATEDIFF(s.exp_date, CURDATE())                 AS daysLeft,
                    s.quantity                                      AS stockQty,
                    CASE WHEN s.exp_date < CURDATE() THEN N'Hết hạn' ELSE N'Cận date' END AS expStatus
                FROM diab_his_pha_stock s
                INNER JOIN diab_his_pha_drugs d ON d.id = s.drug_id AND d.tenant_id = s.tenant_id
                WHERE s.tenant_id = @tenantId
                  AND s.quantity > 0
                  AND s.exp_date <= DATE_ADD(CURDATE(), INTERVAL 90 DAY)
                ORDER BY s.exp_date ASC
                LIMIT 2000";

            return (sql, p);
        }
    };

    // ================= F4: Xuat - Nhap - Ton Theo Ky ================= //
    // TODO schema: diab_his_pha_stock_movements hien 0 dong (chua co nghiep vu ghi nhan bien dong kho
    // thuc te qua bang nay) — bao cao nay se rong (0 dong) cho den khi co du lieu, KHONG phai loi SQL.
    private static ReportDescriptor XuatNhapTon() => new()
    {
        Code = "xuat-nhap-ton",
        Title = "BÁO CÁO XUẤT - NHẬP - TỒN",
        Group = ReportGroupCategory.Pharmacy,
        GroupOrder = 4,
        Icon = "repeat",
        PdfTypeCode = "XNT",
        Columns = new List<ReportColumn>
        {
            new("code",         "Mã thuốc",     ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("drugName",     "Tên thuốc",    ReportColumnType.Text,   ReportAlign.Left,  1.8f),
            new("unit",         "ĐVT",          ReportColumnType.Text,   ReportAlign.Left,  0.6f),
            new("qtyIn",        "SL Nhập",      ReportColumnType.Number, ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("qtyOut",       "SL Xuất",      ReportColumnType.Number, ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("currentStock", "SL tồn hiện tại", ReportColumnType.Number, ReportAlign.Right, 0.9f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG SL NHẬP", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "qtyIn"))), IsMoney: false),
            new("TỔNG SL XUẤT", "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "qtyOut"))), IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    d.code                                          AS code,
                    COALESCE(NULLIF(d.name_vi, ''), d.name)         AS drugName,
                    d.unit                                          AS unit,
                    COALESCE(SUM(CASE WHEN mv.movement_type IN ('IMPORT', 'RETURN') THEN ABS(mv.quantity) ELSE 0 END), 0) AS qtyIn,
                    COALESCE(SUM(CASE WHEN mv.movement_type = 'EXPORT' THEN ABS(mv.quantity) ELSE 0 END), 0)              AS qtyOut,
                    COALESCE(cur.stockQty, 0)                       AS currentStock
                FROM diab_his_pha_drugs d
                LEFT JOIN diab_his_pha_stock st
                    ON st.drug_id = d.id AND st.tenant_id = d.tenant_id
                LEFT JOIN diab_his_pha_stock_movements mv
                    ON mv.stock_id = st.id AND mv.tenant_id = d.tenant_id
                   AND mv.deleted_at IS NULL
                   AND mv.movement_at BETWEEN @from AND @to
                LEFT JOIN (
                    SELECT drug_id, SUM(quantity) AS stockQty
                    FROM diab_his_pha_stock
                    WHERE tenant_id = @tenantId AND quantity > 0
                    GROUP BY drug_id
                ) cur ON cur.drug_id = d.id
                WHERE d.tenant_id = @tenantId
                GROUP BY d.id, d.code, d.name_vi, d.name, d.unit, cur.stockQty
                HAVING qtyIn > 0 OR qtyOut > 0
                ORDER BY drugName
                LIMIT 2000";

            return (sql, p);
        }
    };

    // ================= F5: Danh Muc Thuoc ================= //
    private static ReportDescriptor DanhMucThuoc() => new()
    {
        Code = "danh-muc-thuoc",
        Title = "BÁO CÁO DANH MỤC THUỐC",
        Group = ReportGroupCategory.Pharmacy,
        GroupOrder = 5,
        Icon = "list",
        PdfTypeCode = "DMT",
        Columns = new List<ReportColumn>
        {
            new("stt",           "STT",         ReportColumnType.Number, ReportAlign.Right, 0.5f),
            new("code",          "Mã",          ReportColumnType.Text,   ReportAlign.Left,  0.8f),
            new("drugName",      "Tên thuốc",   ReportColumnType.Text,   ReportAlign.Left,  1.8f),
            new("activeIngredient", "Hoạt chất",ReportColumnType.Text,   ReportAlign.Left,  1.5f),
            new("unit",          "ĐVT",         ReportColumnType.Text,   ReportAlign.Left,  0.6f),
            new("atcCode",       "Mã ATC",      ReportColumnType.Text,   ReportAlign.Left,  0.8f),
            new("sellPrice",     "Giá bán",     ReportColumnType.Money,  ReportAlign.Right, 0.9f),
            new("requiresRx",    "Kê đơn",      ReportColumnType.Text,   ReportAlign.Left,  0.6f),
            new("controlLabel",  "Kiểm soát",   ReportColumnType.Text,   ReportAlign.Left,  0.9f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG SỐ THUỐC", "#F0FDFA", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            // Snapshot danh muc — khong loc theo khoang ngay.
            const string sql = @"
                SELECT
                    ROW_NUMBER() OVER (ORDER BY COALESCE(NULLIF(d.name_vi, ''), d.name)) AS stt,
                    d.code                                              AS code,
                    COALESCE(NULLIF(d.name_vi, ''), d.name)             AS drugName,
                    COALESCE(d.generic_name, N'Chưa ghi nhận')          AS activeIngredient,
                    d.unit                                              AS unit,
                    COALESCE(d.atc_code, N'')                           AS atcCode,
                    COALESCE(NULLIF(d.sell_price, 0), d.price, 0)       AS sellPrice,
                    CASE WHEN d.requires_prescription = 1 OR d.requires_rx = 1 THEN N'Có' ELSE N'Không' END AS requiresRx,
                    CASE
                        WHEN d.is_narcotic = 1     THEN N'Gây nghiện'
                        WHEN d.is_psychotropic = 1 THEN N'Hướng thần'
                        WHEN d.is_controlled = 1   THEN N'Kiểm soát'
                        ELSE N'Thường'
                    END                                                  AS controlLabel
                FROM diab_his_pha_drugs d
                WHERE d.tenant_id = @tenantId
                  AND d.deleted_at IS NULL
                  AND d.is_active = 1
                ORDER BY drugName
                LIMIT 3000";

            return (sql, p);
        }
    };

    // ================= F6: Thuoc Kiem Soat Dac Biet (TT 20/2017) ================= //
    private static ReportDescriptor ThuocKiemSoat() => new()
    {
        Code = "thuoc-kiem-soat",
        Title = "BÁO CÁO THUỐC KIỂM SOÁT ĐẶC BIỆT",
        Group = ReportGroupCategory.Pharmacy,
        GroupOrder = 6,
        Icon = "shield-alert",
        PdfTypeCode = "TKS",
        Columns = new List<ReportColumn>
        {
            new("drugName",    "Tên thuốc",   ReportColumnType.Text,   ReportAlign.Left,  1.8f),
            new("controlType", "Phân loại",   ReportColumnType.Text,   ReportAlign.Left,  1.1f),
            new("lotNumber",   "Lô",          ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("expDate",     "HSD",         ReportColumnType.Date,   ReportAlign.Left,  0.9f),
            new("stockQty",    "SL tồn",      ReportColumnType.Number, ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("importPrice", "Đơn giá",     ReportColumnType.Money,  ReportAlign.Right, 0.9f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LÔ THUỐC KIỂM SOÁT", "#FEF2F2", rows => rows.Count, IsMoney: false),
            new("TỔNG SL TỒN",           "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "stockQty"))), IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            // Snapshot — chi lay thuoc gan co it nhat 1 trong 3 co: gay nghien/huong than/kiem soat.
            // Neu chua co thuoc nao gan co (data hien tai) thi tra ve rong an toan (khong loi).
            const string sql = @"
                SELECT
                    COALESCE(NULLIF(d.name_vi, ''), d.name)             AS drugName,
                    CASE
                        WHEN d.is_narcotic = 1     THEN N'Gây nghiện'
                        WHEN d.is_psychotropic = 1 THEN N'Hướng thần'
                        ELSE N'Kiểm soát'
                    END                                                  AS controlType,
                    s.lot_number                                        AS lotNumber,
                    s.exp_date                                          AS expDate,
                    s.quantity                                          AS stockQty,
                    s.import_price                                      AS importPrice
                FROM diab_his_pha_drugs d
                INNER JOIN diab_his_pha_stock s ON s.drug_id = d.id AND s.tenant_id = d.tenant_id
                WHERE d.tenant_id = @tenantId
                  AND (d.is_narcotic = 1 OR d.is_psychotropic = 1 OR d.is_controlled = 1)
                  AND s.quantity > 0
                ORDER BY drugName, s.exp_date
                LIMIT 2000";

            return (sql, p);
        }
    };

    // ================= F7: Thuoc Duoi Dinh Muc Ton ================= //
    private static ReportDescriptor ThuocDuoiDinhMuc() => new()
    {
        Code = "thuoc-duoi-dinh-muc",
        Title = "BÁO CÁO THUỐC DƯỚI ĐỊNH MỨC TỒN",
        Group = ReportGroupCategory.Pharmacy,
        GroupOrder = 7,
        Icon = "trending-down",
        PdfTypeCode = "DDM",
        Columns = new List<ReportColumn>
        {
            new("code",          "Mã",              ReportColumnType.Text,   ReportAlign.Left,  0.8f),
            new("drugName",      "Tên thuốc",       ReportColumnType.Text,   ReportAlign.Left,  1.8f),
            new("unit",          "ĐVT",             ReportColumnType.Text,   ReportAlign.Left,  0.6f),
            new("stockQty",      "SL tồn",          ReportColumnType.Number, ReportAlign.Right, 0.8f),
            new("reorderLevel",  "Định mức",        ReportColumnType.Number, ReportAlign.Right, 0.8f),
            new("needMore",      "Cần đặt thêm",    ReportColumnType.Number, ReportAlign.Right, 0.9f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ THUỐC CẦN ĐẶT",  "#FEF2F2", rows => rows.Count, IsMoney: false),
            new("TỔNG SL CẦN ĐẶT",   "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "needMore"))), IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            // Snapshot — gop ton kho theo thuoc, loc SUM(quantity) < reorder_level (reorder_level > 0).
            const string sql = @"
                SELECT
                    d.code                                          AS code,
                    COALESCE(NULLIF(d.name_vi, ''), d.name)         AS drugName,
                    d.unit                                          AS unit,
                    COALESCE(SUM(s.quantity), 0)                    AS stockQty,
                    d.reorder_level                                 AS reorderLevel,
                    GREATEST(d.reorder_level - COALESCE(SUM(s.quantity), 0), 0) AS needMore
                FROM diab_his_pha_drugs d
                LEFT JOIN diab_his_pha_stock s
                    ON s.drug_id = d.id AND s.tenant_id = d.tenant_id AND s.quantity > 0
                WHERE d.tenant_id = @tenantId
                  AND d.reorder_level > 0
                  AND d.deleted_at IS NULL
                GROUP BY d.id, d.code, d.name_vi, d.name, d.unit, d.reorder_level
                HAVING stockQty < d.reorder_level
                ORDER BY needMore DESC
                LIMIT 2000";

            return (sql, p);
        }
    };

    // ============================================================================
    // DOT 11 — BI (Statistics) + Kiem ke kho (Pharmacy)
    // ============================================================================

    // ================= BI-1: Luot Kham Theo Gio (peak hour) ================= //
    private static ReportDescriptor LuotKhamTheoGio() => new()
    {
        Code = "luot-kham-theo-gio",
        Title = "BÁO CÁO LƯỢT KHÁM THEO GIỜ",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 9,
        Icon = "clock",
        PdfTypeCode = "LKG",
        Columns = new List<ReportColumn>
        {
            new("hourLabel", "Khung giờ",  ReportColumnType.Text,   ReportAlign.Left,  1f),
            new("visitCount","Số lượt",    ReportColumnType.Number, ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("percent",   "Tỷ lệ %",    ReportColumnType.Number, ReportAlign.Right, 0.9f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG LƯỢT KHÁM", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "visitCount"))), IsMoney: false),
            new("SỐ KHUNG GIỜ CÓ KHÁCH", "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    t.hr                                                              AS hourOrder,
                    CONCAT(LPAD(t.hr, 2, '0'), N':00')                                AS hourLabel,
                    t.visitCount                                                       AS visitCount,
                    ROUND(t.visitCount / NULLIF(SUM(t.visitCount) OVER (), 0) * 100, 1) AS percent
                FROM (
                    SELECT HOUR(COALESCE(e.started_at, e.created_at)) AS hr, COUNT(*) AS visitCount
                    FROM diab_his_enc_encounters e
                    WHERE e.tenant_id = @tenantId
                      AND e.deleted_at IS NULL
                      AND COALESCE(e.started_at, e.created_at) BETWEEN @from AND @to
                    GROUP BY hr
                ) t
                ORDER BY hourOrder
                LIMIT 24";

            return (sql, p);
        }
    };

    // ================= BI-2: Ty Le No-Show Lich Hen ================= //
    private static ReportDescriptor TyLeNoShow() => new()
    {
        Code = "ty-le-no-show",
        Title = "BÁO CÁO TỶ LỆ NO-SHOW LỊCH HẸN",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 10,
        Icon = "calendar-x",
        PdfTypeCode = "NSW",
        Columns = new List<ReportColumn>
        {
            new("statusLabel", "Trạng thái", ReportColumnType.Text,   ReportAlign.Left,  1.4f),
            new("count",       "Số lượng",   ReportColumnType.Number, ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("percent",     "Tỷ lệ %",    ReportColumnType.Number, ReportAlign.Right, 0.9f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG LỊCH HẸN", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "count"))), IsMoney: false),
            new("TỶ LỆ NO-SHOW %", "#FEF2F2", rows =>
            {
                var total = rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "count")));
                var noShow = rows.Where(r => (string?)ReportValueConverter.Get(r, "statusCode") == "NO_SHOW")
                                  .Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "count")));
                return total > 0 ? Math.Round(noShow / total * 100, 1) : 0m;
            }, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    t.status                                                       AS statusCode,
                    t.statusLabel                                                   AS statusLabel,
                    t.cnt                                                           AS count,
                    ROUND(t.cnt / NULLIF(SUM(t.cnt) OVER (), 0) * 100, 1)          AS percent
                FROM (
                    SELECT
                        a.status AS status,
                        CASE a.status
                            WHEN 'PENDING'     THEN N'Chờ xác nhận'
                            WHEN 'CONFIRMED'   THEN N'Đã xác nhận'
                            WHEN 'CHECKED_IN'  THEN N'Đã đến'
                            WHEN 'CANCELLED'   THEN N'Đã hủy'
                            WHEN 'NO_SHOW'     THEN N'Không đến'
                            ELSE N'Khác'
                        END AS statusLabel,
                        COUNT(*) AS cnt
                    FROM diab_his_sch_appointments a
                    WHERE a.tenant_id = @tenantId
                      AND a.deleted_at IS NULL
                      AND a.appointment_at BETWEEN @from AND @to
                    GROUP BY a.status
                ) t
                ORDER BY t.cnt DESC
                LIMIT 20";

            return (sql, p);
        }
    };

    // ================= BI-3: Thong Ke Su Dung Khang Sinh ================= //
    // Ghi chu du lieu: pha_drugs.is_antibiotic hien co rat it thuoc duoc gan co (dev seed) —
    // bao cao co the tra ve rong an toan cho den khi danh muc thuoc kiem soat khang sinh day du hon.
    private static ReportDescriptor SuDungKhangSinh() => new()
    {
        Code = "su-dung-khang-sinh",
        Title = "BÁO CÁO THỐNG KÊ SỬ DỤNG KHÁNG SINH",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 11,
        Icon = "pill",
        PdfTypeCode = "KSI",
        Columns = new List<ReportColumn>
        {
            new("code",           "Mã thuốc",     ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("drugName",       "Tên kháng sinh", ReportColumnType.Text, ReportAlign.Left,  1.8f),
            new("prescribedTimes","Số lần kê",    ReportColumnType.Number, ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("totalQuantity",  "Tổng SL",      ReportColumnType.Number, ReportAlign.Right, 1f, IsGroupSubtotal: true)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ LOẠI KHÁNG SINH ĐƯỢC KÊ", "#F0FDFA", rows => rows.Count, IsMoney: false),
            new("TỔNG LƯỢT KÊ", "#FFFBEB", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "prescribedTimes"))), IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    d.code                                          AS code,
                    COALESCE(NULLIF(d.name_vi, ''), d.name)         AS drugName,
                    COUNT(*)                                        AS prescribedTimes,
                    SUM(pi.quantity)                                AS totalQuantity
                FROM diab_his_pha_prescription_items pi
                INNER JOIN diab_his_pha_drugs d ON d.id = pi.drug_id AND d.tenant_id = pi.tenant_id
                INNER JOIN diab_his_pha_prescriptions pr ON pr.id = pi.prescription_id AND pr.tenant_id = pi.tenant_id
                WHERE pi.tenant_id = @tenantId
                  AND pi.deleted_at IS NULL
                  AND d.is_antibiotic = 1
                  AND pr.created_at BETWEEN @from AND @to
                GROUP BY d.id, d.code, d.name_vi, d.name
                ORDER BY prescribedTimes DESC
                LIMIT 500";

            return (sql, p);
        }
    };

    // ================= BI-4: Thoi Gian Tra Ket Qua CLS (TAT) ================= //
    // Ghi chu du lieu: dung lab_results.performed_at lam moc "co ket qua" (verified_at hau nhu chua duoc
    // dien trong du lieu hien tai — chi 1/30 dong) — fallback verified_at neu performed_at rong.
    private static ReportDescriptor TatCls() => new()
    {
        Code = "tat-cls",
        Title = "BÁO CÁO THỜI GIAN TRẢ KẾT QUẢ CLS (TAT)",
        Group = ReportGroupCategory.Statistics,
        GroupOrder = 12,
        Icon = "timer",
        PdfTypeCode = "TAT",
        Columns = new List<ReportColumn>
        {
            new("testName",  "Tên XN",             ReportColumnType.Text,   ReportAlign.Left,  2f),
            new("sampleCount","Số mẫu",            ReportColumnType.Number, ReportAlign.Right, 1f, IsGroupSubtotal: true),
            new("avgTatHours","TAT trung bình (giờ)", ReportColumnType.Number, ReportAlign.Right, 1.2f),
            new("maxTatHours","TAT lâu nhất (giờ)",ReportColumnType.Number, ReportAlign.Right, 1.2f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("TỔNG SỐ MẪU CÓ KẾT QUẢ", "#F0FDFA", rows => rows.Sum(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "sampleCount"))), IsMoney: false),
            new("SỐ LOẠI XN", "#FFFBEB", rows => rows.Count, IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    lo.test_name                                                              AS testName,
                    COUNT(*)                                                                  AS sampleCount,
                    ROUND(AVG(TIMESTAMPDIFF(MINUTE, lo.ordered_at, COALESCE(lr.performed_at, lr.verified_at))) / 60, 1) AS avgTatHours,
                    ROUND(MAX(TIMESTAMPDIFF(MINUTE, lo.ordered_at, COALESCE(lr.performed_at, lr.verified_at))) / 60, 1) AS maxTatHours
                FROM diab_his_lab_results lr
                INNER JOIN diab_his_lab_orders lo ON lo.id = lr.order_id AND lo.tenant_id = lr.tenant_id
                WHERE lr.tenant_id = @tenantId
                  AND lr.deleted_at IS NULL
                  AND lo.deleted_at IS NULL
                  AND COALESCE(lr.performed_at, lr.verified_at) IS NOT NULL
                  AND lo.ordered_at BETWEEN @from AND @to
                GROUP BY lo.test_name
                ORDER BY avgTatHours DESC
                LIMIT 500";

            return (sql, p);
        }
    };

    // ================= Kiem Ke Kho — dot 11 (migration 9044) ================= //
    private static ReportDescriptor KiemKeKho() => new()
    {
        Code = "kiem-ke-kho",
        Title = "BÁO CÁO KIỂM KÊ KHO",
        Group = ReportGroupCategory.Pharmacy,
        GroupOrder = 8,
        Icon = "clipboard-check",
        PdfTypeCode = "KKO",
        Columns = new List<ReportColumn>
        {
            new("stocktakeDate", "Ngày KK",      ReportColumnType.Date,   ReportAlign.Left,  0.9f),
            new("stocktakeCode", "Mã phiếu",     ReportColumnType.Text,   ReportAlign.Left,  0.9f),
            new("drugCode",      "Mã thuốc",     ReportColumnType.Text,   ReportAlign.Left,  0.8f),
            new("drugName",      "Tên thuốc",    ReportColumnType.Text,   ReportAlign.Left,  1.6f),
            new("lotNumber",     "Lô",           ReportColumnType.Text,   ReportAlign.Left,  0.8f),
            new("systemQty",     "Tồn hệ thống", ReportColumnType.Number, ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("countedQty",    "Tồn thực tế",  ReportColumnType.Number, ReportAlign.Right, 0.9f, IsGroupSubtotal: true),
            new("difference",    "Chênh lệch",   ReportColumnType.Number, ReportAlign.Right, 0.8f, IsGroupSubtotal: true),
            new("note",          "Ghi chú",      ReportColumnType.Text,   ReportAlign.Left,  1.4f)
        },
        Kpis = new List<ReportKpiSpec>
        {
            new("SỐ DÒNG KIỂM KÊ", "#F0FDFA", rows => rows.Count, IsMoney: false),
            new("SỐ DÒNG CHÊNH LỆCH", "#FEF2F2", rows => rows.Count(r => ReportValueConverter.ToDecimal(ReportValueConverter.Get(r, "difference")) != 0), IsMoney: false)
        },
        Filters = new List<ReportFilter>(),
        BuildQuery = ctx =>
        {
            var p = new DynamicParameters();
            p.Add("tenantId", ctx.TenantId);
            p.Add("from", ctx.From.ToDateTime(TimeOnly.MinValue));
            p.Add("to", ctx.To.ToDateTime(TimeOnly.MaxValue));

            const string sql = @"
                SELECT
                    st.stocktake_date                                       AS stocktakeDate,
                    st.code                                                 AS stocktakeCode,
                    d.code                                                  AS drugCode,
                    COALESCE(NULLIF(d.name_vi, ''), d.name)                 AS drugName,
                    it.lot_number                                           AS lotNumber,
                    it.system_qty                                           AS systemQty,
                    it.counted_qty                                          AS countedQty,
                    it.difference                                           AS difference,
                    it.note                                                 AS note
                FROM diab_his_pha_stocktake_items it
                INNER JOIN diab_his_pha_stocktakes st
                    ON st.id = it.stocktake_id AND st.tenant_id = it.tenant_id AND st.deleted_at IS NULL
                INNER JOIN diab_his_pha_drugs d
                    ON d.id = it.drug_id COLLATE utf8mb4_0900_ai_ci AND d.tenant_id = it.tenant_id
                WHERE it.tenant_id = @tenantId
                  AND it.deleted_at IS NULL
                  AND st.stocktake_date BETWEEN @from AND @to
                ORDER BY st.stocktake_date DESC, drugName, lotNumber
                LIMIT 3000";

            return (sql, p);
        }
    };
}
