using Microsoft.AspNetCore.Mvc.Filters;

namespace ProDiabHis.Api.Filters;

/// <summary>
/// BM-01 (Lark Bug-Feedback, role Le tan/CC): "Khong duoc phep xem Bao cao *thang* / *quy*
/// (ly do: bao mat so lieu cong ty)". ReportsController cho phep goi bat ky endpoint bao cao
/// nao voi tham so from/to tuy y (RequirePermission("report.read") khong mang theo "pham vi
/// thoi gian"), nen le_tan van xem duoc bao cao doanh thu ca thang/quy neu biet URL.
///
/// Filter nay ap dung tren action co CA HAI tham so "from" va "to" (khoang ngay) trong
/// ActionArguments (sau model binding). Neu nguoi dung KHONG co quyen
/// "report.date_range.extended" (xem migration 9204) thi bat buoc from == to (dung 1 ngay -
/// khop "bao cao trong NGAY"); thieu 1 trong 2 gia tri (vd de trong de dung default nhieu
/// ngay trong code cua action) cung bi chan vi khong xac dinh duoc la 1 ngay.
///
/// Action KHONG co ca 2 tham so "from"/"to" (vd endpoint dung tham so "date"/"as_of" don le,
/// hoac khong co tham so ngay - datasets/catalog/dashboards list...) khong bi filter nay dieu
/// chinh - da von chi tra du lieu 1 thoi diem/khong theo ky.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ReportDateRangeGuardAttribute : Attribute, IActionFilter
{
    private const string ExtendedRangePermission = "report.date_range.extended";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ActionArguments.ContainsKey("from") || !context.ActionArguments.ContainsKey("to"))
        {
            return; // Action khong co khoang ngay -> khong ap dung guard nay.
        }

        var user = context.HttpContext.User;
        var isSuperAdmin = user.FindFirst("is_super_admin")?.Value == "true";
        if (isSuperAdmin) return;

        var permissions = user.FindAll("permissions").Select(c => c.Value).ToList();
        if (permissions.Contains(ExtendedRangePermission)) return;

        var from = context.ActionArguments["from"] as DateOnly?;
        var to = context.ActionArguments["to"] as DateOnly?;

        if (from is null || to is null || from.Value != to.Value)
        {
            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(new
            {
                error = new
                {
                    code = "REPORT_DATE_RANGE_FORBIDDEN",
                    message = "Bạn chỉ được xem báo cáo trong 1 ngày, không được xem theo khoảng ngày/tháng/quý"
                }
            })
            { StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
