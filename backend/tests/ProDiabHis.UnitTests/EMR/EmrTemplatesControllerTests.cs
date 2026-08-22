using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using ProDiabHis.Api.Controllers;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.EMR;
using Xunit;

namespace ProDiabHis.UnitTests.EMR;

/// <summary>
/// Unit test tầng Controller cho EMR Template (không đi qua Handler thật) — verify
/// EmrTemplatesController map đúng HTTP status code + error code trong body dựa trên
/// ErrorCode mà handler trả về, dùng hằng số StatusCodes thay vì hard-code số.
/// </summary>
public class EmrTemplatesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly EmrTemplatesController _controller;

    public EmrTemplatesControllerTests()
    {
        _controller = new EmrTemplatesController(_mediator);
    }

    // ─── Update mẫu hệ thống: handler trả TEMPLATE_SYSTEM -> controller phải trả 422 kèm error code ───
    [Fact]
    public async Task Update_HandlerReturnsTemplateSystem_Returns422WithErrorCode()
    {
        var templateId = Guid.NewGuid();
        var request = new EmrTemplateRequest(
            Name: "Nội dung bẩn từ tenant khác",
            ContentJson: new { hacked = true },
            Speciality: "GENERAL");

        _mediator.Send(Arg.Any<UpdateEmrTemplateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Failure("TEMPLATE_SYSTEM", "Không thể sửa mẫu bệnh án hệ thống"));

        var response = await _controller.Update(templateId, request, CancellationToken.None);

        var objectResult = response.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);

        var errorProp = objectResult.Value!.GetType().GetProperty("error")!.GetValue(objectResult.Value);
        var codeProp = errorProp!.GetType().GetProperty("code")!.GetValue(errorProp);
        codeProp.Should().Be("TEMPLATE_SYSTEM");
    }

    // ─── Edge case: Update mẫu không tồn tại -> handler trả TEMPLATE_NOT_FOUND, controller vẫn trả 404 (không regression) ───
    [Fact]
    public async Task Update_HandlerReturnsTemplateNotFound_Returns404WithErrorCode()
    {
        var templateId = Guid.NewGuid();
        var request = new EmrTemplateRequest("Không tồn tại", new { }, "GENERAL");

        _mediator.Send(Arg.Any<UpdateEmrTemplateCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Failure("TEMPLATE_NOT_FOUND", "Không tìm thấy mẫu bệnh án"));

        var response = await _controller.Update(templateId, request, CancellationToken.None);

        var objectResult = response.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var errorProp = objectResult.Value!.GetType().GetProperty("error")!.GetValue(objectResult.Value);
        var codeProp = errorProp!.GetType().GetProperty("code")!.GetValue(errorProp);
        codeProp.Should().Be("TEMPLATE_NOT_FOUND");
    }
}
