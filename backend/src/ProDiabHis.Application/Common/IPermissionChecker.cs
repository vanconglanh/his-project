namespace ProDiabHis.Application.Common;

/// <summary>
/// Kiem tra quyen o tang handler (defense-in-depth, bo sung cho [RequirePermission] o controller).
/// Dung khi nghiep vu can tra ve ma loi rieng (vd FORBIDDEN cua G03) hoac quyet dinh logic theo quyen.
/// </summary>
public interface IPermissionChecker
{
    /// <summary>true neu user hien tai co quyen (super admin luon true).</summary>
    bool HasPermission(string permissionCode);
}
