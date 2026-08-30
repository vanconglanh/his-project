using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Application.Common;

namespace ProDiabHis.Infrastructure.Bhyt;

/// <summary>
/// Query encounters + billings + items trong period_month, build XML Bang 1-5 theo QD 4750.
/// ma_lien_ket = {tenant_code}{encounter_id} (toi da 200 ky tu).
/// Toan bo SQL nam trong <see cref="BhytXmlSql"/> de review + unit test ten bang.
/// </summary>
public class BhytXmlGeneratorImpl : IBhytXmlGenerator
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<BhytXmlGeneratorImpl> _logger;

    private readonly IEncryptionService _encryption;
    private readonly IAuditService _audit;

    public BhytXmlGeneratorImpl(IDapperConnectionFactory db, ILogger<BhytXmlGeneratorImpl> logger,
        IEncryptionService encryption, IAuditService audit)
    {
        _db = db; _logger = logger; _encryption = encryption; _audit = audit;
    }

    /// <summary>Giai ma so the BHYT. Loi giai ma -> log ERROR va tra null (khong bao gio xuat ciphertext ra XML).</summary>
    private string? DecryptCardNo(string? stored, string encounterId)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;
        try
        {
            return PiiCrypto.Current is { } pii && pii.IsProtected(stored)
                ? pii.Unprotect(stored)
                : _encryption.Decrypt(stored);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BhytXmlGenerator: giai ma so the BHYT that bai encounter={EncId}", encounterId);
            return null;
        }
    }

    // Cot tra ve tu MySQL co the la INT/DECIMAL/DOUBLE tuy bang -> unbox truc tiep sang decimal se nem
    // InvalidCastException. Luon di qua Convert.
    private static decimal Dec(object? v) => v is null || v is DBNull ? 0m : Convert.ToDecimal(v);
    private static int Int(object? v, int fallback) => v is null || v is DBNull ? fallback : Convert.ToInt32(v);
    private static string Str(object? v) => v is null || v is DBNull ? "" : Convert.ToString(v) ?? "";
    private static DateTime? Dt(object? v) => v is null || v is DBNull ? null : Convert.ToDateTime(v);

    private static object? Col(IDictionary<string, object?>? row, string name)
        => row is not null && row.TryGetValue(name, out var v) ? v : null;

    public async Task<BhytXmlGenerateResult> GenerateAsync(
        int exportId, int tenantId, string periodMonth,
        string? scopeFilterJson, CancellationToken ct)
    {
        _logger.LogInformation("BhytXmlGenerator: start exportId={Id} period={Period}", exportId, periodMonth);

        using var conn = (IDbConnection)_db.CreateConnection();

        // Lay tenant code de build ma_lien_ket
        var tenantCode = await conn.ExecuteScalarAsync<string>(
            BhytXmlSql.TenantCode, new { t = tenantId }) ?? tenantId.ToString();

        // Parse period_month -> date range
        if (!TryParsePeriod(periodMonth, out var dateFrom, out var dateTo))
            return new BhytXmlGenerateResult(false, 0, 0, [], "period_month khong hop le");

        // Query encounters trong ky co BHYT
        var encounters = (await conn.QueryAsync<dynamic>(
            BhytXmlSql.Encounters,
            new { t = tenantId, df = dateFrom, dt = dateTo })).ToList();

        if (encounters.Count == 0)
            return new BhytXmlGenerateResult(false, 0, 0, [], "BHYT_EXPORT_NO_ENCOUNTERS");

        // Hang muc 6: xuat XML giam dinh = GIAI MA HANG LOAT so the BHYT -> bat buoc ghi audit.
        // Chinh sach chong spam audit: chi ghi 1 ban ghi cho ca lo, khong ghi tung benh nhan.
        await _audit.LogAsync("PII_BULK_DECRYPT", "BhytExport", exportId.ToString(),
            AuditSeverity.WARN, false, null,
            new { tenantId, periodMonth, encounterCount = encounters.Count, field = "insurance_card_no" }, ct);

        var items = new List<BhytExportItemData>();
        decimal totalRequested = 0;
        int table1Idx = 0;

        // Migration 9180: DUONG_DUNG (XML 4210 Bang 2) BAT BUOC. Thu tu lay: prescription_items.route
        // -> drugs.route (da COALESCE trong BhytXmlSql.PrescriptionItems). Neu ca 2 rong => KHONG phat
        // hanh XML, gom danh sach thuoc thieu duong dung roi bao loi DRUG_ROUTE_MISSING (khong hardcode).
        var missingRouteDrugs = new List<string>();

        foreach (var enc in encounters)
        {
            var e = (IDictionary<string, object?>)enc;
            var encId = Str(Col(e, "id"));
            var maLienKet = $"{tenantCode}{encId}";
            if (maLienKet.Length > 200) maLienKet = maLienKet[..200];

            // Muc huong BHYT lay tu the cua benh nhan; the khong ghi -> 80% (muc pho bien nhat).
            var mucHuong = Int(Col(e, "muc_huong"), 80);

            // Bang 1: Tong hop dot kham
            var billing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                BhytXmlSql.BillingSummary, new { eid = encId, t = tenantId });
            var b = billing as IDictionary<string, object?>;

            var tThuoc = Dec(Col(b, "t_thuoc"));
            var tTongchi = Dec(Col(b, "t_tongchi"));
            var tBhtt = Dec(Col(b, "t_bhtt"));
            var tBntt = Dec(Col(b, "t_bntt"));

            // QD 4750 - Bang 1: MA_BENH = chan doan CHINH, MA_BENH_KHAC = cac chan doan kem theo (ngan cach bang ";")
            // Luu y: bang dung la diab_his_enc_diagnoses, phan biet chinh/phu bang cot `type`.
            var diagRows = (await conn.QueryAsync<dynamic>(
                BhytXmlSql.Diagnoses, new { t = tenantId, eid = encId })).ToList();

            var primaryCode = diagRows
                .Where(d => string.Equals((string?)d.type, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                .Select(d => (string)d.icd10_code)
                .FirstOrDefault();

            var secondaryCodes = diagRows
                .Where(d => !string.Equals((string?)d.type, "PRIMARY", StringComparison.OrdinalIgnoreCase))
                .Select(d => (string)d.icd10_code)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var maBenh = string.IsNullOrWhiteSpace(primaryCode) ? "Z00" : primaryCode!;
            var maBenhKhac = secondaryCodes.Count > 0
                ? string.Join(BhytXmlConst.MaBenhKtSeparator_ChuaXacMinh, secondaryCodes)
                : null;

            if (string.IsNullOrWhiteSpace(primaryCode))
                _logger.LogWarning(
                    "BhytXmlGenerator: encounter {EncId} khong co chan doan CHINH, MA_BENH_CHINH fallback Z00 - nguy co xuat toan",
                    encId);

            var patientCode = Str(Col(e, "patient_code"));

            var table1Row = new BhytTable1Row(
                MaLienKet: maLienKet,
                // MA_BN = ma benh nhan cua co so (pat_patients.code); chua co ma -> fallback ve patient_id.
                MaBn: string.IsNullOrWhiteSpace(patientCode) ? Str(Col(e, "patient_id")) : patientCode,
                HoTen: Str(Col(e, "full_name")),
                NgaySinh: Dt(Col(e, "date_of_birth"))?.ToString("yyyy-MM-dd") ?? "",
                GioiTinh: Str(Col(e, "gender")) == "FEMALE" ? 2 : 1,
                // Hang muc 6: so the BHYT luu ma hoa AES-256-GCM -> BAT BUOC giai ma,
                // neu khong file XML giam dinh se chua ciphertext.
                MaTheBhyt: DecryptCardNo(Str(Col(e, "ma_the_bhyt_enc")), encId) ?? "",
                MaDkbd: Str(Col(e, "ma_dkbd")),
                GtTheTu: Dt(Col(e, "gt_the_tu"))?.ToString("yyyy-MM-dd") ?? "",
                GtTheDen: Dt(Col(e, "gt_the_den"))?.ToString("yyyy-MM-dd") ?? "",
                MaLoaiKcb: 1,
                NgayVao: Dt(Col(e, "started_at")) ?? DateTime.UtcNow,
                NgayRa: Dt(Col(e, "finished_at")) ?? DateTime.UtcNow,
                SoNgayDtri: 1,
                KetQuaDtri: 1,
                MaBenhChinh: maBenh,
                MaBenhKt: maBenhKhac,
                LyDoVvien: "Kham benh dinh ky",
                ChanDoanRv: "",
                TThuoc: tThuoc,
                // TODO(BHYT): schema chua co truong phan loai vat tu y te (VTYT) tren billing item
                // (item_type chi co SERVICE|DRUG|PROCEDURE|LAB|RAD|PACKAGE|OTHER) -> T_VTYT tam de 0.
                // KHONG duoc bia cong thuc (vd SUM(amount*0.1)) - giu 0 cho toi khi co nguon that.
                TVtyt: 0m,
                TTongchi: tTongchi,
                TBhtt: tBhtt,
                TBntt: tBntt,
                TBncct: 0m);

            var rowJson1 = JsonSerializer.Serialize(table1Row);
            items.Add(new BhytExportItemData(1, table1Idx++, rowJson1, maLienKet, encId, null, tBhtt));
            totalRequested += tBhtt;

            // Bang 2: Thuoc BHYT
            var prescItems = await conn.QueryAsync<dynamic>(
                BhytXmlSql.PrescriptionItems, new { eid = encId, t = tenantId });

            int tbl2Idx = 0;
            foreach (var drug in prescItems)
            {
                var d = (IDictionary<string, object?>)drug;
                var donViTinh = Str(Col(d, "don_vi_tinh"));
                var duongDung = Str(Col(d, "duong_dung"));
                var maThuoc = Str(Col(d, "ma_thuoc"));
                var tenThuoc = Str(Col(d, "ten_thuoc"));

                // Migration 9180: KHONG hardcode "uong". Rong => gom vao danh sach thieu, bao loi cuoi ky.
                if (duongDung.Length == 0)
                {
                    var label = $"[{(maThuoc.Length > 0 ? maThuoc : "?")}] {tenThuoc}".Trim();
                    if (!missingRouteDrugs.Contains(label))
                        missingRouteDrugs.Add(label);
                }

                var table2Row = new BhytTable2Row(
                    MaLienKet: maLienKet,
                    MaThuoc: maThuoc,
                    TenThuoc: tenThuoc,
                    DonViTinh: donViTinh.Length > 0 ? donViTinh : "vien",
                    HamLuong: Str(Col(d, "ham_luong")),
                    DuongDung: duongDung,
                    LieuDung: Str(Col(d, "lieu_dung")),
                    // TODO(BHYT): diab_his_pha_drugs chua co nguon nhap lieu SO_DANG_KY
                    // (them cot rong trong migration 9110, cho module quan ly dau thau/dang ky thuoc) -> de trong.
                    // MA_NHA_THAU: KHONG thuoc chuan XML2 BYT -> da bo map (cot DB van giu cho quan ly kho noi bo).
                    SoDangKy: Str(Col(d, "so_dang_ky")),
                    PhamViTt: 1,
                    SoLuong: Dec(Col(d, "so_luong")),
                    DonGia: Dec(Col(d, "don_gia")),
                    ThanhTien: Dec(Col(d, "thanh_tien")),
                    // TODO(BHYT): prescription_items chi co co bhyt_applicable, chua co so tien BHYT chi tra
                    // theo tung dong thuoc -> T_BHTT dong thuoc de 0; tong quyet toan lay tu Bang 1/Bang 5.
                    TBhtt: 0m,
                    TNguonkhac: 0m, TNguonkhacBhtt: 0m, TNguonkhacKhac: 0m,
                    MucHuong: mucHuong,
                    NgayYl: Dt(Col(d, "ngay_yl")) ?? DateTime.UtcNow,
                    // TODO(BHYT): don thuoc chua luu ma phong kham theo danh muc BHYT.
                    MaPhong: "",
                    // Luu y: doctor_id la UUID noi bo, schema chua co ma bac si/CCHN theo chuan BHYT.
                    MaBs: Str(Col(d, "ma_bs")),
                    MaDichvuKem: null,
                    // MAHIEU_LO / HAN_DUNG: KHONG thuoc chuan XML2 BYT -> da bo map. Du lieu so lo/han dung
                    // (diab_his_pha_dispense_items, FEFO) van con trong DB/query cho quan ly kho noi bo.
                    SoHop: null);

                items.Add(new BhytExportItemData(2, tbl2Idx++,
                    JsonSerializer.Serialize(table2Row), maLienKet, encId, null, 0m));
            }

            // Bang 3: Dich vu ky thuat / CLS.
            // Nguon = dong hoa don khong phai thuoc, vi bang chi dinh CLS khong luu gia/BHYT.
            var serviceItems = await conn.QueryAsync<dynamic>(
                BhytXmlSql.ServiceItems, new { eid = encId, t = tenantId });

            int tbl3Idx = 0;
            foreach (var cls in serviceItems)
            {
                var c = (IDictionary<string, object?>)cls;
                var svcBhtt = Dec(Col(c, "t_bhtt"));

                var table3Row = new BhytTable3Row(
                    MaLienKet: maLienKet,
                    MaDichVu: Str(Col(c, "ma_dich_vu")),
                    MaVatTu: null, TenVatTu: null,
                    // TODO(BHYT): billing_items chua co cot don vi tinh -> mac dinh "lan".
                    DonViTinh: "lan",
                    PhamVi: 1,
                    SoLuong: Dec(Col(c, "so_luong")),
                    DonGia: Dec(Col(c, "don_gia")),
                    TtThau: null,
                    ThanhTien: Dec(Col(c, "thanh_tien")),
                    TBhtt: svcBhtt,
                    MucHuong: mucHuong,
                    NgayYl: Dt(Col(c, "ngay_yl")) ?? DateTime.UtcNow,
                    // TODO(BHYT): billing_items khong luu phong / bac si chi dinh.
                    MaPhong: "",
                    MaBs: "",
                    MaBenh: maBenh,
                    // TODO(BHYT): ngay tra ket qua CLS chua duoc lien ket tu bang ket qua sang dong hoa don.
                    NgayKq: null);

                items.Add(new BhytExportItemData(3, tbl3Idx++,
                    JsonSerializer.Serialize(table3Row), maLienKet, encId, null, svcBhtt));
            }

            // Bang 5: Tong hop chi phi
            var table5Row = new BhytTable5Row(
                MaLienKet: maLienKet,
                MaChiPhi: "CP01",
                TenChiPhi: "Tong chi phi kham benh",
                NhomChiPhi: 1,
                ThanhTien: tTongchi,
                TBhtt: tBhtt,
                TBntt: tBntt,
                TNguonkhac: 0m);

            items.Add(new BhytExportItemData(5, 0, JsonSerializer.Serialize(table5Row), maLienKet, encId, null, 0m));
        }

        // Migration 9180: chan phat hanh XML khi con thuoc thieu DUONG_DUNG (bat buoc XML 4210 Bang 2).
        if (missingRouteDrugs.Count > 0)
        {
            _logger.LogWarning("BhytXmlGenerator: exportId={Id} thieu duong dung o {Count} thuoc",
                exportId, missingRouteDrugs.Count);
            var msg = "DRUG_ROUTE_MISSING: Không thể phát hành XML giám định vì thiếu đường dùng (DUONG_DUNG) "
                    + "cho các thuốc sau: " + string.Join("; ", missingRouteDrugs)
                    + ". Vui lòng bổ sung đường dùng trong danh mục thuốc hoặc đơn thuốc trước khi xuất.";
            return new BhytXmlGenerateResult(false, encounters.Count, 0, [], msg);
        }

        _logger.LogInformation("BhytXmlGenerator: done exportId={Id}, {Count} encounters, {Items} items",
            exportId, encounters.Count, items.Count);

        return new BhytXmlGenerateResult(true, encounters.Count, totalRequested, items, null);
    }

    private static bool TryParsePeriod(string periodMonth, out DateTime from, out DateTime to)
    {
        from = to = default;
        if (!DateTime.TryParseExact(periodMonth + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out from))
            return false;
        to = from.AddMonths(1);
        return true;
    }
}
