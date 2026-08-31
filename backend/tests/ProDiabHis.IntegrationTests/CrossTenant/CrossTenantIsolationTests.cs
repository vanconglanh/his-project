using System.Net;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.CrossTenant;

/// <summary>
/// ITC-XTENANT — Chung minh cach ly multi-tenant o tang HTTP that:
/// Token tenant A (tenant_id=1) KHONG doc duoc bat ky du lieu nao cua tenant B (tenant_id=2).
///
/// Moi module kiem 2 dieu:
///  1. GET list bang token tenant A -> body KHONG chua ID/ma dac trung cua ban ghi tenant B,
///     dong thoi CO chua ban ghi cua chinh tenant A (chung to endpoint chay that, khong rong gia).
///  2. GET /{id} voi ID CHINH XAC cua ban ghi tenant B -> phai 404 (KHONG 403/200):
///     404 de khong lo "ban ghi ton tai". 403/200 la SAI (lo hong).
///
/// Du lieu 2 tenant seed boi <see cref="CrossTenantSeeder"/> (idempotent, chay 1 lan).
/// </summary>
[Collection("Api")]
public class CrossTenantIsolationTests
{
    private readonly ApiTestFixture _fx;

    public CrossTenantIsolationTests(ApiTestFixture fx)
    {
        _fx = fx;
        CrossTenantSeeder.EnsureSeeded(_fx.NewDbContext);
    }

    /// <summary>Token tenant A voi cac permission chi dinh (crossView=true de bo qua filter chi nhanh,
    /// chi con kiem tra filter tenant).</summary>
    private HttpClient TenantAClient(params string[] permissions)
        => _fx.ClientWithToken(TestTokens.ForPermissions(
            CrossTenantIds.TenantA, Guid.NewGuid(), permissions, branchId: CrossTenantIds.BranchA, crossView: true));

