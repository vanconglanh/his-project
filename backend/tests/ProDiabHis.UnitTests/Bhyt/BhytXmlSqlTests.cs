using System.Text.RegularExpressions;
using FluentAssertions;
using ProDiabHis.Infrastructure.Bhyt;
using Xunit;

namespace ProDiabHis.UnitTests.Bhyt;

/// <summary>
/// Test chong tai phat (regression guard) cho su co: BhytXmlGeneratorImpl query cac bang KHONG TON TAI
/// -> GenerateAsync nem "unknown table" -> toan bo luong xuat XML 4210 khong bao gio chay duoc.
///
/// Cach lam: toan bo SQL cua generator duoc tach ra hang so trong <see cref="BhytXmlSql"/>,
/// nho do test doc duoc chinh cau SQL that (khong can DB) va kiem tra:
///   1. Khong con ten bang sai da biet.
///   2. Moi ten bang xuat hien trong FROM/JOIN deu nam trong danh sach bang THAT cua schema.
///   3. Moi cau lenh doc du lieu nghiep vu deu co filter tenant_id.
/// </summary>
public class BhytXmlSqlTests
{
    /// <summary>Cac ten bang tung bi dung sai — tuyet doi khong duoc quay lai.</summary>
    public static readonly string[] ForbiddenTables =
    [
        "diab_his_clinic_encounters",
        "diab_his_billings",
        "diab_his_billing_items",
        "diab_his_pharma_prescriptions",
        "diab_his_pharma_prescription_items",
        "diab_his_encounter_diagnoses",
        "diab_pat_patients",
        "diab_his_patient_insurances",
        "diab_his_clinic_lab_orders",
        "diab_his_tenants"
    ];

    /// <summary>Bang THAT — doi chieu voi builder.ToTable(...) va db/migrations.</summary>
    private static readonly HashSet<string> KnownTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "diab_his_sys_tenants",
        "diab_his_enc_encounters",
        "diab_his_enc_diagnoses",
        "diab_his_pat_patients",
        "diab_his_pat_insurances",
        "diab_his_bil_billing",
        "diab_his_bil_billing_items",
        "diab_his_pha_prescriptions",
        "diab_his_pha_prescription_items",
        "diab_his_pha_drugs",
        "diab_his_pha_dispense_items"
    };

    private static string AllSql => string.Join("\n", BhytXmlSql.All);

    [Fact]
    public void Sql_khong_chua_ten_bang_sai_da_biet()
    {
        var sql = AllSql;
        foreach (var bad in ForbiddenTables)
        {
            // dung word-boundary de "diab_his_billing_items" khong an nham vao "diab_his_bil_billing_items"
            Regex.IsMatch(sql, $@"\b{Regex.Escape(bad)}\b")
                .Should().BeFalse($"'{bad}' KHONG ton tai trong schema - query se nem 'unknown table'");
        }
    }

    [Fact]
    public void Moi_bang_trong_FROM_va_JOIN_deu_ton_tai_trong_schema()
    {
        var referenced = Regex.Matches(AllSql, @"\b(?:FROM|JOIN)\s+([a-zA-Z0-9_]+)", RegexOptions.IgnoreCase)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        referenced.Should().NotBeEmpty();
        referenced.Should().OnlyContain(t => KnownTables.Contains(t),
            "moi bang duoc query phai co CREATE TABLE / ToTable tuong ung");
    }

    [Fact]
    public void Moi_cau_sql_nghiep_vu_deu_co_filter_tenant_id()
    {
        foreach (var sql in BhytXmlSql.All)
        {
            if (ReferenceEquals(sql, BhytXmlSql.TenantCode)) continue; // bang tenant loc bang t.id
            sql.Should().Contain("tenant_id", "multi-tenant bat buoc filter tenant_id");
        }
    }

    [Fact]
    public void Query_encounters_chi_lay_mot_the_bhyt_moi_nhat_moi_benh_nhan()
    {
        // Nhan ban dong do nhieu the BHYT -> trung MA_LIEN_KET -> rui ro xuat toan.
        BhytXmlSql.Encounters.Should().Contain("LIMIT 1");
    }

    [Fact]
    public void Query_bang_1_va_bang_3_dung_cot_that_cua_billing_items()
    {
        // billing_items khong co cot amount / patient_amount — chi co line_total / bhyt_amount.
        var sql = BhytXmlSql.BillingSummary + BhytXmlSql.ServiceItems;
        sql.Should().Contain("line_total");
        sql.Should().Contain("bhyt_amount");
        Regex.IsMatch(sql, @"\bbi\.amount\b").Should().BeFalse();
        Regex.IsMatch(sql, @"\bpatient_amount\b").Should().BeFalse();
    }
}
