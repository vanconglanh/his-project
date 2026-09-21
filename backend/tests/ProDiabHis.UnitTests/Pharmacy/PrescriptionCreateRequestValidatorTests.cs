using FluentAssertions;
using ProDiabHis.Application.Pharmacy.Prescriptions;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

/// <summary>
/// BM-28 (Lark Bug-Feedback) - "[BUG] BS tạo đơn thuốc thất bại". Root cause: rule
/// "Items phải >= 1" (thêm bởi NEW-BUG-02 để chặn thiếu patient_id/encounter_id gây
/// FK violation 500) vô tình chặn luôn luồng tạo đơn thuốc THẬT của FE -
/// PrescriptionForm.tsx tạo đơn RỖNG trước (ensurePrescription, không kèm items) rồi
/// mới gọi riêng useAddPrescriptionItems khi bác sĩ chọn từng thuốc. Với rule cũ,
/// POST /prescriptions đầu tiên (lúc chưa có thuốc nào) luôn 500
/// "Danh sách thuốc là bắt buộc" - bác sĩ không tạo được đơn thuốc nào cả.
/// </summary>
public class PrescriptionCreateRequestValidatorTests
{
    private readonly PrescriptionCreateRequestValidator _validator = new();

    [Fact]
    public void TaoDonRong_ItemsNull_HopLe()
    {
        var request = new PrescriptionCreateRequest(
            EncounterId: Guid.NewGuid(), PatientId: Guid.NewGuid(),
            Note: null, Items: null);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue("PrescriptionForm.tsx tao don DRAFT rong truoc, chua co thuoc nao");
    }

    [Fact]
    public void TaoDonRong_ItemsMangRong_HopLe()
    {
        var request = new PrescriptionCreateRequest(
            EncounterId: Guid.NewGuid(), PatientId: Guid.NewGuid(),
            Note: null, Items: Array.Empty<PrescriptionItemRequest>());

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    // NEW-BUG-02 (loi goc ma rule cu dinh chan) van phai con nguyen tac dung: thieu
    // patient_id/encounter_id van phai bi chan o tang validation, khong duoc de roi
    // xuong INSERT gay 500 do vi pham FK constraint.
    [Fact]
    public void ThieuEncounterId_BiChan()
    {
        var request = new PrescriptionCreateRequest(
            EncounterId: Guid.Empty, PatientId: Guid.NewGuid(),
            Note: null, Items: null);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Lượt khám là bắt buộc");
    }

    [Fact]
    public void ThieuPatientId_BiChan()
    {
        var request = new PrescriptionCreateRequest(
            EncounterId: Guid.NewGuid(), PatientId: Guid.Empty,
            Note: null, Items: null);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Bệnh nhân là bắt buộc");
    }

    [Fact]
    public void ItemKemTheo_ThieuLieuDung_VanBiChanTheoDungDong()
    {
        var request = new PrescriptionCreateRequest(
            EncounterId: Guid.NewGuid(), PatientId: Guid.NewGuid(), Note: null,
            Items: new[]
            {
                new PrescriptionItemRequest(
                    DrugId: "D001", Dosage: "", Frequency: "2 lan/ngay",
                    Route: "Uong", DurationDays: 5, Quantity: 10, Instructions: null),
            });

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Liều dùng là bắt buộc");
    }
}
