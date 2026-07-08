using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Minio;
using ProDiabHis.Application.Auth;
using ProDiabHis.Application.Bhyt;
using ProDiabHis.Infrastructure.Bhyt;
using ProDiabHis.Application.Pharmacy;
using ProDiabHis.Infrastructure.Pharmacy;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.EMR;
using ProDiabHis.Application.LabIntegration;
using ProDiabHis.Application.LabPartners;
using ProDiabHis.Application.LabResults;
using ProDiabHis.Application.Billing;
using ProDiabHis.Application.PublicApi;
using ProDiabHis.Application.Reports;
using ProDiabHis.Infrastructure.Reports;
using ProDiabHis.Infrastructure.ApiKey;
using ProDiabHis.Infrastructure.Auth;
using ProDiabHis.Infrastructure.Billing;
using ProDiabHis.Infrastructure.Dapper;
using ProDiabHis.Infrastructure.Email;
using ProDiabHis.Infrastructure.EMR;
using ProDiabHis.Infrastructure.Jobs;
using ProDiabHis.Infrastructure.Lab;
using ProDiabHis.Infrastructure.FeatureFlags;
using ProDiabHis.Infrastructure.Notifications;
using ProDiabHis.Infrastructure.Persistence;
using ProDiabHis.Infrastructure.RateLimit;
using ProDiabHis.Infrastructure.Security;
using ProDiabHis.Infrastructure.Sms;
using ProDiabHis.Infrastructure.Storage;
using StackExchange.Redis;
using System.Text;

