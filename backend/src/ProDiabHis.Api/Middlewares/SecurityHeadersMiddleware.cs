namespace ProDiabHis.Api.Middlewares;

/// <summary>
/// Them security headers vao moi response.
/// HSTS, X-Frame-Options, X-Content-Type-Options, CSP, Referrer-Policy, Permissions-Policy.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // HSTS — chi co hieu luc khi HTTPS, nhung header van ghi de nginx forward
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        // Chong clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Chong MIME sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Content Security Policy
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        // Referrer Policy
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions Policy — tat cac feature khong can thiet
        headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";

        // Remove Server header de an thong tin server
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        await _next(context);
    }
}
