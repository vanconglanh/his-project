using FluentAssertions;
using ProDiabHis.Application.Files;
using Xunit;

namespace ProDiabHis.UnitTests.Files;

/// <summary>
/// Test validator cho FR-311 (Đính kèm hình ảnh lâm sàng + annotation).
/// Annotation la layer JSON rieng (list shape rectangle/circle/arrow/text),
/// khong sua anh goc.
/// </summary>
public class FileAnnotationCommandValidatorTests
{
    private static readonly string ValidShapesJson =
        "[{\"type\":\"rectangle\",\"x\":10,\"y\":20,\"width\":100,\"height\":50,\"color\":\"#FF0000\",\"note\":\"Vet thuong\"}," +
        "{\"type\":\"arrow\",\"x1\":5,\"y1\":5,\"x2\":50,\"y2\":50,\"color\":\"#00FF00\"}]";

    [Fact]
    public void CreateFileAnnotationCommand_WithValidShapesJson_PassesValidation()
    {
        // Happy path: annotation data la mang JSON hop le cac shape
        var command = new CreateFileAnnotationCommand(
            FileId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            EncounterId: Guid.NewGuid(),
            AnnotationData: ValidShapesJson);

        var validator = new CreateFileAnnotationCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateFileAnnotationCommand_WithInvalidJson_FailsValidation()
    {
        // Edge case: du lieu annotation khong phai JSON hop le (client gui sai dinh dang)
        var command = new CreateFileAnnotationCommand(
            FileId: Guid.NewGuid(),
            PatientId: null,
            EncounterId: null,
            AnnotationData: "{not-a-valid-json-array");

        var validator = new CreateFileAnnotationCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFileAnnotationCommand.AnnotationData));
    }

    [Fact]
    public void CreateFileAnnotationCommand_WithEmptyFileId_FailsValidation()
    {
        // Edge case: khong chi dinh file anh can danh dau
        var command = new CreateFileAnnotationCommand(
            FileId: Guid.Empty,
            PatientId: null,
            EncounterId: null,
            AnnotationData: ValidShapesJson);

        var validator = new CreateFileAnnotationCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateFileAnnotationCommand.FileId));
    }

    [Fact]
    public void UpdateFileAnnotationCommand_WithValidData_PassesValidation()
    {
        var command = new UpdateFileAnnotationCommand(
            FileId: Guid.NewGuid(),
            Id: Guid.NewGuid(),
            AnnotationData: ValidShapesJson);

        var validator = new UpdateFileAnnotationCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateFileAnnotationCommand_WithObjectInsteadOfArray_FailsValidation()
    {
        // Edge case: JSON hop le nhung khong phai mang shape (root la object)
        var command = new UpdateFileAnnotationCommand(
            FileId: Guid.NewGuid(),
            Id: Guid.NewGuid(),
            AnnotationData: "{\"type\":\"rectangle\"}");

        var validator = new UpdateFileAnnotationCommandValidator();
        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
