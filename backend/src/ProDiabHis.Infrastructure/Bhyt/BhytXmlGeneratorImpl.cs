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
/// </summary>
public class BhytXmlGeneratorImpl : IBhytXmlGenerator
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<BhytXmlGeneratorImpl> _logger;

    public BhytXmlGeneratorImpl(IDapperConnectionFactory db, ILogger<BhytXmlGeneratorImpl> logger)
    {
        _db = db; _logger = logger;
    }

    public async Task<BhytXmlGenerateResult> GenerateAsync(
        int exportId, int tenantId, string periodMonth,
        string? scopeFilterJson, CancellationToken ct)
    {
        _logger.LogInformation("BhytXmlGenerator: start exportId={Id} period={Period}", exportId, periodMonth);

        using var conn = (IDbConnection)_db.CreateConnection();

        // Lay tenant code de build ma_lien_ket
        var tenantCode = await conn.ExecuteScalarAsync<string>(
            "SELECT IFNULL(clinic_code, CAST(id AS CHAR)) FROM diab_his_tenants WHERE id=@t",
            new { t = tenantId }) ?? tenantId.ToString();

        // Parse period_month -> date range
        if (!TryParsePeriod(periodMonth, out var dateFrom, out var dateTo))
            return new BhytXmlGenerateResult(false, 0, 0, [], "period_month khong hop le");

        // Query encounters trong ky co BHYT
        var encounters = (await conn.QueryAsync<dynamic>(
            @"SELECT e.id, e.patient_id, e.doctor_id, e.started_at, e.finished_at,
                     p.full_name, p.date_of_birth, p.gender,
                     i.insurance_code as ma_the_bhyt, i.registered_hospital_code as ma_dkbd,
                     i.valid_from as gt_the_tu, i.valid_to as gt_the_den,
                     i.coverage_percent as muc_huong
              FROM diab_his_clinic_encounters e
              JOIN diab_pat_patients p ON p.id = e.patient_id
              LEFT JOIN diab_his_patient_insurances i ON i.patient_id = e.patient_id AND i.is_active = 1
              WHERE e.tenant_id = @t
                AND e.started_at >= @df AND e.started_at < @dt
                AND e.deleted_at IS NULL
                AND i.insurance_code IS NOT NULL
              ORDER BY e.started_at",
            new { t = tenantId, df = dateFrom, dt = dateTo })).ToList();

        if (encounters.Count == 0)
            return new BhytXmlGenerateResult(false, 0, 0, [], "BHYT_EXPORT_NO_ENCOUNTERS");

        var items = new List<BhytExportItemData>();
        decimal totalRequested = 0;
        int table1Idx = 0;

        foreach (var enc in encounters)
        {
            var encId = (string)enc.id;
            var maLienKet = $"{tenantCode}{encId}";
            if (maLienKet.Length > 200) maLienKet = maLienKet[..200];

            // Bang 1: Tong hop dot kham
            var billing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT COALESCE(SUM(bi.amount),0) as t_thuoc,
                         COALESCE(SUM(bi.amount * 0.1),0) as t_vtyt,
                         COALESCE(SUM(bi.amount),0) as t_tongchi,
                         COALESCE(SUM(bi.bhyt_amount),0) as t_bhtt,
                         COALESCE(SUM(bi.patient_amount),0) as t_bntt
                  FROM diab_his_billing_items bi
                  JOIN diab_his_billings b ON b.id = bi.billing_id
                  WHERE b.encounter_id = @eid AND b.tenant_id = @t AND b.deleted_at IS NULL",
                new { eid = encId, t = tenantId }) ?? new { t_thuoc = 0m, t_vtyt = 0m, t_tongchi = 0m, t_bhtt = 0m, t_bntt = 0m, t_bncct = 0m };

            var diagnosis = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT icd10_code FROM diab_his_encounter_diagnoses WHERE encounter_id=@eid AND is_primary=1 AND deleted_at IS NULL LIMIT 1",
                new { eid = encId });

            var table1Row = new BhytTable1Row(
                MaLienKet: maLienKet,
                MaBn: (string)enc.patient_id,
                HoTen: (string)(enc.full_name ?? ""),
                NgaySinh: enc.date_of_birth != null ? ((DateTime)enc.date_of_birth).ToString("yyyy-MM-dd") : "",
                GioiTinh: (string?)enc.gender == "FEMALE" ? 2 : 1,
                MaTheBhyt: (string)(enc.ma_the_bhyt ?? ""),
                MaDkbd: (string)(enc.ma_dkbd ?? ""),
                GtTheTu: enc.gt_the_tu != null ? ((DateTime)enc.gt_the_tu).ToString("yyyy-MM-dd") : "",
                GtTheDen: enc.gt_the_den != null ? ((DateTime)enc.gt_the_den).ToString("yyyy-MM-dd") : "",
                MaLoaiKcb: 1,
                NgayVao: enc.started_at != null ? (DateTime)enc.started_at : DateTime.UtcNow,
                NgayRa: enc.finished_at != null ? (DateTime)enc.finished_at : DateTime.UtcNow,
                SoNgayDtri: 1,
                KetQuaDtri: 1,
                MaBenh: (string)(diagnosis?.icd10_code ?? "Z00"),
                MaBenhPhu: null,
                LyDoVvien: "Kham benh dinh ky",
                ChanDoanRv: "",
                TThuoc: (decimal)(billing?.t_thuoc ?? 0m),
                TVtyt: (decimal)(billing?.t_vtyt ?? 0m),
                TTongchi: (decimal)(billing?.t_tongchi ?? 0m),
                TBhtt: (decimal)(billing?.t_bhtt ?? 0m),
                TBntt: (decimal)(billing?.t_bntt ?? 0m),
                TBncct: 0m);

            var rowJson1 = JsonSerializer.Serialize(table1Row);
            items.Add(new BhytExportItemData(1, table1Idx++, rowJson1, maLienKet, encId, null,
                (decimal)(billing?.t_bhtt ?? 0m)));
            totalRequested += (decimal)(billing?.t_bhtt ?? 0m);

            // Bang 2: Thuoc BHYT
            var prescItems = await conn.QueryAsync<dynamic>(
                @"SELECT pi.drug_code as ma_thuoc, pi.drug_name as ten_thuoc,
                         pi.unit as don_vi_tinh, pi.concentration as ham_luong,
                         pi.route as duong_dung, pi.dosage as lieu_dung,
                         pi.registration_no as so_dang_ky, pi.supplier_code as ma_nha_thau,
                         pi.quantity as so_luong, pi.unit_price as don_gia,
                         pi.total_price as thanh_tien, pi.bhyt_amount as t_bhtt,
                         pi.lot_no as mahieu_lo, pi.expiry_date as han_dung,
                         pi.coverage_level as muc_huong, pr.prescribed_at as ngay_yl,
                         pr.room_code as ma_phong, pr.doctor_code as ma_bs
                  FROM diab_his_pharma_prescription_items pi
                  JOIN diab_his_pharma_prescriptions pr ON pr.id = pi.prescription_id
                  WHERE pr.encounter_id = @eid AND pr.tenant_id = @t
                    AND pi.is_bhyt = 1 AND pr.deleted_at IS NULL",
                new { eid = encId, t = tenantId });

            int tbl2Idx = 0;
            foreach (var drug in prescItems)
            {
                var table2Row = new BhytTable2Row(
                    MaLienKet: maLienKet,
                    MaThuoc: (string)(drug.ma_thuoc ?? ""),
                    TenThuoc: (string)(drug.ten_thuoc ?? ""),
                    DonViTinh: (string)(drug.don_vi_tinh ?? "vien"),
                    HamLuong: (string)(drug.ham_luong ?? ""),
                    DuongDung: (string)(drug.duong_dung ?? "uong"),
                    LieuDung: (string)(drug.lieu_dung ?? ""),
                    SoDangKy: (string)(drug.so_dang_ky ?? ""),
                    MaNhaThau: (string)(drug.ma_nha_thau ?? ""),
                    PhamViTt: 1,
                    SoLuong: (decimal)(drug.so_luong ?? 0m),
                    DonGia: (decimal)(drug.don_gia ?? 0m),
                    ThanhTien: (decimal)(drug.thanh_tien ?? 0m),
                    TBhtt: (decimal)(drug.t_bhtt ?? 0m),
                    TNguonkhac: 0m, TNguonkhacBhtt: 0m, TNguonkhacKhac: 0m,
                    MucHuong: (int)(drug.muc_huong ?? 80),
                    NgayYl: drug.ngay_yl != null ? (DateTime)drug.ngay_yl : DateTime.UtcNow,
                    MaPhong: (string)(drug.ma_phong ?? ""),
                    MaBs: (string)(drug.ma_bs ?? ""),
                    MaDichvuKem: null,
                    MahieuLo: (string?)drug.mahieu_lo,
                    HanDung: drug.han_dung != null ? ((DateTime)drug.han_dung).ToString("yyyy-MM-dd") : null,
                    SoHop: null);

                items.Add(new BhytExportItemData(2, tbl2Idx++,
                    JsonSerializer.Serialize(table2Row), maLienKet, encId, null,
                    (decimal)(drug.t_bhtt ?? 0m)));
            }

            // Bang 3: CLS
            var clsOrders = await conn.QueryAsync<dynamic>(
                @"SELECT lo.service_code as ma_dich_vu, lo.service_name as ten_dich_vu,
                         lo.unit as don_vi_tinh, lo.quantity as so_luong,
                         lo.unit_price as don_gia, lo.total_price as thanh_tien,
                         lo.bhyt_amount as t_bhtt, lo.coverage_level as muc_huong,
                         lo.ordered_at as ngay_yl, lo.room_code as ma_phong,
                         lo.doctor_code as ma_bs, lo.result_at as ngay_kq
                  FROM diab_his_clinic_lab_orders lo
                  WHERE lo.encounter_id = @eid AND lo.tenant_id = @t
                    AND lo.is_bhyt = 1 AND lo.deleted_at IS NULL",
                new { eid = encId, t = tenantId });

            int tbl3Idx = 0;
            foreach (var cls in clsOrders)
            {
                var table3Row = new BhytTable3Row(
                    MaLienKet: maLienKet,
                    MaDichVu: (string)(cls.ma_dich_vu ?? ""),
                    MaVatTu: null, TenVatTu: null,
                    DonViTinh: (string)(cls.don_vi_tinh ?? "lan"),
                    PhamVi: 1,
                    SoLuong: (decimal)(cls.so_luong ?? 1m),
                    DonGia: (decimal)(cls.don_gia ?? 0m),
                    TtThau: null,
                    ThanhTien: (decimal)(cls.thanh_tien ?? 0m),
                    TBhtt: (decimal)(cls.t_bhtt ?? 0m),
                    MucHuong: (int)(cls.muc_huong ?? 80),
                    NgayYl: cls.ngay_yl != null ? (DateTime)cls.ngay_yl : DateTime.UtcNow,
                    MaPhong: (string)(cls.ma_phong ?? ""),
                    MaBs: (string)(cls.ma_bs ?? ""),
                    MaBenh: (string)(diagnosis?.icd10_code ?? "Z00"),
                    NgayKq: (DateTime?)cls.ngay_kq);

                items.Add(new BhytExportItemData(3, tbl3Idx++,
                    JsonSerializer.Serialize(table3Row), maLienKet, encId, null,
                    (decimal)(cls.t_bhtt ?? 0m)));
            }

            // Bang 5: Tong hop chi phi
            var table5Row = new BhytTable5Row(
                MaLienKet: maLienKet,
                MaChiPhi: "CP01",
                TenChiPhi: "Tong chi phi kham benh",
                NhomChiPhi: 1,
                ThanhTien: (decimal)(billing?.t_tongchi ?? 0m),
                TBhtt: (decimal)(billing?.t_bhtt ?? 0m),
                TBntt: (decimal)(billing?.t_bntt ?? 0m),
                TNguonkhac: 0m);

            items.Add(new BhytExportItemData(5, 0, JsonSerializer.Serialize(table5Row), maLienKet, encId, null, 0m));
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
