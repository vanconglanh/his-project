using System.Net;
using FluentAssertions;
using ProDiabHis.IntegrationTests.Infrastructure;
using Xunit;

namespace ProDiabHis.IntegrationTests.Ops;

/// <summary>ITC-FHIR-01 — Kiem tra bao mat va phan quyen endpoint FHIR R4.</summary>
[Collection("Api")]
public class FhirOpsIntegrationTests
{
    private const string Rid = "33333333-3333-3333-3333-333333333333";
    private readonly ApiTestFixture _fx;

    public FhirOpsIntegrationTests(ApiTestFixture fx) => _fx = fx;

    // ITC-FHIR-01: chua dang nhap doc Patient theo id phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DocPatientTheoId_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/fhir/r4/Patient/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FHIR-01: chua dang nhap tim Patient phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TimPatient_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/fhir/r4/Patient");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FHIR-01: chua dang nhap doc Encounter theo id phai 401
    [ApiFact]
    public async Task ChuaDangNhap_DocEncounterTheoId_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync($"/api/fhir/r4/Encounter/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FHIR-01: chua dang nhap tim Encounter phai 401
    [ApiFact]
    public async Task ChuaDangNhap_TimEncounter_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/fhir/r4/Encounter");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FHIR-01: chua dang nhap lay Bundle phai 401
    [ApiFact]
    public async Task ChuaDangNhap_LayBundle_Tra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/fhir/r4/Bundle");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ITC-FHIR-01: metadata la AllowAnonymous nen khong duoc tra 401
    [ApiFact]
    public async Task ChuaDangNhap_Metadata_KhongTra401()
    {
        var res = await _fx.AnonymousClient().GetAsync("/api/fhir/r4/metadata");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ITC-FHIR-01: thieu quyen tim Patient phai 403
    [ApiFact]
    public async Task ThieuQuyen_TimPatient_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/fhir/r4/Patient");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FHIR-01: thieu quyen tim Encounter phai 403
    [ApiFact]
    public async Task ThieuQuyen_TimEncounter_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/fhir/r4/Encounter");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FHIR-01: thieu quyen doc Patient theo id phai 403
    [ApiFact]
    public async Task ThieuQuyen_DocPatientTheoId_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/fhir/r4/Patient/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FHIR-01: thieu quyen doc Encounter theo id phai 403
    [ApiFact]
    public async Task ThieuQuyen_DocEncounterTheoId_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync($"/api/fhir/r4/Encounter/{Rid}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FHIR-01: thieu quyen lay Bundle phai 403
    [ApiFact]
    public async Task ThieuQuyen_LayBundle_Tra403()
    {
        var res = await _fx.ClientNoPermission().GetAsync("/api/fhir/r4/Bundle");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.Content.ReadAsStringAsync()).Should().Contain("PERMISSION_DENIED");
    }

    // ITC-FHIR-01: dung quyen tim Patient khong loi he thong
    [ApiFact]
    public async Task DungQuyen_TimPatient_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("fhir.read").GetAsync("/api/fhir/r4/Patient");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ITC-FHIR-01: dung quyen tim Encounter khong loi he thong
    [ApiFact]
    public async Task DungQuyen_TimEncounter_KhongLoiHeThong()
    {
        var res = await _fx.ClientWith("fhir.read").GetAsync("/api/fhir/r4/Encounter");
        ((int)res.StatusCode).Should().BeLessThan(500);
        res.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        res.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
