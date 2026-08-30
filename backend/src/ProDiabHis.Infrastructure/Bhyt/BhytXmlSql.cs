namespace ProDiabHis.Infrastructure.Bhyt;

/// <summary>
/// Tap trung TOAN BO cau lenh SQL cua BhytXmlGeneratorImpl vao mot noi de:
/// 1. Review de dang: moi query deu phai co filter tenant_id.
/// 2. Unit test co the assert khong con ten bang "ma" (bang khong ton tai trong schema).
/// Ten bang o day PHAI khop voi builder.ToTable(...) trong Persistence/Configurations
/// va CREATE TABLE trong db/migrations.
/// </summary>
public static class BhytXmlSql
{
    /// <summary>Lay ma tenant de build ma_lien_ket. Bang: diab_his_sys_tenants.</summary>
    public const string TenantCode = @"
SELECT IFNULL(NULLIF(t.code, ''), CAST(t.id AS CHAR))
FROM diab_his_sys_tenants t
WHERE t.id = @t AND t.deleted_at IS NULL";

    /// <summary>
    /// Encounters co BHYT trong ky. Chi lay 1 the BHYT moi nhat / benh nhan
    /// (tranh nhan ban dong -> trung MA_LIEN_KET -> xuat toan).
    /// </summary>
    public const string Encounters = @"
SELECT e.id, e.patient_id, e.doctor_id, e.room_id, e.started_at, e.finished_at,
       p.code AS patient_code, p.full_name, p.date_of_birth, p.gender,
       i.card_no_enc AS ma_the_bhyt_enc, i.hospital_code AS ma_dkbd,
       i.valid_from AS gt_the_tu, i.valid_to AS gt_the_den,
       i.coverage_percent AS muc_huong
FROM diab_his_enc_encounters e
JOIN diab_his_pat_patients p
     ON p.id = e.patient_id AND p.tenant_id = e.tenant_id
JOIN diab_his_pat_insurances i
     ON i.id = (SELECT i2.id
                FROM diab_his_pat_insurances i2
                WHERE i2.patient_id = e.patient_id
                  AND i2.tenant_id = @t
                  AND i2.type = 'BHYT'
                  AND i2.deleted_at IS NULL
                  AND i2.card_no_enc IS NOT NULL
                ORDER BY i2.valid_to DESC, i2.created_at DESC
                LIMIT 1)
WHERE e.tenant_id = @t
  AND e.started_at >= @df AND e.started_at < @dt
  AND e.deleted_at IS NULL
ORDER BY e.started_at";

    /// <summary>
    /// Bang 1 - tong hop chi phi 1 dot kham, lay tu hoa don (diab_his_bil_billing[_items]).
    /// t_thuoc = cac dong item_type='DRUG'; t_tongchi = tong line_total; t_bntt = phan con lai.
    /// </summary>
    public const string BillingSummary = @"
SELECT COALESCE(SUM(CASE WHEN bi.item_type = 'DRUG' THEN bi.line_total ELSE 0 END), 0) AS t_thuoc,
       COALESCE(SUM(bi.line_total), 0)                                                 AS t_tongchi,
       COALESCE(SUM(bi.bhyt_amount), 0)                                                AS t_bhtt,
       COALESCE(SUM(bi.line_total - bi.bhyt_amount), 0)                                AS t_bntt
FROM diab_his_bil_billing_items bi
JOIN diab_his_bil_billing b
     ON b.id = bi.billing_id AND b.tenant_id = bi.tenant_id
WHERE b.encounter_id = @eid
  AND b.tenant_id = @t
  AND bi.tenant_id = @t
  AND b.deleted_at IS NULL";

    /// <summary>Chan doan: PRIMARY -> MA_BENH_CHINH, con lai -> MA_BENH_KT.</summary>
    public const string Diagnoses = @"
SELECT icd10_code, type
FROM diab_his_enc_diagnoses
WHERE tenant_id = @t AND encounter_id = @eid AND deleted_at IS NULL
ORDER BY (type = 'PRIMARY') DESC, created_at";

