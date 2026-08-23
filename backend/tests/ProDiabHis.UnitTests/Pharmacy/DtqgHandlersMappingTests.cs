using System.Dynamic;
using FluentAssertions;
using ProDiabHis.Application.Pharmacy.Dtqg;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

/// <summary>
/// Unit test cho SubmitDtqgHandler.MapSubmission — logic map dong Dapper dynamic sang
/// DtqgSubmissionResponse sau khi fix migration 9078 (diab_his_int_dtqg_submissions.id /
/// prescription_id doi tu INT sang CHAR(36)).
///
/// Truoc fix: DtqgSubmissionResponse.PrescriptionId la `int` va code lam `(int)row.prescription_id`
/// / `(int)pres.ID` — vi pha_prescriptions.ID la CHAR(36) (GUID) nen luon nem
/// RuntimeBinderException/InvalidCastException ngay khi co du lieu, khien SubmitDtqgHandler,
/// GetDtqgStatusHandler, RetryDtqgHandler hong hoan toan (khong lien quan gi den migration).
/// Sau fix: PrescriptionId la Guid, parse an toan qua Guid.TryParse (fallback Guid.Empty).
/// </summary>
public class DtqgHandlersMappingTests
{
    // Tra ve `object` (khong phai `dynamic`) de cac lenh goi SubmitDtqgHandler.MapSubmission(row)
    // trong test duoc BIEN DICH TINH (static binding) — tra ve dung DtqgSubmissionResponse thay vi
    // `dynamic`, cho phep FluentAssertions .Should() hoat dong binh thuong. ExpandoObject van la
    // dynamic object luc runtime (IDynamicMetaObjectProvider) nen MapSubmission (nhan tham so
    // `dynamic row`) van truy cap duoc row.id/row.prescription_id/... nhu Dapper dynamic row that.
    private static object MakeRow(
        string id,
        string prescriptionId,
        string? maDonThuoc = "VN260821000001",
        string status = "ACCEPTED",
        int retryCount = 0)
    {
        dynamic row = new ExpandoObject();
        row.id = id;
        row.tenant_id = 1;
        row.prescription_id = prescriptionId;
        row.ma_don_thuoc = maDonThuoc;
        row.qr_payload = (string?)null;
        row.status = status;
        row.error_code = (string?)null;
        row.error_message = (string?)null;
        row.submitted_at = (DateTime?)DateTime.UtcNow;
        row.accepted_at = (DateTime?)DateTime.UtcNow;
        row.retry_count = retryCount;
        row.last_retry_at = (DateTime?)null;
        return row;
    }

    [Fact]
    public void MapSubmission_ValidGuidStrings_ParsesIdAndPrescriptionId()
    {
        // Happy path: id / prescription_id la CHAR(36) GUID that (dung schema sau migration 9078)
        var id = Guid.NewGuid();
        var prescriptionId = Guid.NewGuid();
        var row = MakeRow(id.ToString(), prescriptionId.ToString());

        var result = SubmitDtqgHandler.MapSubmission(row);

        result.Id.Should().Be(id);
        result.PrescriptionId.Should().Be(prescriptionId);
        result.Status.Should().Be("ACCEPTED");
        result.MaDonThuoc.Should().Be("VN260821000001");
        result.RetryCount.Should().Be(0);
    }

    [Fact]
    public void MapSubmission_LegacyNonGuidNumericString_FallsBackToGuidEmpty_KhongNemException()
    {
        // Edge case: du lieu cu (truoc migration 9078, khi cot con la INT) sau khi MODIFY COLUMN
        // INT -> CHAR(36) se duoc MySQL tu chuyen thanh chuoi so (vd id=1 -> "1"), khong phai dinh
        // dang GUID chuan. MapSubmission phai KHONG nem exception, chi fallback ve Guid.Empty.
        var row = MakeRow("1", "2");

        var act = () => SubmitDtqgHandler.MapSubmission(row);

        act.Should().NotThrow();
        var result = act();
        result.Id.Should().Be(Guid.Empty);
        result.PrescriptionId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void MapSubmission_NullMaDonThuocAndOptionalFields_HandledGracefully()
    {
        var row = MakeRow(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), maDonThuoc: null, status: "PENDING");

        var result = SubmitDtqgHandler.MapSubmission(row);

        result.MaDonThuoc.Should().BeNull();
        result.Status.Should().Be("PENDING");
    }
}
