using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
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
using ProDiabHis.Application.Notifications;
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
        services.AddScoped<IBranchProvider, BranchProvider>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        // G03 — guard khoa benh an
        services.AddScoped<IEncounterLockGuard, ProDiabHis.Infrastructure.Clinical.EncounterLockGuard>();

        // EF Core
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

        // BUG-004 (Blocker): MySqlConnector mac dinh (GuidFormat=Default, tuong duong Char36) tu suy dien
        // MOI cot CHAR(36) la Guid va tra ve System.Guid ngay o tang ADO.NET, bat ke property C# tuong ung
        // duoc EF map la string (vd Encounter.PatientId/DoctorId/RoomId). Khi EF materialize goi
        // reader.GetString(ordinal) cho cac cot nay -> InvalidCastException "Unable to cast object of type
        // 'System.Guid' to type 'System.String'". Ep GuidFormat=None de MySqlConnector luon tra CHAR(36)
        // ve string; cac property C# kieu Guid (Id, LockedBy, CreatedBy...) van duoc Pomelo tu convert
        // Guid<->string binh thuong qua ValueConverter rieng, khong phu thuoc GuidFormat.
        connectionString = EnsureGuidFormatNone(connectionString);

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
        // LoginCommandHandler duoc inject truc tiep vao Verify2faLoginCommandHandler de tai dung
        // BuildSuccessResponseAsync. MediatR chi dang ky handler qua interface nen phai dang ky
        // concrete type de resolve duoc.
        services.AddScoped<ProDiabHis.Application.Auth.LoginCommandHandler>();

        // Encryption
        services.AddSingleton<IEncryptionService, AesGcmEncryptor>();

        // Sprint 12: Encryption key store + rotation
        services.AddSingleton<EncryptionKeyStoreImpl>();
        services.AddSingleton<IEncryptionKeyStore>(sp => sp.GetRequiredService<EncryptionKeyStoreImpl>());
        services.AddScoped<IKeyRotationService, KeyRotationServiceImpl>();

        // Sprint 12: PII Masker
        services.AddSingleton<IPiiMasker, PiiMaskerImpl>();

        // Hang muc 6: Ma hoa PII + blind index
        services.AddSingleton<PiiProtector>();
        services.AddSingleton<Application.Common.IPiiProtector>(sp => sp.GetRequiredService<PiiProtector>());
        services.AddScoped<Application.Common.IPiiBackfillService, PiiBackfillService>();

        // Email
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Audit
        services.AddScoped<IAuditService, AuditService>();

        // Goi dinh muc tra truoc (FR-1201..1206)
        services.AddScoped<ProDiabHis.Application.Common.Interfaces.IPackageEntitlementService,
            ProDiabHis.Infrastructure.Services.PackageEntitlementService>();
        services.AddScoped<ProDiabHis.Application.Common.ISettingsProvider,
            ProDiabHis.Infrastructure.Services.SettingsProvider>();
        services.AddScoped<ProDiabHis.Application.Codes.ICodeResolver,
            ProDiabHis.Infrastructure.Services.CodeResolver>();

        // MinIO / File storage
        // Dung IsNullOrWhiteSpace thay vi "??" cho ca 3 gia tri - env var Docker Compose co the
        // bi set thanh CHUOI RONG "" (khong phai null, xem ghi chu o Minio:PublicEndpoint ben
        // duoi) neu bien nguon (vd MINIO_ROOT_USER trong .env) chua duoc dien - "??" khong bat
        // duoc chuoi rong nen se de lot gia tri rong xuong tang duoi gay loi kho hieu.
        string OrDefault(string key, string fallback) =>
            string.IsNullOrWhiteSpace(configuration[key]) ? fallback : configuration[key]!;
        var minioEndpoint = OrDefault("Minio:Endpoint", "localhost:9000");
        var minioAccessKey = OrDefault("Minio:AccessKey", "minioadmin");
        var minioSecretKey = OrDefault("Minio:SecretKey", "minioadmin");
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

        // Client MinIO rieng dung de sinh presigned URL TRA VE CHO CLIENT (trinh duyet nguoi dung).
        // "Minio:Endpoint" chi resolve duoc trong docker network noi bo (vd "minio:9000") -> KHONG
        // dung endpoint nay de tra ve FE. Dung "Minio:PublicEndpoint" (vd "localhost:9000" o dev,
        // domain that o prod) rieng cho muc dich nay, giu nguyen Minio:Endpoint cho ket noi server-to-server.
        // Dung IsNullOrWhiteSpace thay vi "??": bien env Minio__PublicEndpoint co the duoc set
        // thanh CHUOI RONG "" (khong phai null) khi khong cau hinh - "??" khong bat duoc chuoi
        // rong nen se truyen "" thang vao WithEndpoint() gay ArgumentException that (da xay ra
        // tren his.diab.vn: moi API tra file MinIO nhu /lab-results deu 500).
        var minioPublicEndpointRaw = configuration["Minio:PublicEndpoint"];
        var minioPublicEndpoint = string.IsNullOrWhiteSpace(minioPublicEndpointRaw) ? minioEndpoint : minioPublicEndpointRaw;
        var minioPublicUseSsl = configuration.GetValue<bool?>("Minio:PublicUseSsl") ?? minioUseSsl;

        services.AddKeyedSingleton<IMinioClient>("public", (sp, _) =>
        {
            var client = new MinioClient()
                .WithEndpoint(minioPublicEndpoint)
                .WithCredentials(minioAccessKey, minioSecretKey);
            if (minioPublicUseSsl)
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
            })
            // Scheme rieng "MfaSetup" cho token tam aud=mfa-setup (user role bat buoc 2FA nhung chua bat).
            // CHI dung duoc cho me/2fa/setup + me/2fa/enable (2 action khai bao
            // [Authorize(AuthenticationSchemes = "Bearer,MfaSetup")]); moi API nghiep vu khac dung scheme
            // Bearer mac dinh (ValidAudience=ProDiabHis) nen tu dong tu choi token nay.
            .AddJwtBearer("MfaSetup", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "ProDiabHis",
                    ValidAudience = "mfa-setup",
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

        // FR-302/FR-402: Ky so bao mat (chu ky so remote-signing - VNPT SmartCA / Viettel-CA).
        // Chon provider qua config "SignatureProvider:Type" = Mock | VnptSmartCa.
        var signatureProviderType = configuration["SignatureProvider:Type"] ?? "Mock";
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        if (string.Equals(signatureProviderType, "VnptSmartCa", StringComparison.OrdinalIgnoreCase))
        {
            var vnptOptions = new Security.VnptSmartCaOptions
            {
                BaseUrl = configuration["SignatureProvider:VnptSmartCa:BaseUrl"],
                ApiKey = configuration["SignatureProvider:VnptSmartCa:ApiKey"],
                SecretKey = configuration["SignatureProvider:VnptSmartCa:SecretKey"],
            };
            if (int.TryParse(configuration["SignatureProvider:VnptSmartCa:TimeoutSeconds"], out var sigTimeout))
                vnptOptions.TimeoutSeconds = sigTimeout;

            services.AddSingleton(vnptOptions);
            services.AddHttpClient(Security.VnptSmartCaSignatureProvider.HttpClientName, c =>
            {
                if (!string.IsNullOrWhiteSpace(vnptOptions.BaseUrl))
                    c.BaseAddress = new Uri(vnptOptions.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(vnptOptions.TimeoutSeconds);
                if (!string.IsNullOrWhiteSpace(vnptOptions.ApiKey))
                    c.DefaultRequestHeaders.Add("X-Api-Key", vnptOptions.ApiKey);
            });
            services.AddScoped<Application.Common.IDigitalSignatureProvider>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(Security.VnptSmartCaSignatureProvider.HttpClientName);
                var logger = sp.GetRequiredService<ILogger<Security.VnptSmartCaSignatureProvider>>();
                return new Security.VnptSmartCaSignatureProvider(httpClient, vnptOptions, logger);
            });
        }
        else
        {
            if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
            {
                // Log canh bao ngay tai thoi diem dang ky DI - Production khong nen dung Mock signer
                // cho chu ky so bao mat (FR-302/FR-402, muc 5.1 SRS).
                Console.Error.WriteLine(
                    "[CANH_BAO_BAO_MAT] SignatureProvider:Type=Mock dang duoc dung o moi truong Production. "
                    + "Chu ky so bao mat (FR-302/FR-402) se KHONG duoc xac thuc PKI that. "
                    + "Hay cau hinh SignatureProvider:Type=VnptSmartCa (hoac CA khac) truoc khi go-live.");
            }
            services.AddScoped<Application.Common.IDigitalSignatureProvider, Security.MockDigitalSignatureProvider>();
        }

        // §4.7.3 — Tich hop lo trinh diaB. Hien dung NullExternalPathwayProvider (diaB chua co endpoint).
        // Khi diaB co API that -> thay bang DiabPathwayProvider, khong doi tang Application/UI.
        services.AddScoped<Application.Common.Interfaces.IExternalPathwayProvider,
            Integrations.Diab.NullExternalPathwayProvider>();

        // EMR services
        services.AddScoped<IEmrSignatureVerifier, EmrSignatureVerifierAdapter>();
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

        services.AddHangfireServer(opts =>
        {
            opts.WorkerCount = 2;
            // "ocr": batch OCR nhap lieu ho so giay cu (LegacyOcrBatchJob). "default": queue mac dinh
            // Hangfire dung khi 1 job khong khai bao [Queue] rieng.
            opts.Queues = new[] { "default", "bhyt", "ocr" };
        });
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
        services.AddScoped<IUsbTokenSigner, UsbTokenSignerAdapter>();
        services.AddScoped<IFefoStrategy, FefoStrategyImpl>();

        // [G05] Dieu phoi kham — kiem tra lich truc bac si dich (canh bao, khong chan)
        services.AddScoped<ProDiabHis.Application.Reception.Reassign.IDoctorDutyChecker,
            ProDiabHis.Infrastructure.Scheduling.DoctorDutyChecker>();
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
        services.AddScoped<ProDiabHis.Application.Billing.BankReconciliation.IBankStatementParser,
            ProDiabHis.Infrastructure.Billing.BankStatementParserImpl>();

        // Telehealth (FR-801..803) - tich hop Docosan, xem docs/erd/telehealth-docosan.md
        {
            var docosanOptions = new ProDiabHis.Infrastructure.Integrations.Docosan.DocosanOptions();
            configuration.GetSection(ProDiabHis.Infrastructure.Integrations.Docosan.DocosanOptions.SectionName).Bind(docosanOptions);
            services.AddSingleton(docosanOptions);
            ProDiabHis.Application.Telehealth.DocosanEnvironment.Current = docosanOptions.Environment;

            services.AddHttpClient(ProDiabHis.Infrastructure.Integrations.Docosan.HttpDocosanClient.ClientName, c =>
            {
                c.BaseAddress = new Uri(docosanOptions.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(docosanOptions.TimeoutSeconds);
            });
            services.AddScoped<ProDiabHis.Application.Telehealth.Integration.IDocosanClient,
                ProDiabHis.Infrastructure.Integrations.Docosan.HttpDocosanClient>();
            services.AddScoped<ProDiabHis.Infrastructure.Jobs.DocosanSessionSyncJob>();
            services.AddScoped<ProDiabHis.Infrastructure.Jobs.DocosanOutboxRetryJob>();
        }

        // FR-711 [P2]: Ket noi thiet bi do duong huyet/CGM qua API (Dexcom/LibreView/...).
        // Chon provider qua config "CgmProvider:Type" = None | Dexcom. Mac dinh None (chua cau hinh).
        {
            var cgmProviderType = configuration["CgmProvider:Type"] ?? "None";
            var dexcomOptions = new Integrations.Cgm.DexcomCgmOptions();
            configuration.GetSection(Integrations.Cgm.DexcomCgmOptions.SectionName).Bind(dexcomOptions);
            services.AddSingleton(dexcomOptions);

            services.AddHttpClient(Integrations.Cgm.DexcomCgmProvider.HttpClientName, c =>
            {
                if (!string.IsNullOrWhiteSpace(dexcomOptions.BaseUrl))
                    c.BaseAddress = new Uri(dexcomOptions.BaseUrl);
                c.Timeout = TimeSpan.FromSeconds(dexcomOptions.TimeoutSeconds);
            });

            if (string.Equals(cgmProviderType, "Dexcom", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<Application.Diabetes.Cgm.ICgmDeviceProvider>(sp =>
                {
                    var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                        .CreateClient(Integrations.Cgm.DexcomCgmProvider.HttpClientName);
                    var logger = sp.GetRequiredService<ILogger<Integrations.Cgm.DexcomCgmProvider>>();
                    return new Integrations.Cgm.DexcomCgmProvider(httpClient, dexcomOptions, logger);
                });
            }
            else
            {
                services.AddScoped<Application.Diabetes.Cgm.ICgmDeviceProvider, Integrations.Cgm.NoneCgmProvider>();
            }

            services.AddScoped<Jobs.CgmReadingsSyncJob>();
        }

        // Sprint 8: Billing + Cashier + Payment services
        services.AddScoped<Application.Billing.IServiceExcelParser, Billing.ServiceExcelParserImpl>();
        // G02 - gate thanh toan dot chi dinh CLS
        services.AddScoped<ProDiabHis.Application.CLS.IClsPaymentGate, ProDiabHis.Infrastructure.CLS.ClsPaymentGateImpl>();

        services.AddScoped<IBillingCalculator, BillingCalculatorImpl>();
        services.AddScoped<IBhytCoPayCalculator, BhytCoPayCalculatorImpl>();
        services.AddScoped<ICashierShiftService, CashierShiftServiceImpl>();
        // E/Dot3 - gia override 3 tang (BR-70..BR-76)
        // Tang resolve DUNG CHUNG cho dich vu + thuoc (gia + an/hien theo chi nhanh)
        services.AddScoped<Application.Billing.IBranchPriceResolver, Billing.BranchPriceResolverImpl>();
        services.AddScoped<Application.Billing.IServicePriceResolver, Billing.ServicePriceResolverImpl>();
        // Payment gateways (IEnumerable<IPaymentGateway> injected)
        services.AddScoped<IPaymentGateway, CashGateway>();
        services.AddScoped<IPaymentGateway, VietQrGateway>();
        services.AddScoped<IPaymentGateway, MomoGateway>();
        services.AddScoped<IPaymentGateway, VnpayGateway>();
        services.AddScoped<IPaymentGateway, VisaMasterGateway>();
        // FR-911 H-9 - QR thanh toan DONG theo hoa don, tai khoan nhan tien doc tu cau hinh tenant
        services.AddScoped<Application.Billing.IVietQrBuilder, Billing.VietQrBuilderImpl>();
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

        // Patient Portal — thong bao benh nhan (fan-out push -> email) + jobs
        services.AddScoped<INotificationChannel, WebPushPatientChannel>();
        services.AddScoped<INotificationChannel, EmailPatientChannel>();
        services.AddScoped<IPatientNotifyService, PatientNotifyService>();
        services.AddScoped<RecallNotifyJob>();
        services.AddScoped<MedReminderJob>();
        services.AddScoped<QueueTurnNotifyJob>();

        // FR-112 (H-1): Kenh gui thong bao ngoai (SMS / Zalo ZNS) per-tenant/branch, config ma hoa.
        services.AddScoped<INotificationChannelCredentialProvider, NotificationChannelCredentialProvider>();
        services.AddScoped<IChannelSender, SmsSender>();
        services.AddScoped<IChannelSender, ZaloZnsSender>();
        services.AddScoped<INotificationSender, NotificationSender>();
        services.AddScoped<AppointmentReminderNotifyJob>();
        services.AddHttpClient(SmsSender.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient(ZaloZnsSender.HttpClientName, c => c.Timeout = TimeSpan.FromSeconds(30));

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
        services.AddScoped<IBhytXmlSerializer, BhytXmlSerializerImpl>();
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

        // InBody OCR (doc PDF ket qua may InBody — xem docs/prd/inbody-ocr-20260830.md)
        services.AddScoped<Application.InBody.IInBodyDataProvider, InBody.InBodyPdfTextProvider>();

        // Legacy scan import - nhap lieu hang loat ho so giay cu (OCR anh scan bang Tesseract,
        // OCR/text-layer file PDF bang PdfPig + PDFtoImage fallback)
        services.AddScoped<Application.LegacyImport.IOcrTextProvider, Ocr.TesseractOcrProvider>();
        services.AddScoped<Application.LegacyImport.IPdfTextExtractor, Ocr.PdfTextExtractor>();
        services.AddScoped<Jobs.LegacyOcrBatchJob>();

        // Lab integration services
        services.AddHttpClient("LabPartner");
        services.AddScoped<ILabPartnerClient, LabPartnerHttpClient>();
        services.AddSingleton<IHmacSignatureVerifier, HmacSignatureVerifier>();
        services.AddSingleton<IHl7v25Parser, Hl7v25ParserStub>();
        services.AddSingleton<ILabResultFlagCalculator, LabResultFlagCalculator>();
        services.AddScoped<ILabResultPdfExporter, LabResultQuestPdfExporter>();

        // Lab result OCR — doc file KQ xet nghiem (PDF/anh) -> parse theo XN dang cho.
        // Tai dung IPdfTextExtractor + IOcrTextProvider (dang ky o tren), chi them provider dieu phoi.
        services.AddScoped<Application.LabResults.Ocr.ILabOcrTextProvider, Lab.LabOcrTextProvider>();

        // Rad OCR (CDHA X-quang/Sieu am/CT) — tai dung IPdfTextExtractor + IOcrTextProvider, chi them provider dieu phoi.
        services.AddScoped<Application.RadResults.Ocr.IRadOcrTextProvider, Rad.RadOcrTextProvider>();

        // Document smart-upload classifier — dieu phoi tai lieu OCR sang dung luong (InBody/LabResult/RadResult).
        services.AddScoped<Application.Documents.IPendingLabTestsProvider, Documents.PendingLabTestsProvider>();
        services.AddScoped<Application.Documents.IPendingRadOrdersProvider, Documents.PendingRadOrdersProvider>();
        services.AddScoped<Application.Documents.IDocumentClassifier, Application.Documents.DocumentClassifierService>();

        return services;
    }

    /// <summary>
    /// Them "Guid Format=None" vao connection string MySQL neu chua co, de MySqlConnector KHONG
    /// tu suy dien cot CHAR(36)/CHAR(32) la Guid (tranh InvalidCastException khi EF/Dapper doc cot
    /// duoc map la string). Xem giai thich chi tiet o noi goi ham nay.
    /// </summary>
    internal static string EnsureGuidFormatNone(string connectionString)
    {
        var builder = new MySqlConnector.MySqlConnectionStringBuilder(connectionString)
        {
            GuidFormat = MySqlConnector.MySqlGuidFormat.None
        };
        return builder.ConnectionString;
    }
}
