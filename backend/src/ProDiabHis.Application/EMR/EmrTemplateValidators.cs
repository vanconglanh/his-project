using FluentValidation;

namespace ProDiabHis.Application.EMR;

/// <summary>
/// BUG-02 (Major, QC final review + tester UTC): EMR Template Create/Update truoc day
/// KHONG co validator BE — request rong/qua dai lot xuong toi EF Core, co the vo constraint
/// DB (name VARCHAR(200) NOT NULL, xem db/migrations/0026_create_emr_templates.sql) va
/// tra ve HTTP 500 thay vi 400. Bo sung validator theo dung convention da dung trong
/// DrugValidators.cs (nested request validator + SetValidator tren tung command).
/// </summary>
public class EmrTemplateRequestValidator : AbstractValidator<EmrTemplateRequest>
{
    // Danh sach chuyen khoa hop le — lay tu comment cot `speciality` trong
    // db/migrations/0026_create_emr_templates.sql (VARCHAR(50) DEFAULT 'GENERAL'
    // COMMENT 'GENERAL|DIABETES|CARDIOLOGY|ENDOCRINOLOGY|NEPHROLOGY|OPHTHALMOLOGY|OTHER').
    // Chua tim thay enum C# rieng cho gia tri nay trong Domain (EmrTemplate.Speciality
    // la string thuan) nen dung danh sach hang so nay lam nguon chuan duy nhat cho validator.
    public static readonly string[] ValidSpecialities =
    {
        "GENERAL", "DIABETES", "CARDIOLOGY", "ENDOCRINOLOGY", "NEPHROLOGY", "OPHTHALMOLOGY", "OTHER"
    };

    public EmrTemplateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tên mẫu bệnh án không được để trống")
            .MaximumLength(200).WithMessage("Tên mẫu bệnh án tối đa 200 ký tự");

        RuleFor(x => x.ContentJson).NotNull().WithMessage("Nội dung mẫu bệnh án không được để trống");

        RuleFor(x => x.Speciality).NotEmpty().WithMessage("Chuyên khoa không được để trống")
            .MaximumLength(50).WithMessage("Chuyên khoa tối đa 50 ký tự")
            .Must(s => ValidSpecialities.Contains(s))
            .WithMessage($"Chuyên khoa không hợp lệ, phải là một trong: {string.Join(", ", ValidSpecialities)}")
            .When(x => !string.IsNullOrEmpty(x.Speciality));
    }
}

public class CreateEmrTemplateCommandValidator : AbstractValidator<CreateEmrTemplateCommand>
{
    public CreateEmrTemplateCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new EmrTemplateRequestValidator());
    }
}

public class UpdateEmrTemplateCommandValidator : AbstractValidator<UpdateEmrTemplateCommand>
{
    public UpdateEmrTemplateCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty().WithMessage("Thiếu mã mẫu bệnh án cần cập nhật");
        RuleFor(x => x.Request).SetValidator(new EmrTemplateRequestValidator());
    }
}
