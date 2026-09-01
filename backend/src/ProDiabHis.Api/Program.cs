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

    // BUG-003: model-binding validation (DataAnnotations / JSON binding) mac dinh tra message tieng Anh
    // theo chuan ProblemDetails. Ghi de de tra dung error envelope + message tieng Viet nhu FluentValidation.
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(opts =>
    {
        opts.InvalidModelStateResponseFactory = ctx =>
        {
            var details = ctx.ModelState
                .Where(kv => kv.Value is not null && kv.Value.Errors.Count > 0)
                .ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value!.Errors
                        .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                            ? "Giá trị không hợp lệ"
                            : ProDiabHis.Api.VietnameseModelBindingMessages.Translate(kv.Key, e.ErrorMessage))
                        .ToArray());

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "Dữ liệu đầu vào không hợp lệ",
                    details
                }
            });
        };
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
            // Dev: cho phep moi origin loopback (localhost/127.0.0.1 moi cong) — phuc vu
            // portal-client chay o cong bat ky khi dev. Prod dung same-origin (khong CORS)
            // + CorsHardeningMiddleware whitelist, khong dung DevPolicy nay.
            policy.SetIsOriginAllowed(origin =>
                    {
                        try { return new Uri(origin).IsLoopback; } catch { return false; }
                    })
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

    // ---------------------------------------------------------------------------------------------
    // CONSOLE COMMAND chay-1-lan: backfill blind index PII (khong listen HTTP).
    // Cach chay:  dotnet run --project backend/src/ProDiabHis.Api -- backfill-bidx [tenantId]
    // Muc dich:   du lieu cu co id_number_enc/card_no_enc day du nhung *_bidx = NULL -> tra cuu benh
    //             nhan theo CCCD/SDT/so the BHYT khong ra. Tai dung PiiBackfillService (idempotent).
    // Neu truyen tenantId -> chi chay tenant do; neu khong -> quet distinct tenant_id tu benh nhan.
    // Xu ly xong -> THOAT, KHONG chay web server.
    // ---------------------------------------------------------------------------------------------
    if (args.Length > 0 && (args[0] == "backfill-bidx" || args[0] == "--backfill-pii"))
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var backfill = sp.GetRequiredService<ProDiabHis.Application.Common.IPiiBackfillService>();
        var dbFactory = sp.GetRequiredService<ProDiabHis.Application.Common.IDapperConnectionFactory>();

        // Batch mac dinh 500 (RunAsync tu ep ve 500 neu ngoai khoang hop le)
        const int batchSize = 500;

        // Xac dinh danh sach tenant can chay
        List<int> tenantIds;
        if (args.Length > 1 && int.TryParse(args[1], out var singleTenant))
        {
            tenantIds = new List<int> { singleTenant };
            Log.Information("PII backfill: chay cho tenant chi dinh {TenantId}", singleTenant);
        }
        else
        {
            using var conn = dbFactory.CreateConnection();
            tenantIds = (await Dapper.SqlMapper.QueryAsync<int>(conn,
                "SELECT DISTINCT tenant_id FROM diab_his_pat_patients ORDER BY tenant_id"))
                .ToList();
            Log.Information("PII backfill: tim thay {Count} tenant co du lieu benh nhan: {Tenants}",
                tenantIds.Count, string.Join(",", tenantIds));
        }

        int totalIndexed = 0, totalInsIndexed = 0, totalErrors = 0;
        foreach (var tid in tenantIds)
        {
            Log.Information("PII backfill: bat dau tenant {TenantId} (batchSize={BatchSize})...", tid, batchSize);
            var result = await backfill.RunAsync(tid, batchSize, dryRun: false);
            totalIndexed += result.PatientsBlindIndexed;
            totalInsIndexed += result.InsurancesBlindIndexed;
            totalErrors += result.Errors.Count;
            Log.Information(
                "PII backfill: xong tenant {TenantId} - scanned={Scanned} encrypted={Enc} bidx={Bidx} insBidx={InsBidx} errors={Err}",
                tid, result.PatientsScanned, result.PatientsEncrypted,
                result.PatientsBlindIndexed, result.InsurancesBlindIndexed, result.Errors.Count);
            foreach (var e in result.Errors) Log.Warning("PII backfill loi: {Error}", e);
        }

        Log.Information(
            "PII backfill HOAN TAT: tenants={TenantCount} tong bidx benh nhan={Bidx} tong bidx the BHYT={InsBidx} tong loi={Err}",
            tenantIds.Count, totalIndexed, totalInsIndexed, totalErrors);
        Log.CloseAndFlush();
        return; // thoat, khong chay web server
    }

    // Hang muc 6: kich hoat ambient PII protector (dung boi cac read-path Dapper raw SQL)
    var piiProtector = app.Services.GetRequiredService<ProDiabHis.Application.Common.IPiiProtector>();
    ProDiabHis.Application.Common.PiiCrypto.Configure(piiProtector);
    if (piiProtector is ProDiabHis.Infrastructure.Security.PiiProtector pp && !pp.BlindIndexEnabled)
        Log.Warning("PII: Encryption:BlindIndexKey chua cau hinh - tra cuu benh nhan theo SDT/CMND/so the BHYT se KHONG hoat dong");

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

    // Ghi chu: middleware nay duoc dang ky TRUOC UseAuthentication trong pipeline, nhung
    // callback EnrichDiagnosticContext chi thuc thi SAU KHI toan bo pipeline phia sau (bao gom
    // Auth/TenantScope) da chay xong (middleware bao ngoai _next), nen HttpContext.User da co
    // claim khi enrich — an toan de doc ICurrentUser tai day.
    // Muc dich: moi dong log request deu gan san UserId/TenantId/Email/Role -> Loki/Grafana loc
    // duoc theo tung nguoi dung + tung endpoint (dashboard "User Activity").
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
        opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var currentUser = httpContext.RequestServices.GetService<ProDiabHis.Application.Common.ICurrentUser>();
            if (currentUser is { IsAuthenticated: true })
            {
                diagnosticContext.Set("UserId", currentUser.UserId);
                diagnosticContext.Set("TenantId", currentUser.TenantId);
                diagnosticContext.Set("UserEmail", currentUser.Email);
                diagnosticContext.Set("RoleCodes", string.Join(",", currentUser.RoleCodes));
            }
        };
    });

    app.UseCors("DevPolicy");
    app.UseAuthentication();
    app.UseMiddleware<TenantScopeMiddleware>();
    app.UseMiddleware<BranchScopeMiddleware>();
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

    // CDSS: phan tang nguy co benh nhan DTD (03:00) + recall tai kham chu dong (03:30)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.PatientRiskStratificationJob>(
        "patient-risk-stratification",
        j => j.ExecuteAsync(default),
        "0 3 * * *");
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.ChronicCareRecallJob>(
        "chronic-care-recall",
        j => j.ExecuteAsync(default),
        "30 3 * * *");

    // Report Builder P3.3: quet lich gui bao cao qua email den han, chay dau moi gio
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.ReportScheduleDispatchJob>(
        "report-schedule-dispatch",
        j => j.ExecuteAsync(default),
        "0 * * * *");

    // Patient Portal: nhac tai kham + nhac lich hen T-1 (08:00 hang ngay)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.RecallNotifyJob>(
        "portal-recall-notify",
        j => j.ExecuteAsync(default),
        "0 8 * * *");
    // FR-112 (H-1): nhac lich hen qua SMS/Zalo ZNS (moi gio, nguong gio cau hinh o Notifications:AppointmentReminderHours)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.AppointmentReminderNotifyJob>(
        "appointment-reminder-notify",
        j => j.ExecuteAsync(default),
        "0 * * * *");
    // Patient Portal: nhac uong thuoc (moi 30 phut)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.MedReminderJob>(
        "portal-med-reminder",
        j => j.ExecuteAsync(default),
        "*/30 * * * *");

    // FR-511: canh bao ket qua XN qua han SLA cam ket voi doi tac lab (moi gio)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.LabOrderOverdueAlertJob>(
        "lab-order-overdue-sla",
        j => j.ExecuteAsync(),
        "0 * * * *");

    // FR-1206: canh bao goi dinh muc sap het han / sap het dinh muc / cong no qua han (hang ngay 00:15)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.PackageAlertJob>(
        "package-entitlement-alert",
        j => j.ExecuteAsync(),
        "15 0 * * *");

    // FR-801..803: Telehealth Docosan - dong bo trang thai phien (moi 5 phut, khong co webhook)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.DocosanSessionSyncJob>(
        "docosan-session-sync",
        j => j.ExecuteAsync(default),
        "*/5 * * * *");
    // Telehealth Docosan: retry outbox (moi 2 phut, backoff 1p/5p/15p/60p/6h)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.DocosanOutboxRetryJob>(
        "docosan-outbox-retry",
        j => j.ExecuteAsync(default),
        "*/2 * * * *");

    // FR-711 [P2]: Ket noi thiet bi do duong huyet/CGM - dong bo readings (moi 15 phut)
    RecurringJob.AddOrUpdate<ProDiabHis.Infrastructure.Jobs.CgmReadingsSyncJob>(
        "cgm-readings-sync",
        j => j.ExecuteAsync(default),
        "*/15 * * * *");

    // Minimal endpoint kiem tra nhanh
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

    // Healthcheck cho Docker/monitoring (docker-compose.prod.yml healthcheck goi endpoint nay).
    // Chi tra ve 200 khi process con song - khong check DB/Redis de tranh false negative
    // khi dependency cham tam thoi (dependency that duoc healthcheck rieng trong compose).
    app.MapGet("/healthz", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

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
