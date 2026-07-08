namespace ProDiabHis.Application.Common;

/// <summary>Cung cap thong tin nguoi dung dang dang nhap</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    int? TenantId { get; }
    string? Email { get; }

    /// <summary>Ten hien thi role (vd "Bác sĩ") — tu ClaimTypes.Role. Dung cho hien thi/kiem tra admin
    /// (contains "admin"/"Quản trị") theo pattern hien co, KHONG dung de so sanh chinh xac voi ma role.</summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>Ma role on dinh (vd "bac_si", "ke_toan") — tu claim "role_code". Dung cho tinh nang can
    /// so sanh chinh xac (vd chia se bao cao theo role — Report Builder P3.2).</summary>
    IReadOnlyList<string> RoleCodes { get; }

    bool IsAuthenticated { get; }
}
