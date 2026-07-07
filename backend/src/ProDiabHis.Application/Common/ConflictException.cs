namespace ProDiabHis.Application.Common;

/// <summary>
/// Loi nghiep vu dang xung dot du lieu (vd: ma code da ton tai) — ErrorHandlingMiddleware
/// bat exception nay va tra ve HTTP 409 voi envelope { "error": { "code": ErrorCode, "message": Message } }.
/// </summary>
public class ConflictException : Exception
{
    public string ErrorCode { get; }

    public ConflictException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