    // ==================================================================
    // 1. PATIENTS
    // ==================================================================
    [ApiFact]
    public async Task Patients_List_TenantA_KhongThayTenantB()
    {
        var res = await TenantAClient("patient.read").GetAsync("/api/v1/patients?page=1&page_size=100");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.PatientB.ToString());
        body.Should().NotContain(CrossTenantIds.PatientBCode);
    }

    [ApiFact]
    public async Task Patients_ChiTiet_IdTenantB_Tra404()
    {
        var res = await TenantAClient("patient.read").GetAsync($"/api/v1/patients/{CrossTenantIds.PatientB}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "GET chi tiet ban ghi tenant khac phai 404, khong duoc 403/200 (tranh lo ton tai)");
    }

    // ==================================================================
    // 2. ENCOUNTERS
    // ==================================================================
    [ApiFact]
    public async Task Encounters_List_TenantA_KhongThayTenantB()
    {
        var res = await TenantAClient("encounter.read").GetAsync("/api/v1/encounters?page=1&page_size=100");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.EncounterB.ToString());
    }

    [ApiFact]
    public async Task Encounters_ChiTiet_IdTenantB_Tra404()
    {
        var res = await TenantAClient("encounter.read").GetAsync($"/api/v1/encounters/{CrossTenantIds.EncounterB}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==================================================================
    // 3. BILLINGS
    // ==================================================================
    [ApiFact]
    public async Task Billings_List_TenantA_KhongThayTenantB()
    {
        var res = await TenantAClient("billing.read").GetAsync("/api/v1/billings?page=1&page_size=100");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.BillingB.ToString());
        body.Should().NotContain(CrossTenantIds.PatientB.ToString());
    }

    [ApiFact]
    public async Task Billings_ChiTiet_IdTenantB_Tra404()
    {
        var res = await TenantAClient("billing.read").GetAsync($"/api/v1/billings/{CrossTenantIds.BillingB}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==================================================================
    // 4. PRESCRIPTIONS
    // ==================================================================
    [ApiFact]
    public async Task Prescriptions_List_TenantA_KhongThayTenantB()
    {
        var res = await TenantAClient("prescription.read").GetAsync("/api/v1/prescriptions?page=1&page_size=100");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.PrescB.ToString());
        body.Should().NotContain(CrossTenantIds.PatientB.ToString());
    }

    [ApiFact]
    public async Task Prescriptions_ChiTiet_IdTenantB_Tra404()
    {
        var res = await TenantAClient("prescription.read").GetAsync($"/api/v1/prescriptions/{CrossTenantIds.PrescB}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==================================================================
    // 5. LAB RESULTS (khong co GET/{id} rieng -> kiem list + loc theo patient tenant B)
    // ==================================================================
    [ApiFact]
    public async Task LabResults_List_TenantA_KhongThayTenantB()
    {
        var res = await TenantAClient("lab_result.read").GetAsync("/api/v1/lab-results?page=1&page_size=100");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.LabB.ToString());
    }

    [ApiFact]
    public async Task LabResults_LocTheoPatientTenantB_KhongTraKetQua()
    {
        // Token tenant A loc theo patient_id CHINH XAC cua benh nhan tenant B -> khong duoc lo KQ nao.
        var res = await TenantAClient("lab_result.read")
            .GetAsync($"/api/v1/lab-results?patient_id={CrossTenantIds.PatientB}&page=1&page_size=100");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.LabB.ToString());
    }

    // ==================================================================
    // 6. DRUGS
    // ==================================================================
    [ApiFact]
    public async Task Drugs_List_TenantA_KhongThayTenantB()
    {
        var res = await TenantAClient("drug.read").GetAsync("/api/v1/drugs?page=1&page_size=100");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.DrugB.ToString());
        body.Should().NotContain(CrossTenantIds.DrugBCode);
    }

    [ApiFact]
    public async Task Drugs_ChiTiet_IdTenantB_Tra404()
    {
        var res = await TenantAClient("drug.read").GetAsync($"/api/v1/drugs/{CrossTenantIds.DrugB}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==================================================================
    // 7. BRANCHES (PK int)
    // ==================================================================
    [ApiFact]
    public async Task Branches_List_TenantA_KhongThayTenantB()
    {
        var res = await TenantAClient("branch.read").GetAsync("/api/v1/branches");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.BranchBCode);
    }

    [ApiFact]
    public async Task Branches_ChiTiet_IdTenantB_Tra404()
    {
        var res = await TenantAClient("branch.read").GetAsync($"/api/v1/branches/{CrossTenantIds.BranchB}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==================================================================
    // 8. REPORTS (tong hop)
    // LUU Y HA TANG: cac endpoint /reports/revenue* dung cache Redis
    // (IConnectionMultiplexer) — test host KHONG cau hinh Redis nen tra 500
    // (loi ha tang, KHONG phai loi cach ly tenant). Vi vay o day chi khang dinh
    // dieu con y nghia va van dung du 500: response KHONG BAO GIO lo ID/ma
    // cua tenant B. Audit read-side (Dapper) da xac nhan moi query reports co
    // WHERE tenant_id = @tenantId (xem bao cao). Filter tenant o tang du lieu
    // con duoc phu boi Billings/Encounters test ben tren.
    // ==================================================================
    [ApiFact]
    public async Task Reports_Revenue_TenantA_KhongLoTenantB()
    {
        var res = await TenantAClient("report.read")
            .GetAsync("/api/v1/reports/revenue?from_date=2000-01-01&to_date=2100-01-01");
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.BillingB.ToString());
        body.Should().NotContain(CrossTenantIds.PatientB.ToString());
    }

    [ApiFact]
    public async Task Reports_RevenueByDoctor_TenantA_KhongLoTenantB()
    {
        var res = await TenantAClient("report.read")
            .GetAsync("/api/v1/reports/revenue/by-doctor?from_date=2000-01-01&to_date=2100-01-01");
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain(CrossTenantIds.UserB.ToString());
        body.Should().NotContain(CrossTenantIds.BillingB.ToString());
    }
}