namespace ProDiabHis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Tenant provider (Scoped — moi request 1 instance)
        services.AddScoped<ITenantProvider, TenantProvider>();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // EF Core
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mysql => mysql.EnableRetryOnFailure(maxRetryCount: 3))
                // Workaround Pomelo 8.0.3 + EF Core 8.0.13+ bug:
                // ValidatePropertyMapping goi FindCollectionMapping(null) -> NullReferenceException khi co byte[] properties
                // Replace IModelValidator de suppress validate primitive collection (tinh nang EF8 ma Pomelo 8.0.3 chua support)
                // Ref: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1842
                .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelValidator, ProDiabHis.Infrastructure.Persistence.SafeModelValidator>()
                .ReplaceService<Microsoft.EntityFrameworkCore.Storage.IRelationalTypeMappingSource, ProDiabHis.Infrastructure.Persistence.SafeTypeMappingSource>();
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Dapper
        services.AddSingleton<Application.Common.IDapperConnectionFactory>(_ => new DapperConnectionFactory(connectionString));

        // Auth services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Encryption
        services.AddSingleton<IEncryptionService, AesGcmEncryptor>();

        // Sprint 12: Encryption key store + rotation
        services.AddSingleton<EncryptionKeyStoreImpl>();
        services.AddSingleton<IEncryptionKeyStore>(sp => sp.GetRequiredService<EncryptionKeyStoreImpl>());
        services.AddScoped<IKeyRotationService, KeyRotationServiceImpl>();

        // Sprint 12: PII Masker
        services.AddSingleton<IPiiMasker, PiiMaskerImpl>();

        // Email
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Audit
        services.AddScoped<IAuditService, AuditService>();

        // MinIO / File storage
        var minioEndpoint = configuration["Minio:Endpoint"] ?? "localhost:9000";
        var minioAccessKey = configuration["Minio:AccessKey"] ?? "minioadmin";
        var minioSecretKey = configuration["Minio:SecretKey"] ?? "minioadmin";
        var minioUseSsl = configuration.GetValue<bool>("Minio:UseSsl", false);

        services.AddSingleton<IMinioClient>(sp =>
        {
            var client = new MinioClient()
                .WithEndpoint(minioEndpoint)
                .WithCredentials(minioAccessKey, minioSecretKey);
            if (minioUseSsl)
                client = client.WithSSL();
            return client.Build();
        });

        // Storage:Provider = "Local" dung khi may dev khong co MinIO/docker (xem appsettings.Development.json)
        var storageProvider = configuration["Storage:Provider"] ?? "Minio";
        if (string.Equals(storageProvider, "Local", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IFileStorage, ProDiabHis.Infrastructure.Storage.LocalFileStorage>();
        else
            services.AddScoped<IFileStorage, MinioFileStorage>();

        // JWT Authentication
        var jwtSecret = configuration["JWT__SECRET"]
            ?? configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "ProDiabHis",
                    ValidAudience = configuration["Jwt:Audience"] ?? "ProDiabHis",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        // Redis (optional — khong throw neu chua co)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnectionString));
        }

        // EMR services
        services.AddScoped<IEmrSignatureVerifier, MockEmrSignatureVerifier>();
        services.AddScoped<IEmrPdfExporter, QuestPdfEmrExporter>();

        // Hangfire (MySQL storage)
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseStorage(new MySqlStorage(
                connectionString,
                new MySqlStorageOptions
                {
                    TablesPrefix = "hangfire_",
                    TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1)
                })));

        services.AddHangfireServer(opts => opts.WorkerCount = 2);
        services.AddScoped<EncounterOver12hAlertJob>();
        services.AddScoped<SendOutboundJob>();
        services.AddScoped<ProcessInboundJob>();
        services.AddScoped<IBackgroundJobEnqueuer, HangfireBackgroundJobEnqueuer>();
        // Pharmacy jobs (Sprint 6-7)
        services.AddScoped<DtqgSubmitRetryJob>();
        services.AddScoped<CucQldSyncDailyJob>();
        services.AddScoped<NearExpiryNotificationJob>();

        // CDSS (clinical decision support - DDI, drug-allergy, drug-lab, critical lab)
        services.AddMemoryCache();
        services.AddScoped<Application.Cdss.ICdssEngine, Cdss.CdssEngineImpl>();

        // Pharmacy services (Sprint 6-7 EPIC 5)
        services.AddScoped<IDdiChecker, DdiCheckerImpl>();
        services.AddScoped<IUsbTokenSigner, MockUsbTokenSigner>();
        services.AddScoped<IFefoStrategy, FefoStrategyImpl>();
        // Builder du lieu don_thuoc cho payload DTQG (doc canonical schema + giai ma the BHYT)
        services.AddScoped<IDtqgPrescriptionPayloadBuilder, DtqgPrescriptionPayloadBuilder>();
        // ĐTQG client: HTTP that (donthuocquocgia.vn) khi Dtqg:Enabled=true, mac dinh dung mock (dev/sandbox)
        if (string.Equals(configuration["Dtqg:Enabled"], "true", StringComparison.OrdinalIgnoreCase))
        {
            var dtqgOptions = new DtqgOptions { Enabled = true };
            if (!string.IsNullOrWhiteSpace(configuration["Dtqg:BaseUrl"])) dtqgOptions.BaseUrl = configuration["Dtqg:BaseUrl"]!;
            if (!string.IsNullOrWhiteSpace(configuration["Dtqg:ApiToken"])) dtqgOptions.ApiToken = configuration["Dtqg:ApiToken"]!;
            if (!string.IsNullOrWhiteSpace(configuration["Dtqg:SubmitPath"])) dtqgOptions.SubmitPath = configuration["Dtqg:SubmitPath"]!;
            if (!string.IsNullOrWhiteSpace(configuration["Dtqg:StatusPath"])) dtqgOptions.StatusPath = configuration["Dtqg:StatusPath"]!;
            if (!string.IsNullOrWhiteSpace(configuration["Dtqg:CancelPath"])) dtqgOptions.CancelPath = configuration["Dtqg:CancelPath"]!;
            if (!string.IsNullOrWhiteSpace(configuration["Dtqg:PingPath"])) dtqgOptions.PingPath = configuration["Dtqg:PingPath"]!;
            if (int.TryParse(configuration["Dtqg:TimeoutSeconds"], out var dtqgTimeout)) dtqgOptions.TimeoutSeconds = dtqgTimeout;

            services.AddSingleton(dtqgOptions);
            services.AddScoped<IDtqgCredentialProvider, DtqgCredentialProvider>();
            services.AddHttpClient(HttpDtqgClient.ClientName, c =>
            {
                c.BaseAddress = new Uri(dtqgOptions.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(dtqgOptions.TimeoutSeconds);
                if (!string.IsNullOrWhiteSpace(dtqgOptions.ApiToken))
                    c.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dtqgOptions.ApiToken);
            });
            services.AddScoped<IDtqgClient, HttpDtqgClient>();
        }
        else
        {
            services.AddScoped<IDtqgClient, MockDtqgClient>();
        }
        services.AddSingleton<IDtqgQrGenerator, QrCoderDtqgQrGenerator>();
        services.AddScoped<IDrugCucQldSync, MockDrugCucQldSync>();
        services.AddScoped<ICucQldLienThong, MockCucQldLienThong>();
        services.AddScoped<IExcelImporter, ClosedXmlImporter>();

        // Sprint 8: Billing + Cashier + Payment services
        services.AddScoped<Application.Billing.IServiceExcelParser, Billing.ServiceExcelParserImpl>();
        services.AddScoped<IBillingCalculator, BillingCalculatorImpl>();
        services.AddScoped<IBhytCoPayCalculator, BhytCoPayCalculatorImpl>();
        services.AddScoped<ICashierShiftService, CashierShiftServiceImpl>();
        // Payment gateways (IEnumerable<IPaymentGateway> injected)
        services.AddScoped<IPaymentGateway, CashGateway>();
        services.AddScoped<IPaymentGateway, VietQrGateway>();
        services.AddScoped<IPaymentGateway, MomoGateway>();
        services.AddScoped<IPaymentGateway, VnpayGateway>();
        services.AddScoped<IPaymentGateway, VisaMasterGateway>();
        // eInvoice providers
        services.AddScoped<IEInvoiceProvider, MisaEInvoiceProvider>();
        services.AddScoped<IEInvoiceProvider, VnptEInvoiceProvider>();
        services.AddScoped<IEInvoiceProvider, EfyEInvoiceProvider>();
        // Background jobs Sprint 8
        services.AddScoped<QrExpireJob>();
        services.AddScoped<EInvoiceRetryJob>();

        // Sprint 10: Public API + Push Notifications + Patient Portal
        services.AddScoped<IApiKeyStore, ApiKeyStoreImpl>();
        services.AddScoped<IPortalAuthService, PortalAuthServiceImpl>();
        services.AddScoped<IVapidKeyService, VapidKeyServiceImpl>();
        services.AddScoped<IWebPushSender, WebPushSenderImpl>();
        services.AddScoped<NotificationDispatcherJob>();

        // SMS Gateway — dung Mock cho dev, override bang DI extension khi can
        var smsProvider = configuration["Sms:Provider"] ?? "mock";
        services.AddScoped<ISmsGateway>(sp => smsProvider switch
        {
            "speedsms" => (ISmsGateway)sp.GetRequiredService<SpeedSmsGateway>(),
            "viettel" => sp.GetRequiredService<ViettelSmsGateway>(),
            "esms" => sp.GetRequiredService<EsmsGateway>(),
            _ => sp.GetRequiredService<MockSmsGateway>()
        });
        services.AddScoped<MockSmsGateway>();
        services.AddScoped<SpeedSmsGateway>();
        services.AddScoped<ViettelSmsGateway>();
        services.AddScoped<EsmsGateway>();

        // Rate limiter (Redis if available, fallback in-memory)
        services.AddScoped<IRateLimiter>(sp =>
        {
            var redis = sp.GetService<IConnectionMultiplexer>();
            if (redis != null && redis.IsConnected)
                return new RedisRateLimiter(redis);
            return new InMemoryRateLimiter();
        });

        // Sprint 9: BHYT Export services
        services.AddScoped<IBhytXmlGenerator, BhytXmlGeneratorImpl>();
        services.AddScoped<IBhytXsdValidator, BhytXsdValidatorImpl>();
        services.AddScoped<IBhytSigner, BhytSignerImpl>();
        services.AddScoped<IBhytSubmissionClient, MockBhytSubmissionClient>();
        services.AddScoped<IBhytReconcileParser, BhytReconcileParserImpl>();
        services.AddScoped<BhytGenerateXmlJob>();
        services.AddScoped<BhytReconcileParseJob>();

        // Sprint 13: Feature flags
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();

        // Sprint 13: FHIR mappers + bundle service
        services.AddScoped<Application.Fhir.Mappers.PatientMapper>();
        services.AddScoped<Application.Fhir.Mappers.EncounterMapper>();
        services.AddScoped<Application.Fhir.Mappers.ConditionMapper>();
        services.AddScoped<Application.Fhir.Mappers.ObservationMapper>();
        services.AddScoped<Application.Fhir.Mappers.MedicationRequestMapper>();
        services.AddScoped<Application.Fhir.Mappers.ProcedureMapper>();
        services.AddScoped<Application.Fhir.Mappers.RadProcedureMapper>();
        services.AddScoped<Application.Fhir.Mappers.AllergyIntoleranceMapper>();
        services.AddScoped<Application.Fhir.Mappers.DiagnosticReportMapper>();
        services.AddScoped<Application.Fhir.FhirBundleService>();

        // Sprint 12: Jobs
        services.AddScoped<Jobs.KeyRotationJob>();
        services.AddScoped<Jobs.AuditAnomalyDetectionJob>();

        // Sprint 11: Reports + Dashboard
        services.AddScoped<IReportCache, ReportCacheImpl>();
        services.AddScoped<IReportingService, ReportingServiceImpl>();
        services.AddScoped<IPdfReportExporter, QuestPdfReportExporter>();
        services.AddScoped<IExcelExporter, ReportExcelExporter>();
        services.AddScoped<ReportCacheRefreshJob>();

        // CDSS Sprint: risk stratification + chronic care recall jobs
        services.AddScoped<PatientRiskStratificationJob>();
        services.AddScoped<ChronicCareRecallJob>();

        // AI treatment suggestion (guideline-driven, chua goi Azure OpenAI that)
        var azureOpenAiOptions = new Ai.AzureOpenAiOptions
        {
            Enabled = string.Equals(configuration["AzureOpenAi:Enabled"], "true", StringComparison.OrdinalIgnoreCase),
            Endpoint = configuration["AzureOpenAi:Endpoint"] ?? "",
            ApiKey = configuration["AzureOpenAi:ApiKey"] ?? "",
            Deployment = configuration["AzureOpenAi:Deployment"] ?? "gpt-4o"
        };
        services.AddSingleton(azureOpenAiOptions);
        services.AddScoped<Application.Ai.ITreatmentSuggestionService, Ai.GuidelineSuggestionService>();

        // Report Engine config-driven (23 bao cao — docs/prd/reports-catalog-prd.md)
        services.AddSingleton<Reports.ReportRegistry>();
        services.AddScoped<Application.Reports.Engine.IGenericReportDataService, Reports.GenericReportDataService>();

        // Report Builder P1 (dataset whitelist + bao cao tu tao — docs/prd/report-builder-prd.md).
        // IReportRegistry doi sang CompositeReportRegistry (Scoped, gop code-defined + dong theo tenant/user).
        services.AddSingleton<Application.Reports.Engine.IDatasetRegistry, Reports.DatasetRegistry>();
        services.AddScoped<Application.Reports.Engine.IReportDefinitionStore, Reports.ReportDefinitionStore>();
        services.AddScoped<Application.Reports.Engine.IReportDashboardStore, Reports.ReportDashboardStore>();
        services.AddScoped<Application.Reports.Engine.IReportRegistry, Reports.CompositeReportRegistry>();
        services.AddScoped<Application.Reports.Engine.IGenericReportPdfExporter, Reports.GenericReportPdfExporter>();

        // Report Builder P3.3 — lich gui bao cao qua email dinh ky (Hangfire recurring job)
        services.AddScoped<Application.Reports.Engine.IReportScheduleStore, Reports.ReportScheduleStore>();
        services.AddScoped<Jobs.ReportScheduleDispatchJob>();
        services.AddScoped<Application.Pharmacy.Prescriptions.IPrescriptionPdfBuilder, Reports.PrescriptionPdfBuilder>();
        services.AddScoped<Application.CLS.IClsOrderSlipPdfBuilder, Reports.ClsOrderSlipPdfBuilder>();
        services.AddScoped<Application.Appointments.IAppointmentSlipPdfBuilder, Reports.AppointmentSlipPdfBuilder>();
        services.AddScoped<Application.Pharmacy.Dispensing.IPharmacyDispenseReceiptPdfBuilder, Reports.PharmacyDispenseReceiptPdfBuilder>();
        services.AddScoped<Application.Pharmacy.Warehouse.IStocktakePdfBuilder, Reports.StocktakePdfBuilder>();
        services.AddScoped<Application.Billing.ICashierShiftReportPdfBuilder, Reports.CashierShiftReportPdfBuilder>();

        // Sprint 14: Report PDF A4 — ma bao cao (Redis INCR, bat buoc — khong fallback)
        services.AddScoped<IReportCodeGenerator>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            return new ReportCodeGenerator(redis);
        });

        // HttpClient cho logo fetch (timeout 5s)
        services.AddHttpClient("ReportLogo", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(5);
        });

        // Lab integration services
        services.AddHttpClient("LabPartner");
        services.AddScoped<ILabPartnerClient, LabPartnerHttpClient>();
        services.AddSingleton<IHmacSignatureVerifier, HmacSignatureVerifier>();
        services.AddSingleton<IHl7v25Parser, Hl7v25ParserStub>();
        services.AddSingleton<ILabResultFlagCalculator, LabResultFlagCalculator>();
        services.AddScoped<ILabResultPdfExporter, LabResultQuestPdfExporter>();

        return services;
    }
}
