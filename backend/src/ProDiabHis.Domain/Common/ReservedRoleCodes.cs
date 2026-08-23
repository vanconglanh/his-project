namespace ProDiabHis.Domain.Common;

/// <summary>
/// Danh sach ma vai tro (Role.Code) danh rieng cho role he thong that su (RoleType = System, chi
/// duoc seed, khong ai tao duoc qua API tao role CUSTOM cua tenant).
///
/// Dung CHUNG 1 noi (khong hard-code lap lai) o 2 diem:
///  - CreateRoleCommandHandler (Application): tu choi tao role CUSTOM neu Code trung 1 ma trong danh
///    sach nay (chan tu goc, khong cho tenant tu tao role gia mao "SUPER_ADMIN"/"ADMIN").
///  - JwtService (Infrastructure): claim "is_super_admin" CHI duoc gan true khi user co role vua
///    khop 1 ma trong danh sach nay VUA la role RoleType = System that su — khong tin tuong role
///    CUSTOM du trung ma, tranh leo thang dac quyen qua role tu tao.
///
/// Neu can bo sung ma reserved moi, chi sua o day, khong sua lap lai o Application/Infrastructure.
/// </summary>
public static class ReservedRoleCodes
{
    public static readonly IReadOnlyCollection<string> All = new[] { "ADMIN", "SUPER_ADMIN" };

    /// <summary>Kiem tra 1 ma vai tro co nam trong danh sach reserved hay khong (khong phan biet hoa/thuong)</summary>
    public static bool IsReserved(string code) =>
        !string.IsNullOrWhiteSpace(code) &&
        All.Any(reserved => reserved.Equals(code, StringComparison.OrdinalIgnoreCase));
}
