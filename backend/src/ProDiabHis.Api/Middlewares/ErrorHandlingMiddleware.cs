using System.Net;
using System.Text.Json;
using FluentValidation;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;

namespace ProDiabHis.Api.Middlewares;

/// <summary>Xu ly loi toan cuc, tra ve error envelope chuan</summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation error: {Errors}", string.Join(", ", ex.Errors.Select(e => e.ErrorMessage)));
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = "application/json";

            var details = ex.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            await WriteError(ctx, "VALIDATION_ERROR", "Du lieu dau vao khong hop le", details);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized: {Message}", ex.Message);
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            ctx.Response.ContentType = "application/json";
            await WriteError(ctx, "UNAUTHORIZED", "Khong co quyen truy cap");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Not found: {Message}", ex.Message);
            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
            ctx.Response.ContentType = "application/json";
            await WriteError(ctx, "NOT_FOUND", ex.Message);
        }
        catch (ReportValidationException ex)
        {
            _logger.LogWarning("Report validation: {Code} — {Message}", ex.ErrorCode, ex.Message);
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = "application/json";
            await WriteError(ctx, ex.ErrorCode, ex.Message);
        }
        catch (ReportEmptyDatasetException ex)
        {
            _logger.LogWarning("Report empty dataset: {Message}", ex.Message);
            ctx.Response.StatusCode = 422;
            ctx.Response.ContentType = "application/json";
            await WriteError(ctx, ex.ErrorCode, ex.Message);
        }
        catch (ConflictException ex)
        {
            _logger.LogWarning("Conflict: {Code} — {Message}", ex.ErrorCode, ex.Message);
            ctx.Response.StatusCode = (int)HttpStatusCode.Conflict;
            ctx.Response.ContentType = "application/json";
            await WriteError(ctx, ex.ErrorCode, ex.Message);
        }
        catch (CrossTenantAccessException ex)
        {
            _logger.LogWarning("Cross-tenant access attempt: {Code} — {Message}", ex.ErrorCode, ex.Message);
            ctx.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            ctx.Response.ContentType = "application/json";
            await WriteError(ctx, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";
            await WriteError(ctx, "INTERNAL_ERROR", "Loi he thong, vui long thu lai sau");
        }
    }

    private static Task WriteError(HttpContext ctx, string code, string message, object? details = null)
    {
        var body = JsonSerializer.Serialize(new
        {
            error = new { code, message, details = details ?? new { } }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        return ctx.Response.WriteAsync(body);
    }
}
