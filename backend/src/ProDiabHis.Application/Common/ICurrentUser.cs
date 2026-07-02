namespace ProDiabHis.Application.Common;

/// <summary>Cung cap thong tin nguoi dung dang dang nhap</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    int? TenantId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}
