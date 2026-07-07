using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProDiabHis.Api.Filters;
using ProDiabHis.Api.Middlewares;
using ProDiabHis.Api.Services;
using ProDiabHis.Application;
using ProDiabHis.Application.AuditLogs;
using ProDiabHis.Infrastructure;
using ProDiabHis.Infrastructure.Jobs;
using Serilog;
using System.Text;

// Bootstrap Serilog truoc khi app khoi dong
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ProDiabHis API...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext());

    // Sentry (chi enable khi co DSN)
    var sentryDsn = builder.Configuration["Sentry:Dsn"];
    if (!string.IsNullOrEmpty(sentryDsn))
    {
        builder.WebHost.UseSentry(o =>
        {
            o.Dsn = sentryDsn;
            o.TracesSampleRate = builder.Configuration.GetValue<double>("Sentry:TracesSampleRate", 0.1);
        });
    }

    // DI
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllers(opts =>
    {
        // Sprint 12: Global rate limit filter ap dung cho moi authenticated request
        opts.Filters.Add<GlobalRateLimitFilter>();
    })
    .AddJsonOptions(opts =>
    {
        // Global JSON naming: snake_case (khop voi FE interface)
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        opts.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });
    builder.Services.AddScoped<ITicketPdfService, TicketPdfService>();
    builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();
    builder.Services.AddScoped<IReceiptPdfService, ReceiptPdfService>();
    builder.Services.AddScoped<ProDiabHis.Application.Billing.IInvoicePdfGenerator, InvoicePdfGeneratorAdapter>();
    builder.Services.AddScoped<ProDiabHis.Application.Billing.IReceiptPdfGenerator, ReceiptPdfGeneratorAdapter>();
    builder.Services.AddScoped<GlobalRateLimitFilter>();

    // Sprint 12: AuditQueryService (Dapper read-side)
    builder.Services.AddScoped<AuditQueryService>();

    // Sprint 10: ApiKeyAuthFilter as service (needs DI)
    builder.Services.AddScoped<ApiKeyAuthFilter>();

    // Sprint 10: Portal JWT scheme (aud=patient-portal)
    var jwtSecret2 = builder.Configuration["JWT__SECRET"] ?? builder.Configuration["Jwt:Secret"] ?? "dev_secret";
    builder.Services.AddAuthentication()
        .AddJwtBearer("PortalBearer", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ProDiabHis",
                ValidAudience = "patient-portal",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret2)),
                ClockSkew = TimeSpan.Zero
            };
        });

    // FluentValidation: da dang ky qua AddApplication() (DependencyInjection.cs) —
    // KHONG dang ky lai o day de tranh validator chay 2 lan / loi trung lap.

    // CORS
    builder.Services.AddCors(opt =>
    {
        opt.AddPolicy("DevPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3100")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Pro-Diab HIS API",
            Version = "v1",
            Description = "He thong quan ly phong kham da khoa — Pro-Diab HIS",
            Contact = new OpenApiContact { Name = "ATDS Team", Email = "co.ltd.atds@gmail.com" }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Nhap JWT token. Vi du: Bearer {token}"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });

        // Include XML comments neu co
        var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            c.IncludeXmlComments(xmlPath);
    });

    var app = builder.Build();

    // Middleware pipeline
    // Sprint 12: Security headers (truoc tat ca)
    app.UseMiddleware<SecurityHeadersMiddleware>();
    // Sprint 12: CORS hardening (whitelist-only)
    app.UseMiddleware<CorsHardeningMiddleware>();
    app.UseMiddleware<ErrorHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pro-Diab HIS API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "Pro-Diab HIS — API Docs";
        });
    }

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
    });

    app.UseCors("DevPolicy");
    app.UseAuthentication();
    app.UseMiddleware<TenantScopeMiddleware>();
    app.UseAuthorization();
    app.UseMiddleware<AuditLogMiddleware>();

    app.MapControllers();

    // Hangfire dashboard (Super Admin only) + recurring job
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new ProDiabHis.Api.Filters.HangfireSuperAdminAuthFilter() }
    });
    RecurringJob.AddOrUpdate<EncounterOver12hAlertJob>(
        "encounter-over-12h",
        j => j.Execute(),
        "*/10 * * * *");
    // Sprint 8: QR expire + eInvoice retry
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.QrExpireJob>(
        "qr-expire",
        j => j.ExecuteAsync(),
        "*/5 * * * *");
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.EInvoiceRetryJob>(
        "einvoice-retry",
        j => j.ExecuteAsync(),
        "*/15 * * * *");

    // Sprint 12: Key rotation (daily at 02:00) + Audit anomaly detection (daily at 01:00)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.KeyRotationJob>(
        "key-rotation",
        j => j.ExecuteAsync(),
        "0 2 * * *");
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.AuditAnomalyDetectionJob>(
        "audit-anomaly-detection",
        j => j.ExecuteAsync(),
        "0 1 * * *");

    // Minimal endpoint kiem tra nhanh
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

    Log.Information("ProDiabHis API started successfully");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "ProDiabHis API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Cho phep integration test reference Program
public partial class Program { }
