using System.Net;
using System.Net.Http.Json;
using Dapper;
using FluentAssertions;
using MySqlConnector;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Portal;

/// <summary>
/// ITC-PORTAL-01 — Bao mat + cach ly du lieu cho cong benh nhan (PatientPortalController,
/// /api/portal/v1). Request di qua DUNG pipeline: JwtBearer scheme "PortalBearer" -> Controller.
///
/// Trong tam:
///  - Endpoint /me* tu choi khi thieu token, token het han, va token SAI audience
///    (token noi bo aud="ProDiabHis" KHONG duoc phep vao cong benh nhan).
///  - Benh nhan A chi thay du lieu cua CHINH minh; token cua A tra ho so A, token B tra ho so B
///    (cach ly theo claim patient_id — khong tin patient_id tu client body/query).
///  - Endpoint dat lich (POST /me/appointments) cung duoc bao ve boi PortalBearer.
/// </summary>
[Collection("Api")]
public class PatientPortalApiIntegrationTests
{
    private readonly ApiTestFixture _fx;

    public PatientPortalApiIntegrationTests(ApiTestFixture fx) => _fx = fx;

    private const int TenantId = 1;

    // Seed 1 benh nhan toi thieu (chi cac cot NOT NULL + cot /me doc). Cac cot ma hoa
    // (phone_enc/street_enc) de null -> PiiCrypto.Unprotect(null)=null, khong loi.
    private async Task SeedPatientAsync(Guid id, string code, string fullName)
    {
        await using var conn = new MySqlConnection(_fx.ConnectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(
            @"INSERT INTO diab_his_pat_patients (id, tenant_id, code, full_name, gender, status, created_at, updated_at)
              VALUES (@Id, @TenantId, @Code, @FullName, 'FEMALE', 'ACTIVE', UTC_TIMESTAMP(), UTC_TIMESTAMP())",
            new { Id = id.ToString(), TenantId, Code = code, FullName = fullName });
    }

    private HttpClient PortalClient(Guid patientId, string code)
        => _fx.ClientWithToken(PortalTestTokens.ForPatient(patientId, TenantId, code));

    // ---------------- Guard: thieu token / het han / sai audience ----------------

    // ITC-PORTAL-01: An danh goi GET /me phai 401.
    [ApiFact]
    public async Task AnDanh_XemHoSo_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/portal/v1/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PORTAL-01: Token portal het han -> 401 (ValidateLifetime=true, ClockSkew=0).
    [ApiFact]
    public async Task TokenHetHan_XemHoSo_Tra401()
    {
        var client = _fx.ClientWithToken(PortalTestTokens.Expired(Guid.NewGuid(), TenantId));
        var res = await client.GetAsync("/api/portal/v1/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PORTAL-01 (RANH GIOI QUAN TRONG): token NOI BO thuong (aud="ProDiabHis") KHONG duoc
    // vao cong benh nhan -> 401 vi sai audience. Chan lan quyen giua 2 he thong.
    [ApiFact]
    public async Task TokenNoiBo_SaiAudience_KhongVaoDuocCongBenhNhan_Tra401()
    {
        // Token noi bo super admin: ky dung secret NHUNG aud="ProDiabHis" != "patient-portal".
        var internalToken = TestTokens.ForSuperAdmin(TenantId);
        var client = _fx.ClientWithToken(internalToken);

        var res = await client.GetAsync("/api/portal/v1/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PORTAL-01: token ky dung secret nhung sai audience (mo phong tu tao) -> 401.
    [ApiFact]
    public async Task TokenSaiAudience_TuTao_Tra401()
    {
        var client = _fx.ClientWithToken(PortalTestTokens.WithWrongAudience(Guid.NewGuid(), TenantId));
        var res = await client.GetAsync("/api/portal/v1/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PORTAL-01: dat lich qua cong (POST /me/appointments) an danh -> 401.
    [ApiFact]
    public async Task AnDanh_DatLich_Tra401()
    {
        var res = await _fx.AnonymousClient().PostAsJsonAsync("/api/portal/v1/me/appointments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-PORTAL-01: dat lich qua cong bang token noi bo sai audience -> 401.
    [ApiFact]
    public async Task TokenNoiBo_DatLich_Tra401()
    {
        var client = _fx.ClientWithToken(TestTokens.ForSuperAdmin(TenantId));
        var res = await client.PostAsJsonAsync("/api/portal/v1/me/appointments", new { });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------------- Happy path + cach ly du lieu ----------------

    // ITC-PORTAL-01: token portal hop le cua benh nhan A -> 200, tra ve DUNG ho so A.
    [ApiFact]
    public async Task TokenHopLe_XemHoSoCuaChinhMinh_TraDungBenhNhan()
    {
        var idA = Guid.NewGuid();
        await SeedPatientAsync(idA, "BN-PORTAL-A", "Nguyen Thi A");

        var res = await PortalClient(idA, "BN-PORTAL-A").GetAsync("/api/portal/v1/me");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("BN-PORTAL-A");
        body.Should().Contain("Nguyen Thi A");
    }

    // ITC-PORTAL-01 (CACH LY): token cua A tra ho so A, token cua B tra ho so B — moi token
    // chi thay du lieu gan voi claim patient_id cua chinh no, KHONG thay cua benh nhan khac.
    [ApiFact]
    public async Task CachLy_TokenAKhongThayDuLieuCuaB()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        await SeedPatientAsync(idA, "BN-ISO-A", "Benh nhan A");
        await SeedPatientAsync(idB, "BN-ISO-B", "Benh nhan B");

        var bodyA = await (await PortalClient(idA, "BN-ISO-A").GetAsync("/api/portal/v1/me")).Content.ReadAsStringAsync();
        var bodyB = await (await PortalClient(idB, "BN-ISO-B").GetAsync("/api/portal/v1/me")).Content.ReadAsStringAsync();

        // A chi thay chinh A, tuyet doi khong lo thong tin cua B (va nguoc lai).
        bodyA.Should().Contain("BN-ISO-A");
        bodyA.Should().NotContain("BN-ISO-B");
        bodyA.Should().NotContain("Benh nhan B");

        bodyB.Should().Contain("BN-ISO-B");
        bodyB.Should().NotContain("BN-ISO-A");
        bodyB.Should().NotContain("Benh nhan A");
    }

    // ITC-PORTAL-01: token portal hop le -> qua duoc lop xac thuc PortalBearer o endpoint danh sach
    // lan kham cua chinh minh.
    //
    // GIOI HAN MOI TRUONG TEST (khong phai bug san pham): endpoint /me/encounters doc bang/cot
    // (vd diab_his_enc_diagnoses, cot lien quan) chi duoc tao day du boi db/migrations/*.sql, ma
    // schema test dung EF EnsureCreated() + TestSchemaSupplement nen con thieu -> co the 500.
    // Vi vay KHONG assert '200' o day; chi assert phan CHAC CHAN dung: token portal DA qua xac thuc
    // (khong 401). Nang len assert 200 khi chuoi migration dung duoc DB sach tu so 0
    // (xem db/migrations/APPLY_ORDER.md). Cach lam nay theo dung tien le ApiPartners "DungQuyen".
    [ApiFact]
    public async Task TokenHopLe_XemDanhSachLanKham_QuaXacThuc()
    {
        var idA = Guid.NewGuid();
        await SeedPatientAsync(idA, "BN-ENC-A", "Benh nhan Enc");

        var res = await PortalClient(idA, "BN-ENC-A").GetAsync("/api/portal/v1/me/encounters");

        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
