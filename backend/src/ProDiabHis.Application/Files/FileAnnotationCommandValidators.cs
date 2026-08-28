using System.Text.Json;
using FluentValidation;

namespace ProDiabHis.Application.Files;

public class CreateFileAnnotationCommandValidator : AbstractValidator<CreateFileAnnotationCommand>
{
    public CreateFileAnnotationCommandValidator()
    {
        RuleFor(x => x.FileId).NotEmpty().WithMessage("Phải chỉ định tệp ảnh cần đánh dấu");

        RuleFor(x => x.AnnotationData)
            .NotEmpty().WithMessage("Dữ liệu annotation không được để trống")
            .MaximumLength(1_000_000).WithMessage("Dữ liệu annotation vượt quá dung lượng cho phép")
            .Must(BeValidJson).WithMessage("Dữ liệu annotation phải là JSON hợp lệ (mảng shape)");
    }

    internal static bool BeValidJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            using var doc = JsonDocument.Parse(value);
            return doc.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public class UpdateFileAnnotationCommandValidator : AbstractValidator<UpdateFileAnnotationCommand>
{
    public UpdateFileAnnotationCommandValidator()
    {
        RuleFor(x => x.FileId).NotEmpty().WithMessage("Phải chỉ định tệp ảnh cần đánh dấu");
        RuleFor(x => x.Id).NotEmpty().WithMessage("Phải chỉ định annotation cần cập nhật");

        RuleFor(x => x.AnnotationData)
            .NotEmpty().WithMessage("Dữ liệu annotation không được để trống")
            .MaximumLength(1_000_000).WithMessage("Dữ liệu annotation vượt quá dung lượng cho phép")
            .Must(CreateFileAnnotationCommandValidator.BeValidJson)
            .WithMessage("Dữ liệu annotation phải là JSON hợp lệ (mảng shape)");
    }
}
