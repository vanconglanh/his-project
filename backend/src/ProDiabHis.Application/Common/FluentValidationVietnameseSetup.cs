using System.Globalization;
using FluentValidation;
using FluentValidation.Resources;

namespace ProDiabHis.Application.Common;

/// <summary>
/// Cau hinh toan cuc (global) de FluentValidation tra ve message mac dinh
/// (NotEmpty, NotNull, MaximumLength, EmailAddress, GreaterThan, Matches, ...)
/// bang TIENG VIET CO DAU, dung quy uoc CLAUDE.md — thay vi phai them
/// .WithMessage("...") thu cong cho tung rule o tung validator.
///
/// FluentValidation da co san resource ngon ngu "vi" (VietnameseLanguage) —
/// ta chi can ep Culture ve "vi" bat ke culture cua thread/request, vi he thong
/// khong dung UI culture de chon ngon ngu response (message JSON luon la tieng Viet).
///
/// Rule nao can dien dat nghiep vu rieng thi validator van tu set .WithMessage(...)
/// cu the — cai do se GHI DE len message mac dinh nay, khong xung dot.
/// </summary>
public static class FluentValidationVietnameseSetup
{
    /// <summary>
    /// Bang tra ten field tieng Viet cho cac property pho bien dung chung nhieu
    /// validator (BillingId, Amount, Method, Code, Name, ...). Property nao khong
    /// co trong bang se fallback ve behavior mac dinh cua FluentValidation
    /// (tach PascalCase thanh cac tu, vd "BillingId" -> "Billing Id").
    /// </summary>
    private static readonly Dictionary<string, string> FieldDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FullName"] = "Họ tên",
        ["Name"] = "Tên",
        ["Code"] = "Mã",
        ["Email"] = "Email",
        ["Phone"] = "Số điện thoại",
        ["Password"] = "Mật khẩu",
        ["OldPassword"] = "Mật khẩu cũ",
        ["NewPassword"] = "Mật khẩu mới",
        ["Token"] = "Mã xác thực",
        ["Amount"] = "Số tiền",
        ["Method"] = "Phương thức thanh toán",
        ["BillingId"] = "Hóa đơn",
        ["PatientId"] = "Bệnh nhân",
        ["PackageId"] = "Gói dịch vụ",
        ["TotalPrice"] = "Tổng giá",
        ["ListPrice"] = "Giá niêm yết",
        ["Price"] = "Giá",
        ["Quantity"] = "Số lượng",
        ["StorageQuotaGb"] = "Hạn mức lưu trữ",
        ["MinDepositPercent"] = "Tỷ lệ đặt cọc tối thiểu",
    };

    public static void Configure()
    {
        // Bat multi-language cua FluentValidation va ep culture "vi" cho moi message
        // mac dinh, khong phu thuoc CultureInfo.CurrentUICulture cua request/thread.
        ValidatorOptions.Global.LanguageManager = new LanguageManager
        {
            Enabled = true,
            Culture = new CultureInfo("vi"),
        };

        // Dich ten field sang tieng Viet cho cac property dung chung; property
        // khong co trong tu dien se giu behavior mac dinh (khong regression).
        ValidatorOptions.Global.DisplayNameResolver = (_, member, _) =>
        {
            if (member is not null && FieldDisplayNames.TryGetValue(member.Name, out var viName))
                return viName;

            return null; // null => FluentValidation tu fallback ve resolver mac dinh
        };
    }
}
