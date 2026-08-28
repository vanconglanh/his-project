using System.Text.RegularExpressions;

namespace ProDiabHis.Api;

/// <summary>
/// BUG-003: Chuyen message validation mac dinh cua ASP.NET model binding (DataAnnotations,
/// JSON deserialize) tu tieng Anh sang tieng Viet co dau, de dong bo voi FluentValidation.
/// Chi ap dung cho message tra ve JSON response; log Serilog van giu khong dau theo CLAUDE.md.
/// </summary>
public static class VietnameseModelBindingMessages
{
    /// <summary>Ban do ten field ky thuat -> nhan tieng Viet hien thi cho nguoi dung.</summary>
    private static readonly Dictionary<string, string> FieldLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["full_name"] = "Họ tên",
        ["FullName"] = "Họ tên",
        ["date_of_birth"] = "Ngày sinh",
        ["DateOfBirth"] = "Ngày sinh",
        ["gender"] = "Giới tính",
        ["Gender"] = "Giới tính",
        ["phone"] = "Số điện thoại",
        ["Phone"] = "Số điện thoại",
        ["email"] = "Email",
        ["Email"] = "Email",
        ["address"] = "Địa chỉ",
        ["Address"] = "Địa chỉ",
        ["password"] = "Mật khẩu",
        ["Password"] = "Mật khẩu",
        ["code"] = "Mã",
        ["Code"] = "Mã",
        ["quantity"] = "Số lượng",
        ["Quantity"] = "Số lượng",
        ["amount"] = "Số tiền",
        ["Amount"] = "Số tiền",
        ["id_number"] = "Số CCCD/CMND",
        ["bhyt_card_no"] = "Số thẻ BHYT",
        ["patient_id"] = "Bệnh nhân",
        ["encounter_id"] = "Lượt khám",
        ["doctor_id"] = "Bác sĩ",
        ["room_id"] = "Phòng khám",
        ["billing_id"] = "Hóa đơn",
    };

    /// <summary>
    /// Dich message tieng Anh mac dinh sang tieng Viet. Neu khong khop mau nao thi
    /// tra ve message goc (khong lam mat thong tin).
    /// </summary>
    public static string Translate(string fieldKey, string message)
    {
        var label = ResolveLabel(fieldKey);

        // "The X field is required."
        if (Regex.IsMatch(message, @"field is required", RegexOptions.IgnoreCase))
            return $"{label} là bắt buộc";

        // "The X field must be a string or array type with a minimum length of 'N'."
        var min = Regex.Match(message, @"minimum length of '(\d+)'", RegexOptions.IgnoreCase);
        if (min.Success)
            return $"{label} phải có tối thiểu {min.Groups[1].Value} ký tự";

        // "The field X must be a string or array type with a maximum length of 'N'."
        var max = Regex.Match(message, @"maximum length of '(\d+)'", RegexOptions.IgnoreCase);
        if (max.Success)
            return $"{label} không được vượt quá {max.Groups[1].Value} ký tự";

        // "The field X must be between A and B."
        var range = Regex.Match(message, @"must be between (\S+) and (\S+)", RegexOptions.IgnoreCase);
        if (range.Success)
            return $"{label} phải nằm trong khoảng {range.Groups[1].Value} đến {range.Groups[2].Value}";

        // "The X field is not a valid e-mail address."
        if (Regex.IsMatch(message, @"not a valid e-?mail", RegexOptions.IgnoreCase))
            return $"{label} không đúng định dạng email";

        // Loi convert kieu khi deserialize JSON, vd: "The JSON value could not be converted to ..."
        if (Regex.IsMatch(message, @"could not be converted|is not valid|invalid", RegexOptions.IgnoreCase))
            return $"{label} không đúng định dạng";

        return message;
    }

    private static string ResolveLabel(string fieldKey)
    {
        if (string.IsNullOrWhiteSpace(fieldKey)) return "Giá trị";

        // Bo tien to kieu "Request.FullName" / "$.full_name"
        var key = fieldKey.Replace("$.", string.Empty);
        var lastDot = key.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < key.Length - 1)
            key = key[(lastDot + 1)..];

        return FieldLabels.TryGetValue(key, out var label) ? label : key;
    }
}
