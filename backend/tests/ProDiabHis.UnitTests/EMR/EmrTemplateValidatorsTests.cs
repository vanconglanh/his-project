using FluentAssertions;
using FluentValidation;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.EMR;
using Xunit;

namespace ProDiabHis.UnitTests.EMR;

/// <summary>
/// BUG-02 (Major, QC final review + tester UTC): EMR Template Create/Update truoc day
/// KHONG co validator BE. Test nay xac nhan:
///  1. EmrTemplateRequestValidator tu choi dung cac gia tri khong hop le (name rong/qua dai,
///     content_json null, speciality khong nam trong danh sach cho phep).
///  2. Khi chay qua ValidationBehavior (pipeline MediatR that su dung trong app), request
///     khong hop le lam ValidationBehavior throw FluentValidation.ValidationException — day
///     chinh la exception ma ErrorHandlingMiddleware bat va tra ve HTTP 400 voi envelope
///     chuan { error: { code: VALIDATION_ERROR, ... } }, THAY VI de lot xuong DB constraint
///     roi tra ve 500.
/// </summary>
public class EmrTemplateValidatorsTests
{
    private readonly EmrTemplateRequestValidator _requestValidator = new();

    [Fact]
    public void RequestValidator_KhiNameRong_TraLoi()
    {
        var request = new EmrTemplateRequest("", new { a = 1 }, "GENERAL");
        var result = _requestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EmrTemplateRequest.Name));
    }

    [Fact]
    public void RequestValidator_KhiNameVuotQua200KyTu_TraLoi()
    {
        var request = new EmrTemplateRequest(new string('A', 201), new { a = 1 }, "GENERAL");
        var result = _requestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EmrTemplateRequest.Name));
    }

    [Fact]
    public void RequestValidator_KhiContentJsonNull_TraLoi()
    {
        var request = new EmrTemplateRequest("Mẫu hợp lệ", null!, "GENERAL");
        var result = _requestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EmrTemplateRequest.ContentJson));
    }

    [Fact]
    public void RequestValidator_KhiSpecialityKhongHopLe_TraLoi()
    {
        var request = new EmrTemplateRequest("Mẫu hợp lệ", new { a = 1 }, "KHONG_TON_TAI");
        var result = _requestValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(EmrTemplateRequest.Speciality));
    }

    [Theory]
    [InlineData("GENERAL")]
    [InlineData("DIABETES")]
    [InlineData("CARDIOLOGY")]
    [InlineData("ENDOCRINOLOGY")]
    [InlineData("NEPHROLOGY")]
    [InlineData("OPHTHALMOLOGY")]
    [InlineData("OTHER")]
    public void RequestValidator_KhiSpecialityHopLe_ThanhCong(string speciality)
    {
        var request = new EmrTemplateRequest("Mẫu hợp lệ", new { a = 1 }, speciality);
        var result = _requestValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    // ─── Test qua pipeline that (ValidationBehavior) — dam bao request loi throw
    // ValidationException (-> 400), KHONG con lot xuong handler / DB (-> 500) ───
    [Fact]
    public async Task CreatePipeline_KhiNameQuaDai_ThrowValidationException_KhongGoiHandler()
    {
        var behavior = new ValidationBehavior<CreateEmrTemplateCommand, Result<EmrTemplateResponse>>(
            new[] { new CreateEmrTemplateCommandValidator() });

        var command = new CreateEmrTemplateCommand(
            new EmrTemplateRequest(new string('X', 500), new { a = 1 }, "GENERAL"));

        var handlerCalled = false;

        Func<Task> act = () => behavior.Handle(command, () =>
        {
            handlerCalled = true;
            return Task.FromResult(Result<EmrTemplateResponse>.Success(null!));
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePipeline_KhiRequestHopLe_KhongThrowVaGoiHandler()
    {
        var behavior = new ValidationBehavior<UpdateEmrTemplateCommand, Result<bool>>(
            new[] { new UpdateEmrTemplateCommandValidator() });

        var command = new UpdateEmrTemplateCommand(Guid.NewGuid(),
            new EmrTemplateRequest("Mẫu hợp lệ", new { a = 1 }, "DIABETES"));

        var handlerCalled = false;

        var result = await behavior.Handle(command, () =>
        {
            handlerCalled = true;
            return Task.FromResult(Result<bool>.Success(true));
        }, CancellationToken.None);

        handlerCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }
}