    /// <summary>
    /// Bang 2 - thuoc BHYT trong don ke.
    /// ma_nha_thau/mahieu_lo/han_dung: van SELECT o day de con dung noi bo cho quan ly kho (cot da
    /// them tu migration 9110 + du lieu cap phat thuc te disp.*), NHUNG KHONG con duoc map vao XML2
    /// vi 3 truong nay KHONG thuoc chuan XML2 BYT (Phu luc 01, CV 47/BHXH-CNTT 08/01/2026) - xem
    /// BhytXmlGeneratorImpl.GenerateAsync, khong con dung Col(d,"ma_nha_thau"/"mahieu_lo"/"han_dung").
    /// so_dang_ky: CO trong chuan XML2 -> van giu mapping; diab_his_pha_drugs CHUA co nguon nhap
    /// lieu nay (xem migration 9110) nen tam de trong.
    /// </summary>
    public const string PrescriptionItems = @"
SELECT d.code                              AS ma_thuoc,
       pi.drug_name                        AS ten_thuoc,
       pi.unit                             AS don_vi_tinh,
       pi.drug_strength                    AS ham_luong,
       -- DUONG_DUNG (XML 4210 Bang 2, bat buoc): uu tien route ke don, fallback route master (9180).
       -- Da bo fallback hardcode duong uong - rong => BhytXmlGeneratorImpl bao loi DRUG_ROUTE_MISSING.
       COALESCE(NULLIF(TRIM(pi.route), ''), NULLIF(TRIM(d.route), '')) AS duong_dung,
       pi.dosage                           AS lieu_dung,
       pi.quantity                         AS so_luong,
       pi.unit_price                       AS don_gia,
       pi.line_total                       AS thanh_tien,
       COALESCE(pr.signed_at, pr.created_at) AS ngay_yl,
       pr.doctor_id                        AS ma_bs,
       d.so_dang_ky                        AS so_dang_ky,
       d.ma_nha_thau                       AS ma_nha_thau,
       disp.batch_no                       AS mahieu_lo,
       disp.expiry_date                    AS han_dung
FROM diab_his_pha_prescription_items pi
JOIN diab_his_pha_prescriptions pr
     ON pr.id = pi.prescription_id AND pr.tenant_id = pi.tenant_id
LEFT JOIN diab_his_pha_drugs d
     ON d.id = pi.drug_id AND d.tenant_id = pi.tenant_id
LEFT JOIN diab_his_pha_dispense_items disp
     ON disp.prescription_item_id = pi.id AND disp.tenant_id = pi.tenant_id AND disp.deleted_at IS NULL
WHERE pr.encounter_id = @eid
  AND pr.tenant_id = @t
  AND pi.tenant_id = @t
  AND pi.bhyt_applicable = 1
  AND pr.deleted_at IS NULL";

    /// <summary>
    /// Bang 3 - dich vu ky thuat (CLS/thu thuat/dich vu). Nguon = dong hoa don KHONG phai thuoc.
    /// Ly do: bang chi dinh CLS (diab_his_cli_lab_orders / diab_his_cli_rad_orders) KHONG co
    /// cot gia / bhyt -> khong the lam nguon chi phi cho ho so giam dinh.
    /// </summary>
    public const string ServiceItems = @"
SELECT bi.code       AS ma_dich_vu,
       bi.name       AS ten_dich_vu,
       bi.quantity   AS so_luong,
       bi.unit_price AS don_gia,
       bi.line_total AS thanh_tien,
       bi.bhyt_amount AS t_bhtt,
       bi.created_at AS ngay_yl
FROM diab_his_bil_billing_items bi
JOIN diab_his_bil_billing b
     ON b.id = bi.billing_id AND b.tenant_id = bi.tenant_id
WHERE b.encounter_id = @eid
  AND b.tenant_id = @t
  AND bi.tenant_id = @t
  AND b.deleted_at IS NULL
  AND bi.item_type <> 'DRUG'
  AND bi.bhyt_applicable = 1";

    /// <summary>Danh sach dung cho unit test quet ten bang.</summary>
    public static readonly string[] All =
    [
        TenantCode, Encounters, BillingSummary, Diagnoses, PrescriptionItems, ServiceItems
    ];
}
