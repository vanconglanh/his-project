namespace ProDiabHis.Application.Reports;

/// <summary>Loi validate tham so bao cao (HTTP 400).</summary>
public class ReportValidationException : Exception
{
    public string ErrorCode { get; }

    public ReportValidationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>Khong co du lieu trong ky bao cao (HTTP 422).</summary>
public class ReportEmptyDatasetException : Exception
{
    public string ErrorCode => "REPORT_EMPTY_DATASET";

    public ReportEmptyDatasetException(string message) : base(message) { }
}

/// <summary>Truy cap cross-tenant bi tu choi (HTTP 403).</summary>
public class CrossTenantAccessException : Exception
{
    public string ErrorCode { get; }

    public CrossTenantAccessException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
